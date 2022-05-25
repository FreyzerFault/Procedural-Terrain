using System;
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;

/**
 * <para>Generador Procedural de Terreno</para>
 *
 * <para>Genera el Terreno a partir de un Mapa de Ruido.
 * Con su malla, su textura y su malla de colisión</para>
 *
 * <para>Utiliza Threading y colas de peticiones
 * para la creación del mapa de ruido y la malla</para>
 */
[ExecuteAlways]
public class TerrainGenerator : MonoBehaviour
{
	// Anchura y Altura del terreno (num de vertices)
	[Range(100,240)]
	public int chunkSize = 241;
	[Range(0, 6)] public int lod;
	public float noiseScale = 5f;

	// Mejora con Octavos
	[Range(1, 10)] public int numOctaves = 3;
	[Range(0, 1)] public float persistance = 0.5f;
	[Range(1, 5)] public float lacunarity = 2;
	
	public Vector2 offset = Vector2.zero; 

	public int seed = DateTime.Now.Millisecond;

	[Range(0.01f, 200f)] public float meshHeightMultiplier = 100;
	
	public AnimationCurve meshHeightCurve = new AnimationCurve();
	public Gradient gradient = new Gradient();
	
	public bool autoUpdate = true;


	public readonly struct MapData
	{
		public readonly float[,] noiseMap;
		private readonly Color[] textureData;

		public MapData(float[,] noiseMap, Color[] textureData)
		{
			this.noiseMap = noiseMap;
			this.textureData = textureData;
		}

		public Texture2D GetTexture2D()
		{
			Texture2D texture = new Texture2D(noiseMap.GetLength(0), noiseMap.GetLength(1));
			texture.SetPixels(textureData);
			texture.Apply();
			return texture;
		}
	}

	private void Update()
	{
		UpdateThreadQueues();
	}


	/// <summary>
	/// Genera los datos del Mapa de Ruido/Altura a partir de los parametros que definen el Perlin Noise
	/// y un Gradiente para la Textura
	/// </summary>
	/// <returns>Los datos del Mapa de Ruido/Altura y de la Textura</returns>
	public MapData GenerateMapData(Vector2? noiseOffset = null)
	{
		noiseOffset ??= this.offset;
		
		// MAPA DE RUIDO
		float[,] noiseMap = NoiseMapGenerator.GetNoiseMap(
			chunkSize, chunkSize, noiseScale, (Vector2) noiseOffset,
			numOctaves, persistance, lacunarity, seed
		);

		// TEXTURA (pixeles con colores sin procesar)
		// Se genera a partir de las alturas del mapa de ruido evaluadas en un Gradiente
		Color[] textureData = NoiseMapGenerator.GetTextureData(noiseMap, gradient);

		// Lo guardamos en el mapData
		return new MapData(noiseMap, textureData);
	}

	/// <summary>
	/// Genera los datos de la Malla
	/// a partir de un Mapa de Alturas, un multiplicador de altura para escalarla,
	/// una curva de altura para ajustarla, un LOD y un Gradiente por si no se usa Textura (color en FragShader)
	/// </summary>
	/// <returns>Datos de la Malla</returns>
	public MeshData GenerateMeshData(float[,] heightMap, int lod = 0)
	{
		return NoiseMeshGenerator.GenerateTerrainMesh(
				heightMap, meshHeightMultiplier, meshHeightCurve, lod, gradient
			);
	}


	// ===================================================================================== //
	//	 THREADING:
	
	/// <summary>
	///  Información de un Hilo: callback que se tiene que ejecutar con el parametro que devuelve
	/// </summary>
	/// <typeparam name="T">Resultado que devuelve el Hilo</typeparam>
	public struct MapThreadInfo<T>
	{
		// Se ejecuta despues del hilo:
		public readonly Action<T> callback;
		// Parametro devuelto por el hilo que usa el callback como argumento:
		public readonly T parameter;

		public MapThreadInfo(Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}

	// Colas en las que se guarda la info de los hilos para ejecutarlos conforme le vengan
	public Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	public Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
	
	/// Ejecuta todos los callbacks guardados en la cola con los parametros que devolvieron los hilos
	public void UpdateThreadQueues()
	{
		if (mapDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
		if (meshDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
	}
	
	// Paraleliza la Creacion del Mapa de Ruido
	public void RequestMapData(Vector2 offset, Action<MapData> callback)
	{
		void ThreadStart()
		{
			MapDataThread(offset, callback);
		}

		new Thread(ThreadStart).Start();
	}

	// Proceso de creacion de MapData paralelizable
	private void MapDataThread(Vector2 offset, Action<MapData> callback)
	{
		// Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue(
				new MapThreadInfo<MapData>(callback, GenerateMapData(offset))
			);
		}
	}

	// Paraleliza la Creacion de la Malla de Alturas
	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
	{
		void ThreadStart()
		{
			MeshDataThread(mapData, lod, callback);
		}

		new Thread(ThreadStart).Start();
	}

	// Proceso de creacion de la Malla en un hilo
	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
	{
		// Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(
				new MapThreadInfo<MeshData>(callback, GenerateMeshData(mapData.noiseMap, lod))
			);
		}
	}

	
	// ===============================================================================================
	// DRAWS:
	public void Draw()
	{
		MapData mapData = GenerateMapData();
		MeshData meshData = GenerateMeshData(mapData.noiseMap);
		DrawTexture(mapData);
		DrawMesh(meshData);
	}
	public void Draw(MapData mapData, MeshData meshData)
	{
		DrawTexture(mapData);
		DrawMesh(meshData);
	}

	private void DrawTexture(MapData mapData)
	{
		Renderer textureRenderer = GetComponent<Renderer>();
		if (textureRenderer)
		{
			Texture2D texture = mapData.GetTexture2D();
			textureRenderer.material.mainTexture = texture;
			textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
		}
		else
		{
			Debug.LogWarning("Se quiere dibujar una textura en un objeto sin Renderer");
		}
	}

	private void DrawMesh(MeshData meshData)
	{
		MeshFilter meshFilter = GetComponent<MeshFilter>();

		if (meshFilter)
		{
			meshFilter.sharedMesh = meshData.CreateMesh();
			//transform.localScale = new Vector3(1, meshHeightMultiplier, 1);
		}
		else
		{
			Debug.LogWarning("Se quiere crear una malla con una textura en un objeto que le falta MeshFilter");
		}
	}

	public void ResetSeed()
	{
		seed = NoiseMapGenerator.generateRandomSeed();
	}
}

[CustomEditor(typeof(TerrainGenerator))]
public class MapGeneratorEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		TerrainGenerator terrainGen = target as TerrainGenerator;
		if (!terrainGen)
		{
			Debug.Log("No existe ningun objeto MapGenerator al que modificar su editor en el inspector");
			return;
		}

		if (DrawDefaultInspector() && terrainGen.autoUpdate)
		{
			terrainGen.Draw();
		}

		// Boton para generar el mapa
		if (GUILayout.Button("Generate Terrain"))
		{
			terrainGen.Draw();
		}

		if (GUILayout.Button("Reset Seed"))
		{
			terrainGen.ResetSeed();
			terrainGen.Draw();
		}
	}
}
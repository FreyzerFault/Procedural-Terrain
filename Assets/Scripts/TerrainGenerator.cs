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
public class TerrainGenerator : MonoBehaviour
{
	// Anchura y Altura del terreno (num de vertices)
	public int mapChunkSize = 241;
	[Range(0, 6)] public int lod;
	public float noiseScale = 5f;

	// Mejora con Octavos
	[Range(1, 10)] public int numOctaves = 3;
	[Range(0, 1)] public float persistance = 0.5f;
	[Range(1, 5)] public float lacunarity = 2;
	
	/// Offset en el mapa de ruido
	public Vector2 offset = Vector2.zero;

	public int seed = DateTime.Now.Millisecond;

	[Range(0.01f, 200f)] public float meshHeightMultiplier = 100;
	
	public AnimationCurve meshHeightCurve = new AnimationCurve();
	public Gradient gradient = new Gradient();
	
	public bool autoUpdate = true;

	private MeshData meshData;


	public struct MapData
	{
		public readonly float[,] noiseMap;
		public readonly Color[] textureData;

		public MapData(float[,] noiseMap, Color[] textureData, MeshData meshData)
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


	// THREADING:

	// Colas en las que se guarda la info de los hilos para ejecutarlos conforme le vengan
	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	/// <summary>
	///  Información de un Hilo: callback que se tiene que ejecutar con el parametro que devuelve
	/// </summary>
	/// <typeparam name="T">Resultado que devuelve el Hilo</typeparam>
	private struct MapThreadInfo<T>
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

	public MapData mapData;

	public MapData GenerateMapData()
	{
		// MAPA DE RUIDO
		float[,] noiseMap = NoiseMapGenerator.GetNoiseMap(
			mapChunkSize, mapChunkSize, noiseScale, offset,
			numOctaves, persistance, lacunarity, seed
		);

		Color[] textureData = NoiseMapGenerator.GetTextureData(noiseMap, gradient);

		return mapData = new MapData(noiseMap, textureData, meshData);
	}

	// Paraleliza la Creacion del Mapa de Ruido
	public void RequestMapData(Action<MapData> callback)
	{
		ThreadStart threadStart = delegate
		{
			MapDataThread(callback);
		};

		new Thread(threadStart).Start();
	}

	// Proceso de creacion de MapData paralelizable
	void MapDataThread(Action<MapData> callback)
	{
		// Crea la MapData
		MapData mapData = GenerateMapData();

		// Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
		}
	}

	// Paraleliza la Creacion de la Malla de Alturas
	public void RequestMeshData(MapData mapData, Action<MeshData> callback)
	{
		ThreadStart threadStart = delegate
		{
			MeshDataThread(mapData, callback);
		};

		new Thread(threadStart).Start();
	}

	// Proceso de creacion de la Malla en un hilo
	void MeshDataThread(MapData mapData, Action<MeshData> callback)
	{
		// Crea la Malla
		MeshData meshData = NoiseMeshGenerator.GenerateTerrainMesh(mapData.noiseMap, meshHeightMultiplier, meshHeightCurve, lod, gradient);

		// Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void Update()
	{
		// Ejecuta todos los callbacks guardados en la cola con los parametros que devolvieron los hilos
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

	// DRAWS:
	public void DrawTexture()
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

	public void DrawMesh()
	{
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

		if (meshFilter && meshRenderer)
		{
			Texture2D texture = mapData.GetTexture2D();
			meshFilter.sharedMesh = meshData.CreateMesh();
			meshRenderer.material.mainTexture = texture;
			//transform.localScale = new Vector3(1, meshHeightMultiplier, 1);
		}
		else
		{
			Debug.LogWarning("Se quiere crear una malla con una textura en un objeto que le falta MeshFilter o MeshRenderer");
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
		if (terrainGen == null)
		{
			Debug.Log("No existe ningun objeto MapGenerator al que modificar su editor en el inspector");
			return;
		}

		if (DrawDefaultInspector() && terrainGen.autoUpdate)
		{
			terrainGen.GenerateMapData();
			terrainGen.DrawMesh();
		}

		// Boton para generar el mapa
		if (GUILayout.Button("Generate Terrain"))
		{
			terrainGen.GenerateMapData();
			terrainGen.DrawMesh();
		}

		if (GUILayout.Button("Reset Seed"))
		{
			terrainGen.ResetSeed();
			terrainGen.GenerateMapData();
			terrainGen.DrawMesh();
		}
	}
}
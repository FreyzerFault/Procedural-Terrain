using System;
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
	public const int mapChunkSize = 241;
	[Range(0, 6)] public int LOD = 0;
	public float noiseScale = 5f;

	[Range(1, 10)] public int numOctaves = 3;
	[Range(0, 1)] public float persistance = 0.5f;
	[Range(1, 5)] public float lacunarity = 2;

	public int seed = DateTime.Now.Millisecond;
	public Vector2 offset = Vector2.zero;

	[Range(0.01f, 200f)] public float meshHeightMultiplier = 100;
	public AnimationCurve meshHeightCurve = new AnimationCurve();

	public Gradient gradient = new Gradient();


	public bool autoUpdate = true;


	private float[,] noiseMap;
	private Texture2D texture;
	private MeshData meshData;


	public struct MapData
	{
		public readonly float[,] noiseMap;
		public readonly Texture2D texture;

		public MapData(float[,] noiseMap, Texture2D texture, MeshData meshData)
		{
			this.noiseMap = noiseMap;
			this.texture = texture;
		}
	}


	// THREADING:

	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	public struct MapThreadInfo<T>
	{
		public readonly Action<T> callback;
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
		noiseMap = NoiseMapGenerator.GetNoiseMap(
			mapChunkSize, mapChunkSize, noiseScale, offset,
			numOctaves, persistance, lacunarity, seed
		);

		// Generamos el array de color
		//texture = NoiseMapGenerator.GetTexture(noiseMap, gradient);
		//meshData = NoiseMeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, LOD, gradient);

		return mapData = new MapData(noiseMap, null, meshData);
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

	// Proceso de creacion de MapData
	void MapDataThread(Action<MapData> callback)
	{
		// Crea la MapData de Forma Paralela
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

	// Proceso de creacion de MapData
	void MeshDataThread(MapData mapData, Action<MeshData> callback)
	{
		// Crea la MapData de Forma Paralela
		MeshData meshData = NoiseMeshGenerator.GenerateTerrainMesh(mapData.noiseMap, meshHeightMultiplier, meshHeightCurve, LOD, gradient);

		// Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void Update()
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

	// DRAWS:
	public void DrawTexture()
	{
		Renderer textureRenderer = GetComponent<Renderer>();
		if (textureRenderer)
		{
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

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		MapGenerator mapGen = target as MapGenerator;
		if (mapGen == null)
		{
			Debug.Log("No existe ningun objeto MapGenerator al que modificar su editor en el inspector");
			return;
		}

		if (DrawDefaultInspector() && mapGen.autoUpdate)
		{
			mapGen.GenerateMapData();
			mapGen.DrawMesh();
		}

		// Boton para generar el mapa
		if (GUILayout.Button("Generate Terrain"))
		{
			mapGen.GenerateMapData();
			mapGen.DrawMesh();
		}

		if (GUILayout.Button("Reset Seed"))
		{
			mapGen.ResetSeed();
			mapGen.GenerateMapData();
			mapGen.DrawMesh();
		}
	}
}
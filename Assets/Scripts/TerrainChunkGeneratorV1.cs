using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkGeneratorV1 : MonoBehaviour
{
	public Transform Player;
	public GameObject TerrainPrefab;

	public int chunkSize = 241;

	// Distancia desde el chunk central al borde
	public int renderDistance = 6;

	public int maxLOD = 6;
	
	[Serializable]
	public class NoiseParameters
	{
		public Vector2 offset = new Vector2(0, 0);

		public float noiseScale = 5f;

		[Range(1, 10)] public int octaves = 3;
		[Range(0, 1)] public float persistance = .5f;
		[Range(1, 5)] public float lacunarity = 2f;

		[Space]

		public Gradient gradient = new Gradient();
		public AnimationCurve heightCurve = new AnimationCurve();

		[Range(0.01f, 100f)] public float heightScale = 50f;

		public int seed = DateTime.Now.Millisecond;
	}

	[InspectorName("Noise Parameters")]
	public NoiseParameters np = new NoiseParameters();

	private Vector2[,] chunksRendered;

	private Vector2 playerChunk = new Vector2(0,0);

	private Dictionary<Vector2, TerrainPlane> terrains = new Dictionary<Vector2, TerrainPlane>();

	// Start is called before the first frame update
	void Start()
	{
		Clear();
		LoadChunks();
	}

	// Update is called once per frame
	void Update()
	{
		// Cuando cambie de chunk recalculamos to
		if (playerChunk != getChunk(Player.position))
		{
			LoadChunks();
			UpdateLODs();
		}

		Vector2 playerPosition = getLocalPos(Player.position);
	}

	// Load ALL chunks (Update if no need to Create)
	public void LoadChunks()
	{
		if (Player == null)
			Player = GameObject.FindGameObjectWithTag("Player").transform;

		playerChunk = getChunk(Player.position);

		int borderLength = GetBorderLength();

		chunksRendered = new Vector2[borderLength,borderLength];

		// Volvemos a generar los chunks con distinto LOD segun su distancia
		for (int x = 0; x < borderLength; x++)
		for (int y = 0; y < borderLength; y++)
		{
			Vector2 chunk = CreateChunk(x,y);

			// Si ya esta cargado actualizamos su LOD y su malla solamente
			if (terrains.ContainsKey(chunk))
			{
				UpdateLOD(chunk);
			}
			else
			{
				GenerateTerrain(chunk);
			}
		}
	}

	// Update ALL chunks
	public void UpdateLODs()
	{
		playerChunk = getChunk(Player.position);

		foreach (var it in terrains)
		{
			it.Value.UpdateLOD(GetLOD(it.Key));
		}
	}


	private Vector2 CreateChunk(int x, int y)
	{
		Vector2 chunk = new Vector2(
			x - renderDistance + playerChunk.x,
			y - renderDistance + playerChunk.y
		);
		return chunksRendered[x, y] = chunk;
	}
	
	private void GenerateTerrain(Vector2 chunk)
	{
		Vector2 globalPos = getGlobalPos(chunk);

		terrains.Add(
			chunk,
			Instantiate(
				TerrainPrefab,
				new Vector3(globalPos.x, 0, globalPos.y),
				Quaternion.identity,
				transform
			).GetComponent<TerrainPlane>()
		);

		terrains[chunk].Generate(
			chunkSize, chunkSize, np.noiseScale, GetOffset(chunk),
			np.octaves, np.persistance, np.lacunarity, np.seed,
			np.heightScale, GetLOD(chunk), np.heightCurve, np.gradient);
	}

	private void UpdateLOD(Vector2 chunk)
	{
		TerrainPlane terrain = terrains[chunk];
		terrain.UpdateLOD(GetLOD(chunk));
	}

	// Parametros que dependen del Area de Chunks Renderizados segun la posicion del Player
	// Longitud del Borde de los chunks, que sera el tama�o de mi matriz de Chunks Renderizados
	private int GetBorderLength()
	{
		return renderDistance * 2 + 1;
	}

	// Parametros que dependen del Chunk:
	// Level Of Detail
	public int GetLOD(Vector2 chunk)
	{
		int LOD = Mathf.FloorToInt((chunk - playerChunk).magnitude);
		return LOD;
	}
	// Noise Offset
	public Vector2 GetOffset(Vector2 chunk)
	{
		return chunk * np.noiseScale;
	}



	// =========================================================
	// Transformaciones de Espacio de Mundo al Espacio del Chunk:
	
	Vector2 getChunk(Vector2 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / chunkSize),
			Mathf.Round(pos.y / chunkSize)
		);
	}
	Vector2 getChunk(Vector3 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / chunkSize),
			Mathf.Round(pos.z / chunkSize)
		);
	}
	
	Vector2 getLocalPos(Vector2 pos)
	{
		return pos - getGlobalPos(getChunk(pos));
	}
	Vector2 getLocalPos(Vector3 pos)
	{
		return new Vector2(pos.x, pos.z) - getGlobalPos(getChunk(pos));
	}

	// Posicion del Chunk en el Espacio de Mundo
	Vector2 getGlobalPos(Vector2 chunkPos)
	{
		return chunkPos * (new Vector2(chunkSize - 1, chunkSize - 1));
	}

	// Resetea la Semilla de forma Aleatoria
	public void ResetRandomSeed()
	{
		np.seed = NoiseMapGenerator.generateRandomSeed();
	}

	// Borra todos los terrenos renderizados
	public void Clear()
	{
		NoiseMeshDisplay[] children = GetComponentsInChildren<NoiseMeshDisplay>();
		foreach (NoiseMeshDisplay child in children)
		{
			Destroy(child.gameObject);
		}
		terrains.Clear();
	}

	// Borra todos los terrenos renderizados
	public void ClearImmediate()
	{
		TerrainPlane[] children = GetComponentsInChildren<TerrainPlane>();
		foreach (TerrainPlane child in children)
		{
			DestroyImmediate(child.gameObject);
		}
		terrains.Clear();
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
	public Transform Player;
	public GameObject TerrainPrefab;

	public int chunkSize = 241;

	// Distancia desde el chunk central al borde
	public int renderDistance = 6;
	
	public class NoiseParameters
	{
		public float noiseScale = .3f;

		[Range(1, 10)] public int octaves = 1;
		[Range(0, 1)] public float persistance = 1f;
		[Range(1, 5)] public float lacunarity = 1f;

		[Space]

		public Gradient gradient = new Gradient();

		public AnimationCurve heightCurve = new AnimationCurve();
		[Range(0.01f, 100f)] public float heightScale;

		public Vector2 offset;

		public int seed = DateTime.Now.Millisecond;
	}

	[InspectorName("Noise Parameters")]
	[SerializeField]
	public NoiseParameters np = new NoiseParameters();

	private Vector2[,] chunks;

	private Vector2 playerChunk = new Vector2(0,0);

	private Dictionary<Vector2, MeshData> meshData;
	private Dictionary<Vector2, GameObject> terrains = new Dictionary<Vector2, GameObject>();

	private float[,] noiseMap;
	private Texture2D texture;

	// Start is called before the first frame update
	void Start()
	{
		Clear();
		Initialize();
		LoadChunks();
	}

	// Update is called once per frame
	void Update()
	{
		// Cuando cambie de chunk recalculamos to
		if (playerChunk != getChunk(Player.position))
		{
			playerChunk = getChunk(Player.position);

			LoadChunks();
		}

		Vector2 playerPosition = getLocalPos(Player.position);
	}

	public void Initialize()
	{
		GenerateNoise();
		GenerateTexture();
		LoadChunks();
	}

	private void GenerateNoise()
	{
		int noiseWidth = GetBorderLength() * chunkSize;
		noiseMap = new float[noiseWidth, noiseWidth];
		noiseMap = NoiseMapGenerator.GetNoiseMap(noiseWidth, noiseWidth, np.noiseScale, np.offset, np.octaves, np.persistance, np.lacunarity, np.seed);
	}
	private void GenerateTexture()
	{
		texture = NoiseMapGenerator.GetTexture(noiseMap, np.gradient);
	}

	// Load ALL chunks (Update if no need to Create)
	public void LoadChunks()
	{
		if (Player == null)
			Player = GameObject.FindGameObjectWithTag("Player").transform;

		playerChunk = getChunk(Player.position);

		int borderLength = GetBorderLength();

		chunks = new Vector2[borderLength,borderLength];

		// Volvemos a generar los chunks con distinto LOD segun su distancia
		for (int x = 0; x < borderLength; x++)
		for (int y = 0; y < borderLength; y++)
		{
			Vector2 chunk = CreateChunk(x,y);

			// Si ya esta cargado actualizamos su LOD y su malla solamente
			if (terrains.ContainsKey(chunk))
			{
				UpdateChunk(chunk);
			}
			else
			{
				CreateTerrain(chunk);
			}
		}
	}

	// Update ALL chunks
	public void ReloadChunks()
	{
		int borderLength = GetBorderLength();
		for (int x = 0; x < borderLength; x++)
		for (int y = 0; y < borderLength; y++)
		{
			UpdateChunk(CreateChunk(x,y));
		}
	}

	private void UpdateChunk(Vector2 chunk)
	{
		GameObject terrain = terrains[chunk];
		// TODO Regenerar la malla
	}

	private Vector2 CreateChunk(int x, int y)
	{
		Vector2 chunk = new Vector2(
			x - renderDistance + playerChunk.x,
			y - renderDistance + playerChunk.y
		);
		return chunks[x, y] = chunk;
	}
	
	private void CreateTerrain(Vector2 chunk)
	{
		// Creo las Mallas de cada chunk

		Vector2 globalPos = getGlobalPos(chunk);

		// Saco la matriz de Ruido de mi chunk a partir de la matriz del mundo
		float[,] heightMap = new float[chunkSize, chunkSize];
		for (int x = 0; x < chunkSize; x++)
		for (int y = 0; y < chunkSize; y++)
		{
			Vector2 origin = getGlobalPos(GetFirstChunk());
			Vector2 final = origin + GetBorderLength() * new Vector2(chunkSize, chunkSize);
			Vector2 offset = new Vector2(
				Mathf.InverseLerp(origin.x, final.x, chunk.x) * noiseMap.GetLength(0),
				Mathf.InverseLerp(origin.y, final.y, chunk.y) * noiseMap.GetLength(0)
			);
			heightMap[x, y] = noiseMap[x + (int)offset.x, y + (int)offset.y];
		}

		meshData.Add(chunk, NoiseMeshGenerator.GenerateTerrainMesh(heightMap, GetLOD(chunk), np.heightCurve, np.gradient));

		// TODO generar la Malla

		Mesh mesh = meshData[chunk].CreateMesh();

		terrains.Add(chunk,
			Instantiate(
				TerrainPrefab,
				new Vector3(globalPos.x, 0, globalPos.y),
				Quaternion.identity,
				transform
			)
		);

		terrains[chunk].GetComponent<MeshFilter>().mesh = mesh;
	}

	private Vector2 GetFirstChunk()
	{
		return chunks[0, 0];
	}

	public int GetLOD(Vector2 chunk)
	{
		return Mathf.FloorToInt((chunk - playerChunk).magnitude);
	}
	public Vector2 GetOffset(Vector2 chunk)
	{
		return chunk * np.noiseScale;
	}

	private void OffsetTerrain(Vector2 chunk)
	{
		NoiseMeshDisplay terrain = TerrainPrefab.GetComponent<NoiseMeshDisplay>();
		terrain.offset = chunk * terrain.noiseScale;
	}


	private int GetBorderLength()
	{
		return renderDistance * 2 + 1;
	}


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

	Vector2 getGlobalPos(Vector2 chunkPos)
	{
		return chunkPos * (new Vector2(chunkSize - 1, chunkSize - 1));
	}

	public void ResetRandomSeed()
	{
		np.seed = NoiseMapGenerator.generateRandomSeed();
	}

	public void Clear()
	{
		NoiseMeshDisplay[] children = GetComponentsInChildren<NoiseMeshDisplay>();
		foreach (NoiseMeshDisplay child in children)
		{
			Destroy(child.gameObject);
		}
		terrains.Clear();
	}

	public void ClearImmediate()
	{
		NoiseMeshDisplay[] children = GetComponentsInChildren<NoiseMeshDisplay>();
		foreach (NoiseMeshDisplay child in children)
		{
			DestroyImmediate(child.gameObject);
		}
		terrains.Clear();
	}
}

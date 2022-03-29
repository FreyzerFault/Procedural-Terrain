using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class ProceduralTerrain : MonoBehaviour
{
	public int depth = 20;

	public int width = 257;
	public int height = 257;

	public float scale = 20f;

	[Range(0.5f, 1f)] public float maxHeight = 1f;
	[Range(0, 0.5f)] public float minHeight = 0f;

	public Vector2 offset;

	[Range(1, 10)] public int numOctaves = 1;
	[Range(0, 1)] public float persistance = 1f;
	[Range(1, 5)] public float lacunarity = 1f;

	[HideInInspector]
	public bool useNoise = true;

	public Terrain terrain;

	public bool autoUpdate = true;

	public int seed;

	private NoiseMapGenerator mapGenerator = new NoiseMapGenerator();

	[ExecuteInEditMode]
	void Awake()
	{
		terrain = GetComponent<Terrain>();

		offset = new Vector2(Random.value * 9999f, Random.value * 9999f);
	}

	void Update()
	{
		UpdateTerrain();

		offset.x += Time.deltaTime * 5f;
	}

	public void UpdateTerrain()
	{
		terrain.terrainData = GenerateTerrain(terrain.terrainData);
	}

	// Modifica el terreno para adaptarse a unas dimensiones
	// Y genera alturas en cada pixel
	TerrainData GenerateTerrain(TerrainData terrainData)
	{
		terrainData.heightmapResolution = Math.Max(width + 1, height + 1);

		// El nuevo terreno tendra las dimensiones dadas
		terrainData.size = new Vector3(width, depth, height);

		// Asigna las alturas a partir de un punto, en este caso el origen del Terreno (0,0)
		terrainData.SetHeights(0, 0, GenerateHeights());

		return terrainData;
	}

	// Crea un array de alturas 2D
	private float[,] GenerateHeights()
	{
		float[,] heigths = new float[width, height];
		if (useNoise)
		{
			heigths = NoiseMapGenerator.GetNoiseMap(width, height, scale, offset, numOctaves, persistance, lacunarity, seed);
			for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				heigths[x, y] = Mathf.InverseLerp(minHeight, maxHeight, heigths[x,y]);
		}


		else
			for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
					heigths[x, y] = Random.value;

		return heigths;
	}

	public void ResetRandomSeed()
	{
		seed = NoiseMapGenerator.generateRandomSeed();
	}
}

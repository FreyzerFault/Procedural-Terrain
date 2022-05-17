using System;
using System.Collections;
using UnityEngine;

[RequireComponent(
	typeof(MeshFilter),
	typeof(MeshRenderer)
)]
public class NoiseMeshDisplay : MonoBehaviour
{
	public MeshData meshData;
	public Texture2D texture;


	[Space]

	[Range(0, 241)] public int chunkSize = 241;
	[Range(0, 6)] public int LOD = 1;
	
	[Range(0,40)] public float noiseScale = .3f;

	[Range(1, 10)] public int octaves = 1;
	[Range(0, 1)] public float persistance = 1f;
	[Range(1, 5)] public float lacunarity = 1f;

	[Space]

	public Gradient gradient = new Gradient();

	public AnimationCurve heightCurve = new AnimationCurve();
	[Range(0.01f, 100f)] public float heightScale;

	public Vector2 offset;

	[Space]

	public bool autoUpdate = true;
	public bool movement = true;
	[Range(0, 1)] public float speed;

	[Space]

	public int seed = DateTime.Now.Millisecond;

	private NoiseMapGenerator mapGenerator = new NoiseMapGenerator();

	private float[,] noiseMap;

	void Awake()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
			UpdateLOD(player.transform.position);

		CreateTerrain();
	}

	void Update()
	{
		if (movement)
		{
			CreateTerrain();
			offset.x += Time.deltaTime * speed;
		}

	}

	public void CreateTerrain()
	{
		CreateNoiseMap();
		CreateTexture();
		CreateMesh();
		//AdjustHeightScale();
		SetTerrainCollider();
	}

	public void CreateNoiseMap()
	{
		noiseMap =
			NoiseMapGenerator.GetNoiseMap(chunkSize, chunkSize, noiseScale, offset, octaves, persistance, lacunarity, seed);
	}
	public void CreateMesh()
	{
		meshData = NoiseMeshGenerator.GenerateTerrainMesh(noiseMap, heightScale, heightCurve, LOD, gradient);
		
		GetComponent<MeshFilter>().mesh = meshData.CreateMesh();
	}
	public void CreateTexture()
	{
		texture = NoiseMapGenerator.GetTexture(noiseMap, gradient);
		GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
	}

	public void AdjustHeightScale()
	{
		transform.localScale = new Vector3(1, heightScale, 1);
	}

	public void UpdateLOD(Vector2 playerWorldPos)
	{
		Vector2 terrainWorldPos = new Vector2(transform.position.x, transform.position.z);
		LOD = Mathf.FloorToInt((terrainWorldPos - playerWorldPos).magnitude / chunkSize);
	}

	private void SetTerrainCollider()
	{
		// Calculo del Terrain Collider a partir del noisemap
		TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
		if (terrainCollider)
		{
			float[,] noiseCollider = new float[chunkSize, chunkSize];
			for (int x = 0; x < chunkSize; x++)
			for (int y = 0; y < chunkSize; y++)
				noiseCollider[x, y] = noiseMap[x, y] * heightScale;

			TerrainData data = terrainCollider.terrainData = new TerrainData();
			data.heightmapResolution = chunkSize;
			data.SetHeights(0,0, noiseCollider);
		}
	}

	public void ResetRandomSeed()
	{
		seed = NoiseMapGenerator.generateRandomSeed();
	}

}

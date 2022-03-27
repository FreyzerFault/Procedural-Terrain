using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class NoiseMeshDisplay : MonoBehaviour
{
	[HideInInspector]
	public Mesh mesh;
	public MeshData meshData;

	[Space]
	// Color
	public Gradient gradient = new Gradient();

	[Space]

	[Range(0, 256)]
	public int xSize = 20;
	[Range(0, 256)]
	public int zSize = 20;
	
	public float noiseScale = .3f;

	[Range(1, 10)] public int octaves = 1;
	[Range(0, 1)] public float persistance = 1f;
	[Range(1, 5)] public float lacunarity = 1f;

	[Range(0.01f, 100f)] public float heightScale;

	public Vector2 offset;

	[Space]

	public bool autoUpdate = true;
	public bool movement = true;

	[Space]

	public int seed = DateTime.Now.Millisecond;

	private NoiseMapGenerator mapGenerator = new NoiseMapGenerator();

	void Awake()
	{
		CreateShape();
		UpdateMesh();
	}

	void Update()
	{
		if (movement)
		{
			CreateShape();
			UpdateMesh();
			offset.x += Time.deltaTime;
		}
	}

	public void CreateShape()
	{
		mapGenerator.setSeed(seed);

		float[,] noiseMap =
			mapGenerator.GetNoiseMap(xSize, zSize, noiseScale, offset, octaves, persistance, lacunarity);
		meshData = NoiseMeshGenerator.GenerateTerrainMesh(noiseMap, gradient);
	}

	public void UpdateMesh()
	{
		transform.localScale = new Vector3(1, heightScale, 1);

		mesh.Clear();
		mesh = meshData.CreateMesh();
		if (GetComponent<MeshFilter>() == null)
		{
			print("No hay MeshFilter!!!");
			return;
		}
		GetComponent<MeshFilter>().mesh = mesh;
	}

	public void ResetRandomSeed()
	{
		seed = mapGenerator.ResetRandomSeed();
	}

}

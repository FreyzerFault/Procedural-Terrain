using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class NoiseMeshDisplay : MonoBehaviour
{
	[HideInInspector]
	public MeshFilter meshFilter;
	public MeshRenderer MeshRenderer;

	public MeshData meshData;
	public Texture2D texture;


	[Space]

	[Range(0, 241)] public int chunkSize = 20;
	[Range(0, 6)] public int LOD = 1;
	
	public float noiseScale = .3f;

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

	void Start()
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
			offset.x += Time.deltaTime * speed;
		}
	}

	public void CreateShape()
	{
		mapGenerator.setSeed(seed);

		float[,] noiseMap =
			mapGenerator.GetNoiseMap(chunkSize, chunkSize, noiseScale, offset, octaves, persistance, lacunarity);
		meshData = NoiseMeshGenerator.GenerateTerrainMesh(noiseMap, LOD, heightCurve, gradient);
		texture = NoiseMapGenerator.GetTexture(noiseMap, gradient);
	}

	public void UpdateMesh()
	{
		meshFilter = GetComponent<MeshFilter>();
		MeshRenderer = GetComponent<MeshRenderer>();
		
		meshFilter.sharedMesh = meshData.CreateMesh();
		
		MeshRenderer.sharedMaterial.mainTexture = texture;

		transform.localScale = new Vector3(1, heightScale, 1);
	}

	public void ResetRandomSeed()
	{
		seed = mapGenerator.ResetRandomSeed();
	}

}

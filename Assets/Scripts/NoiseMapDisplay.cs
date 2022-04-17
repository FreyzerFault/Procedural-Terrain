using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class NoiseMapDisplay : MonoBehaviour
{
	[Range(0,2000)] public int width = 256;
	[Range(0, 2000)] public int height = 256;

	public float NoiseScale = 20f;

	public Vector2 offset;

	[Range(0.5f,1f)] public float maxHeight =  1f;
	[Range(0,0.5f)] public float minHeight = 0f;

	[Range(1,10)] public int octaves = 1;
	[Range(0, 1)] public float persistance = 1f;
	[Range(1, 5)] public float lacunarity= 1f;

	[Space]

	public int seed = DateTime.Now.Millisecond;

	[Space]
	// Color
	public Gradient Gradient;

	[Space]

	public bool autoUpdate;
	public bool movement;
	[Range(0,10)] public float speed = 1;
	[Range(0, 10)] public float movementRadius = 1;

	public Renderer TextureRenderer;

	private NoiseMapGenerator mapGenerator = new NoiseMapGenerator();

	void Start()
	{
		GenerateTexture();
	}

	private float angle = 0;

	void Update()
	{
		if (movement)
		{
			angle += Time.deltaTime * speed;

			offset.x = Mathf.Cos(angle) * movementRadius;
			offset.y = Mathf.Sin(angle) * movementRadius;
		}

		GenerateTexture();
	}

	public void GenerateTexture(bool noise = true)
	{
		var noiseMap =
			noise
			? NoiseMapGenerator.GetNoiseMap(width, height, NoiseScale, offset, octaves, persistance, lacunarity, seed)
			: GetRandomMap(width, height);

		TextureRenderer.sharedMaterial.mainTexture = NoiseMapGenerator.GetTexture(noiseMap, Gradient);

		TextureRenderer.transform.localScale = new Vector3(width, 1, height);
	}

	public void ResetRandomSeed()
	{
		seed = NoiseMapGenerator.generateRandomSeed();
	}

	public float[,] GetRandomMap(int width, int height)
	{
		float[,] map = new float[width,height];

		for (int x = 0; x < width; x++)
		for (int y = 0; y < height; y++)
			map[x, y] = Random.value;

		return map;
	}
}

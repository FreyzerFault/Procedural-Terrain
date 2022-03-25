using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
	public int depth = 20;

	public int width = 256;
	public int height = 256;

	public float scale = 20f;

	public float offsetX = 0f;
	public float offsetY = 0f;

	public bool useNoise = true;

	public Terrain terrain;

	public bool autoUpdate = true;

	[ExecuteInEditMode]
	void Awake()
	{
		terrain = GetComponent<Terrain>();

		offsetX = Random.value * 9999f;
		offsetY = Random.value * 9999f;
	}

	void Update()
	{
		UpdateTerrain();

		offsetX += Time.deltaTime * 5f;
	}

	public void UpdateTerrain()
	{
		terrain.terrainData = GenerateTerrain(terrain.terrainData);
	}

	// Modifica el terreno para adaptarse a unas dimensiones
	// Y genera alturas en cada pixel
	TerrainData GenerateTerrain(TerrainData terrainData)
	{
		terrainData.heightmapResolution = width + 1;

		// El nuevo terreno tendra las dimensiones dadas
		terrainData.size = new Vector3(width, depth, height);

		// Asigna las alturas a partir de un punto, en este caso el origen del Terreno (0,0)
		terrainData.SetHeights(0, 0, GenerateHeights());

		return terrainData;
	}

	// Crea un array de alturas 2D
	private float[,] GenerateHeights()
	{
		float[,] heights = new float[width, width];

		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				if (useNoise)
					heights[x, y] = CalculateHeight(x, y);
				else
					heights[x, y] = Random.value;

					return heights;
	}

	float CalculateHeight(int x, int y)
	{
		float xCoord = (float)x / width * scale + offsetX;
		float yCoord = (float)y / height * scale + offsetY;

		return Mathf.PerlinNoise(xCoord, yCoord);
	}
}

using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
	[HideInInspector]
	public Mesh mesh;

	private Vector3[] vertices;
	private int[] triangles;
	private Color[] colors;

	public Gradient materialGradient;

	[Space]

	[Range(0, 256)]
	public int xSize = 20;
	[Range(0, 256)]
	public int zSize = 20;
	
	public float noiseScale = .3f;
	
	public float maxHeight = 2f;
	public float minHeight = 0f;

	public float offsetX = 0f;
	public float offsetZ = 0f;

	[Space]

	public bool autoUpdate = true;

	public bool movement = true;

	public bool useNoise = true;
	
	void Awake()
	{
		mesh = new Mesh();

		GetComponent<MeshFilter>().mesh = mesh;

		CreateShape();
		UpdateMesh();
	}

	void Update()
	{
		if (movement)
		{
			CreateShape();
			UpdateMesh();
			offsetX += Time.deltaTime;
		}
	}

	public void CreateShape()
	{
		int vertexCount = (xSize + 1) * (zSize + 1);
		vertices = new Vector3[vertexCount];
		colors = new Color[vertexCount];

		for (int i = 0, z = 0; z <= zSize; z++)
			for (int x = 0; x <= xSize; x++)
			{
				// Altura del vertice:
				float y;
				if (useNoise)
					y = Mathf.Lerp(
						minHeight, maxHeight,
						Mathf.PerlinNoise(
							x * noiseScale + offsetX,
							z * noiseScale + offsetZ)
						);
				else
					y = Random.value * maxHeight;

				vertices[i] = new Vector3(x, y, z);

				// Color con gradiente segun la Altura
				colors[i] = materialGradient.Evaluate(Mathf.InverseLerp(minHeight, maxHeight, vertices[i].y) );

				i++;
			}

		// Indices de Triangulos
		triangles = new int[6 * xSize * zSize];

		int vert = 0, tris = 0;
		for (int z = 0; z < zSize; z++)
		{
			for (int x = 0; x < xSize; x++)
			{
				triangles[tris + 0] = vert + 0;
				triangles[tris + 1] = vert + xSize + 1;
				triangles[tris + 2] = vert + 1;
				triangles[tris + 3] = vert + 1;
				triangles[tris + 4] = vert + xSize + 1;
				triangles[tris + 5] = vert + xSize + 2;

				vert++;
				tris += 6;
			}
			vert++;
		}

	}

	public void UpdateMesh()
	{
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.colors = colors;

		mesh.RecalculateNormals();
	}
	
}

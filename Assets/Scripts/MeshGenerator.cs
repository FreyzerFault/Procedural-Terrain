using JetBrains.Annotations;
using UnityEngine;

public static class NoiseMeshGenerator
{
	// Generamos los datos de la malla (Vertices, Triangulos, Colors / UVs)
	// No generamos la Mesh en este metodo
	// Porque Generar una MESH tiene la limitacion de que no se puede hacer multithreading
	// Por lo que este proceso se puede hacer en hilos
	public static MeshData GenerateTerrainMesh(float[,] heightMap, int LOD = 0, AnimationCurve heightCurve = null, Gradient gradient = null)
	{
		heightCurve ??= new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

		// Si no se pone ningun gradiente le metemos color de Negro a Blanco
		if (gradient == null)
		{
			gradient = new Gradient();
			GradientColorKey[] colors = new GradientColorKey[2]
				{ new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) };
			GradientAlphaKey[] alphas = new GradientAlphaKey[2]
				{ new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
			gradient.SetKeys(colors, alphas);
		}

		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		// La malla la creamos centrada en 0:
		float initX = (width - 1) / -2f;
		float initY = (height - 1) / -2f;

		// Incremento entre vertices para asegurar el LOD
		int simplificationIncrement = LOD == 0 ? 1 : LOD * 2;
		int verticesPerLine = (width - 1) / simplificationIncrement + 1;

		MeshData data = new MeshData(verticesPerLine, verticesPerLine);

		int vertIndex = 0;
		for (int y = 0; y < height; y += simplificationIncrement)
		for (int x = 0; x < width; x += simplificationIncrement)
		{
			data.vertices[vertIndex] = new Vector3(initX + x, heightCurve.Evaluate(heightMap[x,y]), initY + y);
			data.uvs[vertIndex] = new Vector2((float)x / width, (float)y / height);
			data.colors[vertIndex] = gradient.Evaluate(heightMap[x, y]);

			// Ignorando la ultima fila y columna de vertices, añadimos los triangulos
			if (x < width - 1 && y < height - 1)
			{
				//data.AddTriangle(vertIndex, vertIndex + verticesPerLine + 1, vertIndex + verticesPerLine);
				//data.AddTriangle(vertIndex + verticesPerLine + 1, vertIndex, vertIndex + 1);
				data.AddTriangle(vertIndex, vertIndex + verticesPerLine, vertIndex + verticesPerLine + 1);
				data.AddTriangle(vertIndex + verticesPerLine + 1, vertIndex + 1, vertIndex);
			}

			vertIndex++;
		}

		return data;
	}
}


public class MeshData
{
	public Vector3[] vertices;
	public int[] triangles;

	// Redundante, para usar o Colores o Textura
	public Vector2[] uvs;
	public Color[] colors;

	public MeshData(int meshWidth, int meshHeight)
	{
		vertices = new Vector3[meshWidth * meshHeight];

		uvs = new Vector2[meshWidth * meshHeight];
		colors = new Color[meshWidth * meshHeight];

		triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
	}

	private int triIndex = 0;

	public void AddTriangle(int a, int b, int c)
	{
		if (a >= vertices.Length || b >= vertices.Length || c >= vertices.Length)
		{
			Debug.Log("Triangle out of Bounds!!! " + vertices.Length + " Vertices. Triangle(" + a + ", " + b + ", " +
			          c + ")");
			return;
		}
		triangles[triIndex + 0] = a;
		triangles[triIndex + 1] = b;
		triangles[triIndex + 2] = c;
		triIndex += 3;
	}

	// Este Metodo no puede hacerse en otro Hilo
	public Mesh CreateMesh()
	{
		Mesh mesh = new Mesh
		{
			vertices = vertices,
			triangles = triangles,
			uv = uvs,
			colors = colors
		};

		mesh.RecalculateNormals();

		return mesh;
	}
}
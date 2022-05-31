using System;
using JetBrains.Annotations;
using UnityEngine;

[RequireComponent(
	typeof(MeshFilter),
	typeof(MeshRenderer)
)]
public class TerrainPlane : MonoBehaviour
{
	public float[,] noiseMap;
	public Texture2D texture;
	public MeshData meshData;
	public Mesh mesh;

	[Serializable]
	public class Params
	{
		public int width = 241, height = 241, numOctaves = 3, LOD = 0, seed = 0;
		public float noiseScale = 5f, persistance = .5f, lacunarity = 2f, HeightScale = 50;
		public Vector2 offset = new Vector2(0, 0);
		[CanBeNull] public AnimationCurve heightCurve = null;
		[CanBeNull] public Gradient gradient = null;
	}
	[InspectorName("Parameters")]
	public Params p = new Params();


	// ""Constructor""
	// Genera el terreno con un Mapa de Ruido, le pone su textura segun un gradiente y crea la malla
	public void Generate(
		int width, int height, float noiseScale, Vector2 offset,
		int numOctaves, float persistance, float lacunarity, int seed, float HeightScale,
		int LOD = 1, [CanBeNull] AnimationCurve heightCurve = null, [CanBeNull] Gradient gradient = null
		)
	{
		// Parametros basicos
		p.width = width; p.height = height; p.noiseScale = noiseScale; p.offset = offset;
		p.numOctaves = numOctaves; p.persistance = persistance; p.lacunarity = lacunarity;
		p.seed = seed; p.HeightScale = HeightScale; p.LOD = LOD;

		// Parametros que pueden ser nulos
		p.heightCurve = heightCurve;
		p.gradient = gradient;

		// Generamos tod0 lo necesario
		GenerateNoiseMap();
		GenerateTexture();
		GenerateMeshData();
		UpdateMesh();
		GenerateCollider();

		// Actualizamos su altura escalandola
		//transform.localScale = new Vector3(1, HeightScale, 1);
	}

	// Actualiza el LOD cambiando solo la Malla
	public void UpdateLOD(int LOD)
	{
		p.LOD = LOD;
		GenerateMeshData(); // Esto es paralelizable
		UpdateMesh(); // Esto no
	}

	public void GenerateNoiseMap()
	{
		noiseMap = NoiseMapGenerator.GetNoiseMap(
			p.width, p.height, p.noiseScale, p.offset, p.numOctaves, p.persistance, p.lacunarity, p.seed
			);
	}

	public void GenerateTexture()
	{
		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		texture = NoiseMapGenerator.GetTexture(noiseMap, p.gradient);
		meshRenderer.material.mainTexture = texture;
		//meshRenderer.transform.localScale = new Vector3(p.width, 1, p.height);
	}

	public void GenerateMeshData()
	{
		meshData = MeshGenerator.GetTerrainMeshData(noiseMap, p.HeightScale, p.heightCurve, p.gradient, p.LOD);
	}

	public void UpdateMesh()
	{
		mesh = GetComponent<MeshFilter>().mesh = meshData.CreateMesh();
	}

	public void GenerateCollider()
	{
		GetComponent<MeshCollider>().sharedMesh = mesh;
		return;
	}
}

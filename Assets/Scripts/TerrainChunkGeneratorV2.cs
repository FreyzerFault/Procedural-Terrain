using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Generador de Terreno Adaptativo a la posición del Jugador
//
// Visualiza solo los Chunks que estan dentro de la distancia de renderizado al Jugador
// Guarda los terrenos en un Mapa según su Coordenada de Chunk (pos / chunkSize)
//
// La generación es lazy:
// no genera el Chunk hasta que el jugador se acerca a menos de la distancia de renderizado
public class TerrainChunkGeneratorV2 : MonoBehaviour
{
	// Material del terreno al que se le aplica la textura
	public Material mapMaterial;
	
	[Range(1,10)]
	public int renderDist = 4;
	
	// Generador de 
	public TerrainGenerator _terrainGenerator;

	private int ChunkSize { get => _terrainGenerator.chunkSize; set => _terrainGenerator.chunkSize = value; }
	private float NoiseScale { get => _terrainGenerator.noiseScale; set => _terrainGenerator.noiseScale = value; }

	public int seed;

	// Almacen de terranos generados indexados por su Chunk [X,Y]
	private Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	// Chunks visibles en el ultimo update, para esconderlos cuando ya no esten a la distancia de renderizado
	private List<TerrainChunk> chunkLastVisible = new List<TerrainChunk>();

	public Transform viewer;
	private Vector2 ViewerPos { get => new Vector2(viewer.position.x, viewer.position.z); }
	private Vector2 ViewerChunk { get => GetChunkCoord(ViewerPos); }
	private Vector2 lastViewerChunk;

	void Start()
	{
		_terrainGenerator = GetComponent<TerrainGenerator>();

		UpdateVisibleChunks();
		ResetRandomSeed();
	}
	
	public void Update()
	{
		// Solo si el viewer cambia de chunk se actualizan los chunks
		if (lastViewerChunk != ViewerChunk)
			UpdateVisibleChunks();
		
		// Ultimo chunk del jugador para comprobar si ha cambiado de chunk
		lastViewerChunk = ViewerChunk;
	}


	public void UpdateVisibleChunks()
	{
		foreach (TerrainChunk chunk in chunkLastVisible)
		{
			chunk.Visible = false;
		}

		// Recorremos toda la malla alrededor del jugador que entra dentro de la distancia de renderizado
		for (int yOffset = -renderDist; yOffset <= renderDist; yOffset++)
		for (int xOffset = -renderDist; xOffset <= renderDist; xOffset++)
		{
			// Se generan los chunks relativos a la distancia con el Viewer
			Vector2 chunkCoords = new Vector2(xOffset, yOffset) + ViewerChunk;

			// Si no existe el chunk se genera y se a�ade
			if (!chunkDictionary.ContainsKey(chunkCoords))
			{
				chunkDictionary.Add(
					chunkCoords,
					new TerrainChunk(
						chunkCoord: chunkCoords,
						size: ChunkSize,
						parent: transform,
						material: mapMaterial,
						lod: Mathf.FloorToInt(Mathf.FloorToInt((ViewerChunk - chunkCoords).magnitude)),
						offset: GetOffset(chunkCoords, NoiseScale),
						terrainGenerator: _terrainGenerator)
					);
			}

			// Actualizamos el chunk
			TerrainChunk chunkTerrain = chunkDictionary[chunkCoords];
			chunkTerrain.UpdateVisibility(renderDist, ViewerPos);

			// Y si es visible recordarlo para hacerlo invisible cuando se escape del rango de renderizado
			if (chunkTerrain.Visible)
				chunkLastVisible.Add(chunkTerrain);
		}
	}
	
	// Resetea la Semilla de forma Aleatoria
	public void ResetRandomSeed()
	{
		seed = NoiseMapGenerator.generateRandomSeed();
		_terrainGenerator.seed = seed;
	}

	public class TerrainChunk
	{
		private TerrainGenerator generator;
		
		private GameObject meshObject;
		public Bounds bounds;
		public float Width { get => bounds.extents.x * 2; }
		public float Height { get => bounds.extents.y * 2; }

		private MeshRenderer meshRenderer;
		private MeshFilter meshFilter;

		private Vector2 chunkCoord;
		private int lod;

		public bool Visible { get => meshObject.activeSelf; set => meshObject.SetActive(value); }

		public TerrainChunk(Vector2 chunkCoord, int size, Transform parent, Material material, int lod, Vector2 offset, TerrainGenerator terrainGenerator)
		{
			this.generator = terrainGenerator;
			this.chunkCoord = chunkCoord;
			
			// Creamos el Terrano en la posicion del mundo y con un Bounds que lo delimite segun su tama�o
			var position = chunkCoord * size;
			bounds = new Bounds(position, Vector3.one * size);
			Vector3 position3D = new Vector3(position.x, 0, position.y);

			// Creamos un Plano (por defecto tiene 10 de tama�o) en la posicion y con el tama�o ajustado al size
			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;

			// Movemos el Chunk a la posicion asignada y asignamos al objeto generador como el padre
			meshObject.transform.position = position3D;
			meshObject.transform.parent = parent;

			// Pone en cola la petición del Mapa de Ruido y ejecuta OnMapDataReceived cuando termina de ejecutarse
			generator.RequestMapData(offset, OnMapDataReceived);
		}

		// Cuando reciba los datos del MAPA de ruido pide la MALLA
		void OnMapDataReceived(TerrainGenerator.MapData mapData)
		{
			meshRenderer.material.mainTexture = mapData.GetTexture2D();

			generator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}

		// Cuando recibe los datos de la MALLA, la crea (la creacion no se puede paralelizar)
		void OnMeshDataReceived(MeshData meshData)
		{
			meshFilter.mesh = meshData.CreateMesh();
		}

		public void UpdateVisibility(int renderDist, Vector2 viewerPos)
		{
			// La distancia del jugador al plano (con Bounds se consigue la distancia mas corta)
			float viewerDist = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
				
			// LOD = Distancia en Chunks al Viewer
			lod = Mathf.FloorToInt(viewerDist / Width);

			// Sera visible si la distancia al viewer es menor a la permitida
			Visible = viewerDist <= renderDist * Width;
		}
	}
	

	// Borra todos los terrenos renderizados
	public void Clear()
	{
		MeshFilter[] children = GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter child in children)
		{
			Destroy(child.gameObject);
		}
		chunkDictionary.Clear();
		chunkLastVisible.Clear();
	}

	// Borra todos los terrenos renderizados
	public void ClearImmediate()
	{
		MeshFilter[] children = GetComponentsInChildren<MeshFilter>(true);
		foreach (MeshFilter child in children)
		{
			DestroyImmediate(child.gameObject);
		}
		chunkDictionary.Clear();
		chunkLastVisible.Clear();
	}
	
	
	// =================================================================================================== //
	// Parametros que dependen del Chunk:

	// Longitud del Borde de los chunks, que sera el tama�o de mi matriz de Chunks Renderizados
	private static int GetVisibilityChunkBorderLength(int renderDistance) => renderDistance * 2 + 1;

	// Noise Offset (desplazamiento del Mapa de Ruido al que se encuentra el Chunk)
	public static Vector2 GetOffset(Vector2 chunkCoord, float noiseScale) => chunkCoord * noiseScale;

	
	// Transformaciones de Espacio de Mundo al Espacio del Chunk:
	private Vector2 GetChunkCoord(Vector2 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / ChunkSize),
			Mathf.Round(pos.y / ChunkSize)
		);
	}

	private Vector2 GetChunkCoord(Vector3 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / ChunkSize),
			Mathf.Round(pos.z / ChunkSize)
		);
	}

	// Posicion relativa al centro del Chunk
	private Vector2 GetLocalPos(Vector2 pos)
	{
		return pos - GetGlobalPos(GetChunkCoord(pos));
	}

	private Vector2 GetLocalPos(Vector3 pos)
	{
		return new Vector2(pos.x, pos.z) - GetGlobalPos(GetChunkCoord(pos));
	}

	// Posicion del Chunk en el Espacio de Mundo
	private Vector2 GetGlobalPos(Vector2 chunkPos)
	{
		return chunkPos * (new Vector2(ChunkSize - 1, ChunkSize - 1));
	}
}

[CustomEditor(typeof(TerrainChunkGeneratorV2))]
public class TerrainGeneratorEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		TerrainChunkGeneratorV2 terrainChunkGen = target as TerrainChunkGeneratorV2;
		if (terrainChunkGen == null)
		{
			Debug.Log("No existe ningun objeto TerrainGeneratorV2 al que modificar su editor en el inspector");
			return;
		}

		DrawDefaultInspector();

		// Boton para generar el mapa
		if (GUILayout.Button("Regenerate Terrain"))
		{
			if (!terrainChunkGen._terrainGenerator)
				terrainChunkGen._terrainGenerator = terrainChunkGen.GetComponent<TerrainGenerator>();
			terrainChunkGen.ClearImmediate();
			terrainChunkGen.UpdateVisibleChunks();
		}

		if (GUILayout.Button("Reset Seed"))
		{
			if (!terrainChunkGen._terrainGenerator)
				terrainChunkGen._terrainGenerator = terrainChunkGen.GetComponent<TerrainGenerator>();
			terrainChunkGen.ClearImmediate();
			terrainChunkGen.ResetRandomSeed();
			terrainChunkGen.UpdateVisibleChunks();
		}

		if (GUILayout.Button("Clear"))
		{
			terrainChunkGen.ClearImmediate();
		}
	}
}
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
	public Transform viewer;
	
	[Range(1,10)]
	public int renderDist = 4;
	[Range(100,240)]
	public int chunkSize = 240;
	
	// Generador de 
	private static TerrainGenerator _terrainGenerator;

	int seed;

	// Almacen de terranos generados indexados por su Chunk [X,Y]
	Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	// Chunks visibles en el ultimo update, para esconderlos cuando ya no esten a la distancia de renderizado
	private List<TerrainChunk> chunkLastVisible = new List<TerrainChunk>();

	private Vector2 lastViewerChunk;

	void Start()
	{
		_terrainGenerator = GetComponent<TerrainGenerator>();
		_terrainGenerator.mapChunkSize = chunkSize + 1;

		ResetRandomSeed();
	}
	
	public void Update()
	{
		Vector2 viewerChunk = getChunk(GetViewerPosition());

		// Solo si el viewer cambia de chunk se actualizan los chunks
		if (lastViewerChunk != viewerChunk)
			UpdateVisibleChunks();
		
		// Ultimo chunk del jugador para comprobar si ha cambiado de chunk
		lastViewerChunk = viewerChunk;
	}


	public void UpdateVisibleChunks()
	{
		foreach (TerrainChunk chunk in chunkLastVisible)
		{
			chunk.visible = false;
		}

		Vector2 currentChunk = getChunk(GetViewerPosition());

		// Recorremos toda la malla alrededor del jugador que entra dentro de la distancia de renderizado
		for (int yOffset = -renderDist; yOffset <= renderDist; yOffset++)
		for (int xOffset = -renderDist; xOffset <= renderDist; xOffset++)
		{
			Vector2 chunkCoords = new Vector2(xOffset, yOffset) + currentChunk;

			// Si no existe el chunk se genera y se a�ade
			if (!chunkDictionary.ContainsKey(chunkCoords))
			{
				chunkDictionary.Add(
					chunkCoords,
					new TerrainChunk(chunkCoords, chunkSize, transform, mapMaterial)
					);
			}

			// Actualizamos el chunk
			TerrainChunk chunk = chunkDictionary[chunkCoords];
			chunk.UpdateVisibility(renderDist, GetViewerPosition());

			// Y si es visible recordarlo para hacerlo invisible cuando se escape del rango de renderizado
			if (chunk.visible)
				chunkLastVisible.Add(chunk);
		}
	}

	Vector2 GetViewerPosition()
	{
		var position = viewer.position;
		return new Vector2(position.x, position.z);
	}

	public class TerrainChunk
	{
		private GameObject meshObject;
		private Vector2 position;
		private Bounds bounds;

		private MeshRenderer meshRenderer;
		private MeshFilter meshFilter;

		public bool visible { get => meshObject.activeSelf; set => meshObject.SetActive(value); }

		public TerrainChunk(Vector2 chunkCoord, int size, Transform parent, Material material)
		{
			// Creamos el Terrano en la posicion del mundo y con un Bounds que lo delimite segun su tama�o
			position = chunkCoord * size;
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

			// Offset dentro del mapa de ruido
			_terrainGenerator.offset = chunkCoord * _terrainGenerator.noiseScale;

			// Pone en cola la petición del Mapa de Ruido y ejecuta OnMapDataReceived cuando termina de ejecutarse
			_terrainGenerator.RequestMapData(OnMapDataReceived);
		}

		// Cuando reciba los datos del MAPA de ruido pide la MALLA
		void OnMapDataReceived(TerrainGenerator.MapData mapData)
		{
			meshRenderer.material.mainTexture = mapData.GetTexture2D();

			_terrainGenerator.RequestMeshData(mapData, OnMeshDataReceived);
		}

		// Cuando recibe los datos de la MALLA, la crea (la creacion no se puede paralelizar)
		void OnMeshDataReceived(MeshData meshData)
		{
			meshFilter.mesh = meshData.CreateMesh();
		}

		public void UpdateVisibility(int renderDist, Vector3 viewerPosition)
		{
			// La distancia del jugador al plano (con Bounds se consigue la distancia mas corta)
			float viewerDist = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

			visible = viewerDist <= renderDist * bounds.extents.x * 2;
		}
	}





	// Resetea la Semilla de forma Aleatoria
	public void ResetRandomSeed()
	{
		seed = NoiseMapGenerator.generateRandomSeed();
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
	}

	// Borra todos los terrenos renderizados
	public void ClearImmediate()
	{
		MeshFilter[] children = GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter child in children)
		{
			DestroyImmediate(child.gameObject);
		}
		chunkDictionary.Clear();
	}

	// Parametros que dependen del Area de Chunks Renderizados segun la posicion del Player
	// Longitud del Borde de los chunks, que sera el tama�o de mi matriz de Chunks Renderizados
	private int GetChunkBorderLength()
	{
		return renderDist * 2 + 1;
	}

	// Parametros que dependen del Chunk:
	// Level Of Detail
	//public int GetLOD(Vector2 chunk)
	//{
	//	int LOD = Mathf.FloorToInt((chunk - playerChunk).magnitude);
	//	return LOD;
	//}
	//// Noise Offset
	//public Vector2 GetOffset(Vector2 chunk)
	//{
	//	return chunk * np.noiseScale;
	//}



	// =========================================================
	// Transformaciones de Espacio de Mundo al Espacio del Chunk:

	Vector2 getChunk(Vector2 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / chunkSize),
			Mathf.Round(pos.y / chunkSize)
		);
	}
	Vector2 getChunk(Vector3 pos)
	{
		return new Vector2(
			Mathf.Round(pos.x / chunkSize),
			Mathf.Round(pos.z / chunkSize)
		);
	}

	Vector2 getLocalPos(Vector2 pos)
	{
		return pos - GetGlobalPos(getChunk(pos));
	}
	Vector2 getLocalPos(Vector3 pos)
	{
		return new Vector2(pos.x, pos.z) - GetGlobalPos(getChunk(pos));
	}

	// Posicion del Chunk en el Espacio de Mundo
	Vector2 GetGlobalPos(Vector2 chunkPos)
	{
		return chunkPos * (new Vector2(chunkSize - 1, chunkSize - 1));
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
		if (GUILayout.Button("Generate Terrain"))
		{
			terrainChunkGen.ClearImmediate();
			terrainChunkGen.Update();
		}

		if (GUILayout.Button("Reset Seed"))
		{
			terrainChunkGen.ClearImmediate();
			terrainChunkGen.ResetRandomSeed();
			terrainChunkGen.Update();
		}

		if (GUILayout.Button("Clear"))
		{
			terrainChunkGen.ClearImmediate();
		}
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Generador de Terreno Adaptativo a la posición del Jugador
//
// Visualiza solo los Chunks que estan dentro de la distancia de renderizado al Jugador
// Guarda los terrenos en un Mapa según su Coordenada de Chunk (pos / chunkSize)
//
// La generación es lazy:
// no genera el Chunk hasta que el jugador se acerca a menos de la distancia de renderizado
public class TerrainChunkGeneratorV2 : MonoBehaviour
{
    public NoiseMapGenerator.NoiseParams noiseParams;

    private int chunkSize { get => noiseParams.width; }
    private float noiseScale { get => noiseParams.noiseScale; }
    private int seed { get => noiseParams.seed; set => noiseParams.seed = value; }

    [Range(0.01f, 200f)] public float heightMultiplier = 100;

    public AnimationCurve heightCurve = new AnimationCurve();
    public Gradient gradient = new Gradient();

    public bool autoUpdate = true;

    // Material del terreno al que se le aplica la textura
    public Material mapMaterial;

    // Distancia de Renderizado
    [Range(1, 10)] public int renderDist = 4;
    
    // Numero de LODs posibles
    [Range(1, 20)] public int numLODs;

    // Almacen de terranos generados indexados por su Chunk [X,Y]
    private Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    // Chunks visibles en el ultimo update, para esconderlos cuando ya no esten a la distancia de renderizado
    private List<TerrainChunk> chunkLastVisible = new List<TerrainChunk>();

    public Transform viewer;

    private Vector2 ViewerPos { get => new Vector2(viewer.position.x, viewer.position.z); }

    private Vector2 ViewerChunk
    {
        get => GetChunkCoord(ViewerPos);
    }

    private Vector2 lastViewerChunk;

    void Start()
    {
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

        TerrainGenerator.Instance.UpdateTerrainLoading();
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
                // El unico valor en el mapa de ruido que diferencia a cada Chunk es el offset:
                NoiseMapGenerator.NoiseParams localNoiseParams = new NoiseMapGenerator.NoiseParams(noiseParams);
                localNoiseParams.offset += GetOffset(chunkCoords, noiseScale);
                
                chunkDictionary.Add(
                    chunkCoords,
                    new TerrainChunk(
                        chunkCoord: chunkCoords,
                        size: chunkSize - 1,
                        parent: transform,
                        material: mapMaterial,
                        localNoiseParams: localNoiseParams,
                        lod: getLOD(chunkCoords, ViewerChunk),
                        gradient: gradient,
                        heightMultiplier: heightMultiplier,
                        heightCurve: heightCurve,
                        numLODs: numLODs
                    )
                );
            }

            // Actualizamos el chunk
            TerrainChunk chunkTerrain = chunkDictionary[chunkCoords];
            chunkTerrain.UpdateVisibility(renderDist, ViewerPos, ViewerChunk);

            // Y si es visible recordarlo para hacerlo invisible cuando se escape del rango de renderizado
            if (chunkTerrain.Visible)
                chunkLastVisible.Add(chunkTerrain);
        }
    }

    // Resetea la Semilla de forma Aleatoria
    public void ResetRandomSeed()
    {
        seed = NoiseMapGenerator.generateRandomSeed();
    }

    public class TerrainChunk
    {
        /// Parametros del Perlin Noise con el que se Genera
        private NoiseMapGenerator.NoiseParams localNoiseParams;
        
        private GameObject meshObject;
        private Bounds bounds;

        private float Width { get => bounds.extents.x * 2; }
        private float Height { get => bounds.extents.y * 2; }

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshData[] meshDataPerLOD;

        private Vector2 chunkCoord;

        // Nivel de Detalle
        private int lod;

        // Modificadores de Altura
        private AnimationCurve heightCurve;
        private float heightMultiplier;

        // Color del Terreno segun la Altura
        private Gradient gradient;

        public bool Visible
        {
            get => meshObject.activeSelf;
            set => meshObject.SetActive(value);
        }

        public TerrainChunk(Vector2 chunkCoord, int size, Transform parent, Material material, NoiseMapGenerator.NoiseParams localNoiseParams,
            Gradient gradient, float heightMultiplier, AnimationCurve heightCurve, int numLODs, int lod = 0)
        {
            this.localNoiseParams = localNoiseParams;
            this.chunkCoord = chunkCoord;
            this.gradient = gradient;
            this.heightMultiplier = heightMultiplier;
            this.heightCurve = heightCurve;
            this.lod = lod;
            this.meshDataPerLOD = new MeshData[numLODs];

            // Creamos el Terrano en la posicion del mundo y con un Bounds que lo delimite segun su tamaño
            Vector2 position = chunkCoord * size;
            bounds = new Bounds(position, Vector3.one * size);
            Vector3 position3D = new Vector3(position.x, 0, position.y);

            // Creamos un Plano (por defecto tiene 10 de tamaño) en la posicion y con el tamaño ajustado al size
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            // Movemos el Chunk a la posicion asignada y asignamos al objeto generador como el padre
            meshObject.transform.position = position3D;
            meshObject.transform.parent = parent;

            Build();
        }

        /// <summary>
        /// Construye los datos necesarios paralelamente en el TerrainGenerator para reducir la carga
        /// </summary>
        public void Build()
        {
            TerrainGenerator.Instance.RequestMapData(localNoiseParams, gradient, mapData =>
            {
                meshRenderer.material.mainTexture = mapData.GetTexture2D();
                for (int i = 0; i < meshDataPerLOD.Length; i++)
                {
                    TerrainGenerator.Instance.RequestMeshDataLOD(mapData, heightMultiplier, heightCurve, gradient, i,
                        meshData =>
                        {
                            int thisLOD = ((StaticMeshData) meshData).lod;
                            meshDataPerLOD[thisLOD] = meshData;
                            if (lod == thisLOD)
                                meshFilter.mesh = meshData.CreateMesh();
                        });
                }
            });
        }

        private void UpdateLODmesh()
        {
            if (meshDataPerLOD.Length > lod && meshDataPerLOD[lod] != null)
                meshFilter.mesh = meshDataPerLOD[lod].CreateMesh();
        }

        /// <summary>
        /// Actualiza la Visibilidad del Chunk (si debe ser renderizado o no).
        /// Y actualiza tambien el LOD
        /// </summary>
        /// <param name="renderDist">Distancia Maxima de Renderizado de Chunks</param>
        /// <param name="viewerPos">Posicion del Jugador</param>
        public void UpdateVisibility(int renderDist, Vector2 viewerPos, Vector2 viewerChunk)
        {
            // La distancia del jugador al plano (con Bounds se consigue la distancia mas corta)
            float viewerDist = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            // Sera visible si la distancia al viewer es menor a la permitida
            Visible = viewerDist <= renderDist * Width;

            // LOD = Distancia en Chunks al Viewer
            lod = getLOD(chunkCoord, viewerChunk);
            // Si va a estar visible actualizamos la malla, sino esperamos a la proxima que sea visible
            if (Visible)
            {
                if (meshDataPerLOD.Length > lod)
                    UpdateLODmesh();
                else
                    Build();
            }
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

    public static int getLOD(Vector2 chunkCoord, Vector2 viewerChunk) =>
        Mathf.FloorToInt((viewerChunk - chunkCoord).magnitude) / 2;
    
    // Longitud del Borde de los chunks, que sera el tama�o de mi matriz de Chunks Renderizados
    private static int GetVisibilityChunkBorderLength(int renderDistance) => renderDistance * 2 + 1;

    // Noise Offset (desplazamiento del Mapa de Ruido al que se encuentra el Chunk)
    public static Vector2 GetOffset(Vector2 chunkCoord, float noiseScale) => chunkCoord * noiseScale;


    // Transformaciones de Espacio de Mundo al Espacio del Chunk:
    private Vector2 GetChunkCoord(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / chunkSize),
            Mathf.Round(pos.y / chunkSize)
        );
    }

    private Vector2 GetChunkCoord(Vector3 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / chunkSize),
            Mathf.Round(pos.z / chunkSize)
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
        return chunkPos * (new Vector2(chunkSize, chunkSize));
    }
}


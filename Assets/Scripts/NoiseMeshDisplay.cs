using System;
using System.Collections;
using GEOMETRY;
using UnityEngine;

[RequireComponent(
    typeof(MeshFilter),
    typeof(MeshRenderer)
)]
public class NoiseMeshDisplay : MonoBehaviour
{
    protected TerrainGenerator.MapData mapData;
    protected MeshData meshData;
    private Texture2D texture;

    public NoiseMapGenerator.NoiseParams noiseParams;
    private int ChunkSize { get => noiseParams.width; }
    private Vector2 Offset { get => noiseParams.offset; set => noiseParams.offset = value; }
    public float NoiseScale { get => noiseParams.noiseScale; set => noiseParams.noiseScale = value; }
    public int seed { get => noiseParams.seed; set => noiseParams.seed = value; }
    
    [Space]    
    [Range(0, 6)] public int LOD = 0;

    [Space] public Gradient gradient = new Gradient();

    public AnimationCurve heightCurve = new AnimationCurve();
    [Range(0.01f, 200f)] public float heightMultiplier = 100;
    
    [Space] public bool autoUpdate = true;
    
    public bool movement = false;
    [Range(0, 1)] public float speed;

    private Transform player;

    void Awake()
    {
        // Busca al Jugador para calcular el LOD dependiendo de su distancia
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            player = playerObj.transform;
            GetLOD(player.transform.position);
        }
    }

    private void Start()
    {
        UpdateMapData();
    }

    public void Update()
    {
        // Movimiento en el Mapa de Ruido al mover el offset
        if (movement)
        {
            Offset = new Vector2(Offset.x + Time.deltaTime * speed, Offset.y);
            UpdateMapData();
        }
        
        // Si hay un Jugador actualizamos el Nivel de Detalle de la Malla
        if (player != null)
        {
            int newLOD = GetLOD(player.transform.position);
            if (newLOD != LOD)
            {
                LOD = newLOD;
                UpdateMeshData();
            }
        }
        
        //TerrainGenerator.UpdateTerrainLoading();
    }

    /// <summary>
    /// Actualiza el Mapa de Alturas y la Textura
    /// </summary>
    public void UpdateMapData()
    {
        TerrainGenerator.Instance.RequestMapData(noiseParams,
            gradient,
            result =>
            {
                mapData = result;
                texture = mapData.GetTexture2D();
                UpdateTexture();
                UpdateMeshData();
            }
            );
    }

    public void UpdateTexture()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer)
            meshRenderer.sharedMaterial.mainTexture = texture;
    }

    /// <summary>
    /// Actualiza la Malla del Terreno.
    /// </summary>
    public virtual void UpdateMeshData()
    {
        TerrainGenerator.Instance.RequestMeshData(
            mapData,
            heightMultiplier,
            heightCurve, gradient,
            result =>
            {
                meshData = result;
                
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                if (meshFilter)
                    meshFilter.mesh = meshData.CreateMesh();
            }
            );
        
    }
    
    private void UpdateCollider()
    {
        // Calculo del Terrain Collider a partir del noisemap
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        if (terrainCollider)
        {
            float[,] noiseCollider = new float[ChunkSize, ChunkSize];
            for (int x = 0; x < ChunkSize; x++)
            for (int y = 0; y < ChunkSize; y++)
                noiseCollider[x, y] = mapData.noiseMap[x, y] * heightMultiplier;

            TerrainData data = terrainCollider.terrainData = new TerrainData();
            data.heightmapResolution = ChunkSize;
            data.SetHeights(0, 0, noiseCollider);
        }
    }
    
    public void AdjustHeightScale()
    {
        transform.localScale = new Vector3(1, heightMultiplier, 1);
    }

    private int GetLOD(Vector2 playerWorldPos)
    {
        var position = transform.position;
        Vector2 terrainWorldPos = new Vector2(position.x, position.z);
        return Mathf.FloorToInt((terrainWorldPos - playerWorldPos).magnitude / ChunkSize);
    }
    
    public void ResetRandomSeed()
    {
        seed = NoiseMapGenerator.generateRandomSeed();
    }
}
using System;
using System.Collections;
using UnityEngine;

[RequireComponent(
    typeof(MeshFilter),
    typeof(MeshRenderer)
)]
public class NoiseMeshDisplay : MonoBehaviour
{
    public NoiseMapGenerator.NoiseParams noiseParams;
    
    protected TerrainGenerator.MapData mapData;
    protected MeshData meshData;
    private Texture2D texture;

    [Space] [Range(0, 241)] public int chunkSize = 241;
    [Range(0, 6)] public int LOD = 1;

    [Range(0, 40)] public float noiseScale = .3f;

    [Range(1, 10)] public int octaves = 1;
    [Range(0, 1)] public float persistance = 1f;
    [Range(1, 5)] public float lacunarity = 1f;

    [Space] public Gradient gradient = new Gradient();

    public AnimationCurve heightCurve = new AnimationCurve();
    [Range(0.01f, 100f)] public float heightScale;

    public Vector2 offset;

    [Space] public bool autoUpdate = true;
    public bool movement = true;
    [Range(0, 1)] public float speed;

    [Space] public int seed = DateTime.Now.Millisecond;

    private Transform player;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        if (player != null)
            GetLOD(player.transform.position);

        UpdateMapData();
        UpdateMeshData();
    }

    public void Update()
    {
        // Movimiento en el Mapa de Ruido al mover el offset
        if (movement)
        {
            offset.x += Time.deltaTime * speed;
            UpdateMapData();
            UpdateMeshData();
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
        
        TerrainGenerator.UpdateTerrainLoading();
    }

    /// <summary>
    /// Actualiza el Mapa de Alturas y la Textura
    /// </summary>
    public void UpdateMapData()
    {
        TerrainGenerator.RequestMapData(noiseParams,
            gradient,
            result => mapData = result 
            );
        texture = mapData.GetTexture2D();
        
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer)
            meshRenderer.sharedMaterial.mainTexture = texture;
    }

    /// <summary>
    /// Actualiza la Malla del Terreno.
    /// </summary>
    public virtual void UpdateMeshData()
    {
        TerrainGenerator.RequestMeshData(
            mapData,
            heightScale,
            heightCurve, gradient,
            result => meshData = result
            );
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter)
            meshFilter.mesh = meshData.CreateMesh();
    }
    
    private void UpdateCollider()
    {
        // Calculo del Terrain Collider a partir del noisemap
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        if (terrainCollider)
        {
            float[,] noiseCollider = new float[chunkSize, chunkSize];
            for (int x = 0; x < chunkSize; x++)
            for (int y = 0; y < chunkSize; y++)
                noiseCollider[x, y] = mapData.noiseMap[x, y] * heightScale;

            TerrainData data = terrainCollider.terrainData = new TerrainData();
            data.heightmapResolution = chunkSize;
            data.SetHeights(0, 0, noiseCollider);
        }
    }
    
    public void AdjustHeightScale()
    {
        transform.localScale = new Vector3(1, heightScale, 1);
    }

    private int GetLOD(Vector2 playerWorldPos)
    {
        var position = transform.position;
        Vector2 terrainWorldPos = new Vector2(position.x, position.z);
        return Mathf.FloorToInt((terrainWorldPos - playerWorldPos).magnitude / chunkSize);
    }
    
    public void ResetRandomSeed()
    {
        seed = NoiseMapGenerator.generateRandomSeed();
    }
}
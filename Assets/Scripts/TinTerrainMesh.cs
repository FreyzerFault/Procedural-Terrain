using UnityEngine;

public class TinTerrainMesh : NoiseMeshDisplay
{
    public override void UpdateMeshData()
    {
        TerrainGenerator.RequestMeshDataTIN(
            mapData: mapData,
            heightMultiplier: heightScale,
            heightCurve: heightCurve,
            gradient: gradient,
            errorTolerance: 1,
            callback: result => meshData = result
            );
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter)
            meshFilter.mesh = meshData.CreateMesh();
    }
}
using System;
using GEOMETRY;
using UnityEngine;

namespace TINManager
{
    public class TinMeshDisplay : NoiseMeshDisplay
    {
        public override void UpdateMeshData()
        {
            // Generar Malla del TIN
            TerrainGenerator.Instance.RequestMeshDataTIN(
                mapData: mapData,
                heightMultiplier: heightMultiplier,
                heightCurve: heightCurve,
                gradient: gradient,
                errorTolerance: 1,
                callback: result =>
                {
                    meshData = result;

                    // Aplicar al MeshFilter
                    MeshFilter meshFilter = GetComponent<MeshFilter>();
                    if (meshFilter)
                        meshFilter.mesh = meshData.CreateMesh();
                }
            );
        }
    }
}
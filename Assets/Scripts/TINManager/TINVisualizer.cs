using System;
using System.Collections;
using GEOMETRY;
using UnityEngine;

namespace TINManager
{
    [ExecuteAlways]
    public class TINVisualizer : MonoBehaviour
    {
        public TIN tin;

        public NoiseMapGenerator.NoiseParams noiseParams;

        public Gradient gradient;
        public AnimationCurve heightCurve;

        public float[,] heightMap;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        public bool autoUpdate = true;
        public bool progressiveBuild = true;
        public bool animationRunning = false;
        public bool withTexture = true;

        public float errorTolerance = 0.1f;
        public float heightMultiplier = 100;
        public int fase = 0;

        public Vector2 newPoint = Vector2.zero;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        // Update is called once per frame
        private void Start()
        {
            UpdateMap();
            fase = 0;
        }

        public void UpdateMap()
        {
            UpdateHeightMap();

            if (progressiveBuild)
                UpdateMeshProgressive();
            else
                UpdateMesh();
        }

        public void UpdateHeightMap()
        {
            heightMap = NoiseMapGenerator.GetNoiseMap(noiseParams);
            if (withTexture)
            {
                meshRenderer.enabled = true;
                meshRenderer.sharedMaterial.mainTexture = NoiseMapGenerator.GetTexture(heightMap, gradient);
            }
            else
                meshRenderer.enabled = false;
        }

        public bool UpdateMeshProgressive()
        {
            if (tin == null)
                fase = 0;
            if (heightMap == null || heightMap.Length == 0)
                UpdateHeightMap();

            bool finished = false;
            if (fase == 0)
            {
                tin = new TIN(heightMap, errorTolerance, heightMultiplier);
                tin.InitGeometry(heightMap);
            }
            else
            {
                if (tin == null)
                {
                    fase = 0;
                    UpdateMeshProgressive();
                }
                
                finished = !tin.AddPointLoopIteration();
                //tin.AddPoint(new Vertex(newPoint.x, heightMap[(int)newPoint.x,(int)newPoint.y] * heightMultiplier, newPoint.y));
            }

            MeshData meshData = MeshGenerator.GenerateTINMeshData(tin, heightCurve, gradient);
            meshFilter.mesh = meshData.CreateMesh();

            return finished;
        }

        public void UpdateMesh()
        {
            MeshData meshData =
                MeshGenerator.GenerateTINMeshData(heightMap, out tin, 100, heightCurve, gradient, 0.1f, fase);
            meshFilter.sharedMesh = meshData.CreateMesh();
        }

        public void ResetRandomSeed()
        {
            noiseParams.seed = DateTime.Now.Millisecond;
        }

        public bool AddNextPoint()
        {
            fase++;
            return UpdateMeshProgressive();
        }

        public IEnumerator AnimatedGeneration()
        {
            fase = 0;
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Space) || AddNextPoint())
                    break;

                yield return new WaitForSeconds(.1f);
            }
        }

        private void OnDrawGizmos()
        {
            if (tin != null)
            {
                tin.OnDrawGizmos();
                
                Gizmos.color = Color.yellow;
                foreach (var v in tin.vertices)
                {
                    //Gizmos.DrawSphere(v.v3D, .1f);
                }
                
                if (tin.lastVertexAdded != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(tin.lastVertexAdded.v3D, .1f);
                }
            }

            // if (meshFilter)
            // {
            //     Mesh mesh = meshFilter.sharedMesh;
            //     if (mesh != null)
            //     {
            //         for (int i = 0; i < mesh.vertices.Length; i++)
            //         {
            //             Gizmos.color = Color.yellow;
            //             Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] + mesh.normals[i] * 10);
            //         }
            //     }
            // }
        }
    }
}
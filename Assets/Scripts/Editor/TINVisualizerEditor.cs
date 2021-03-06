using System;
using GEOMETRY;
using TINManager;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TINVisualizer))]
    class TINVisualizerEditor : UnityEditor.Editor
    {
        private bool showHeightInfo = false;
        private bool showVertexIndices = false;
        private bool showEdgeInfo = false;
        private bool showTriInfo = false;
        
        public void OnSceneGUI()
        {
            TINVisualizer tinVisualizer = target as TINVisualizer;
			
			
            if (!tinVisualizer)
            {
                Debug.LogError("No existe ningun objeto TINVisualizer al que añadir GUI");
                return;
            }

            TIN tin = tinVisualizer.tin;
            if (tin == null)
                return;
            
            GUIStyle cyanStyle = new GUIStyle();
            cyanStyle.normal.textColor = Color.cyan;
            GUIStyle magentaStyle = new GUIStyle();
            magentaStyle.normal.textColor = Color.magenta;
            GUIStyle greenStyle = new GUIStyle();
            greenStyle.normal.textColor = Color.green;
            GUIStyle whiteStyle = new GUIStyle();
            whiteStyle.normal.textColor = Color.white;
            
            if (tin.lastVertexAdded != null && tin.lastVertexAdded.Count > 0)
            {
                for (int i = 0; i < tin.lastVertexAdded.Count; i++)
                {
                    Handles.Label(tin.lastVertexAdded[i].v3D, tin.lastVertexAdded[i].v3D.ToString(), cyanStyle);  
                    Handles.Label(tin.lastVertexAdded[i].v3D + Vector3.up * 10, tin.lastVertexError[i].ToString(), magentaStyle);   
                }
            }
            
            if (showHeightInfo)
                foreach (Vertex v in tin.vertices)
                {
                    Handles.color = Color.yellow;
                    Handles.Label(v.v3D + Vector3.up * 10,  v.y.ToString(), cyanStyle);
                }

            if (showVertexIndices || showEdgeInfo)
                foreach (Edge e in tin.edges)
                {
                    if (showEdgeInfo)
                        Handles.Label((e.end.v3D + e.begin.v3D) / 2, e.ToString(), magentaStyle);
                    if (showVertexIndices)
                    {
                        Handles.Label(e.begin.v3D, e.begin.ToString(), greenStyle);
                        Handles.Label(e.end.v3D, e.end.ToString(), greenStyle);
                    }
                }

            if (showTriInfo)
                foreach (Triangle tri in tin.triangles)
                {
                    Vector3 triCenter = (tri.v1.v3D + tri.v2.v3D + tri.v3.v3D) / 3;
                
                    Handles.Label(triCenter, tri.ToString(), cyanStyle);
                }
        }
        public override void OnInspectorGUI()
        {
            TINVisualizer tinVisualizer = target as TINVisualizer;
            if (!tinVisualizer)
            {
                Debug.LogError("No existe ningun objeto TINVisualizer al que modificar su editor en el inspector");
                return;
            }

            // Si se cambio algun valor tambien generamos el mapa
            if (DrawDefaultInspector() && tinVisualizer.autoUpdate)
            {
                tinVisualizer.UpdateMap();
            }

            // Boton para generar el mapa
            if (GUILayout.Button("Generate Terrain"))
            {
                tinVisualizer.ResetTIN();
            }


            if (GUILayout.Button("Reset Seed"))
            {
                tinVisualizer.ResetRandomSeed();
                tinVisualizer.ResetTIN();
            }

            if (GUILayout.Button("Next Point"))
            {
                tinVisualizer.AddNextPoint();
            }

            if (GUILayout.Button("Animated Generation"))
            {
                tinVisualizer.BuildingAnimation();
            }

            
            
            // if (GUILayout.Button("Load From File"))
            // {
            //     String filePath = EditorUtility.OpenFilePanel("Load TIN", "", "txt");
            //     tinVisualizer.LoadFromFile(filePath);
            // }
        }
    }
}
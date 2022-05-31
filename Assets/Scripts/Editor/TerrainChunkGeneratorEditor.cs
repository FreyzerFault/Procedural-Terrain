using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TerrainChunkGeneratorV2))]
    public class TerrainChunkGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TerrainChunkGeneratorV2 terrainChunkGen = target as TerrainChunkGeneratorV2;
            if (terrainChunkGen == null)
            {
                Debug.Log("No existe ningun objeto TerrainGeneratorV2 al que modificar su editor en el inspector");
                return;
            }

            if (DrawDefaultInspector() && terrainChunkGen.autoUpdate)
            {
                terrainChunkGen.UpdateVisibleChunks();
            }

            // Boton para generar el mapa
            if (GUILayout.Button("Regenerate Terrain"))
            {
                terrainChunkGen.ClearImmediate();
                terrainChunkGen.UpdateVisibleChunks();
            }

            if (GUILayout.Button("Reset Seed"))
            {
                terrainChunkGen.ClearImmediate();
                terrainChunkGen.ResetRandomSeed();
                terrainChunkGen.UpdateVisibleChunks();
            }

            if (GUILayout.Button("Clear"))
                terrainChunkGen.ClearImmediate();
        }
    }
}
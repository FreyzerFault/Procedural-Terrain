using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(TerrainChunkGeneratorV1))]
	public class TerrainChunkGeneratorV1Editor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			TerrainChunkGeneratorV1 terrainChunkGen = target as TerrainChunkGeneratorV1;
			if (terrainChunkGen == null)
			{
				Debug.Log("No existe ningun objeto TerrainGenerator al que modificar su editor en el inspector");
				return;
			}
			
			DrawDefaultInspector();

			// Boton para generar el mapa
			if (GUILayout.Button("Generate Terrain"))
			{
				terrainChunkGen.ClearImmediate();
				terrainChunkGen.LoadChunks();
			}

			if (GUILayout.Button("Reset Seed"))
			{
				terrainChunkGen.ClearImmediate();
				terrainChunkGen.ResetRandomSeed();
				terrainChunkGen.LoadChunks();
			}

			if (GUILayout.Button("Clear"))
			{
				terrainChunkGen.ClearImmediate();
			}
		}
	}
}
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
	[CustomEditor(typeof(TerrainGenerator))]
	public class TerrainGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			TerrainGenerator terrainGen = target as TerrainGenerator;
			if (terrainGen == null)
			{
				Debug.Log("No existe ningun objeto TerrainGenerator al que modificar su editor en el inspector");
				return;
			}
			
			DrawDefaultInspector();

			// Boton para generar el mapa
			if (GUILayout.Button("Generate Terrain"))
			{
				terrainGen.ClearImmediate();
				terrainGen.LoadChunks();
			}

			if (GUILayout.Button("Reset Seed"))
			{
				terrainGen.ClearImmediate();
				terrainGen.ResetRandomSeed();
				terrainGen.LoadChunks();
			}

			if (GUILayout.Button("Clear"))
			{
				terrainGen.ClearImmediate();
			}
		}
	}
}
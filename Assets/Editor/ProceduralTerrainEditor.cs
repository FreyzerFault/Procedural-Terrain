using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
	[CustomEditor(typeof(ProceduralTerrain))]
	public class ProceduralTerrainEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			ProceduralTerrain proceduralTerrainGen = target as ProceduralTerrain;
			if (proceduralTerrainGen == null)
			{
				Debug.Log("No existe ningun objeto TerrainGenerator al que modificar su editor en el inspector");
				return;
			}

			// Si se cambio algun valor tambien generamos el mapa
			if (DrawDefaultInspector() && proceduralTerrainGen.autoUpdate)
				proceduralTerrainGen.UpdateTerrain();

			// Boton para generar el mapa
			if (GUILayout.Button("Generate Noise Terrain"))
			{
				proceduralTerrainGen.useNoise = true;
				proceduralTerrainGen.UpdateTerrain();
			}
			if (GUILayout.Button("Generate Random Terrain"))
			{
				proceduralTerrainGen.useNoise = false;
				proceduralTerrainGen.UpdateTerrain();
			}

			if (GUILayout.Button("Reset Seed"))
			{
				proceduralTerrainGen.ResetRandomSeed();
				proceduralTerrainGen.UpdateTerrain();
			}
		}
	}
}
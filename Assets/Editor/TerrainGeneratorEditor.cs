using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralTerrain))]
public class TerrainGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ProceduralTerrain proceduralTerrainGen = target as ProceduralTerrain;
		if (!proceduralTerrainGen)
		{
			Debug.Log("No existe ningun objeto TerrainGenerator al que modificar su editor en el inspector");
			return;
		}

		// Si se cambio algun valor tambien generamos el mapa
		if (DrawDefaultInspector())
			if (proceduralTerrainGen.autoUpdate)
			{
				proceduralTerrainGen.UpdateTerrain();
			}

		// Boton para generar el mapa
		if (proceduralTerrainGen && GUILayout.Button("Generate Noise Terrain"))
		{
			proceduralTerrainGen.useNoise = true;
			proceduralTerrainGen.UpdateTerrain();
		}
		if (proceduralTerrainGen && GUILayout.Button("Generate Random Terrain"))
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
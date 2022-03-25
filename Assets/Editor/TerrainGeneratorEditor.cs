using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		TerrainGenerator terrainGen = target as TerrainGenerator;
		if (!terrainGen)
		{
			Debug.Log("No existe ningun objeto TerrainGenerator al que modificar su editor en el inspector");
			return;
		}

		// Si se cambio algun valor tambien generamos el mapa
		if (DrawDefaultInspector())
			if (terrainGen.autoUpdate)
			{
				terrainGen.UpdateTerrain();
			}

		// Boton para generar el mapa
		if (terrainGen && GUILayout.Button("Generate Noise Terrain"))
		{
			terrainGen.useNoise = true;
			terrainGen.UpdateTerrain();
		}
		if (terrainGen && GUILayout.Button("Generate Random Terrain"))
		{
			terrainGen.useNoise = false;
			terrainGen.UpdateTerrain();
		}

	}
}
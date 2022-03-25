using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		NoiseGenerator mapGen = target as NoiseGenerator;
		if (!mapGen)
		{
			Debug.Log("No existe ningun objeto MapGenerator al que modificar su editor en el inspector");
			return;
		}

		// Si se cambio algun valor tambien generamos el mapa
		if (DrawDefaultInspector())
			if (mapGen.autoUpdate)
			{
				mapGen.GenerateTexture();
			}

		// Boton para generar el mapa
		if (mapGen && GUILayout.Button("Generate Noise Map"))
			mapGen.GenerateTexture(true);
		if (mapGen && GUILayout.Button("Generate Random Map"))
			mapGen.GenerateTexture(false);
	}
}

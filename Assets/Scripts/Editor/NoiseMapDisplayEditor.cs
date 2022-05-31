using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(NoiseMapDisplay))]
	public class NoiseMapDisplayEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			NoiseMapDisplay mapDisplay = target as NoiseMapDisplay;
			if (mapDisplay == null)
			{
				Debug.Log("No existe ningun objeto NoiseMapDisplay al que modificar su editor en el inspector");
				return;
			}

			// Si se cambio algun valor tambien generamos el mapa
			if (DrawDefaultInspector() && mapDisplay.autoUpdate)
				mapDisplay.GenerateTexture();

			// Boton para generar el mapa
			if (GUILayout.Button("Generate Noise Map"))
				mapDisplay.GenerateTexture(true);
			if (GUILayout.Button("Generate Random Map"))
				mapDisplay.GenerateTexture(false);

			if (GUILayout.Button("Reset Seed"))
			{
				mapDisplay.ResetRandomSeed();
				mapDisplay.GenerateTexture();
			}
		}
	}
}

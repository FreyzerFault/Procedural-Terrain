using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
	[CustomEditor(typeof(NoiseMeshDisplay))]
	public class NoiseMeshDisplayEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			NoiseMeshDisplay meshDisplay = target as NoiseMeshDisplay;
			if (meshDisplay == null)
			{
				Debug.Log("No existe ningun objeto MeshGenerator al que modificar su editor en el inspector");
				return;
			}

			// Si se cambio algun valor tambien generamos el mapa
			if (DrawDefaultInspector() && meshDisplay.autoUpdate)
				meshDisplay.CreateTerrain();

				// Boton para generar el mapa
			if (GUILayout.Button("Generate Noise Terrain"))
				meshDisplay.CreateTerrain();


			if (GUILayout.Button("Reset Seed"))
			{
				meshDisplay.ResetRandomSeed();
				meshDisplay.CreateTerrain();
			}
		}
	}
}
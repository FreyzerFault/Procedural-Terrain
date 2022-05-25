using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
	[CustomEditor(typeof(NoiseMeshDisplay))]
	public class NoiseMeshDisplayEditor : UnityEditor.Editor
	{
		private NoiseMeshDisplay meshDisplay;
		
		public override void OnInspectorGUI()
		{
			meshDisplay = target as NoiseMeshDisplay;
			if (!meshDisplay)
			{
				Debug.LogError("No existe ningun objeto MeshGenerator al que modificar su editor en el inspector");
				return;
			}

			// Si se cambio algun valor tambien generamos el mapa
			if (DrawDefaultInspector() && meshDisplay.autoUpdate)
				meshDisplay.Update();

				// Boton para generar el mapa
			if (GUILayout.Button("Generate Noise Terrain"))
				meshDisplay.Update();


			if (GUILayout.Button("Reset Seed"))
			{
				meshDisplay.ResetRandomSeed();
				meshDisplay.Update();
			}
		}
	}
}
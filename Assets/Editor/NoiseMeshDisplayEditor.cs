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
			{
				meshDisplay.CreateShape();
				meshDisplay.UpdateMesh();
			}

			// Boton para generar el mapa
			if (GUILayout.Button("Generate Noise Terrain"))
			{
				meshDisplay.CreateShape();
				meshDisplay.UpdateMesh();
			}


			if (GUILayout.Button("Reset Seed"))
			{
				meshDisplay.ResetRandomSeed();
				meshDisplay.CreateShape();
				meshDisplay.UpdateMesh();
			}
		}
	}
}
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshGenerator))]
public class MeshGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MeshGenerator meshGen = target as MeshGenerator;
		if (!meshGen)
		{
			Debug.Log("No existe ningun objeto MeshGenerator al que modificar su editor en el inspector");
			return;
		}

		if (!meshGen.mesh)
			meshGen.GetComponent<MeshFilter>().mesh = meshGen.mesh = new Mesh();

		// Si se cambio algun valor tambien generamos el mapa
		if (DrawDefaultInspector())
			if (meshGen.autoUpdate)
			{
				meshGen.CreateShape();
				meshGen.UpdateMesh();
			}

		// Boton para generar el mapa
		if (meshGen && GUILayout.Button("Generate Noise Terrain"))
		{
			meshGen.useNoise = true;
			meshGen.CreateShape();
			meshGen.UpdateMesh();
		}
		if (meshGen && GUILayout.Button("Generate Random Terrain"))
		{
			meshGen.useNoise = false;
			meshGen.CreateShape();
			meshGen.UpdateMesh();
		}

	}
}
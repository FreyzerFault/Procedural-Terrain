using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
	public List<Camera> cameras;
	public int defaultCameraIndex = 0;
	public int activeCameraIndex = 0;

	public Camera ActiveCamera { get => cameras[activeCameraIndex]; set => activeCameraIndex = cameras.IndexOf(value); }

	void Awake()
	{
		if (cameras.Count == 0)
			cameras = new List<Camera>(GetComponentsInChildren<Camera>());

		// Camara por defecto activada => 0
		activeCameraIndex = defaultCameraIndex;
	}

	public void SwitchCamera(int i)
	{
		if (i >= 0 && i < cameras.Count)
		{
			// Desactivo la Anterior
			ActiveCamera.gameObject.SetActive(false);
			
			// Activo la nueva
			activeCameraIndex = i;
			ActiveCamera.gameObject.SetActive(true);
		}
		else
			Debug.LogError("Camera " + i + " no existe");
	}
}

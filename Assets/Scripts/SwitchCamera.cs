using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCamera : MonoBehaviour
{
	public Camera[] Cameras;

	private int currentCam = 0;

	// Cambia la camara actual a la siguiente
	public void Switch()
	{
		Cameras[currentCam].gameObject.SetActive(false);
		currentCam = (currentCam + 1) % Cameras.Length;
		Cameras[currentCam].gameObject.SetActive(true);
	}
}

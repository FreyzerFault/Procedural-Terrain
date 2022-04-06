using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderScaleNoise : MonoBehaviour
{
	private Slider slider;

	public NoiseMeshDisplay meshDisplay;

	void Start()
	{
		slider = GetComponent<Slider>();
	}

	public void OnSlider()
	{
		meshDisplay.noiseScale = slider.value;
	}
}

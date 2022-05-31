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
		meshDisplay.NoiseScale = slider.value;
     	}
}

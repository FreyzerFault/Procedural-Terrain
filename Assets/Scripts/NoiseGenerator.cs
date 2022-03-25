using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{
	public int width = 256;
	public int height = 256;

	public float NoiseScale = 20f;

	public float OffsetX = 100f;
	public float OffsetY = 100f;

	public bool autoUpdate;

	public Renderer TextureRenderer;

	void Start()
	{
		OffsetX = Random.value * 100000;
		OffsetY = Random.value * 100000;
		GenerateTexture();
	}

	public Texture2D GenerateTexture(bool noise = true)
	{
		Texture2D texture = new Texture2D(width, height);

		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				if (noise)
					texture.SetPixel(x, y, getNoiseColor(x, y));
				else
					texture.SetPixel(x, y, Color.white * Random.value);

		texture.Apply();

		TextureRenderer.sharedMaterial.mainTexture = texture;
		TextureRenderer.transform.localScale = new Vector3(width, 1, height);

		return texture;
	}

	// Sample the Noise in pixels (x,y) and lerp to a gradient of two Colors
	private Color getNoiseColor(int x, int y, Color? lowColor = null, Color? highColor = null)
	{
		float xCoord = (float)x / width * NoiseScale + OffsetX;
		float yCoord = (float)y / height * NoiseScale + OffsetY;

		lowColor ??= Color.black;
		highColor ??= Color.white;

		float sample = Mathf.PerlinNoise(xCoord, yCoord);
		return Color.Lerp((Color)lowColor, (Color)highColor, sample);
	}
}

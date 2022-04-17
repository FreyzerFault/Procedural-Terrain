using System;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

public class NoiseMapGenerator
{
	// Devuelve un Mapa de Ruido
	public static float[,] GetNoiseMap(
		int width, int height, float noiseScale, Vector2 offset,
		int numOctaves, float persistance, float lacunarity, int seed
		)
	{
		// Generamos los octavos con offsets distintos segun la SEED
		Vector2[] octaveOffsets = GetRandomOctaveOffsets(numOctaves, seed);
		
		// Calculamos el maximo valor para luego interpolarlo a [0,1]
		float amplitude = 1f;
		float maxNoise = 0;
		for (int i = 0; i < numOctaves; i++)
		{
			maxNoise += amplitude;
			amplitude *= persistance;
		}

		float[,] noiseMap = new float[width, height];

		// Recorremos el mapa en 2D
		for (int x = 0; x < width; x++)
		for (int y = 0; y < height; y++)
		{
			// Amplitud y frecuencia de cada Octavo
			amplitude = 1;
			float frecuency = 1;

			// Ruido acumulado por cada octavo
			float noiseHeight = 0;

			// Acumulamos el ruido de cada Octavo
			for (int i = 0; i < numOctaves; i++)
			{
				// Mapeamos las coordenadas de 0,width a las que necesita el Ruido
				Vector2 mapCoords = GetMapCoordinates(x, y, width, height, offset + octaveOffsets[i], noiseScale);

				// Reduce la frecuencia segun la lacunarity
				mapCoords *= frecuency;

				// Calculamos el Ruido
				float sample = Mathf.PerlinNoise(mapCoords.x, mapCoords.y);

				// Mapeamos la onda de [-1,1] para aplicarle la Amplitud y acumular el Octavo
				sample = sample * 2 - 1;

				// Acumulamos el Octavo segun su Amplitud que depende de la persistencia
				noiseHeight += sample * amplitude;

				// Actualizamos Amplitud y Frecuencia segun la Persistencia y Lacunarity
				amplitude *= persistance;
				frecuency *= lacunarity;
			}

			// Almacenamos el Ruido resultante interpolado entre el Maximo y el Minimo
			noiseMap[x, y] = Mathf.InverseLerp(-maxNoise, maxNoise, noiseHeight);
		}

		return noiseMap;
	}

	public static Texture2D GetTexture(float[,] noiseMap, [CanBeNull] Gradient gradient = null)
	{
		int width = noiseMap.GetLength(0), height = noiseMap.GetLength(1);

		Texture2D texture = new Texture2D(width, height);

		texture.SetPixels(GetTextureData(noiseMap, gradient));

		return texture;
	}

	public static Color[] GetTextureData(float[,] noiseMap, [CanBeNull] Gradient _gradient = null)
	{
		Gradient gradient = new Gradient();
		if (_gradient == null)
		{
			gradient.alphaKeys = new GradientAlphaKey[2];
			gradient.colorKeys = new GradientColorKey[2];
			gradient.alphaKeys.SetValue(new GradientAlphaKey(1, 0), 0);
			gradient.alphaKeys.SetValue(new GradientAlphaKey(1, 1), 1);
			gradient.colorKeys.SetValue(new GradientColorKey(Color.black, 0), 0);
			gradient.colorKeys.SetValue(new GradientColorKey(Color.white, 1), 1);
		}
		else
		{
			// Evaluar una Curva tiene problemas con el paralelismo
			// asi que si creamos una curva por cada hilo en vez de reutilizarla
			// podemos evitar bloquear el hilo:
			gradient.alphaKeys = _gradient.alphaKeys;
			gradient.colorKeys = _gradient.colorKeys;
		}

		// Si no se pasa un Gradiente se utiliza uno basico entre Negro y Blanco
		gradient ??= NoiseMeshGenerator.GetDefaultGradient();

		int width = noiseMap.GetLength(0), height = noiseMap.GetLength(1);
		
		Color[] texColors = new Color[width * height];

		// Coloreamos la textura segun el mapa
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			texColors[y * width + x] = heightToColor(noiseMap[x, y], gradient);
		}

		return texColors;
	}

	public static Color heightToColor(float height, Gradient gradient, float min = 0, float max = 1)
	{
		float tNoiseValue = Mathf.InverseLerp(min, max, height);

		return gradient.Evaluate(tNoiseValue);
	}
	

	private static Vector2 GetMapCoordinates(int x, int y, int width, int height, Vector2 offset, float scale)
	{
		// Centro del mapa para escalar desde el centro
		Vector2 center = new Vector2(width / 2f, height / 2f);

		return new Vector2(
			(x - center.x) / width * scale + offset.x,
			(y - center.y) / height * scale + offset.y
			);
	}


	private static Vector2[] GetRandomOctaveOffsets(int numOctaves, int seed)
	{
		// ALEATORIEDAD por SEED:
		// Generamos un offset para los octavos aleatorio segun la semilla
		System.Random rand = new System.Random(seed);
		Vector2[] octaveOffsets = new Vector2[numOctaves];
		for (int i = 0; i < numOctaves; i++)
		{
			float offsetX = rand.Next(-100000, 100000);
			float offsetY = rand.Next(-100000, 100000);

			octaveOffsets[i] = new Vector2(offsetX, offsetY);
		}

		return octaveOffsets;
	}

	public static int generateRandomSeed()
	{
		return DateTime.Now.Millisecond;
	}
}

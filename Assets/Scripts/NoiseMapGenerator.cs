using System;
using UnityEngine;

public class NoiseMapGenerator
{
	// TODO Maximo y Minimo de Altura calculado a ojo, debe de haber otra forma
	private const float maxHeight = 2f, minHeight = -2f;

	private Vector2[] octaveOffsets;
	private int seed = DateTime.Now.Millisecond;

	// Devuelve un Mapa de Ruido
	public float[,] GetNoiseMap(
		int width, int height, float noiseScale, Vector2 offset,
		int numOctaves, float persistance, float lacunarity
		)
	{
		// Si los offset de los octavos no coinciden con el numero de los octavos tenemos que generarlos
		if (octaveOffsets == null || octaveOffsets.Length != numOctaves)
			octaveOffsets = GetRandomOctaveOffsets(numOctaves, seed);

		float[,] noiseMap = new float[width, height];

		// Recorremos el mapa en 2D
		for (int x = 0; x < width; x++)
		for (int y = 0; y < height; y++)
		{
			// Amplitud y frecuencia de cada Octavo
			float amplitude = 1;
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

			// Volvemos a mapear la onda de -1,1 a 0,1
			noiseHeight += (noiseHeight + 1) / 2;

			// Almacenamos el Ruido resultante interpolado entre el Maximo y el Minimo
			noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseHeight);
		}

		return noiseMap;
	}

	public static Color heightToColor(float height, Gradient gradient, float min = 0, float max = 1)
	{
		float tNoiseValue = Mathf.InverseLerp(min, max, height);

		return gradient.Evaluate(tNoiseValue);
	}

	public int ResetRandomSeed()
	{
		// Reseteo los offsets de los octavos para volverlos a calcular la proxima vez
		octaveOffsets = null;

		return seed = DateTime.Now.Millisecond;
	}

	public void setSeed(int s) { seed = s; }
	public int getSeed() { return seed; }

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
}

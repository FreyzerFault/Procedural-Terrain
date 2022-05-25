using System;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

public class NoiseMapGenerator
{
    [Serializable]
    public class NoiseParams
    {
        // Anchura y Altura del terreno (num de vertices)
        // TODO Limitar Width y Height a 240
        [Range(50, 241)] public int width = 241;
        [Range(50, 241)] public int height = 241;
        public float noiseScale = 5f;

        // Mejora con Octavos
        [Range(1, 10)] public int numOctaves = 4;
        [Range(0, 1)] public float persistance = 0.5f;
        [Range(1, 5)] public float lacunarity = 2;

        public Vector2 offset = Vector2.zero;

        public int seed = DateTime.Now.Millisecond;

        // COPY
        public NoiseParams(NoiseParams orig)
        {
            this.width = orig.width;
            this.height = orig.height;
            this.noiseScale = orig.noiseScale;
            this.offset = orig.offset;
            this.numOctaves = orig.numOctaves;
            this.persistance = orig.persistance;
            this.lacunarity = orig.lacunarity;
            this.seed = orig.seed;
        }
    }

    public static float[,] GetNoiseMap(NoiseParams noiseParams)
    {
        return GetNoiseMap(noiseParams.width, noiseParams.height, noiseParams.noiseScale, noiseParams.offset, noiseParams.numOctaves, noiseParams.persistance,
            noiseParams.lacunarity, noiseParams.seed);
    }

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
        texture.Apply();

        return texture;
    }

    public static Color[] GetTextureData(float[,] noiseMap, [CanBeNull] Gradient _gradient = null)
    {
        Gradient gradient = null;

        // Evaluar una Curva tiene problemas con el paralelismo
        // asi que si creamos una curva por cada hilo en vez de reutilizarla
        // podemos evitar bloquear el hilo:
        if (_gradient != null)
        {
            gradient = new Gradient
            {
                alphaKeys = _gradient.alphaKeys,
                colorKeys = _gradient.colorKeys
            };
        }

        // Si no se pasa un Gradiente se utiliza uno basico entre Negro y Blanco
        gradient ??= MeshGenerator.GetDefaultGradient();

        int width = noiseMap.GetLength(0), height = noiseMap.GetLength(1);

        // Pasamos de un mapa 2D de alturas a uno de Color 1D que es el input que necesita la Texture2D
        Color[] texColors = new Color[width * height];

        // Coloreamos la textura segun las alturas
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            texColors[y * width + x] = HeightToColor(noiseMap[x, y], gradient);
        }

        return texColors;
    }

    // Mapea la altura a un color dentro del Gradiente, se asume que las alturas son [0,1]
    // Si no son [0,1] se mapea el valor de altura a [0,1] antes de evaluar
    private static Color HeightToColor(float height, Gradient gradient, float min = 0, float max = 1)
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
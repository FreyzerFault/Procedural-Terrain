using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GEOMETRY;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Clase en la que se almacenan los parametros usados en el ruido
/// </summary>
public class NoiseMapGenerator
{
    [Serializable]
    public class NoiseParams
    {
        // Anchura y Altura del terreno (num de vertices)
        public int width
        {
            get => size;
            set => size = value;
        }

        public int height
        {
            get => size;
            set => size = value;
        }

        [Range(0, 241)] public int size = 241;

        public float noiseScale = 5f;

        // Mejora con Octavos
        [Range(1, 10)] public int numOctaves = 4;
        [Range(0, 1)] public float persistance = 0.5f;
        [Range(1, 5)] public float lacunarity = 2;

        public Vector2 offset = Vector2.zero;

        public int seed = DateTime.Now.Millisecond;

        // Default
        public NoiseParams()
        {
            width = 241;
            height = 241;
            noiseScale = 5;
            offset = Vector2.zero;
            numOctaves = 4;
            persistance = 0.5f;
            lacunarity = 2;
            seed = DateTime.Now.Millisecond;
        }


        public NoiseParams(int width, int height, float scale, Vector2 offset, int numOctaves, float persistance,
            float lacunarity, int seed)
        {
            this.width = width;
            this.height = height;
            this.noiseScale = noiseScale;
            this.offset = offset;
            this.numOctaves = numOctaves;
            this.persistance = persistance;
            this.lacunarity = lacunarity;
            this.seed = seed;
        }

        // COPY
        public NoiseParams(NoiseParams orig)
        {
            width = orig.width;
            height = orig.height;
            noiseScale = orig.noiseScale;
            offset = orig.offset;
            numOctaves = orig.numOctaves;
            persistance = orig.persistance;
            lacunarity = orig.lacunarity;
            seed = orig.seed;
        }
    }

    /// <summary>
    /// Genera un Mapa de Ruido con las caracteristicas dadas en NoiseParams
    /// </summary>
    /// <param name="np">Parametros del ruido</param>
    /// <returns>Array Bidimensional de alturas</returns>
    public static float[,] GetNoiseMap(NoiseParams np)
    {
        // Scale no puede ser negativa
        if (np.noiseScale <= 0)
            np.noiseScale = 0.0001f;

        // Generamos los octavos con offsets distintos segun la SEED
        Vector2[] octaveOffsets = GetRandomOctaveOffsets(np.numOctaves, np.seed, np.offset);

        // Calculamos el maximo valor para luego interpolarlo a [0,1]
        float maxNoiseValue = GetMaxNoiseValue(np);

        float[,] noiseMap = new float[np.width, np.height];
        Vector2 center = new Vector2(np.width / 2f, np.height / 2f);

        // Recorremos el mapa en 2D
        for (int y = 0; y < np.height; y++)
        for (int x = 0; x < np.width; x++)
        {
            // Almacenamos el Ruido resultante
            noiseMap[x, y] = GetNoiseHeight(new Vector2(x - center.x, y - center.y), np, octaveOffsets, maxNoiseValue);
        }

        return noiseMap;
    }

    /// <summary>
    /// Genera un mapa de ruido con las caracteristicas dadas
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="noiseScale"></param>
    /// <param name="offset"></param>
    /// <param name="numOctaves"></param>
    /// <param name="persistance"></param>
    /// <param name="lacunarity"></param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static float[,] GetNoiseMap(
        int width, int height, float noiseScale, Vector2 offset,
        int numOctaves, float persistance, float lacunarity, int seed
    )
    {
        return GetNoiseMap(
            new NoiseParams(width, height, noiseScale, offset, numOctaves, persistance, lacunarity, seed));
    }


    /// <summary>
    /// Genera un set de offsets para cada octavo, a partir de una semilla, bajo un offset inicial
    /// </summary>
    /// <param name="numOctaves">Numero de Octavos usado</param>
    /// <param name="seed">Semilla con la que genera los Random</param>
    /// <param name="offset">Offset inicial</param>
    /// <returns>Array de offsets 2D</returns>
    private static Vector2[] GetRandomOctaveOffsets(int numOctaves, int seed, Vector2 offset)
    {
        // ALEATORIEDAD por SEED:
        // Generamos un offset para los octavos aleatorio segun la semilla
        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++)
        {
            float offsetX = rand.Next(-100000, 100000) + offset.x;
            float offsetY = rand.Next(-100000, 100000) + offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        return octaveOffsets;
    }


    /// <summary>
    /// Genera una textira a partir de una Nube de Puntos.
    /// Con una resolucion dada, inversa a la densidad de puntos sampleados de color
    /// </summary>
    /// <param name="points">Nube de puntos</param>
    /// <param name="maxPoint">AABB.max</param>
    /// <param name="minPoint">AABB.min</param>
    /// <param name="gradient">Colores por altura</param>
    /// <param name="resolutionScale">Resolucion (10 => 0.1 entre puntos sampleados)</param>
    public static Texture2D GetTexture(Vector3[] points, AABB aabb,
        [CanBeNull] Gradient gradient = null, int resolutionScale = 1)
    {
        int width = Mathf.CeilToInt(aabb.Width * resolutionScale) + 1;
        int height = Mathf.CeilToInt(aabb.Height * resolutionScale) + 1;
        
        Texture2D texture = new Texture2D(Math.Max(width, height), height);

        texture.SetPixels(GetTextureData(points, aabb, resolutionScale, gradient));
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// Genera una Textura a partir de un Mapa de Alturas bidimensional.
    /// </summary>
    /// <param name="noiseMap">Mapa de Alturas</param>
    /// <param name="gradient">Colores por altura</param>
    public static Texture2D GetTexture(float[,] noiseMap, [CanBeNull] Gradient gradient = null)
    {
        int width = noiseMap.GetLength(0), height = noiseMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        texture.SetPixels(GetTextureData(noiseMap, gradient));
        texture.Apply();

        return texture;
    }


    /// <summary>
    /// Crea datos de Color para una textura.
    /// A partir de una Nube de Puntos, con un AABB (width, height).
    /// Los colores estan en un array de puntos regulares, formando u Grid
    /// asi que hay que aproximar cada punto a su punto del Grid mas cercano
    /// </summary>
    /// <param name="points">Nube de Puntos</param>
    /// <param name="width">AABB.width</param>
    /// <param name="height">AABB.height</param>
    /// <param name="resolutionScale">escalado inverso de las casillas del grid, 10 => tamaño 0.1 por casilla</param>
    /// <param name="_gradient">Colores por altura</param>
    /// <returns>Datos de color</returns>
    public static Color[] GetTextureData(Vector3[] points, AABB aabb, int resolutionScale, [CanBeNull] Gradient _gradient = null)
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

        int width = Mathf.CeilToInt(aabb.Width * resolutionScale) + 1;
        int height = Mathf.CeilToInt(aabb.Height * resolutionScale) + 1;
        
        // Pasamos de un mapa 2D de alturas a uno de Color 1D que es el input que necesita la Texture2D
        Color[] texColors = new Color[width * height];

        // Coloreamos la textura segun las alturas
        foreach (Vector3 point in points)
        {
            // Pasamos de la escala real a la reducida en la resolucion dada (10x10)
            Vector2 sampleCoords = new Vector2(
                Mathf.FloorToInt(Mathf.Lerp(0, width - 1, Mathf.InverseLerp(aabb.min.x, aabb.max.x, point.x))),
                Mathf.FloorToInt(Mathf.Lerp(0, height - 1, Mathf.InverseLerp(aabb.min.y, aabb.max.y, point.z)))
            );
            texColors[(int) (sampleCoords.y * width + sampleCoords.x)] = HeightToColor(point.y, gradient);
        }

        return texColors;
    }

    /// <summary>
    /// Crea datos de Color para una textura.
    /// A partir de un Mapa de Alturas bidimensional.
    /// </summary>
    /// <param name="noiseMap">Mapa de Alturas</param>
    /// <param name="_gradient">Colores por altura</param>
    /// <returns>Datos de Color</returns>
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

    /// <summary>
    /// Mapea la altura a un color dentro del Gradiente, se asume que las alturas son [0,1]
    /// Si no son [0,1] se mapea el valor de altura a [0,1] antes de evaluar
    /// </summary>
    /// <param name="height">Valor de Altura</param>
    /// <param name="gradient">Color por altura</param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns>Color mapeado en el Gradiente</returns>
    private static Color HeightToColor(float height, Gradient gradient, float min = 0, float max = 1)
    {
        float tNoiseValue = Mathf.InverseLerp(min, max, height);

        return gradient.Evaluate(tNoiseValue);
    }

    /// <summary>
    ///  Genera una semilla random
    /// </summary>
    public static int GenerateRandomSeed()
    {
        return DateTime.Now.Millisecond;
    }

    /// <summary>
    /// Muestrea una Nube de Puntos de un Archivo
    /// Le añade una altura basada en la funcion de Ruido de Perlin
    /// Con los parametros que le pasemos
    /// </summary>
    /// <param name="np">Parametros del Ruido</param>
    /// <param name="filePath">Nombre del Archivo con la Nube de Puntos</param>
    /// <returns>Nube de Puntos con Alturas segun el Ruido de Perlin</returns>
    public static Vector3[] SampleNoiseInPointsFromFile(NoiseParams np, string filePath, out AABB aabb)
    {
        // Scale no puede ser negativa
        if (np.noiseScale <= 0)
            np.noiseScale = 0.0001f;

        // Generamos los octavos con offsets distintos segun la SEED
        Vector2[] octaveOffsets = GetRandomOctaveOffsets(np.numOctaves, np.seed, np.offset);

        // Calculamos el maximo valor para luego interpolarlo a [0,1]
        float maxNoiseValue = GetMaxNoiseValue(np);

        // Nube de puntos
        Vector3[] points = Array.Empty<Vector3>();
        int index = 0;

        // Leemos el archivo de texto
        string[] lines = File.ReadAllLines(filePath);

        // Puntos del AABB para crear puntos en las esquinas
        aabb = new AABB();

        foreach (string line in lines)
        {
            // Tamaño de la Nube de Puntos
            if (!line.Contains(' '))
            {
                // Le añadimos 4 mas por las esquinas
                points = new Vector3[int.Parse(line) + 4];
                continue;
            }

            // Extraemos el punto
            String[] sCoords = line.Split(' ');
            Vector2 mapCoords = new Vector2(float.Parse(sCoords[0]), float.Parse(sCoords[1]));

            // Lo añadimos a la Nube con su altura
            if (points.Length > index)
                points[index++] = new Vector3(
                    mapCoords.x,
                    GetNoiseHeight(mapCoords, np, octaveOffsets, maxNoiseValue),
                    mapCoords.y);

            // Pillamos el maximo y el minimo con cada punto para el AABB
            aabb.max.x = Mathf.Max(aabb.max.x, mapCoords.x);
            aabb.max.y = Mathf.Max(aabb.max.y, mapCoords.y);
            aabb.min.x = Mathf.Min(aabb.min.x, mapCoords.x);
            aabb.min.y = Mathf.Min(aabb.min.y, mapCoords.y);
        }

        // Añadimos las ESQUINAS
        Vector3[] corners = GetCorners(aabb, np, octaveOffsets, maxNoiseValue);
        foreach (Vector3 corner in corners)
            points[index++] = corner;

        return points;
    }

    /// <summary>
    /// Obtiene el valor de altura maximo del Ruido de Perlin segun su persistencia y num de Octavos.
    /// Sumando amplitudes
    /// </summary>
    /// <param name="np">Parametros del Ruido</param>
    /// <returns>Valor maximo posible</returns>
    private static float GetMaxNoiseValue(NoiseParams np)
    {
        // Calculamos el maximo valor para luego interpolarlo a [0,1]
        float amplitude = 1f;
        float maxNoise = 0;
        for (int i = 0; i < np.numOctaves; i++)
        {
            maxNoise += amplitude;
            amplitude *= np.persistance;
        }

        return maxNoise;
    }

    /// <summary>
    /// Calcula la Altura de un punto con Ruido de Perlin a partir de unos parametros y unos octavos
    /// </summary>
    /// <param name="point">Punto en el mapa</param>
    /// <param name="np">Parametros del Ruido</param>
    /// <param name="octaveOffsets">Offsets del Octavo (le da un toque random)</param>
    /// <param name="maxNoiseValue">Valor maximo de altura posible</param>
    private static float GetNoiseHeight(Vector2 point, NoiseParams np, Vector2[] octaveOffsets, float maxNoiseValue)
    {
        // Amplitud y frecuencia de cada Octavo
        float amplitude = 1;
        float frecuency = 1;

        // Ruido acumulado por cada octavo
        float noiseHeight = 0;

        // Acumulamos el ruido de cada Octavo
        for (int i = 0; i < np.numOctaves; i++)
        {
            // Mapeamos las coordenadas a las que necesita el Ruido
            Vector2 noiseCoords = new Vector2(
                (point.x + octaveOffsets[i].x) / np.noiseScale * frecuency,
                (point.y + octaveOffsets[i].y) / np.noiseScale * frecuency
            );

            // Calculamos el Ruido
            float heightSample = Mathf.PerlinNoise(noiseCoords.x, noiseCoords.y);

            // Mapeamos la onda de [-1,1] para aplicarle la Amplitud y acumular el Octavo
            heightSample = heightSample * 2 - 1;

            // Acumulamos el Octavo segun su Amplitud que depende de la persistencia
            noiseHeight += heightSample * amplitude;

            // Actualizamos Amplitud y Frecuencia segun la Persistencia y Lacunarity
            amplitude *= np.persistance;
            frecuency *= np.lacunarity;
        }

        // El Ruido resultante se interpola entre el Maximo y el Minimo
        return Mathf.InverseLerp(-maxNoiseValue, maxNoiseValue, noiseHeight);
    }

    /// <summary>
    /// Genera las esquinas de un espacio bidimensional definido por un AABB (maxpoint, minpoint)
    ///  con la altura correspondiente en el Ruido de Perlin
    /// </summary>
    /// <param name="aabb"></param>
    /// <param name="np">Parametros del Ruido</param>
    /// <param name="octaveOffsets">Offsets de cada octavo</param>
    /// <param name="maxNoiseValue">Valor maximo de ruido posible</param>
    /// <returns>Array con las Esquinas {BOT LEFT, BOT RIGHT, TOP LEFT, TOP RIGHT}</returns>
    private static Vector3[] GetCorners(AABB aabb, NoiseParams np, Vector2[] octaveOffsets,
        float maxNoiseValue)
    {
        Vector3[] corners = new Vector3[4];

        corners[0] = new Vector3(aabb.min.x, 0, aabb.min.y); // BOT LEFT
        corners[1] = new Vector3(aabb.max.x, 0, aabb.min.y); // BOT RIGHT
        corners[2] = new Vector3(aabb.min.x, 0, aabb.max.y); // TOP LEFT
        corners[3] = new Vector3(aabb.max.x, 0, aabb.max.y); // TOP RIGHT

        corners[0].y = GetNoiseHeight(new Vector2(corners[0].x, corners[0].z), np, octaveOffsets, maxNoiseValue);
        corners[1].y = GetNoiseHeight(new Vector2(corners[1].x, corners[1].z), np, octaveOffsets, maxNoiseValue);
        corners[2].y = GetNoiseHeight(new Vector2(corners[2].x, corners[2].z), np, octaveOffsets, maxNoiseValue);
        corners[3].y = GetNoiseHeight(new Vector2(corners[3].x, corners[3].z), np, octaveOffsets, maxNoiseValue);

        return corners;
    }
}
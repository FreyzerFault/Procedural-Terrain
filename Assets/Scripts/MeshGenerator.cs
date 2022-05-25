using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public static class MeshGenerator
{
    /// <summary>
    /// Generamos los datos de la malla (Vertices, Triangulos, Colors / UVs)
    /// No generamos la Mesh en este metodo
    /// Porque Generar una MESH tiene la limitacion de que no se puede hacer multithreading
    /// Por lo que este proceso se puede hacer en hilos
    /// </summary>
    /// <param name="heightMap">Mapa/Matriz con valores de altura</param>
    /// <param name="heightMultiplier">Factor de la altura para pasar de un rango 0,1 a uno mucho mas grande</param>
    /// <param name="_heightCurve">Curva que modifica el output de alturas</param>
    /// <param name="LOD">Level of Detail (0 = normal, 1,2,3,... menor geometria)</param>
    /// <param name="gradient">Gradiente de Color con el que asignar color a los vertices</param>
    /// <returns>Datos de un Mesh</returns>
    // 
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier,
        AnimationCurve _heightCurve = null, Gradient gradient = null, int LOD = 0)
    {
        // Evaluar una Curva tiene problemas con el paralelismo
        // asi que si creamos una curva por cada hilo en vez de reutilizarla
        // podemos evitar bloquear el hilo:
        var heightCurve = _heightCurve != null
            ? new AnimationCurve(_heightCurve.keys)
            : new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        // Lo mismo pasa con los Gradientes:
        Gradient gradCopy = null;
        if (gradient != null)
            gradCopy = GetGradientCopy(gradient);


        var width = heightMap.GetLength(0);
        var height = heightMap.GetLength(1);

        // La malla la creamos centrada en 0:
        var initX = (width - 1) / -2f;
        var initY = (height - 1) / -2f;

        // Si el LOD no es multiplo de la anchura lo incrementamos hasta que lo sea
        while (LOD != 0 && (width - 1) % LOD != 0)
            LOD += 1;

        // Incremento entre vertices para asegurar el LOD
        var simplificationIncrement = LOD == 0 ? 1 : LOD * 2;
        var verticesPerLine = (width - 1) / simplificationIncrement + 1;

        var data = new StaticMeshData(verticesPerLine, verticesPerLine);

        var vertIndex = 0;
        for (var y = 0; y < height; y += simplificationIncrement)
        for (var x = 0; x < width; x += simplificationIncrement)
        {
            data.AddVertex(new Vector3(initX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, initY + y));
            data.AddUV(new Vector2((float) x / width, (float) y / height));

            if (gradCopy != null)
                data.AddColor(gradCopy.Evaluate(heightMap[x, y]));

            // Ignorando la ultima fila y columna de vertices, añadimos los triangulos
            if (x < width - 1 && y < height - 1)
            {
                data.AddTriangle(vertIndex, vertIndex + verticesPerLine, vertIndex + verticesPerLine + 1);
                data.AddTriangle(vertIndex + verticesPerLine + 1, vertIndex + 1, vertIndex);
            }

            vertIndex++;
        }

        data.lod = LOD;

        return data;
    }

    public static DynamicMeshData GenerateTINMeshData(float[,] heightMap, float heightMultiplier,
        AnimationCurve _heightCurve = null, Gradient gradient = null, float errorTolerance = 1)
    {
        // Para poderlo paralelizar de nuevo hacemos copias:
        AnimationCurve heightCurve = _heightCurve != null
            ? new AnimationCurve(_heightCurve.keys)
            : new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        Gradient gradCopy = null;
        if (gradient != null)
            gradCopy = GetGradientCopy(gradient);

        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);

        DynamicMeshData data = new DynamicMeshData();

        // Al principio añadimos las 4 esquinas (centradas en 0,0):
        data.AddVertex(new Vector2(-mapWidth / 2, -mapHeight / 2), heightMap[0, 0]);
        data.AddVertex(new Vector2(mapWidth / 2, -mapHeight / 2), heightMap[mapWidth - 1, 0]);
        data.AddVertex(new Vector2(-mapWidth / 2, mapHeight / 2), heightMap[0, mapHeight - 1]);
        data.AddVertex(new Vector2(mapWidth / 2, mapHeight / 2), heightMap[mapWidth - 1, mapHeight - 1]);

        data.AddTriangle(0, 2, 1);
        data.AddTriangle(1, 2, 3);

        data.AddUV(0, 0);
        data.AddUV(1, 0);
        data.AddUV(0, 1);
        data.AddUV(1, 1);

        if (gradCopy != null)
        {
            data.AddColor(gradCopy.Evaluate(heightMap[0, 0]));
            data.AddColor(gradCopy.Evaluate(heightMap[mapWidth - 1, 0]));
            data.AddColor(gradCopy.Evaluate(heightMap[0, mapHeight - 1]));
            data.AddColor(gradCopy.Evaluate(heightMap[mapWidth - 1, mapHeight - 1]));
        }

        // Empieza el bucle
        // Condicion de parada: ningun punto del Mapa de Alturas tiene un error mayor al tolerado
        bool canAddPoints = false;
        while (canAddPoints)
        {
        }

        return data;
    }

    /// <summary>
    /// Gradiente por defecto 
    /// </summary>
    /// <returns>[negro -> blanco] [0,1]</returns>
    public static Gradient GetDefaultGradient()
    {
        var gradient = new Gradient();
        var colors = new GradientColorKey[2]
            {new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1)};
        var alphas = new GradientAlphaKey[2]
            {new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1)};
        gradient.SetKeys(colors, alphas);
        return gradient;
    }

    /// <summary>
    /// Copia de un Gradiente para paralelizar su uso
    /// </summary>
    /// <returns>Copia de un Gradiente</returns>
    private static Gradient GetGradientCopy(Gradient gradient)
    {
        Gradient gradCopy = new Gradient();
        gradCopy.SetKeys(gradient.colorKeys, gradient.alphaKeys);

        return gradCopy;
    }
}


public interface MeshData
{
    Mesh CreateMesh();
}

public class StaticMeshData : MeshData
{
    public int lod = 0;

    protected Vector3[] vertices;
    protected int[] triangles;
    protected Vector2[] uvs;

    // Colores por vertice en caso de no usar textura
    protected readonly Color[] colors;

    public StaticMeshData(int width, int height)
    {
        vertices = new Vector3[width * height];
        uvs = new Vector2[width * height];
        colors = new Color[width * height];
        triangles = new int[(width - 1) * (height - 1) * 6];
    }

    private int vertIndex;

    public void AddVertex(Vector3 vertex)
    {
        vertices[vertIndex++] = vertex;
    }

    private int uvIndex;

    public void AddUV(Vector2 uv)
    {
        uvs[uvIndex++] = uv;
    }

    private int colorIndex;

    public void AddColor(Color color)
    {
        colors[colorIndex++] = color;
    }

    private int triIndex;

    public void AddTriangle(int a, int b, int c)
    {
        if (a >= vertices.Length || b >= vertices.Length || c >= vertices.Length)
        {
            Debug.LogError("Triangle out of Bounds!!! " + vertices.Length + " Vertices. Triangle(" + a + ", " + b +
                           ", " + c + ")");
            return;
        }

        triangles[triIndex++] = a;
        triangles[triIndex++] = b;
        triangles[triIndex++] = c;
    }

    /// Creacion del Objeto Mesh que necesita Unity (no Paralelizable)
    public Mesh CreateMesh()
    {
        var mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
            uv = uvs,
            colors = colors,
        };

        mesh.RecalculateNormals();

        return mesh;
    }
}

public class DynamicMeshData : MeshData
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color> colors = new List<Color>();

    public void AddVertex(Vector3 vertex) => vertices.Add(vertex);
    public void AddVertex(Vector2 vertex, float height) => vertices.Add(new Vector3(vertex.x, height, vertex.y));
    public void AddVertex(float x, float y, float height) => vertices.Add(new Vector3(x, height, y));

    public void AddUV(Vector2 uv) => uvs.Add(uv);
    public void AddUV(float u, float v) => uvs.Add(new Vector2(u, v));

    public void AddColor(Color color) => colors.Add(color);
    public void AddColor(float r, float g, float b) => colors.Add(new Color(r, g, b));

    public void AddTriangle(int a, int b, int c)
    {
        if (a >= vertices.Count || b >= vertices.Count || c >= vertices.Count)
        {
            Debug.LogError("Triangle out of Bounds!!! " + vertices.Count + " Vertices. Triangle(" + a + ", " + b +
                           ", " +
                           c + ")");
            return;
        }

        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    // Creacion del Objeto Mesh que necesita Unity (no Paralelizable)
    public Mesh CreateMesh()
    {
        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            colors = colors.ToArray(),
        };

        mesh.RecalculateNormals();

        return mesh;
    }
}
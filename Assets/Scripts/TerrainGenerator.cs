using System;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GEOMETRY;
using JetBrains.Annotations;
using UnityEditor;

/**
 * <para>Generador Procedural de Terreno</para>
 *
 * <para>Genera el Terreno a partir de un Mapa de Ruido.
 * Con su malla, su textura y su malla de colisión</para>
 *
 * <para>Utiliza Threading y colas de peticiones
 * para la creación del mapa de ruido y la malla</para>
 */
[ExecuteAlways]
public class TerrainGenerator : MonoBehaviour
{
    // SINGLETON
    public static TerrainGenerator Instance { get; private set; }
    
    public TIN tin;
    
    private void Awake() 
    { 
        // Elimina la Instancia y la sobreescribe si se crea un objeto con TerrainGenerator nuevo
        if (Instance != null && Instance != this) 
            Destroy(this); 
        else
            Instance = this;
        
    }

    private void Update()
    {
        // Actualiza las peticiones de las Colas
        UpdateTerrainLoading();
    }

    public readonly struct MapData
    {
        public readonly float[,] noiseMap;
        public readonly NoiseMapGenerator.NoiseParams noiseParams;
        private readonly Color[] textureData;

        public MapData(float[,] noiseMap, NoiseMapGenerator.NoiseParams noiseParams, Color[] textureData)
        {
            this.noiseMap = noiseMap;
            this.noiseParams = noiseParams;
            this.textureData = textureData;
        }

        public Texture2D GetTexture2D()
        {
            Texture2D texture = new Texture2D(noiseMap.GetLength(0), noiseMap.GetLength(1));
            texture.SetPixels(textureData);
            texture.Apply();
            return texture;
        }
    }


    #region Generation

    /// <summary>
    /// Genera los datos del Mapa de Ruido/Altura a partir de los parametros que definen el Perlin Noise
    /// y un Gradiente para la Textura
    /// </summary>
    /// <returns>Los datos del Mapa de Ruido/Altura y de la Textura</returns>
    public MapData GenerateMapData(NoiseMapGenerator.NoiseParams noiseParams, Gradient gradient)
    {
        // MAPA DE RUIDO
        float[,] noiseMap = NoiseMapGenerator.GetNoiseMap(noiseParams);

        // TEXTURA (pixeles con colores sin procesar)
        // Se genera a partir de las alturas del mapa de ruido evaluadas en un Gradiente
        Color[] textureData = NoiseMapGenerator.GetTextureData(noiseMap, gradient);

        // Lo guardamos en el mapData
        return new MapData(noiseMap, noiseParams, textureData);
    }

    /// <summary>
    /// Genera los datos de la Malla
    /// a partir de un Mapa de Alturas, un multiplicador de altura para escalarla,
    /// una curva de altura para ajustarla y un Gradiente por si no se usa Textura (color en FragShader)
    /// </summary>
    /// <returns>Datos de la Malla</returns>
    public MeshData GenerateMeshData(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient)
    {
        return MeshGenerator.GetTerrainMeshData(
            heightMap, heightMultiplier, heightCurve, gradient
        );
    }

    public MeshData GenerateMeshDataLOD(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, int lod)
    {
        return MeshGenerator.GetTerrainMeshData(
            heightMap, heightMultiplier, heightCurve, gradient, lod
        );
    }

    public MeshData GenerateMeshDataTIN(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, float errorTolerance)
    {
        return MeshGenerator.GenerateTINMeshData(
            heightMap, out tin, heightMultiplier, heightCurve, gradient, errorTolerance
        );
    }
    #endregion

    // ===================================================================================== //

    #region THREADING

    /// <summary>
    ///  Información de un Hilo: callback que se tiene que ejecutar con el parametro que devuelve
    /// </summary>
    /// <typeparam name="T">Resultado que devuelve el Hilo</typeparam>
    public struct MapThreadInfo<T>
    {
        // Se ejecuta despues del hilo:
        public readonly Action<T> callback;

        // Parametro devuelto por el hilo que usa el callback como argumento:
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    // Colas en las que se guarda la info de los hilos para ejecutarlos conforme le vengan
    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    /// Ejecuta todos los callbacks guardados en la cola con los parametros que devolvieron los hilos
    public void UpdateTerrainLoading()
    {
        lock (mapDataThreadInfoQueue)
        {
            if (mapDataThreadInfoQueue.Count > 0)
            {
                for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
                {
                    MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }

        lock (meshDataThreadInfoQueue)
        {
            if (meshDataThreadInfoQueue.Count > 0)
            {
                for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
                {
                    MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }
    }

    // Paraleliza la Creacion del Mapa de Ruido
    #region MapDataThreading
    public void RequestMapData(NoiseMapGenerator.NoiseParams noiseParams, Gradient gradient,
        Action<MapData> callback)
    {
        void ThreadStart()
        {
            MapDataThread(noiseParams, gradient, callback);
        }

        new Thread(ThreadStart).Start();
    }

    // Proceso de creacion de MapData paralelizable
    private void MapDataThread(NoiseMapGenerator.NoiseParams noiseParams, Gradient gradient,
        Action<MapData> callback)
    {
        // Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(
                new MapThreadInfo<MapData>(callback, GenerateMapData(noiseParams, gradient))
            );
        }
    }
    #endregion

    #region MeshDataRequests

    // Paraleliza la Creacion de la Malla de Alturas
    public void RequestMeshData(MapData mapData, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, Action<MeshData> callback)
    {
        void ThreadStart() => MeshDataThread(mapData, heightMultiplier, heightCurve, gradient, callback);
        new Thread(ThreadStart).Start();
    }
    public void RequestMeshDataLOD(MapData mapData, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, int lod, Action<MeshData> callback)
    {
        void ThreadStart() => MeshDataThreadLOD(mapData, heightMultiplier, heightCurve, gradient, lod, callback);
        new Thread(ThreadStart).Start();
    }
    public void RequestMeshDataTIN(MapData mapData, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, float errorTolerance, Action<MeshData> callback)
    {
        void ThreadStart() => MeshDataThreadTIN(mapData, heightMultiplier, heightCurve, gradient, errorTolerance, callback);
        new Thread(ThreadStart).Start();
    }

    #endregion
    

    #region MeshDataThread
    // Proceso de creacion de la Malla en un hilo
    public void MeshDataThread(MapData mapData, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, Action<MeshData> callback)
    {
        // Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
        lock (meshDataThreadInfoQueue)
            meshDataThreadInfoQueue.Enqueue(
                new MapThreadInfo<MeshData>(callback,
                    GenerateMeshData(mapData.noiseMap, heightMultiplier, heightCurve, gradient))
            );
    }

    public void MeshDataThreadLOD(MapData mapData, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, int lod, Action<MeshData> callback)
    {
        // Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
        lock (meshDataThreadInfoQueue)
            meshDataThreadInfoQueue.Enqueue(
                new MapThreadInfo<MeshData>(callback,
                    GenerateMeshDataLOD(mapData.noiseMap, heightMultiplier, heightCurve, gradient, lod))
            );
    }

    public void MeshDataThreadTIN(MapData mapData, float heightMultiplier, AnimationCurve heightCurve,
        [CanBeNull] Gradient gradient, float errorTolerance, Action<MeshData> callback)
    {
        // Hace inaccesible la Cola cuando esta en esta linea de codigo para no pisarse entre hilos
        lock (meshDataThreadInfoQueue)
            meshDataThreadInfoQueue.Enqueue(
                new MapThreadInfo<MeshData>(callback,
                    GenerateMeshDataTIN(mapData.noiseMap, heightMultiplier, heightCurve, gradient, errorTolerance))
            );
    }
    #endregion
    
    #endregion


    private void OnDrawGizmos()
    {
        if (tin != null)
            tin.OnDrawGizmos();
    }
}
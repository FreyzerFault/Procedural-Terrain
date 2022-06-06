using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GEOMETRY;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TINManager
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    public class TINVisualizer : MonoBehaviour
    {
        public TIN tin;

        private TINControlUI uiController;

        public NoiseMapGenerator.NoiseParams noiseParams;

        public Gradient gradient;
        public AnimationCurve heightCurve;

        private float[,] heightMap;
        public int Width => heightMap.GetLength(0);
        public int Height => heightMap.GetLength(1);

        public Vector3[] pointCloud;


        public bool autoUpdate = true;
        public bool progressiveBuild = true;
        public bool animationRunning = false;
        public bool withTexture = true;
        public bool drawNormals = false;

        public float errorTolerance = 0.1f;
        [Range(1, 30)] public int maxPointsPerCycle = 15;
        [Range(0, 20)] public int minDistanceBetweenPointPerCycle = 5;

        public float heightMultiplier = 100;
        public int fase = 0;

        private float timeConsumed = 0;

        public GameObject arrowPrefab;

        public Vector3 startPoint = Vector3.zero;
        public Vector3 endPoint = Vector3.zero;
        private GameObject startPointSprite;
        private GameObject endPointSprite;
        public Vector3[] intersections;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private LineRenderer lineRenderer;
        private CameraManager cameraManager;

        public Minimap minimap;


        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            lineRenderer = GetComponent<LineRenderer>();
            cameraManager = GameObject.FindGameObjectWithTag("Camera Manager").GetComponent<CameraManager>();

            uiController = GetComponent<TINControlUI>();

            Time.timeScale = 1;
        }

        // Update is called once per frame
        private void Start()
        {
            UpdateMap();
            fase = 0;
        }

        private bool startPointPlaced = false;
        private bool endPointPlaced = false;

        private void Update()
        {
            if (minimap.MouseInMap())
                UpdateLineExtremes();

            // Actualizamos el perfil de la linea de la vuelta ciclista
            if (lineRenderer.positionCount > 0)
            {
                Vector3[] points = new Vector3[lineRenderer.positionCount];
                lineRenderer.GetPositions(points);
                uiController.UpdateLine(points);
            }
            else
            {
                uiController.UpdateLine(Array.Empty<Vector3>());
            }
        }


        private static Vector2? startPoint2D = null;
        private static Vector2? endPoint2D = null;


        // Actualiza el Mapa
        public void UpdateMap()
        {
            UpdateHeightMap();

            if (progressiveBuild)
                UpdateMeshProgressive();
            else
                UpdateMesh();
        }

        // Actualiza el Mapa de Ruido y la Textura asociada
        private void UpdateHeightMap()
        {
            heightMap = NoiseMapGenerator.GetNoiseMap(noiseParams);
            meshRenderer.sharedMaterial.mainTexture = NoiseMapGenerator.GetTexture(heightMap, gradient);
            meshRenderer.enabled = withTexture;
        }

        private Dictionary<int, int> distribucionPuntosConsecutivos = new Dictionary<int, int>();

        public bool UpdateMeshProgressive()
        {
            if (tin == null)
                fase = 0;
            if (heightMap == null || heightMap.Length == 0)
                UpdateHeightMap();

            bool finished = false;
            if (fase == 0)
            {
                timeConsumed = 0;
                tin = new TIN(heightMap, errorTolerance, heightMultiplier);
                tin.InitGeometry(heightMap);
            }
            else
            {
                if (tin == null)
                {
                    fase = 0;
                    UpdateMeshProgressive();
                }

                finished = !tin.AddPointLoopIteration(maxPointsPerCycle, minDistanceBetweenPointPerCycle);
                DrawAddedPointArrow();

                if (!finished)
                {
                    // Actualiza la distribucion de puntos consecutivos añadidos en una iteracion
                    int numPuntos = tin.lastVertexAdded.Count;
                    if (distribucionPuntosConsecutivos.ContainsKey(numPuntos))
                        distribucionPuntosConsecutivos[numPuntos]++;
                    else
                        distribucionPuntosConsecutivos.Add(numPuntos, 1);

                    // for (int i = 0; i < tin.lastVertexAdded.Count; i++)
                    //     Debug.Log("Añadido Vertice: " + tin.lastVertexAdded[i] + " (Error: " + tin.lastVertexError[i] +
                    //               ")");
                }
                else
                {
                    uiController.UpdateProgressBar(1);

                    // Muestra los Resultados
                    Debug.Log("TERMINADO");
                    StringBuilder sb = new StringBuilder();
                    foreach (KeyValuePair<int, int> entry in distribucionPuntosConsecutivos)
                    {
                        sb.AppendLine("Iteraciones con " + entry.Key + " puntos de golpe: " + entry.Value);
                    }

                    Debug.Log("Distribucion de puntos consecutivos: \n" + sb.ToString());

                    Debug.Log("Tiempo consumido: " + timeConsumed);

                    animationRunning = false;
                    StopAllCoroutines();
                }
            }

            MeshData meshData = MeshGenerator.GenerateTINMeshData(tin, heightCurve, gradient);
            meshFilter.mesh = meshData.CreateMesh();
            UpdateCollider();

            UpdateUIStats();

            timeConsumed += Time.deltaTime;

            UpdateProgressionBar();


            return finished;
        }

        // Actualiza la Malla de golpe (muy costoso)
        public void UpdateMesh()
        {
            MeshData meshData =
                MeshGenerator.GenerateTINMeshData(heightMap, out tin, 100, heightCurve, gradient, 0.1f, fase);
            meshFilter.sharedMesh = meshData.CreateMesh();
            UpdateCollider();
        }

        public void ResetRandomSeed() => noiseParams.seed = DateTime.Now.Millisecond;


        public bool AddNextPoint()
        {
            fase++;
            return UpdateMeshProgressive();
        }

        public void ResetTIN()
        {
            tin = null;
            fase = 0;

            StopAllCoroutines();
            animationRunning = false;

            lineRenderer.positionCount = 0;

            DrawAddedPointArrow();
            UpdateMap();
        }

        // Inicia o para la Animacion de Construccion del TIN
        public void BuildingAnimation()
        {
            if (!animationRunning)
            {
                StartCoroutine(AnimatedGeneration());
                animationRunning = true;
            }
            else
            {
                StopAllCoroutines();
                animationRunning = false;
            }
        }

        // Corutina que ejecuta una iteracion de la generacion de un TIN
        private IEnumerator AnimatedGeneration()
        {
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Space) || AddNextPoint())
                    break;

                //yield return new WaitForSeconds(.1f);
                yield return null;
            }
        }

        private void UpdateCollider()
        {
            MeshCollider mc = GetComponent<MeshCollider>();
            mc.sharedMesh = meshFilter.sharedMesh;
        }

        private float firstPointError = 100;
        private float maxPercentageReached = 0;

        private void UpdateProgressionBar()
        {
            if (fase == 0)
            {
                uiController.UpdateProgressBar(0);
                maxPercentageReached = 0;
                return;
            }

            // Guarda el primer punto de mayor error como el maximo error
            if (fase == 1 && tin.lastVertexAdded.Count > 0)
                firstPointError = tin.lastVertexError[tin.lastVertexError.Count - 1];

            // Actualiza la Barra de Progreso con un valor entre 0% y 100%
            // 0% -> Error del Primer punto añadido
            // 100% -> Error maximo Tolerado
            if (tin.lastVertexAdded.Count > 0)
            {
                float error = tin.lastVertexError[0];
                float percentage = (error - errorTolerance) / (firstPointError - errorTolerance);
                float inversePercentage = 1f - percentage;
                float progressValue =
                    Mathf.Clamp(Mathf.Max(Mathf.Pow(inversePercentage, 6), maxPercentageReached), 0, 1);
                maxPercentageReached = Mathf.Max(progressValue, maxPercentageReached);
                uiController.UpdateProgressBar(maxPercentageReached);
            }
        }

        private void UpdateUIStats()
        {
            uiController.time.text = Mathf.FloorToInt(timeConsumed / 60) + ":" + Mathf.FloorToInt(timeConsumed % 60);
            uiController.iterations.text = fase + " iterations";
            uiController.vertices.text = tin.vertices.Count + " vertices";
            uiController.triangles.text = tin.triangles.Count + " triangles";
        }


        private List<GameObject> lastArrows = new List<GameObject>();

        public void DrawAddedPointArrow()
        {
            // Eliminamos las anteriores
            foreach (GameObject lastArrow in lastArrows)
            {
                Destroy(lastArrow);
            }

            lastArrows.Clear();

            if (tin != null && tin.lastVertexAdded != null && tin.lastVertexAdded.Count != 0)
            {
                for (int i = 0; i < tin.lastVertexAdded.Count; i++)
                {
                    lastArrows.Add(Instantiate(arrowPrefab, tin.lastVertexAdded[i].v3D + Vector3.up * 10,
                        Quaternion.identity));
                }
            }
        }


        /// <summary>
        /// Actualiza los extremos de la Linea (startPoint y endPoint)
        /// </summary>
        private void UpdateLineExtremes()
        {
            // Se lo aplicamos al punto inicial o al final (Boton Izquierdo => Punto INICIAL, Derecho => Punto FINAL)
            if (Input.GetMouseButtonUp(0))
            {
                Destroy(startPointSprite);
                startPointSprite = minimap.DrawPointInMousePosition(Color.red);
                startPoint2D = GetMousePoint2D();
                startPoint = new Vector3(startPoint2D.Value.x, 0, startPoint2D.Value.y);
                startPointPlaced = true;

                // Calculamos el Triangulo o el Eje y la altura que le toca interpolando
                // Punto inicial
                if (tin.GetTriangle(new Vector2(startPoint.x, startPoint.z), out Triangle tri,
                        out Edge edge))
                {
                    // Segun si cae en una Eje o Triangulo interpola ahi
                    startPoint.y =
                        tri?.GetHeightInterpolation((Vector2) startPoint2D) ??
                        edge.GetHeightInterpolation((Vector2) startPoint2D);
                }

                // Actualizamos la linea de la vuelta ciclista
                if (startPoint2D != null && endPoint2D != null)
                    UpdateLine((Vector2) startPoint2D, (Vector2) endPoint2D);
            }

            if (Input.GetMouseButtonUp(1))
            {
                Destroy(endPointSprite);
                endPointSprite = minimap.DrawPointInMousePosition(Color.green);
                endPoint2D = GetMousePoint2D();
                endPoint = new Vector3(endPoint2D.Value.x, 0, endPoint2D.Value.y);
                endPointPlaced = true;

                // Punto final
                if (tin.GetTriangle((Vector2) endPoint2D, out Triangle tri,
                        out Edge edge))
                {
                    // Segun si cae en una Eje o Triangulo interpola ahi
                    endPoint.y =
                        tri != null
                            ? tri.GetHeightInterpolation((Vector2) endPoint2D)
                            : edge.GetHeightInterpolation((Vector2) endPoint2D);
                }

                if (startPoint2D != null && endPoint2D != null)
                    UpdateLine((Vector2) startPoint2D, (Vector2) endPoint2D);
            }
        }

        /// <summary>
        /// Punto del mundo en 2D (X,Z) al que apunta el mouse
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Necesita que haya un Camera Manager y una Camara con Indice 1 (Cenital)</exception>
        private Vector2? GetMousePoint2D()
        {
            if (minimap != null && minimap.MouseInMap())
            {
                // Coordenada del Raton relativa a la camara del minimapa
                Vector2 screenPoint = minimap.GetScreenSpaceMousePoint();

                // La Z sera la distancia desde la camara al terreno
                Vector3 screenPoint3D = new Vector3(screenPoint.x, screenPoint.y,
                    minimap.renderCamera.WorldToScreenPoint(transform.position).z);

                // Lo cambiamos a Coordenadas del mundo y en 2D
                Vector3 worldPoint = minimap.renderCamera.ScreenToWorldPoint(screenPoint3D);
                Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.z);
                return worldPoint2D;
            }

            return null;
        }

        /// <summary>
        /// Actualiza la Linea que representa el trazado de la VUELTA CICLISTA
        /// </summary>
        /// <param name="start">Punto Inicial 2D</param>
        /// <param name="end">Punto Final 2D</param>
        /// <exception cref="Exception"></exception>
        private void UpdateLine(Vector2 start, Vector2 end)
        {
            // Primero añadimos el inicio y el final
            lineRenderer.positionCount = 1;
            lineRenderer.SetPosition(0, startPoint);

            // Calculamos el trazado en 2D
            Vector2[] intersections2D = tin.GetIntersections(start, end);
            intersections = new Vector3[intersections2D.Length];

            // Para cada punto calculamos su altura
            for (int i = 0; i < intersections2D.Length; i++)
            {
                Vector2 intersection2D = intersections2D[i];
                if (tin.GetTriangle(intersection2D, out Triangle tri,
                        out Edge edge))
                {
                    // Segun si cae en una Eje o Triangulo interpola ahi
                    Vector3 intersection3D = new Vector3(intersection2D.x, 0, intersection2D.y);
                    intersection3D.y = (
                        tri != null
                            ? tri.GetHeightInterpolation(intersection2D)
                            : edge.GetHeightInterpolation(intersection2D)
                    ) + 1;

                    intersections[i] = intersection3D;

                    // Añadimos el punto
                    lineRenderer.SetPosition(lineRenderer.positionCount++, intersection3D);
                }
            }

            lineRenderer.SetPosition(lineRenderer.positionCount++, endPoint);
        }


        private void OnDrawGizmos()
        {
            if (tin != null)
            {
                tin.OnDrawGizmos();

                // Vertices
                //Gizmos.color = Color.yellow;
                // foreach (var v in tin.vertices)
                // {
                //     Gizmos.DrawSphere(v.v3D, .1f);
                // }

                // Vertices añadidos en esta iteracion
                if (tin.lastVertexAdded != null && tin.lastVertexAdded.Count != 0)
                {
                    Gizmos.color = Color.red;
                    for (int i = 0; i < tin.lastVertexAdded.Count; i++)
                    {
                        Gizmos.DrawSphere(tin.lastVertexAdded[i].v3D, 1);
                    }
                }
            }

            // Extremos de la linea
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(startPoint, 1);
            Gizmos.DrawSphere(endPoint, 1);

            // Punto de control de la linea
            foreach (Vector3 intersection in intersections)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(intersection, 1);
            }

            // Normales
            if (meshFilter && drawNormals)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    for (int i = 0; i < mesh.vertices.Length; i++)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] + mesh.normals[i] * 10);
                    }
                }
            }

            // if (minimap.MouseInMap())
            // {
            //     Vector2 mousePoint = minimap.GetLocalMousePosNormalized() * minimap.renderCamera.orthographicSize * 2;
            //     Vector3 mousePoint3D = new Vector3(mousePoint.x, 0, mousePoint.y);
            //     Vector3 camPos = minimap.renderCamera.transform.position;
            //     Vector3 originCamPos = camPos - new Vector3(minimap.renderCamera.orthographicSize, 0,
            //         minimap.renderCamera.orthographicSize);
            //     Gizmos.DrawSphere(originCamPos + mousePoint3D, 5);
            //     Gizmos.DrawLine(originCamPos, originCamPos + mousePoint3D);
            // }
        }

        // Esto es para cargar una Nube de puntos 2D a la que aplicar el Ruido de Perlin, asi los puntos no tienen una
        // distribucion uniforme
        public void LoadFromFile(String filePath)
        {
            if (filePath.Length != 0)
            {
                // Nube de Puntos con Alturas generadas proceduralmente
                pointCloud = NoiseMapGenerator.SampleNoiseInPointsFromFile(noiseParams, filePath, out AABB aabb);

                // Textura
                // meshRenderer.sharedMaterial.mainTexture =
                //     NoiseMapGenerator.GetTexture(pointCloud, aabb, gradient);
                // meshRenderer.enabled = withTexture;

                // TIN
                tin = new TIN(pointCloud, aabb, errorTolerance, heightMultiplier);
                tin.InitGeometry();

                // Mesh
                MeshData meshData = MeshGenerator.GenerateTINMeshData(tin, heightCurve, gradient);
                meshFilter.sharedMesh = meshData.CreateMesh();

                fase = 1;
            }
        }
    }
}
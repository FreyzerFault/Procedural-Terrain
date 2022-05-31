using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace GEOMETRY
{
    public class TIN
    {
        /// Mapa de Alturas. Clave => Punto 2D, Valor => Altura del Punto (2.5D)
        /// Como Ventaja al Array 2D usado antes, al utilizar la creacion de TIN Incremental,
        /// podemos borrar los puntos ya añadidos para mejorar la busqueda del Punto de Máximo Error
        /// Lista de puntos 
        private HashSet<Vertex> heightMap = new HashSet<Vertex>();

        public HashSet<Triangle> triangles = new HashSet<Triangle>();
        public HashSet<Edge> edges = new HashSet<Edge>();
        public HashSet<Vertex> vertices = new HashSet<Vertex>();

        private float errorTolerance = 0.1f;
        private float heightMultiplier = 100;

        public float width;
        public float height;

        public Vertex lastVertexAdded;

        public TIN()
        {
        }

        public TIN(HashSet<Triangle> triangles, HashSet<Edge> edges, HashSet<Vertex> vertices)
        {
            this.triangles = triangles;
            this.edges = edges;
            this.vertices = vertices;
        }

        /// <summary>
        /// Creacion del TIN a partir de un Mapa de Alturas como INPUT
        /// </summary>
        /// <param name="heightMap">Array 2D con los valores de la Altura</param>
        /// <param name="errorTolerance">Error Minimo tolerado => Condicion de Añadir un Punto</param>
        /// <param name="maxIterations">Iteraciones maximas permitidas (para una creacion progresiva y debugging)</param>
        public TIN(float[,] heightMap, float errorTolerance = 1, float heightMultiplier = 100, int maxIterations = -1)
        {
            this.errorTolerance = errorTolerance;
            this.heightMultiplier = heightMultiplier;

            width = heightMap.GetLength(0);
            height = heightMap.GetLength(1);

            // Guardamos el Mapa de Alturas como un conjunto de Vertices potenciales
            for (int x = 0; x < heightMap.GetLength(0); x++)
            for (int y = 0; y < heightMap.GetLength(1); y++)
                this.heightMap.Add(new Vertex(x, heightMap[x, y] * heightMultiplier, y));
        }

        /// <summary>
        /// Encapsula la adición inicial de Vértices, Aristas y Triángulos
        /// al principio del Algoritmo de Creacion
        /// </summary>
        public void InitGeometry(float[,] heightMap)
        {
            int width = heightMap.GetLength(0), height = heightMap.GetLength(1);

            // Extraemos las esquinas (0,0), (width-1,0), (0,height-1), (width-1, height-1)
            Vertex vBotLeft = new Vertex(0, heightMap[0, 0] * heightMultiplier, 0, 0);
            Vertex vBotRight = new Vertex(width - 1, heightMap[width - 1, 0] * heightMultiplier, 0, 1);
            Vertex vTopLeft = new Vertex(0, heightMap[0, height - 1] * heightMultiplier, height - 1, 2);
            Vertex vTopRight = new Vertex(width - 1, heightMap[width - 1, height - 1] * heightMultiplier, height - 1,
                3);

            // Al principio añadimos las 4 esquinas:
            vertices.Add(vBotLeft);
            vertices.Add(vBotRight);
            vertices.Add(vTopLeft);
            vertices.Add(vTopRight);

            // Las unimos con Aristas formando 2 Triangulos
            Edge e1 = AddEdge(vBotLeft, vBotRight);
            Edge e2 = AddEdge(vBotRight, vTopRight);
            Edge e3 = AddEdge(vTopRight, vBotLeft);
            Edge e4 = AddEdge(vTopRight, vTopLeft);
            Edge e5 = AddEdge(vTopLeft, vBotLeft);

            // Triangulos
            AddTri(e1, e2, e3);
            AddTri(e3, e4, e5);
        }

        /// <summary>
        /// Bucle Incremental de Adición de nuevos Vertices que cumplen con la condicion de ser añadidos:
        /// Mayor error del tolerado
        /// </summary>
        public void AddPointLoop(int maxIterations = -1)
        {
            int iterations = 0;

            // Condicion de parada: ningun punto del Mapa de Alturas tiene un error mayor al tolerado
            while (true)
            {
                if (!AddPointLoopIteration() && iterations >= maxIterations)
                    break;

                iterations++;
            }
        }

        /// <summary>
        /// Iteracion standalone del bucle principal para ejecutar progresivamente.
        /// </summary>
        /// <returns>Devuelve false en caso de haber acabado de añadir puntos por encima del error tolerado.
        /// O cuando ocurra algun error</returns>
        public bool AddPointLoopIteration()
        {
            Vertex point;
            Tuple<Triangle, Edge> pointTestData;
            // Busca el Punto de Maximo Error, y si ninguno supera la Tolerancia devuelve NULL

            try
            {
                point = FindMaxErrorPoint(out pointTestData);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
                return false;
            }

            // No encuentra un Punto => Se cumple la condicion de parada
            if (point == null)
                return false;

            // Lo añade a la Malla actualizando la Topologia
            // y se le pasa la Informacion sobre la posicion del punto calculada en el Calculo del Error (Triangulo o Eje)
            AddPoint(point, pointTestData);

            // Y lo elimina del Mapa de Alturas
            heightMap.Remove(point);

            return true;
        }

        /// <summary>
        /// Añade un Punto como Vertice del TIN y actualiza la Topologia.
        /// <p>
        /// Elimina el Triangulo en el que cae (o no si pertenece a una Arista).
        /// Y Crea 3 Triangulos (o 4 si pertenece a una Arista) añadiendo las aristas necesarias
        /// </p>
        /// </summary>
        /// <param name="tri">Triangulo al que pertenece el Punto. Se pasa como argumento para ahorrar recalcularlo
        /// ya que el calculo se realiza antes al buscar el punto de mayor error</param>
        /// <returns>Devuelve this para poder llamar otros metodos en cadena</returns>
        public TIN AddPoint(Vertex point, Tuple<Triangle, Edge> tuple = null)
        {
            // Si no se ha precalculamos lo calculamos
            if (tuple == null)
                tuple = GetTriangle(point);

            // Si aun no se consigue nada es que o esta fuera o ya se añadio
            if (tuple == null)
            {
                Debug.LogError("Uno de los Puntos del Mapa de Alturas no aporta nada" +
                               " (Esta fuera o ya estaba en los vertices del TIN");
                return this;
            }

            // Comprobamos si esta en el Triangulo o en una de sus Aristas
            Triangle tri = tuple.Item1;
            Edge edge = tuple.Item2;

            if (tri != null)
                AddPointInTri(point, tri);
            else if (edge != null)
                AddPointInEdge(point, edge);

            return this;
        }

        /// <summary>
        /// Añade un Punto dentro de un Triangulo (caso normal).
        /// Crea 3 nuevas Aristas, 3 nuevos Triangulos y elimina el Triangulo antiguo
        /// </summary>
        /// <param name="point"></param>
        /// <param name="tri"></param>
        private void AddPointInTri(Vertex point, Triangle tri)
        {
            // Añade el nuevo Vertice
            point.index = vertices.Count;
            vertices.Add(point);
            lastVertexAdded = point;

            // Creamos las nuevas Aristas uniendo el Punto nuevo con los Vertices del Triangulo
            Edge e1 = AddEdge(point, tri.v1);
            Edge e2 = AddEdge(point, tri.v2);
            Edge e3 = AddEdge(point, tri.v3);

            // Añadimos los triangulos con los ejes nuevos + la arista antigua
            // (aquella cuyo begin y end sean el end de la nueva arista)
            // Y ademas asigna a esos ejes el propio triangulo nuevo, ya sea a la Izquierda o a la Derecha
            Triangle tri1 = AddTri(e1, e2, tri);
            Triangle tri2 = AddTri(e2, e3, tri);
            Triangle tri3 = AddTri(e3, e1, tri);

            // Elimina el Triangulo Viejo
            triangles.Remove(tri);
            
            LegalizeEdge(tri1.GetEdge(e1.end, e2.end), tri1, point);
            LegalizeEdge(tri2.GetEdge(e2.end, e3.end), tri2, point);
            LegalizeEdge(tri3.GetEdge(e3.end, e1.end), tri3, point);
        }

        /// <summary>
        /// Añade un Punto dentro de un Eje (caso excepcional).
        /// Crea 4 nuevas Aristas, 4 nuevos Triangulos y elimina el Eje antiguo y los 2 Triangulos vecinos
        /// </summary>
        /// <param name="point"></param>
        /// <param name="edge"></param>
        private void AddPointInEdge(Vertex point, Edge edge)
        {
            // Añade el nuevo Vertice
            point.index = vertices.Count;
            vertices.Add(point);
            lastVertexAdded = point;

            // Creamos 2 nuevas Aristas uniendo el Punto nuevo con los Vertices opuestos de los Triangulos Vecinos

            // Hay que tener en cuenta que podria ser frontera el eje:
            Edge e1 = null;
            Edge e2 = null;

            if (edge.tIzq != null)
                e1 = AddEdge(point, edge.tIzq.OppositeVertex(edge));
            if (edge.tDer != null)
                e2 = AddEdge(point, edge.tDer.OppositeVertex(edge));

            if (e1 == null && e2 == null)
                throw new Exception("Al añadir un Punto en una Arista " +
                                    "no se han encontrado ningun triangulo vecino");

            // Y 2 mas subdividiendo la arista del punto en 2 segmentos
            Edge e3 = AddEdge(point, edge.begin);
            Edge e4 = AddEdge(point, edge.end);

            // Añadimos los triangulos, los 2 ejes nuevos y el eje antiguo
            // (aquella cuyo begin y end sean el end de la nueva arista)
            // Y ademas asigna a esos ejes el propio triangulo nuevo, ya sea a la Izquierda o a la Derecha
            // El e1 esta en el tIzq y el e2 en el tDer, e3 y e4 forman el eje compartido por ambos
            // Hay que tener en cuenta que puede ser Eje Frontera
            Triangle tri1 = null;
            Triangle tri2 = null;
            Triangle tri3 = null;
            Triangle tri4 = null;
            if (e1 != null)
            {
                tri1 = AddTri(e1, e3, edge.tIzq);
                tri2 = AddTri(e1, e4, edge.tIzq);
            }
            if (e2 != null)
            {
                tri3 = AddTri(e2, e3, edge.tDer);
                tri4 = AddTri(e2, e4, edge.tDer);
            }

            // Elimina los Triangulos Antiguos y el Eje
            if (edge.tIzq != null)
                triangles.Remove(edge.tIzq);
            if (edge.tDer != null)
                triangles.Remove(edge.tDer);
            
            edges.Remove(edge);
            
            // Legalizamos los ejes opuestos de cada tri al vertice nuevo
            if (tri1 != null && tri2 != null)
            {
                LegalizeEdge(tri1.GetEdge(e1.end, e3.end), tri1, point);
                LegalizeEdge(tri2.GetEdge(e1.end, e4.end), tri2, point);
            }
            if (tri3 != null && tri4 != null)
            {
                LegalizeEdge(tri3.GetEdge(e2.end, e3.end), tri3, point);
                LegalizeEdge(tri4.GetEdge(e2.end, e4.end), tri4, point);
            }
            
        }

        /// <summary>
        /// Añade una Arista
        /// </summary>
        /// <param name="v1">Begin</param>
        /// <param name="v2">End</param>
        /// <returns>Devuelve el eje</returns>
        private Edge AddEdge(Vertex v1, Vertex v2)
        {
            Edge edge = new Edge(v1, v2, null, null, edges.Count);
            edges.Add(edge);

            return edge;
        }

        /// <summary>
        /// Añade un Triangulo a partir de 3 Aristas.
        /// Y además asigna a estas aristas el propio triángulo que se va a añadir.
        /// </summary>
        /// <param name="e1">Arista 1</param>
        /// <param name="e2">Arista 2</param>
        /// <param name="e3">Arista 3</param>
        /// <returns>Triangulo creado</returns>
        private Triangle AddTri(Edge e1, Edge e2, Edge e3)
        {
            // Se crea el Triangulo a base de las Aristas,
            // los Vertices se añaden de forma que siempre estan ordenados de forma Antihoraria
            Triangle tri = new Triangle(e1, e2, e3, triangles.Count);
            triangles.Add(tri);

            // Asignamos a cada Arista el nuevo Triangulo,
            // que implicitamente ya se encarga de ponerlo como Izq o Der segun la posicion del Vertice opuesto
            e1.AssignTriangle(tri);
            e2.AssignTriangle(tri);
            e3.AssignTriangle(tri);

            return tri;
        }

        /// <summary>
        /// Añade un Triangulo dentro de otro.
        /// <para>Comprueba cual de las aristas del Triangulo contenedor es la que permite crear el triangulo con e1 y e2</para>
        /// <para>Para ello suponemos que e1 y e2 acaban en la Arista del Tri antiguo y
        /// con GetEdge() busca la que coincida con e1.end -> e2.end o al contrario e2.end -> e3.end</para>
        /// <para>Ademas tambien legalizamos la Arista antigua conforme al nuevo Triangulo</para>
        /// </summary>
        /// <param name="e1">Nuevo Eje 1</param>
        /// <param name="e2">Nuevo Eje 2</param>
        /// <param name="oldTri">Triangulo contenedor del nuevo</param>
        /// <returns></returns>
        private Triangle AddTri(Edge e1, Edge e2, Triangle oldTri)
        {
            Edge oldEdge = oldTri.GetEdge(e1.end, e2.end);

            if (oldEdge != null)
            {
                // Creamos el Nuevo Triangulo
                Triangle newTri = AddTri(e1, e2, oldEdge);
                
                return newTri;
            }

            return null;
        }

        /// <summary>
        /// Legaliza una Arista con el metodo de Delaunay (si esta dentro del circulo el vertice opuesto => FLIP)
        /// </summary>
        /// <param name="edge">Arista a Legalizar</param>
        /// <param name="tri">Triangulo que contiene la Arista y el Vertice nuevo</param>
        /// <param name="newVertex">Vertice Nuevo (opuesto a la arista)</param>
        /// <returns>True si se ha tenido que Legalizar</returns>
        private bool LegalizeEdge(Edge edge, Triangle tri, Vertex newVertex)
        {
            // Si es frontera no hay que legalizarlo
            if (edge.IsFrontier)
                return false;
            
            // Buscamos el vecino del eje contrario a Tri
            Triangle neighbour = edge.tIzq == tri ? edge.tDer : edge.tIzq;

            // Si no tiene es que el Eje es FRONTERA, no hace falta hacer FLIP
            if (neighbour == null)
                return false;

            Vertex oppositeVertex = neighbour.OppositeVertex(edge);

            // Comprobamos si vertice de el vertice del Vecino opuesto al Eje
            // esta dentro del Circulo formado por el vertice de Tri opuesto al Eje (el nuevo) y los demas vertices del Eje
            if (Utils.IsInsideCircle(
                    oppositeVertex.v2D,
                    newVertex.v2D,
                    edge.begin.v2D, edge.end.v2D
                ))
            {
                // FLIP:
                
                // Creamos el nuevo Eje
                Edge newEdge = AddEdge(newVertex, oppositeVertex);
                
                // Y cogemos los ejes externos de cada triangulo
                Edge triE1 = tri.GetEdge(newVertex, edge.begin);
                Edge triE2 = tri.GetEdge(newVertex, edge.end);
                Edge neighE1 = neighbour.GetEdge(oppositeVertex, edge.begin);
                Edge neighE2 = neighbour.GetEdge(oppositeVertex, edge.end);
                
                // Creo los Triangulos nuevos a partir de los ejes antiguos y el nuevo
                // Los Ejes antiguos seran (nuevo -> oldEdge.begin) y (opposite -> oldEdge.begin)
                // Y lo mismo para el oldEdge.end
                Triangle tri1 = AddTri(
                    newEdge, 
                    triE1, 
                    neighE1
                    );
                Triangle tri2 = AddTri(
                    newEdge,
                    triE2, 
                    neighE2
                    );

                
                // Eliminamos los Triangulos y la Arista antiguos
                edges.Remove(edge);
                triangles.Remove(tri);
                triangles.Remove(neighbour);
                
                // Como cambia la topologia, tenemos que volverlo a comprobar para los ejes nuevos, de forma recursiva
                // Estos vertices son los del triangulo vecino que aun se mantienen, con cada triangulo nuevo 
                LegalizeEdge(neighE1, tri1, newVertex);
                LegalizeEdge(neighE2, tri2, newVertex);

                return true;
            }

            return false;
        }


        /// <summary>
        /// Busca el Punto de mayor Error
        /// </summary>
        /// <param name="pointTestData">Datos de la posicion del Punto de Maximo error (si caen en un Triangulo o un Eje)</param>
        /// <returns>Devuelve el Punto de mayor Error, o null si ninguno supera el error minimo tolerado</returns>
        [CanBeNull]
        private Vertex FindMaxErrorPoint(out Tuple<Triangle, Edge> pointTestData)
        {
            // Ordenamos los Puntos por el Error
            float maxError = 0;
            Vertex maxErrorPoint = null;
            pointTestData = null;

            // Hacemos una copia del Mapa de Alturas por si tengo que descartar algun punto
            // por estar fuera de rango o que ya estuviera añadido como Vertice
            // Como voy a recorrerlo con foreach daria error si elimino el propio punto por el que itero
            Vertex[] heightMapCopy = new Vertex[heightMap.Count];
            heightMap.CopyTo(heightMapCopy);

            foreach (Vertex point in heightMapCopy)
            {
                float error = GetError(point, out pointTestData);

                if (error > maxError && error > errorTolerance)
                {
                    maxError = error;
                    maxErrorPoint = point;
                }
            }
            
            Debug.Log("Added Point with ERROR = " + maxError);

            // Lo devolvemos siempre que supere el error tolerado, sino devuelve null
            return maxErrorPoint;
        }

        private float GetError(Vertex point, out Tuple<Triangle, Edge> pointTestData)
        {
            // Buscamos el Triangulo al que pertenece (si no lo encuentra devuelve -1)
            pointTestData = GetTriangle(point);

            if (pointTestData == null)
                return 0;

            Triangle tri = pointTestData.Item1;
            Edge edge = pointTestData.Item2;

            // La heuristica del Error es la diferencia de altura entre el punto del triangulo con el que coincide en 2D
            // y el mismo punto 2D de la muestra
            // Para ello podemos interpolar las alturas de cada vertice
            // Una interpolacion lineal es lo ideal para los triangulos ya que son superficies planas
            float error = 0;
            if (tri != null)
            {
                float distV1 = (point.v2D - tri.v1.v2D).magnitude;
                float distV2 = (point.v2D - tri.v2.v2D).magnitude;
                float distV3 = (point.v2D - tri.v3.v2D).magnitude;
             
                float distanceInterpolation = 0;   
                distanceInterpolation += tri.v1.y / distV1;
                distanceInterpolation += tri.v2.y / distV2;
                distanceInterpolation += tri.v3.y / distV3;
                distanceInterpolation /= 1 / distV1 + 1 / distV2 + 1 / distV3;

                error = Mathf.Abs(distanceInterpolation - point.y);

                // error += Mathf.Abs(point.y - tri.v1.y) / (point.v2D - tri.v1.v2D).magnitude;
                // error += Mathf.Abs(point.y - tri.v2.y) / (point.v2D - tri.v2.v2D).magnitude;
                // error += Mathf.Abs(point.y - tri.v3.y) / (point.v2D - tri.v3.v2D).magnitude;
            }
            else if (edge != null)
            {
                // En una arista interpolamos la altura entre begin y end
                float distBegin = (point.v2D - edge.begin.v2D).magnitude;
                float distEnd = (point.v2D - edge.end.v2D).magnitude;
             
                float distanceInterpolation = 0;
                distanceInterpolation += edge.begin.y / distBegin;
                distanceInterpolation += edge.end.y / distEnd;
                distanceInterpolation /= 1 / distBegin + 1 / distEnd;

                error = Mathf.Abs(distanceInterpolation - point.y);
                
                // error += Mathf.Abs(point.y - edge.begin.y) / (point.v2D - edge.begin.v2D).magnitude;
                // error += Mathf.Abs(point.y - edge.end.y) / (point.v2D - edge.end.v2D).magnitude;
                //
                // // Hay que tener en cuenta que puede ser frontera y solo tener un triangulo vecino
                // if (edge.tIzq != null)
                // {
                //     Vertex opposite = edge.tIzq.OppositeVertex(edge);
                //     error += Mathf.Abs(point.y - opposite.y) / (point.v2D - opposite.v2D).magnitude;
                // }
                // if (edge.tDer != null)
                // {
                //     Vertex opposite = edge.tDer.OppositeVertex(edge);
                //     error += Mathf.Abs(point.y - opposite.y) / (point.v2D - opposite.v2D).magnitude;
                // }
                
                // Como son 4 vertices, si lo reducimos a 3/4 de su valor se equiparara al de 3 vertices
                //error *= 3f / 4f;
            }

            return error;
        }

        /// <summary>
        /// Busca el Triangulo al que pertenece un punto usando el Test Point-Triangle
        /// </summary>
        /// <param name="point">Punto 2D (la altura no es necesaria)</param>
        /// <returns>Devuelve el Triangulo, o null si no lo encuentra</returns>
        [CanBeNull]
        private Tuple<Triangle, Edge> GetTriangle(Vertex point)
        {
            // Buscamos en todos los Triangulos
            foreach (Triangle triangle in triangles)
            {
                // Test Punto-Triangulo
                var test = triangle.PointInTriangle(point.v2D);
                switch (test.Key)
                {
                    // Si esta fuera descarta el Triangulo
                    case Triangle.PointTriPosition.OUT:
                        continue;

                    // Si esta DENTRO devuelve el Triangulo
                    case Triangle.PointTriPosition.IN:
                        return new Tuple<Triangle, Edge>(triangle, null);

                    // Si esta en una Arista devuelve la Arista
                    case Triangle.PointTriPosition.COLINEAR:
                        return new Tuple<Triangle, Edge>(null, test.Value);

                    // Si es su vertice, descartamos el punto por completo y no devolvemos NADA
                    case Triangle.PointTriPosition.VERTEX:
                        vertices.Remove(point);
                        return null;
                }
            }

            return null;
        }


        public void OnDrawGizmos()
        {
            // Dibuja las aristas
            Gizmos.color = Color.magenta;
            foreach (Edge e in edges)
            {
                Gizmos.DrawLine(e.begin.v3D, e.end.v3D);
            }
        }
    }
}
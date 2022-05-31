using System;
using System.Collections.Generic;
using UnityEngine;

namespace GEOMETRY
{
    [Serializable]
    public class Triangle
    {
        public int index;

        public readonly Vertex v1;
        public readonly Vertex v2;
        public readonly Vertex v3;

        public Edge e1;
        public Edge e2;
        public Edge e3;

        public Triangle(Edge e1, Edge e2, Edge e3, int index = -1)
        {
            this.index = index;

            this.e1 = e1;
            this.e2 = e2;
            this.e3 = e3;

            // Los vertices los extraemos de las aristas
            v1 = e1.begin;

            // No tienen por que ser todos el begin de las aristas
            // Si el begin de la 2 coincide con el v1, se elige el end
            v2 = e2.begin;
            if (v2 == v1) v2 = e2.end;

            // Lo mismo para v3, si se repite, elegir el otro vertice
            v3 = e3.begin;
            if (v3 == v2 || v3 == v1) v3 = e3.end;

            if (v1 == v2 || v2 == v3 || v3 == v1)
                throw new Exception("Alguno de los vertices de el Triangulo esta mal: " +
                                    "{" + v1 + ", " + v2 + ", " + v3 + "}");

            // Y hay que ordenarlos en orden ANTIHORARIO
            // (si alguno esta a la Derecha de la arista opuesta se hace un Swap de la opuesta):
            if (Utils.isRight(v1.v2D, v2.v2D, v3.v2D))
                (v3, v2) = (v2, v3); // SWAP v2 <-> v3
        }

        public Triangle(Vertex v1, Vertex v2, Vertex v3, Edge e1, Edge e2, Edge e3, int index = -1)
        {
            this.index = index;

            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
            this.e1 = e1;
            this.e2 = e2;
            this.e3 = e3;
        }

        /// <summary>
        /// Busca el Eje que concuerda con los Vertices pasados como argumentos.
        /// No tiene por que tener la misma orientacion
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Edge GetEdge(Vertex begin, Vertex end)
        {
            Edge[] edges = {e1, e2, e3};

            foreach (Edge edge in edges)
            {
                if ((edge.begin == begin || edge.begin == end) &&
                    (edge.end == begin || edge.end == end))
                    return edge;
            }
            
            return null;
        }

        /// <summary>
        /// Busca el Vertice que no pertenece a la arista que se pasa
        /// </summary>
        /// <param name="edge">Arista opuesta</param>
        /// <returns>Vertice Opuesto de la Arista</returns>
        /// <exception cref="Exception">No encuentra el opuesto</exception>
        public Vertex OppositeVertex(Edge edge)
        {
            // Buscamos el vertice que no pertenece a la arista (no es ni Begin ni End)
            Vertex oppositeVertex = null;

            if (v1 != edge.begin && v1 != edge.end)
                oppositeVertex = v1;
            else if (v2 != edge.begin && v2 != edge.end)
                oppositeVertex = v2;
            else if (v3 != edge.begin && v3 != edge.end)
                oppositeVertex = v3;

            if (oppositeVertex == null)
                throw new Exception("No se encuentra el vertice opuesto de " + edge + " en el Triangulo " + this);

            return oppositeVertex;
        }

        public enum PointTriPosition
        {
            IN,
            OUT,
            COLINEAR,
            VERTEX
        }

        /// <summary>
        /// Esta a la DERECHA de cualquier Eje => OUT;
        /// Es COLINEAR de algun Eje => EdgeX;
        /// Esta a la Izquierda de TODOS los Ejes => IN;
        /// </summary>
        /// <param name="p"></param>
        /// <returns>RIGHT / LEFT / COLINEAR + El Eje si es COLINEAR</returns>
        public KeyValuePair<PointTriPosition, Edge> PointInTriangle(Vector2 p)
        {
            // Posicion Relativa del Punto a cada Arista (alineada en orden Antihorario)
            Edge.PointEdgePosition pos1 = new Edge(v1, v2).GetPointEdgePosition(p);
            Edge.PointEdgePosition pos2 = new Edge(v2, v3).GetPointEdgePosition(p);
            Edge.PointEdgePosition pos3 = new Edge(v3, v1).GetPointEdgePosition(p);
            
            // En cuanto este a la derecha de cualquiera de las Aristas, esta FUERA
            if (pos1 == Edge.PointEdgePosition.RIGHT ||
                pos2 == Edge.PointEdgePosition.RIGHT ||
                pos3 == Edge.PointEdgePosition.RIGHT)
                return new KeyValuePair<PointTriPosition, Edge>(PointTriPosition.OUT, null);

            // Si esta a la IZQUIERDA de TODOS => esta DENTRO
            if (pos1 == Edge.PointEdgePosition.LEFT &&
                pos2 == Edge.PointEdgePosition.LEFT &&
                pos3 == Edge.PointEdgePosition.LEFT)
                return new KeyValuePair<PointTriPosition, Edge>(PointTriPosition.IN, null);
            
            // Si no, puede ser colinear con un eje, o estar en el vertice
            // Por si acaso comprobamos primero que no sea un vertice
            if (Utils.Equals(p, v1.v2D) || Utils.Equals(p, v2.v2D) || Utils.Equals(p, v3.v2D))
                return new KeyValuePair<PointTriPosition, Edge>(PointTriPosition.VERTEX, null);
            
            // En este punto si o si esta en un eje
            // Pero debemos saber que eje de los 3
            bool colinear1 = pos1 == Edge.PointEdgePosition.COLINEAR;
            bool colinear2 = pos2 == Edge.PointEdgePosition.COLINEAR;
            
            // Buscamos la Arista que concuerda con los vertices del Eje en el que esta
            return new KeyValuePair<PointTriPosition, Edge>(PointTriPosition.COLINEAR,
                colinear1 ? GetEdge(v1, v2) : colinear2 ? GetEdge(v2, v3) : GetEdge(v3, v1));
        }
        
        /// <summary>
        /// Se basa en la Tecnica del Baricentro, que calcula P como la suma de vectores en la direccion
        /// de las Aristas del Triangulo (A->B y A->C) con una magnitud u y v.
        /// <para>Se usa la función paramétrica del Plano: P = A + u(C-A) + v(B-A)</para>
        /// <para>Calculamos u y v, y para que P esté dentro del Triángulo deben ser positivos y su suma menor a 1.
        /// u >= 0, v >= 0, u + v &lt; 1</para>
        /// <see cref="blackpawn.com/texts/pointinpoly/"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public PointTriPosition PointInTriangleBarycentricTechnique(Vector2 p)
        {
            Vector2 a = this.v1.v2D, b = this.v2.v2D, c = this.v3.v2D;
            
            // Vectores
            Vector2 V0 = c - a;
            Vector2 V1 = b - a;
            Vector2 V2 = p - a;
            
            // Dot Products
            float dot00 = Vector2.Dot(V0, V0);
            float dot01 = Vector2.Dot(V0, V1);
            float dot02 = Vector2.Dot(V0, V2);
            float dot11 = Vector2.Dot(V1, V1);
            float dot12 = Vector2.Dot(V1, V2);
            
            // Coordenadas Barycentricas
            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // u Negativa o v Negativa o u+v > 1 => OUT
            if (u < -Utils.EPSILON || v < -Utils.EPSILON || u + v > 1 + Utils.EPSILON)
                return PointTriPosition.OUT;
                
            // u Positiva, v Positiva, u+v < 1 => IN
            if (u > Utils.EPSILON && v > Utils.EPSILON && u + v < 1 - Utils.EPSILON)
                return PointTriPosition.IN;
            
            // Comprobamos que no este en la misma posicion que los vertices
            if (Utils.Equals(p, v1.v2D) || Utils.Equals(p, v2.v2D) || Utils.Equals(p, v3.v2D))
                return PointTriPosition.VERTEX;
            
            // Al final si o si es Colinear en alguna arista
            return PointTriPosition.COLINEAR;
        }

        public override String ToString() =>
            "t" + index + " {" + v1 + " -> " + v2 + " -> " + v3 + "} (" + e1 + ", " + e2 + ", " + e3 + ")";

        public override int GetHashCode() => v1.GetHashCode() + v2.GetHashCode() + v3.GetHashCode();
    }
}
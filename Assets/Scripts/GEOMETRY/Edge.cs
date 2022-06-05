
using System;
using System.Drawing;
using UnityEngine;

namespace GEOMETRY
{
    public class Edge
    {
        public int index;
        
        // Begin -> End
        public readonly Vertex begin;
        public readonly Vertex end;

        // Izquierda => Antihorario; Derecha => Horario
        public Triangle tIzq;
        public Triangle tDer;

        public Edge(Vertex begin, Vertex end, Triangle tIzq = null, Triangle tDer = null, int index = -1)
        {
            this.index = index;
            
            this.begin = begin;
            this.end = end;
            this.tIzq = tIzq;
            this.tDer = tDer;
        }

        /// <summary> 
        /// Asigna un Triangulo segun su posicion como Izquierdo o Derecho
        /// </summary>
        public void AssignTriangle(Triangle tri)
        {
            if (Utils.isRight(
                    tri.OppositeVertex(this).v2D,
                    begin.v2D,
                    end.v2D
                ))
                tDer = tri;
            else
                tIzq = tri;
        } 
        
        /// <summary>
        /// El Eje es Frontera siempre que le falte asignarle un Triangulo a la Izquierda o Derecha
        /// </summary>
        public bool IsFrontier => tDer == null || tIzq == null;


        public enum PointEdgePosition {RIGHT, LEFT, COLINEAR}

        /// <summary>
        /// NEGATIVA => DERECHA; POSITIVA => IZQUIERDA; ~0 => COLINEAR
        /// (tiene un margen grande para no crear triangulos sin apenas grosor)
        /// </summary>
        /// <param name="p"></param>
        /// <returns>RIGHT / LEFT / COLINEAR</returns>
        public static PointEdgePosition GetPointEdgePosition(Vector2 p, Vector2 begin, Vector2 end)
        {
            float area = Utils.TriArea2(begin, end, p);
            
            // EPSILON Grande en este caso, porque las veces que cae un punto en un triangulo
            // puede estar muy cerca de una arista y el resultado puede ser un Triangulo muy estirado

            return area > 0.1f 
                ? PointEdgePosition.LEFT 
                : area < -0.1f 
                    ? PointEdgePosition.RIGHT 
                    : PointEdgePosition.COLINEAR;
        } 
        
        /// <summary>
        /// Interpolacion de la altura en un punto 2D en la Arista.
        /// Inversamente proporcional a la distancia de cada vertice al punto 2D
        /// </summary>
        /// <param name="point">Punto 2D</param>
        /// <returns></returns>
        public float GetHeightInterpolation(Vector2 point)
        {
            // Interpolamos la altura entre begin y end
            float distBegin = (point - begin.v2D).magnitude;
            float distEnd = (point - end.v2D).magnitude;

            float distanceInterpolation = 0;
            distanceInterpolation += begin.y / distBegin;
            distanceInterpolation += end.y / distEnd;
            return distanceInterpolation / (1 / distBegin + 1 / distEnd);
        }
        
        
        /// <summary>
        /// Calcula el Punto de interseccion de un Segmento A -> B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="intersectionPoint"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool GetIntersectionPoint(Vector2 a, Vector2 b, out Vector2? intersectionPoint)
        {
            intersectionPoint = null;
            PointEdgePosition posA = GetPointEdgePosition(a, begin.v2D, end.v2D);
            PointEdgePosition posB = GetPointEdgePosition(b, begin.v2D, end.v2D);
            
            // Solo hay interseccion si los dos puntos estan en lados opuestos de la arista
            if ((posA == PointEdgePosition.RIGHT && posB == PointEdgePosition.LEFT) ||
                (posA == PointEdgePosition.LEFT && posB == PointEdgePosition.RIGHT))
            {
                Vector2 c = begin.v2D;
                Vector2 d = end.v2D;
                
                Vector2 ab = b - a;
                Vector2 cd = d - c;
                Vector2 ac = c - a;

                float denominador = cd.x * ab.y - ab.x * cd.y;

                if (denominador == 0)
                    throw new Exception("La interseccion es paralela");

                float s = (cd.x * ac.y - ac.x * cd.y) / denominador;
                float t = (ab.x * ac.y - ac.x * ab.y) / denominador;

                // Si s o t estan fuera de [0,1] => la interseccion esta fuera de los segmentos
                if (s < 0 || s > 1 || t < 0 || t > 1)
                    return false;

                intersectionPoint = a + (b - a) * s;

                return true;
            }

            intersectionPoint = null;
            return false;
        }
        
        public override String ToString() => "e" + index + " {" + begin + " -> " + end + "}";

        /// <summary>
        /// No puede haber mas de un Eje con los mismos vertices
        /// </summary>
        public override int GetHashCode() => begin.GetHashCode() + end.GetHashCode();
    }
}
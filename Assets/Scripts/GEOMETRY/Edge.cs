
using System;
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
        /// (tiene un margen de EPSILON para no crear triangulos sin apenas grosor)
        /// </summary>
        /// <param name="p"></param>
        /// <returns>RIGHT / LEFT / COLINEAR</returns>
        public PointEdgePosition GetPointEdgePosition(Vector2 p)
        {
            float area = Utils.TriArea2(begin.v2D, end.v2D, p);
            
            // EPSILON Grande en este caso, porque las veces que cae un punto en un triangulo
            // puede estar muy cerca de una arista y el resultado puede ser un Triangulo muy estirado
            
            if (area < -0.1f)
                return PointEdgePosition.RIGHT;
            if (area > 0.1f)
                return PointEdgePosition.LEFT;
            
            return PointEdgePosition.COLINEAR;
        } 
        
        
        public override String ToString() => "e" + index + " {" + begin + " -> " + end + "}";

        /// <summary>
        /// No puede haber mas de un Eje con los mismos vertices
        /// </summary>
        public override int GetHashCode() => begin.GetHashCode() + end.GetHashCode();
    }
}
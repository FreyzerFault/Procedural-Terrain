using System;
using UnityEngine;

namespace GEOMETRY
{
    public class Utils
    {
        public static float EPSILON = 0.0001f;

        public static bool Equals(Vector2 v1, Vector2 v2) => Mathf.Abs((v1 - v2).magnitude) < EPSILON;

        /// <summary>
        /// AreaTri (p,begin,end) == NEGATIVO => Esta a la Derecha de la Arista (begin -> end)
        /// </summary>
        public static bool isRight(Vector2 begin, Vector2 end, Vector2 p) => TriArea2(begin, end, p) < 0;

        /// <summary>
        /// Area del Triangulo al Cuadrado (para clasificar puntos a la derecha o izquierda de un segmento)
        /// Area POSITIVA => IZQUIERDA
        /// Area NEGATIVA => DERECHA
        /// </summary>
        public static float TriArea2(Vector2 p1, Vector2 p2, Vector2 p3)
            => Det3X3(
                p1.x, p1.y, 1,
                p2.x, p2.y, 1,
                p3.x, p3.y, 1
            );

        /// <summary>
        /// Determinante de una Matriz 3x3
        /// ((a,b,c),(d,e,f),(g,h,i)
        /// </summary>
        private static float Det3X3(
            float a, float b, float c,
            float d, float e, float f,
            float g, float h, float i 
        ) => a * e * i + g * b * f + c * d * h - c * e * g - i * d * b - a * h * f;

        /// <summary>
        /// <para>Comprueba si el punto p esta dentro del Circulo formado por c1,c2,c3</para>
        /// <para>Implicitamente lo que hace es comprobar si el Angulo(a,b,c) &lt;= Angulo(p,b,c).
        /// Siendo el Angulo del punto a y p</para>
        /// <para>Si p pertenece a la Circunferencia, se considera FUERA</para>
        /// </summary>
        /// <param name="p">Punto fuera o dentro</param>
        /// <returns>FALSE si esta fuera o si los 3 puntos a,b,c son colineares</returns>
        public static bool IsInsideCircle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2? centro = centroCirculo(a, b, c);
            
            // Son colineares, no hay circulo
            if (centro == null)
                return false;

            // Si el radio es mayor que la distancia de P al Centro => DENTRO
            if ((a - (Vector2) centro).magnitude > (p - (Vector2) centro).magnitude)
                return true;

            return false;
            //return angle(a, b, c) < angle(p, b, c);
        }

        /// <summary>
        /// Calculo del angulo entre 2 Vectores (a->b) y (a->c)
        /// como el Arcoseno del Producto Escalar de los angulos normalizados
        /// </summary>
        private static float angle(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 u = (b - a).normalized;
            Vector2 v = (c - a).normalized;
            
            return Mathf.Acos(Vector2.Dot(u, v));
        }

        /// <summary>
        /// Calcula el Centro de un Circulo que pasa por 3 puntos (a,b,c)
        /// </summary>
        /// <returns>NULL si son colineares</returns>
        private static Vector2? centroCirculo(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 abMediatriz = Vector2.Perpendicular(b - a).normalized;            
            Vector2 bcMediatriz = Vector2.Perpendicular(b - c).normalized;

            Vector2 abMedio = a + (b - a) / 2;
            Vector2 bcMedio = b + (c - b) / 2;

            return intersectionPoint(abMedio, abMedio + abMediatriz, bcMedio, bcMedio + bcMediatriz);
        }

        /// <summary>
        /// Calcula la interseccion de dos rectas definidas por los puntos (a,b) y (c,d)
        /// </summary>
        /// <returns>NULL si son paralelas</returns>
        private static Vector2? intersectionPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2 ab = b - a;
            Vector2 cd = d - c;
            Vector2 ac = c - a;

            float denominador = cd.x * ab.y - ab.x * cd.y;

            if (Mathf.Abs(denominador) < EPSILON)
                return null;

            float s = (cd.x * ac.y - ac.x * cd.y) / denominador;
            //float t = (ab.x * ac.y - ac.x * ab.y) / denominador;

            // Solo hace falta el parametro de una recta para encontrar el punto
            return a + ab * s;
        }
    }
}
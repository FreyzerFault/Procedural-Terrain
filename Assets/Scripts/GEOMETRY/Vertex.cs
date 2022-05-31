using System;
using UnityEngine;

namespace GEOMETRY
{
    [Serializable]
    public class Vertex
    {
        public int index;
        
        public float x;
        public float y;
        public float z;

        public readonly Vector3 v3D;
        public readonly Vector2 v2D; // 2.5D (x,z)

        public Vertex(float x, float y, float z, int index = -1)
        {
            this.index = index;
            
            this.x = x;
            this.y = y;
            this.z = z;

            v3D = new Vector3(x, y, z);
            v2D = new Vector2(x, z);
        }

        public Vertex(Vector3 v, int index = -1) : this(v.x, v.y, v.z, index) {}

        public override string ToString() => "v" + index;

        public string ToString(bool withCoords) =>
            ToString() + (withCoords ? "v" + "(" + x + ", " + z + ") H = " + y : "");

        /// Se identifica por su coordenada 2D en el plano X,Z.
        /// No puede haber mas de 1 punto con distinta altura
        public override int GetHashCode() => v2D.GetHashCode();
    }
}
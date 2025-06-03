using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2.Shape
{
    public struct Triangle
    {
        public Vector3[] p;
        public Vector3 this[int index] => p[index];
        public Vector3 p0 => p[0];
        public Vector3 p1 => p[1];
        public Vector3 p2 => p[2];

        public Triangle(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            this.p = new Vector3[3] { p0, p1, p2 };
        }

        public static Triangle operator + (Triangle tri, Vector3 displacement)
        {
            return new Triangle(
                tri.p0 + displacement,
                tri.p1 + displacement,
                tri.p2 + displacement);
        }
    }
}
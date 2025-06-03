using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2
{
	public static class Vector2Extend
	{
		#region Common
		public static bool IsNaN(this Vector2 self) => float.IsNaN(self.x) || float.IsNaN(self.y);
		public static bool IsInfinity(this Vector2 self) => float.IsInfinity(self.x) || float.IsInfinity(self.y);

		#endregion // Common

		#region cast to Vector3
		public static Vector3 CastXY2XYZ(this Vector2 xy)
		{
			return new Vector3(xy.x, xy.y, 0f);
		}
		public static Vector3 CastXZ2XYZ(this Vector2 xz)
        {
			return new Vector3(xz.x, 0f, xz.y);
		}
		#endregion cast to Vector3

		#region cast to Vector2
		/// <summary>
		/// Cast Vector3 to Vector2 on a plane
		/// <see cref="http://answers.unity3d.com/questions/742205/how-to-cast-vector3-on-a-plane-to-get-vector2.html"/>
		/// </summary>
		/// <param name="self"></param>
		/// <param name="normal"></param>
		/// <returns></returns>
		public static Vector2 CastVector2ByNormal(this Vector3 self, Vector3 normal)
		{
			Vector3 d = self - self.PointOnDistance(normal, 1f);
			return new Vector2(Mathf.Sqrt(d.x * d.x + d.z * d.z), d.y);
		}
		#endregion

		/// <summary>
		/// <see cref="https://forum.unity3d.com/threads/making-a-square-vector2-fit-a-circle-vector2.422352/"/>
		/// </summary>
		/// <param name="input"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		public static Vector2 ConvertCircleToSquare(this Vector2 input, float threshold = float.Epsilon)
		{
			const float COS_45 = 0.70710678f;
			float sqrThreshold = threshold * threshold;
			if (input.sqrMagnitude <= sqrThreshold)
			{
				return Vector2.zero;
			}

			Vector2 normal = input.normalized;
			float x, y;

			if (normal.x != 0f && normal.y >= -COS_45 && normal.y <= COS_45)
			{
				x = normal.x >= 0f ? input.x / normal.x : -input.x / normal.x;
			}
			else
			{
				x = input.x / Mathf.Abs(normal.y);
			}

			if (normal.y != 0f && normal.x >= -COS_45 && normal.x <= COS_45)
			{
				y = normal.y >= 0f ? input.y / normal.y : -input.y / normal.y;
			}
			else
			{
				y = input.y / Mathf.Abs(normal.x);
			}

			return new Vector2(x, y);
		}

		/// <summary>
		/// <see cref="http://amorten.com/blog/2017/mapping-square-input-to-circle-in-unity/"/>
		/// <see cref="http://mathproofs.blogspot.hk/2005/07/mapping-square-to-circle.html"/>
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static Vector2 ConvertSquareToCircle(this Vector2 input)
		{
			return new Vector2(
				input.x * Mathf.Sqrt(1f - input.y * input.y * 0.5f),
				input.y * Mathf.Sqrt(1f - input.x * input.x * 0.5f)
				);
		}
		
		public static Vector2 Scale(this Vector2 self, float fromMin, float fromMax, float toMin, float toMax)
		{
			return new Vector2(
				self.x.Remap(fromMin, fromMax, toMin, toMax),
				self.y.Remap(fromMin, fromMax, toMin, toMax)
				);
		}

		/// <summary>find angle between two vector, with signed</summary>
		/// <param name="lhs">from left hand side</param>
		/// <param name="rhs">to right hand side</param>
		/// <returns></returns>
		/// <see cref="http://referencesource.microsoft.com/#WindowsBase/Base/System/Windows/Vector.cs,102"/>
		/// <remarks>result can be flip, if you flip input (lhs ~ rhs)</remarks>
		public static float SignedAngle(this Vector2 lhs, Vector2 rhs)
		{
			lhs.Normalize();
			rhs.Normalize();
			var sin = rhs.x * lhs.y - lhs.x * rhs.y;
			var cos = lhs.x * rhs.x + lhs.y * rhs.y;
			return Mathf.Atan2(sin, cos) * Mathf.Rad2Deg;
		}

		/// <summary>
		/// to calculate the perpendicular distance
		/// between <see cref="self"/> & line P1 P2.
		/// </summary>
		/// <param name="c">input</param>
		/// <param name="a">point 1 of base line</param>
		/// <param name="b">point 2 of base line</param>
		/// <returns>the perpendicular distance between line.</returns>
		public static float PerpendicularDistance(this Vector2 c, Vector2 a, Vector2 b)
		{
#if true
			var slope	= (b.y - a.y) / (b.x - a.x);
            var f0		= slope * c.x - c.y + a.y - slope * a.x;
			var f1		= Mathf.Sqrt(1f + slope * slope);
			var distance = Mathf.Abs(f0 / f1);
			return distance;
#else
            var dir			= (b - a).normalized;		// normalize direction
            var projectC	= Vector2.Dot(c - a, dir);	// projected c on line(ba)
            var p			= a + dir * projectC;		// perpendicular cross on point p
            var distance	= (c - p).magnitude;		// distance between p & c
			return distance;
#endif
        }

        public static float CrossProduct(this Vector2 p, Vector2 a, Vector2 b)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        public static float InverseLerp(Vector2 a, Vector2 b, Vector2 value)
        {
            if (a == b)
                return 0f;
            var AB = b - a;
            var AV = value - a;
            return Vector2.Dot(AV, AB) / Vector2.Dot(AB, AB);
        }

        /// <summary>
        /// <see cref="https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect"/>
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="intersect"></param>
        /// <returns></returns>
        public static bool TryIntersection(
            Vector2 p0, Vector2 p1,
            Vector2 p2, Vector2 p3, out Vector2 intersect)
        {
            // Calculate the direction vectors
            var p0_x = p0.x;  var p0_y = p0.y;
            var p1_x = p1.x;  var p1_y = p1.y;
            var p2_x = p2.x;  var p2_y = p2.y;
            var p3_x = p3.x;  var p3_y = p3.y;
            var s1_x = p1_x - p0_x;
            var s1_y = p1_y - p0_y;
            var s2_x = p3_x - p2_x;
            var s2_y = p3_y - p2_y;
            var denominator = -s2_x * s1_y + s1_x * s2_y;
            if (denominator == 0f)
            {
                // Collinear
                intersect = Vector2.zero;
                return false;
            }

            var s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / denominator;
            var t = ( s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / denominator;

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                // Collision detected
                intersect = new Vector2(
                    p0_x + (t * s1_x),
                    p0_y + (t * s1_y)
                );
                return true;
            }
            intersect = Vector2.zero;
            return false;
        }
    }
}
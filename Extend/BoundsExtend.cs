using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
	public static class BoundsExtend
	{
		public static Bounds GetForward(this Bounds self)
		{
			return new Bounds(new Vector3(
				self.center.x,
				self.center.y,
				self.center.z + self.size.z),
				self.size);
		}

		public static Bounds GetBackward(this Bounds self)
		{
			return new Bounds(new Vector3(
				self.center.x,
				self.center.y,
				self.center.z - self.size.z),
				self.size);
		}

		public static Bounds GetUp(this Bounds self)
		{
			return new Bounds(new Vector3(
				self.center.x,
				self.center.y + self.size.y,
				self.center.z),
				self.size);
		}

		public static Bounds GetDown(this Bounds self)
		{
			return new Bounds(new Vector3(
				self.center.x,
				self.center.y - self.size.y,
				self.center.z),
				self.size);
		}

		public static Bounds GetLeft(this Bounds self)
		{
			return new Bounds(new Vector3(
				self.center.x - self.size.x,
				self.center.y,
				self.center.z),
				self.size);
		}

		public static Bounds GetRight(this Bounds self)
		{
			return new Bounds(new Vector3(
				self.center.x + self.size.x,
				self.center.y,
				self.center.z),
				self.size);
		}

		/// <summary>
		/// Checks if outerBounds encapsulates innerBounds.
		/// </summary>
		/// <param name="outerBounds">Outer bounds.</param>
		/// <param name="innerBounds">Inner bounds.</param>
		/// <returns>True if innerBounds is fully encapsulated by outerBounds.</returns>
		public static bool IsFullyEncapsulate(this Bounds outerBounds, Bounds innerBounds)
		{
			return outerBounds.Contains(innerBounds.min) && outerBounds.Contains(innerBounds.max);
		}

		public static Bounds GetStructuredBounds(Vector3 point, float size)
		{
			return GetStructuredBounds(point, new Vector3(size, size, size));
		}

		/// <summary>Get Structure Grid based on giving cell size,
		/// and convert giving point into grid in related coordinate.
		/// <see cref="https://www.clonefactor.com/wordpress/public/1841/"/></summary>
		/// <param name="point">reference point</param>
		/// <param name="size">grid cell size</param>
		/// <returns>the related coordinate based on grid system.</returns>
		public static Bounds GetStructuredBounds(Vector3 point, Vector3 size)
		{
			Vector3 center = point.GetStructurePivot(size);
			return new Bounds(center, size);
		}


		/// <summary>Does another bounding box intersect with this bounding box?</summary>
		/// <param name="self"></param>
		/// <param name="other"></param>
		/// <param name="containSurfaceCollision">false = to ignore the surface collision (Which are included in Unity default API).</param>
		/// <returns>ture = intersect each other</returns>
		/// <remarks>
		/// to ignore surface collision, aim for finding neighbors within a grid of bounds,
		/// A quick AABB test to sort out which one are correct neighbors.</remarks>
		public static bool Intersects(this Bounds self, Bounds other, bool containSurfaceCollision = true)
		{
			if (containSurfaceCollision)
			{
				// Unity API.
				return self.Intersects(other);
			}
			else
			{
				Vector3 c = self.center - other.center;
				Vector3 r = self.extents + other.extents;
				// math hack: to ignore surface collision by using "<" instead of "<="
				return
					Mathf.Abs(c.x) < r.x &&
					Mathf.Abs(c.y) < r.y &&
					Mathf.Abs(c.z) < r.z;
			}
		}

		/// <summary>
		/// Find Closest point in world space.
		/// 1) convert into local space 
		/// 2) find closest local point
		/// 3) convert into world space.
		/// </summary>
		/// <param name="self"></param>
		/// <param name="worldPos"></param>
		/// <param name="parent"></param>
		/// <returns>world space closest point</returns>
		public static Vector3 ClosestPoint(this Bounds self, Vector3 worldPos, Matrix4x4 parent)
		{
			Vector3 localPos = parent.InverseTransformPoint(worldPos);
			Vector3 localClosest = self.ClosestPoint(localPos);
			Vector3 worldClosest = parent.TransformPoint(localClosest);
			return worldClosest;
		}

		public static Vector3 ClosestPoint(this Bounds self, Vector3 worldPos, Transform transform)
		{
			return self.ClosestPoint(worldPos, transform.localToWorldMatrix);
		}

		/// <summary>Check if world position within giving bounds,
		/// based on bounds' parent matrix.</summary>
		/// <param name="bounds"></param>
		/// <param name="worldPos"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static bool Contains(this Bounds bounds, Vector3 worldPos, Matrix4x4 matrix)
		{
			Vector3 localPos = matrix.InverseTransformPoint(worldPos);
			return bounds.Contains(localPos);
		}

		public static bool Contains(this Bounds bounds, Vector3 worldPos, Transform boundsTransform)
		=> bounds.Contains(worldPos, boundsTransform.localToWorldMatrix);

		public static bool Contains(this Bounds bounds, Vector3 worldPos, Vector3 boundsPos, Quaternion boundsRotate)
		=> bounds.Contains(worldPos, Matrix4x4.TRS(boundsPos, boundsRotate, Vector3.one));

		public static Vector3[] GetVertices(this Bounds bounds)
        {
            var c = bounds.center;
            var l = bounds.extents;

            var v = new Vector3[8];
			var i = 0;
            v[i++] = c + new Vector3( l.x,  l.y,  l.z); // RTF
            v[i++] = c + new Vector3(-l.x,  l.y,  l.z); // LTF
            v[i++] = c + new Vector3( l.x, -l.y,  l.z); // RDF
            v[i++] = c + new Vector3(-l.x, -l.y,  l.z); // LDF
            v[i++] = c + new Vector3( l.x,  l.y, -l.z); // RTB
            v[i++] = c + new Vector3(-l.x,  l.y, -l.z); // LTB
            v[i++] = c + new Vector3( l.x, -l.y, -l.z); // RDB
            v[i++] = c + new Vector3(-l.x, -l.y, -l.z); // LDB
            return v;
        }
        public static Vector3[] GetPolygon(this Bounds bounds)
        {
			var v = bounds.GetVertices();
            int[] order = new int[]
            {
                0, 1, 2, // RTF, LTF, RBF
				2, 1, 3, // RBF, LTF, LBF
				4, 0, 6, // RTB, RTF, RBB
				6, 0, 2, // RBB, RTF, RBF
				7, 5, 6, // LBB, LTB, RBB
				6, 5, 4, // RBB, LTB, RTB
				3, 1, 7, // LBF, LTF, LBB
				7, 1, 5, // LBB, LTF, LTB
				4, 5, 0, // RTB, LTB, RTF
				0, 5, 1, // RTF, LTB, LTF
				6, 2, 7, // RBB, RBF, LBB
				7, 2, 3  // LBB, RBF, LBF
            };
			var p = new Vector3[order.Length];
			for (int i = 0; i<order.Length; ++i)
			{
				p[i] = v[order[i]];
			}
			return p;
        }
	}
}
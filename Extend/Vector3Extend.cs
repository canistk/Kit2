using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
	public static class Vector3Extend
	{
		#region Basic
		/// <summary>To find out this vector3 is Nan</summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static bool IsNaN(this Vector3 self)
		{
			return float.IsNaN(self.x) || float.IsNaN(self.y) || float.IsNaN(self.z);
		}

		public static bool IsInfinity(this Vector3 self)
		{
			return float.IsInfinity(self.x) || float.IsInfinity(self.y) || float.IsInfinity(self.z);
		}

		public static bool IsZero(in this Vector3 self)
		{
			return self.x == 0f && self.y == 0f && self.z == 0f;
		}

		public static bool IsNormalized(in this Vector3 self)
		{
			var len		= (self.x * self.x) + (self.y * self.y) + (self.z * self.z);
			var diff	= len - 1f;
			return diff < 1e-6f;
		}

		private static Vector3 normalized(Vector3 v)
        {
			float length = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
			return new Vector3(v.x / length, v.y / length, v.z / length);
        }

		/// <summary>Compare all axis by <see cref="Mathf.Approximately(float, float)"/></summary>
		/// <param name="self"></param>
		/// <param name="target"></param>
		/// <returns>return true when it's close enought to each other.</returns>
		public static bool Approximately(in this Vector3 self, in Vector3 target)
		{
			return Mathf.Approximately(self.x, target.x) &&
				Mathf.Approximately(self.y, target.y) &&
				Mathf.Approximately(self.z, target.z);
		}

		/// <summary>Compare two Vector is roughly equal to each others</summary>
		/// <param name="self">Vector3</param>
		/// <param name="target">Vector3</param>
		/// <param name="threshold">The threshold value that can ignore.</param>
		/// <returns>true/false</returns>
		public static bool EqualRoughly(in this Vector3 self, in Vector3 target, float threshold = float.Epsilon)
		{
			return self.x.EqualRoughly(target.x, threshold) &&
				self.y.EqualRoughly(target.y, threshold) &&
				self.z.EqualRoughly(target.z, threshold);
		}

		/// <summary>Absolute value of vector</summary>
		/// <param name="self"></param>
		/// <returns></returns>
		/// <example>Vector3(2f,-1f,-100f) = Vector3(2f,1f,100f)</example>
		public static Vector3 Abs(this Vector3 self)
		{
			return new Vector3(Mathf.Abs(self.x), Mathf.Abs(self.y), Mathf.Abs(self.z));
		}

		/// <summary>Divide current Vector by the other</summary>
		/// <param name="self"></param>
		/// <param name="denominator"></param>
		/// <returns></returns>
		/// <example>Vector3(6,4,2).Divide(new Vector3(2,2,2)) == Vector3(3,2,1)</example>
		public static Vector3 Divide(this Vector3 self, Vector3 denominator)
		{
			return new Vector3(self.x / denominator.x, self.y / denominator.y, self.z / denominator.z);
		}

		/// <summary>Return a vector containing smallest components of the giving vectors</summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="result"></param>
		public static void Min(in Vector3 left, in Vector3 right, out Vector3 result)
		{
			result = new Vector3
			{
				x = left.x < right.x ? left.x : right.x,
				y = left.y < right.y ? left.y : right.y,
				z = left.z < right.z ? left.z : right.z
			};
		}

		/// <summary>Return a vector containing smallest components of the giving vectors</summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector3 Min(in Vector3 left, in Vector3 right)
		{
			Min(left, right, out Vector3 result);
			return result;
		}

		/// <summary>Return a vector containing largest components of the giving vectors</summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="result"></param>
		public static void Max(in Vector3 left, in Vector3 right, out Vector3 result)
		{
			result = new Vector3
			{
				x = left.x > right.x ? left.x : right.x,
				y = left.y > right.y ? left.y : right.y,
				z = left.z > right.z ? left.z : right.z
			};
		}

		/// <summary>Return a vector containing largest components of the giving vectors</summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector3 Max(in Vector3 left, in Vector3 right)
		{
			Max(left, right, out Vector3 result);
			return result;
		}
        
		public static Vector3 AppendX(this Vector3 v, in float _x)
        {
            v.x += _x;
            return v;
        }

        public static Vector3 AppendY(this Vector3 v, in float _y)
		{
			v.y += _y;
			return v;
		}

        public static Vector3 AppendZ(this Vector3 v, in float _z)
        {
            v.z += _z;
            return v;
        }

		public static Vector3 SetX(this Vector3 v, in float _x)
		{
			v.x = _x;
			return v;
		}

		public static Vector3 SetY(this Vector3 v, in float _y)
		{
			v.y = _y;
			return v;
		}

		public static Vector3 SetZ(this Vector3 v, in float _z)
		{
			v.y = _z;
			return v;
		}

        public static Vector2 XX(this Vector3 v) => new Vector2(v.x, v.x);
        public static Vector2 YY(this Vector3 v) => new Vector2(v.y, v.y);
        public static Vector2 ZZ(this Vector3 v) => new Vector2(v.z, v.z);
		public static Vector2 XY(this Vector3 v) => new Vector2(v.x, v.y);
        public static Vector2 XZ(this Vector3 v) => new Vector2(v.x, v.z);
        public static Vector2 YZ(this Vector3 v) => new Vector2(v.y, v.z);
		public static Vector2 YX(this Vector3 v) => new Vector2(v.y, v.x);
        public static Vector2 ZX(this Vector3 v) => new Vector2(v.z, v.x);
        public static Vector2 ZY(this Vector3 v) => new Vector2(v.x, v.y);

        #endregion

        #region Position
        /// <summary>Transforms position from local space to world space.</summary>
        /// <param name="position"></param>
        /// <param name="localRotate"></param>
        /// <param name="localScale"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <remarks>As same as Transform.TransformPoint</remarks>
        /// <see cref="http://docs.unity3d.com/412/Documentation/ScriptReference/Transform.TransformPoint.html"/>
        /// <seealso cref="https://en.wikipedia.org/wiki/Transformation_matrix"/>
        public static Vector3 TransformPoint(this Vector3 position, Quaternion localRotate, Vector3 localScale, Vector3 offset)
		{
			return Matrix4x4.TRS(position, localRotate, localScale).MultiplyPoint3x4(offset);
		}

		/// <summary>Transforms position from local space to world space.
		/// consider no scale invoked</summary>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		/// <remarks>As same as Transform.TransformPoint</remarks>
		public static Vector3 TransformPoint(this Vector3 position, Quaternion rotation, Vector3 offset)
		{
			return position + rotation * offset;
			// return TransformPoint(position, rotation, Vector3.one, offset);
		}
		/// <summary>Transforms position from world space to local space.</summary>
		/// <param name="position"></param>
		/// <param name="localRotate"></param>
		/// <param name="localScale"></param>
		/// <param name="targetPosition"></param>
		/// <returns></returns>
		public static Vector3 InverseTransformPoint(this Vector3 position, Quaternion localRotate, Vector3 localScale, Vector3 targetPosition)
		{
			// http://answers.unity3d.com/questions/1124805/world-to-local-matrix-without-transform.html
			// return (localRotate.Inverse() * (targetPosition - position)).Divide(localScale);
			return Matrix4x4.TRS(position, localRotate, localScale).inverse.MultiplyPoint(targetPosition);
		}
		/// <summary>Transforms position from world space to local space.</summary>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <param name="targetPosition"></param>
		/// <returns></returns>
		public static Vector3 InverseTransformPoint(this Vector3 position, Quaternion rotation, Vector3 targetPosition)
		{
			return Quaternion.Inverse(rotation) * (position - targetPosition);
			// return position.InverseTransformPoint(rotation, Vector3.one, targetPosition);
		}

		/// <summary>Snap giving point into Structure grid's pivot point in related coordinate.
		/// depend on giving cell size.
		/// <see cref="https://www.clonefactor.com/wordpress/public/1841/"/></summary>
		/// <param name="point">Reference in world space</param>
		/// <param name="size">Size of giving cell, assume all cell are same size.</param>
		/// <returns>the related coordinate based on grid system.</returns>
		public static Vector3 GetStructurePivot(this Vector3 point, Vector3 size)
		{
			return new Vector3(
				StructureAxis(size.x, point.x),
				StructureAxis(size.y, point.y),
				StructureAxis(size.z, point.z));
		}

		/// <summary>Find the closest session anchor based on session length & distance.</summary>
		/// <param name="sessionLength">giving session length</param>
		/// <param name="distance">giving distance</param>
		/// <returns>the closest session</returns>
		private static float StructureAxis(float sessionLength, float distance)
		{
			float anchor = Mathf.Sign(distance) * (Mathf.Abs(distance) + (sessionLength * 0.5f));
			float remainder = anchor % sessionLength;
			return anchor - remainder;
		}

		public static bool TryGetFarest(Vector3 pivot, Vector3 axis, 
			out Vector3 farestPoint, params Vector3[] p)
		{
			if (axis == Vector3.zero)
				throw new System.Exception("Axis cannot be zero");

			axis = axis.normalized;
			float tmp, farSqr = 0f;
			int idx = -1;
			for (int i = 0; i < p.Length; ++i)
			{
				var pj = Vector3.Project(p[i] - pivot, axis);
				var dot = Vector3.Dot(pj, axis);
				if (dot < 0)
					continue;
				tmp = pj.sqrMagnitude;
				if (farSqr < tmp)
				{
					farSqr = tmp;
                    idx = i;
				}
			}

			if (idx < 0)
			{
				farestPoint = Vector3.zero;
				return false;
			}

			farestPoint = p[idx];
			return true;
		}

        public static bool TryGetClosest(Vector3 pivot, Vector3 axis,
            out Vector3 closestPoint, params Vector3[] p)
        {
            if (axis == Vector3.zero)
                throw new System.Exception("Axis cannot be zero");

            axis = axis.normalized;
            float tmp, closerSqr = float.PositiveInfinity;
            int idx = -1;
            for (int i = 0; i < p.Length; ++i)
            {
                var pj = Vector3.Project(p[i] - pivot, axis);
                var dot = Vector3.Dot(pj, axis);
                if (dot < 0)
                    continue;
                tmp = pj.sqrMagnitude;
                if (closerSqr > tmp)
                {
                    closerSqr = tmp;
                    idx = i;
                }
            }

            if (idx < 0)
            {
                closestPoint = Vector3.zero;
                return false;
            }

            closestPoint = p[idx];
            return true;
        }
        #endregion

        #region Direction
        /// <summary>Transforms Direction from local space to world space</summary>
        /// <param name="position"></param>
        /// <param name="localRotate"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Vector3 TransformDirection(this Vector3 position, Quaternion localRotate, Vector3 direction)
		{
			return Matrix4x4.TRS(position, localRotate, Vector3.one).MultiplyVector(direction);
		}
		/// <summary>Transform Direction from world space to local space</summary>
		/// <param name="position"></param>
		/// <param name="localRotate"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static Vector3 InverseTransformDirection(this Vector3 position, Quaternion localRotate, Vector3 direction)
		{
			return Matrix4x4.TRS(position, localRotate, Vector3.one).inverse.MultiplyVector(direction);
		}
		/// <summary>Transform vector from local space to world space</summary>
		/// <param name="position"></param>
		/// <param name="localRotate"></param>
		/// <param name="localScale"></param>
		/// <param name="vector"></param>
		/// <returns></returns>
		public static Vector3 TransformVector(this Vector3 position, Quaternion localRotate, Vector3 localScale, Vector3 vector)
		{
			return Matrix4x4.TRS(position, localRotate, localScale).MultiplyVector(vector);
		}
		/// <summary>Transforms vector from world space to local space</summary>
		/// <param name="position"></param>
		/// <param name="localRotate"></param>
		/// <param name="localScale"></param>
		/// <param name="vector"></param>
		/// <returns></returns>
		public static Vector3 InverseTransformVector(this Vector3 position, Quaternion localRotate, Vector3 localScale, Vector3 vector)
		{
			return Matrix4x4.TRS(position, localRotate, localScale).inverse.MultiplyVector(vector);
		}
		/// <summary>Direction between 2 position</summary>
		/// <param name="from">Position</param>
		/// <param name="to">Position</param>
		/// <returns>Direction Vector</returns>
		public static Vector3 Direction(this Vector3 from, Vector3 to)
		{
			return (to - from).normalized;
		}
		/// <summary>Rotate X axis on current direction vector</summary>
		/// <param name="self"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static Vector3 RotateX(this Vector3 self, float angle)
		{
			float sin = Mathf.Sin(angle);
			float cos = Mathf.Cos(angle);

			float ty = self.y;
			float tz = self.z;
			self.y = (cos * ty) - (sin * tz);
			self.z = (cos * tz) + (sin * ty);
			return self;
		}
		/// <summary>Rotate Y axis on current direction vector</summary>
		/// <param name="self"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static Vector3 RotateY(this Vector3 self, float angle)
		{
			float sin = Mathf.Sin(angle);
			float cos = Mathf.Cos(angle);

			float tx = self.x;
			float tz = self.z;
			self.x = (cos * tx) + (sin * tz);
			self.z = (cos * tz) - (sin * tx);
			return self;
		}
		/// <summary>Rotate Z axis on current direction vector</summary>
		/// <param name="self"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static Vector3 RotateZ(this Vector3 self, float angle)
		{
			float sin = Mathf.Sin(angle);
			float cos = Mathf.Cos(angle);

			float tx = self.x;
			float ty = self.y;
			self.x = (cos * tx) - (sin * ty);
			self.y = (cos * ty) + (sin * tx);
			return self;
		}

		/// <summary>Returns vector projected to a plane and multiplied by weight</summary>
		/// <param name="tangent"></param>
		/// <param name="normal"></param>
		/// <param name="weight"></param>
		/// <returns></returns>
		public static Vector3 ExtractHorizontal(Vector3 tangent, Vector3 normal, float weight = 1f)
		{
			if (weight == 0f)
				return Vector3.zero;
			Vector3 copy = tangent;
			Vector3.OrthoNormalize(ref normal, ref copy);
			return Vector3.Project(tangent, copy) * weight;
		}

		/// <summary>Returns vector projection on axis multiplied by weight.</summary>
		/// <param name="tangent"></param>
		/// <param name="verticalAxis"></param>
		/// <param name="weight"></param>
		/// <returns></returns>
		public static Vector3 ExtractVertical(Vector3 tangent, Vector3 verticalAxis, float weight = 1f)
		{
			if (weight == 0f)
				return Vector3.zero;
			return Vector3.Project(tangent, verticalAxis) * weight;
		}

		/// <summary>Find the relative vector from giving angle & axis</summary>
		/// <param name="self"></param>
		/// <param name="angle">0~360</param>
		/// <param name="axis">Vector direction e.g. Vector.up</param>
		/// <param name="useRadians">0~360 = false, 0~1 = true</param>
		/// <returns></returns>
		public static Vector3 RotateAroundAxis(this Vector3 self, float angle, Vector3 axis, bool useRadians = false)
		{
			if (useRadians) angle *= Mathf.Rad2Deg;
			Quaternion q = Quaternion.AngleAxis(angle, axis);
			return (q * self);
		}
		#endregion

		#region Distance
		/// <summary>Distance between two position</summary>
		/// <param name="self"></param>
		/// <param name="position"></param>
		/// <returns>Disatnce</returns>
		/// <see cref="http://answers.unity3d.com/questions/384932/best-way-to-find-distance.html"/>
		/// <seealso cref="http://forum.unity3d.com/threads/square-root-runs-1000-times-in-0-01ms.147661/"/>
		public static float Distance(this Vector3 self, Vector3 position)
		{
			//position -= self;
			//return Mathf.Sqrt( position.x * position.x + position.y * position.y + position.z * position.z);
			return Vector3.Distance(self, position);
		}
		/// <summary>Return lerp Vector3 by giving distance</summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="distance"></param>
		/// <returns></returns>
		public static Vector3 LerpByDistance(Vector3 start, Vector3 end, float distance)
		{
			return distance * (end - start) + start;
		}
		/// <summary>Use start position, direction and known distance to find the end point position</summary>
		/// <param name="position"></param>
		/// <param name="direction"></param>
		/// <param name="distance"></param>
		/// <returns>End point position</returns>
		public static Vector3 PointOnDistance(this Vector3 position, Vector3 direction, float distance)
		{
			return position + (direction * distance);
		}

		/// <summary>Locate the closest distance from point to line</summary>
		/// <param name="origin">the origin position of line</param>
		/// <param name="directionNormalize">the line direction (require unit vector)</param>
		/// <param name="point">the giving point</param>
		/// <returns>the smallest distance from point to line.</returns>
		[System.Obsolete("use Closest.DistanceToPoint")]
		public static float ClosestDistanceToPoint(Vector3 origin, Vector3 directionNormalize, Vector3 point)
		{
			return Vector3.Cross(directionNormalize, point - origin).magnitude;
		}

		/// <summary>
		/// calculate the distance from <paramref name="p"/> and line of <paramref name="a"/>, <paramref name="b"/>
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="p"></param>
		/// <returns></returns>
        public static float PerpendicularDistance(Vector3 a, Vector3 b, Vector3 p)
        {
            var vector = b - a;
            var fromLineToPoint = p - a;

            // projection
			// Vector3.Project(fromLineToPoint, vector.normalized);
            var t = Vector3.Dot(fromLineToPoint, vector) / Vector3.Dot(vector, vector);
            Vector3 projectedPoint = a + t * vector;

            return Vector3.Magnitude(p - projectedPoint);
        }

        /// <summary>
        /// Find a cloest point on a line.
        /// <see cref="https://forum.unity.com/threads/how-do-i-find-the-closest-point-on-a-line.340058/"/>
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="directionNormalize"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        [System.Obsolete("use Closest.PointOnRay")]
		public static Vector3 NearestPointOnLine(Vector3 origin, Vector3 directionNormalize, Vector3 point)
		{
			directionNormalize.Normalize();
			Vector3 vector = point - origin;
			float projectedDistance = Vector3.Dot(vector, directionNormalize);
			return origin + directionNormalize * projectedDistance;
		}

		[System.Obsolete("use Closest.PointInBox")]
		public static Vector3 ClosestPointInBox(Vector3 boxOrigin, Quaternion boxRotate, Vector3 boxScale, in Vector3 boxCenter, in Vector3 boxSize, Vector3 point)
		{
			return ClosestPointInBox(
				Matrix4x4.TRS(boxOrigin, boxRotate, boxScale),
				boxCenter, boxSize, point);
		}

		[System.Obsolete("use Closest.PointInBox")]
		public static Vector3 ClosestPointInBox(in Matrix4x4 matrix, in Vector3 boxCenter, in Vector3 boxSize, in Vector3 point)
		{
			Vector3 local = matrix.inverse.MultiplyPoint(point); // world to local
			Vector3 half = boxSize * 0.5f;
			Max(local, boxCenter - half, out Vector3 tmp);
			Min(tmp, boxCenter + half, out Vector3 localClosest);
			return matrix.MultiplyPoint(localClosest); // local to world.
		}
		#endregion

		#region Angle
		/// <summary>find angle between 2 position, using itself as center</summary>
		/// <param name="center"></param>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <returns></returns>
		public static float AngleBetweenPosition(this Vector3 center, Vector3 point1, Vector3 point2)
		{
			return Vector3.Angle((point1 - center), (point2 - center));
		}

		/// <summary>
		/// Determine the signed angle between two vectors, with normal as the rotation axis.
		
		/// </summary>
		/// <example>Vector3.AngleBetweenDirectionSigned(Vector3.forward,Vector3.right)</example>
		/// <param name="direction1">Direction vector</param>
		/// <param name="direction2">Direction vector</param>
		/// <param name="normal">normal vector e.g. AxisXZ = Vector3.Cross(Vector3.forward, Vector3.right);</param>
		/// <see cref="http://forum.unity3d.com/threads/need-vector3-angle-to-return-a-negtive-or-relative-value.51092/"/>
		/// <see cref="http://stackoverflow.com/questions/19675676/calculating-actual-angle-between-two-vectors-in-unity3d"/>
		public static float AngleBetweenDirectionSigned(this Vector3 direction1, Vector3 direction2, Vector3 normal)
		{
#if true
			return Vector3.Angle(direction1, direction2) * Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(direction1, direction2)));
#else
			// Regular Sign Angle.
			return Vector3.SignedAngle(direction1, direction2, normal);
#endif
		}

		/// <summary>Returns the angle between two vectors on a plane with the specified normal</summary>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="normal">plane normal</param>
		/// <returns></returns>
		public static float AngleOrthogonalOnPlane(this Vector3 v1, Vector3 v2, Vector3 normal)
		{
			Vector3.OrthoNormalize(ref normal, ref v1);
			Vector3.OrthoNormalize(ref normal, ref v2);
			return Vector3.Angle(v1, v2);
		}

		/// <summary>Project the vector on bias surface normal, and calculate the angle based on projected vector.</summary>
		/// <param name="v1">vector 1</param>
		/// <param name="v2">vector 2</param>
		/// <param name="normal">the surface normal(unit) used for projection.</param>
		/// <returns></returns>
		public static float AngleProjectOnPlaneSigned(this Vector3 v1, Vector3 v2, Vector3 normal)
        {
#if false
			// use ATan2 to compress angle into the axis we choose,
			// however the ATan2 lookup table, cost more then dot product
			return Mathf.Rad2Deg * Mathf.Atan2(Vector3.Dot(normal, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2));
			/* Full Version
			Vector3 realNormal = Vector3.Cross(v1, v2);
			float axisFilter = Vector3.Dot(normal, realNormal);
			float cos0 = Vector3.Dot(v1, v2);
			return Mathf.Rad2Deg * Mathf.Atan2(axisFilter, cos0);
			//**/
#else
			v1 = Vector3.ProjectOnPlane(v1, normal);
			v2 = Vector3.ProjectOnPlane(v2, normal);
			return Vector3.Angle(v1, v2) * Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(v1, v2)));
			// return Vector3.SignedAngle(v1, v2, normal);
#endif
		}

		public static Vector3 LimitAngleBySpeed(Vector3 from, Vector3 to, Vector3 axis, float angularSpeed, float deltaTime)
		{
			float angleSign = Vector3.SignedAngle(from, to, axis);
			if (angleSign == 0f)
				return to;
			float angleAbs = Mathf.Abs(angleSign);
			float deltaStep = Mathf.Sign(angleSign) * angularSpeed * deltaTime;
			if (angleAbs > deltaStep)
				return Quaternion.AngleAxis(deltaStep, axis) * Vector3.forward;
			return to;
		}
		#endregion

		#region Intersect
		/// <summary>
		/// Find the line of intersection between two planes.	The planes are defined by a normal and a point on that plane.
		/// The outputs are a point on the line and a vector which indicates it's direction. If the planes are not parallel, 
		/// the function outputs true, otherwise false.
		/// </summary>
		/// <remarks><see cref="http://wiki.unity3d.com/index.php/3d_Math_functions?_ga=2.160965757.1324856874.1583386230-1518867862.1582864016"/></remarks>
		/// <param name="linePoint"></param>
		/// <param name="lineVec"></param>
		/// <param name="plane1Normal"></param>
		/// <param name="plane1Position"></param>
		/// <param name="plane2Normal"></param>
		/// <param name="plane2Position"></param>
		/// <returns></returns>
		public static bool PlanePlaneIntersection(out Vector3 linePoint, out Vector3 lineVec, Vector3 plane1Normal, Vector3 plane1Position, Vector3 plane2Normal, Vector3 plane2Position)
		{
			linePoint = Vector3.zero;
			lineVec = Vector3.zero;

			//We can get the direction of the line of intersection of the two planes by calculating the 
			//cross product of the normals of the two planes. Note that this is just a direction and the line
			//is not fixed in space yet. We need a point for that to go with the line vector.
			lineVec = Vector3.Cross(plane1Normal, plane2Normal);

			//Next is to calculate a point on the line to fix it's position in space. This is done by finding a vector from
			//the plane2 location, moving parallel to it's plane, and intersecting plane1. To prevent rounding
			//errors, this vector also has to be perpendicular to lineDirection. To get this vector, calculate
			//the cross product of the normal of plane2 and the lineDirection.		
			Vector3 ldir = Vector3.Cross(plane2Normal, lineVec);

			float denominator = Vector3.Dot(plane1Normal, ldir);

			//Prevent divide by zero and rounding errors by requiring about 5 degrees angle between the planes.
			if (Mathf.Abs(denominator) > 0.006f)
			{

				Vector3 plane1ToPlane2 = plane1Position - plane2Position;
				float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / denominator;
				linePoint = plane2Position + t * ldir;

				return true;
			}

			//output not valid
			else
			{
				return false;
			}
		}
		#endregion

		public static Vector3 Centroid(ReadOnlySpan<Vector3> weightDeltaPosition)
		{
			var cnt = weightDeltaPosition.Length;
			if (cnt <= 0)
				return Vector3.zero;
			var centroid = Vector3.zero;
			for (int i = 0; i < cnt; i++)
			{
				centroid += weightDeltaPosition[i];
			}
			return centroid / cnt;
		}

		public static Vector3 Centroid(ReadOnlySpan<Vector3> coordinates, ReadOnlySpan<float> weights)
		{
			if (coordinates == null)	throw new System.ArgumentNullException($"{nameof(coordinates)} can not be null.");
			if (weights == null)		throw new System.ArgumentNullException($"{nameof(weights)} can not be null.");
			if (coordinates.Length != weights.Length)
				throw new System.ArgumentException($"{nameof(coordinates)} and {nameof(weights)} must have the same length.");

			var cnt = coordinates.Length;
			if (cnt <= 0)
				return Vector3.zero;
			var centroid = Vector3.zero;
			var totalWeight = 0f;
			for (int i = 0; i < cnt; ++i)
			{
				if (weights[i] <= 0f)
					continue;
				centroid += coordinates[i] * weights[i];
				totalWeight += Mathf.Max(0f, weights[i]);
			}
			if (totalWeight <= 0f || centroid.IsNaN() || centroid.IsInfinity())
				return Vector3.zero;
			return centroid / totalWeight;
		}

		public static Vector3 Centroid(ReadOnlySpan<Vector4> weightDeltaPosition)
		{
			var centroid = Vector3.zero;
			var cnt = weightDeltaPosition.Length;
			var totalWeight = 0f;

			for (int i = 0; i < cnt; ++i)
			{
				float weight = weightDeltaPosition[i].w;
				if (weight <= 0f)
					continue;
				centroid += (Vector3)weightDeltaPosition[i] * weight; // SUM(N * w)
				totalWeight += Mathf.Max(0f, weight); // Sum(weight);
			}
			if (totalWeight <= 0f || centroid.IsNaN() || centroid.IsInfinity())
				return Vector3.zero;
			return centroid / totalWeight;
		}

		public static Vector3 CenterOfMass(Vector3 sumOfPosition, int totalCount)
		{
			return sumOfPosition / (float)totalCount;
		}

        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
			if (a == b)
				return 0f;
            var AB = b - a;
            var AV = value - a;
            return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
        }

		public static bool TryGetInnerOverlapPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
			out Vector3 inner0, out Vector3 inner1, float threshold = 1e-6f)
		{
			inner0 = default;
			inner1 = default;
            var v0 = p1 - p0;
            var v1 = p3 - p2;
            var d0 = Vector3.Dot(v0, v1);
			if (Mathf.Abs(d0) < 1f - float.Epsilon)
				return false; // only apply on align edge.

			// Check if lines are parallel by comparing the cross product of their direction vectors to zero vector
			if (Vector3.Cross(v0, v1).sqrMagnitude > threshold)
				return false;

            // Check if lines are collinear by ensuring the vector between any point on one line and any point on the other line is parallel to the direction vectors
            Vector3 vecBetween = p2 - p0;
			if (Vector3.Cross(v0, vecBetween).sqrMagnitude > threshold)
				return false;

            // Project points onto the direction vector of the first line to find overlapping segment
            var dir = v0.normalized;
            var t0 = Vector3.Dot(p0, dir);
            var t1 = Vector3.Dot(p1, dir);
            var t2 = Vector3.Dot(p2, dir);
            var t3 = Vector3.Dot(p3, dir);

            // Ensure t0 <= t1 and t2 <= t3
            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
            }
            if (t2 > t3)
            {
                (t2, t3) = (t3, t2);
            }
            // Check if there is an overlap
            if (t1 < t2 || t3 < t0)
            {
				return false;
            }

            // Calculate overlapping segment
            var overlapStart	= Mathf.Max(t0, t2);
            var overlapEnd		= Mathf.Min(t1, t3);
            inner0 = p0 + dir * (overlapStart	- t0);
            inner1 = p0 + dir * (overlapEnd		- t0);
			return true;
        }
    }
}

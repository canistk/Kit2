using UnityEngine;
using System.Collections;

namespace Kit2
{
	public static class MatrixExtend
    {
		public static Matrix4x4 ToMatrix(this Transform transform, bool local = false)
		{
			Matrix4x4 matrix = new Matrix4x4();
			if (local)
				matrix.SetTRS(transform.localPosition, transform.localRotation, transform.localScale);
			else
				matrix.SetTRS(transform.position, transform.rotation, transform.localScale);
			return matrix;
		}

		public static void ToTransform(this Matrix4x4 matrix, Transform transform, bool local = false)
		{
			if (local)
			{
				transform.localPosition = matrix.GetPosition();
				transform.localRotation = matrix.GetRotation();
			}
			else
			{
				transform.SetPositionAndRotation(matrix.GetPosition(), matrix.GetRotation());
			}
			transform.localScale = matrix.GetScale();
		}

        public static Vector3 GetPosition(this Matrix4x4 matrix)
        {
            return matrix.GetColumn(3);
        }

		public static Matrix4x4 SetPosition(this Matrix4x4 matrix, Vector3 position)
		{
			return Matrix4x4.TRS(position, matrix.rotation, matrix.GetScale());
		}

		/// <summary>
		/// Set Position & Rotation in matrix4x4, assume scale = 1f,1f,1f.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="forward"></param>
		/// <param name="_up"></param>
		/// <returns></returns>
        public static Matrix4x4 SetTR(Vector3 pos, Vector3 forward, Vector3? _up = null)
        {
			Vector3 up		= _up.HasValue ? _up.Value : Vector3.up;
			Vector3 right	= Vector3.Cross(forward, up);
			Vector3.OrthoNormalize(ref forward, ref up, ref right);
			//DebugExtend.DrawTransform(pos, Quaternion.LookRotation(forward, up));

            return new Matrix4x4
            {
                m00 = right.x,
                m10 = right.y,
                m20 = right.z,

                m01 = up.x,
                m11 = up.y,
                m21 = up.z,

                m02 = forward.x,
                m12 = forward.y,
                m22 = forward.z,

                m03 = pos.x,
                m13 = pos.y,
                m23 = pos.z,

                m33 = 1f,
            };
        }

        /// <summary>
        /// Set Position & Rotation in matrix4x4, assume scale = 1f,1f,1f.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="forward"></param>
        /// <param name="_up"></param>
        /// <returns></returns>
        public static Matrix4x4 SetTR(Vector3 pos, Quaternion orientation)
        {
			Vector3 up		= orientation * Vector3.up;
			Vector3 right	= orientation * Vector3.right;
			Vector3 forward = orientation * Vector3.forward;
			// Matrix4x4.TRS(pos, orientation, Vector3.one);
			// DebugExtend.DrawTransform(pos, orientation);
            return new Matrix4x4
            {
                m00 = right.x,
                m10 = right.y,
                m20 = right.z,

                m01 = up.x,
                m11 = up.y,
                m21 = up.z,

                m02 = forward.x,
                m12 = forward.y,
                m22 = forward.z,

                m03 = pos.x,
                m13 = pos.y,
                m23 = pos.z,

                m33 = 1f,
            };
        }

        /// <summary>Get Local scale of matrix</summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        /// <remarks>Not able to get negative scale</remarks>
        /// <see cref="http://answers.unity3d.com/questions/402280/how-to-decompose-a-trs-matrix.html"/>
        /// <seealso cref="http://forum.unity3d.com/threads/benchmarking-mathf-with-system-math-big-performance-differences.194938/"/>
        public static Vector3 GetScale(this Matrix4x4 matrix)
        {
            Vector3 scale = new Vector3(
                matrix.GetColumn(0).magnitude,
                matrix.GetColumn(1).magnitude,
                matrix.GetColumn(2).magnitude);
			if (Vector3.Cross(matrix.GetColumn(0), matrix.GetColumn(1)).normalized != (Vector3)matrix.GetColumn(2).normalized)
			{
				scale.x *= Mathf.Sign(-1);
			}
			return scale;
		}

#if true
        /// <summary>Get Local rotation of matrix</summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        /// <see cref="http://forum.unity3d.com/threads/is-it-possible-to-get-a-quaternion-from-a-matrix4x4.142325/"/>
        public static Quaternion GetRotation(this Matrix4x4 matrix)
        {
			Vector4
				lhs = matrix.GetColumn(2),
				rhs = matrix.GetColumn(1);
			if (lhs == Vector4.zero && rhs == Vector4.zero)
				return Quaternion.identity;
			else
				return Quaternion.LookRotation(lhs, rhs);
		}
#else
        /// <summary>Get Rotation</summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        /// <see cref="http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm"/>
        /// <seealso cref="http://answers.unity3d.com/questions/11363/converting-matrix4x4-to-quaternion-vector3.html"/>
        public static Quaternion GetRotation(this Matrix4x4 m)
        {
            Quaternion q = new Quaternion();
			q.w = Mathf.Sqrt(Mathf.Max(0, 1 + matrix[0, 0] + matrix[1, 1] + matrix[2, 2])) * 0.5f;
			q.x = Mathf.Sqrt(Mathf.Max(0, 1 + matrix[0, 0] - matrix[1, 1] - matrix[2, 2])) * 0.5f;
			q.y = Mathf.Sqrt(Mathf.Max(0, 1 - matrix[0, 0] + matrix[1, 1] - matrix[2, 2])) * 0.5f;
			q.z = Mathf.Sqrt(Mathf.Max(0, 1 - matrix[0, 0] - matrix[1, 1] + matrix[2, 2])) * 0.5f;
			q.x *= Mathf.Sign(q.x * (matrix[2, 1] - matrix[1, 2]));
			q.y *= Mathf.Sign(q.y * (matrix[0, 2] - matrix[2, 0]));
			q.z *= Mathf.Sign(q.z * (matrix[1, 0] - matrix[0, 1]));
			return q;
        }
#endif
		public static Matrix4x4 SetRotation(this Matrix4x4 matrix, Quaternion rotation)
		{
			return Matrix4x4.TRS(matrix.GetPosition(), rotation, matrix.GetScale());
		}

		public static string Print(this Matrix4x4 m)
		{
			return $"Log\n" +
				$"{m.GetRow(0):F2}\n" +
				$"{m.GetRow(1):F2}\n" +
				$"{m.GetRow(2):F2}\n" +
				$"{m.GetRow(3):F2}";
		}

        public static string PrintCompare(this Matrix4x4 m, Matrix4x4 o)
        {
            return $"Compare\n" +
                $"{m.GetRow(0):F2}\t{o.GetRow(0):F2}\n" +
                $"{m.GetRow(1):F2}\t{o.GetRow(1):F2}\n" +
                $"{m.GetRow(2):F2}\t{o.GetRow(2):F2}\n" +
                $"{m.GetRow(3):F2}\t{o.GetRow(3):F2}\n";
        }

        /// <summary>Transforms position from local space to world space.</summary>
        /// <param name="matrix"></param>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        /// <remarks>As same as Transform.TransformPoint
        /// <see cref="http://docs.unity3d.com/412/Documentation/ScriptReference/Transform.TransformPoint.html"/>
        /// <seealso cref="https://en.wikipedia.org/wiki/Transformation_matrix"/>
        /// </remarks>
        public static Vector3 TransformPoint(this Matrix4x4 matrix, Vector3 localPosition)
		=> matrix.MultiplyPoint(localPosition);

		/// <summary>Transforms position from world space to local space.</summary>
		/// <param name="matrix"></param>
		/// <param name="worldPosition"></param>
		/// <returns></returns>
		/// <remarks>http://answers.unity3d.com/questions/1124805/world-to-local-matrix-without-transform.html</remarks>
		public static Vector3 InverseTransformPoint(this Matrix4x4 matrix, Vector3 worldPosition)
		=> matrix.inverse.MultiplyPoint(worldPosition);

		/// <summary>Transforms direction from local space to world space.
		/// This operation is not affected by scale or position of the transform.The returned vector has the same length as direction.
		/// You should use Transform.TransformPoint for the conversion if the vector represents a position rather than a direction.</summary>
		/// <param name="matrix"></param>
		/// <param name="localDirection"></param>
		/// <returns></returns>
		public static Vector3 TransformDirection(this Matrix4x4 matrix, Vector3 localDirection)
		=> matrix.TransformVector(localDirection);

		/// <summary>
		/// Transforms a direction from world space to local space. The opposite of Transform.TransformDirection.
		/// This operation is unaffected by scale.
		/// You should use Transform.InverseTransformPoint if the vector represents a position in space rather than a direction.</summary>
		/// <param name="matrix"></param>
		/// <param name="worldDirection"></param>
		/// <returns></returns>
		public static Vector3 InverseTransfromDirection(this Matrix4x4 matrix, Vector3 worldDirection)
		=> matrix.InverseTransformVector(worldDirection);

		/// <summary>Transforms vector from local space to world space.
		/// This operation is not affected by position of the transform, but it is affected by scale.The returned vector may have a different length than vector.</summary>
		/// <param name="matrix"></param>
		/// <param name="localVector"></param>
		/// <returns></returns>
		public static Vector3 TransformVector(this Matrix4x4 matrix, Vector3 localVector)
		=> matrix.MultiplyVector(localVector);

		/// <summary>Transforms a vector from world space to local space. The opposite of Transform.TransformVector.
		/// This operation is affected by scale.</summary>
		/// <param name="matrix"></param>
		/// <param name="worldVector"></param>
		/// <returns></returns>
		public static Vector3 InverseTransformVector(this Matrix4x4 matrix, Vector3 worldVector)
		=> matrix.inverse.MultiplyVector(worldVector);
	}
}

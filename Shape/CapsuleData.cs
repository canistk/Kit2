using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2.Shape
{
	public enum eCapsuleDirection
	{
		XAxis = 0,
		YAxis = 1,
		ZAxis = 2,
	}

	[System.Serializable]
	public struct CapsuleConfig
    {
		public static readonly CapsuleConfig Default = new CapsuleConfig
		{
			center = Vector3.zero,
			radius = 1f,
			height = 1f,
			direction = eCapsuleDirection.YAxis,
		};
		public Vector3 center;
		public float radius;
		public float height;
		public float maxDistance;
		public eCapsuleDirection direction;
	}

	/// <summary>A middle class to represent the Capsule in pure data format.
	/// respect Unity's Capsule Collider behaviour.</summary>
	public struct CapsuleData : IEqualityComparer<CapsuleData>
	{
		public static readonly CapsuleData Zero = default(CapsuleData);
		private Vector3 m_Center;
		public Vector3 center { get { return m_Center; } set { IsDirty(m_Center, value); m_Center = value; } }
		private float m_Radius;
		public float radius { get { return m_Radius; } set { IsDirty(m_Radius, value); m_Radius = value; } }
		private float m_Height;
		public float height { get { return m_Height; } set { IsDirty(m_Height, value); m_Height = value; } }
		private int m_Direction;
		public int direction { get { return m_Direction; } set { IsDirty(m_Direction, value); m_Direction = value; } }
		private bool m_Dirty;
		private void IsDirty<T>(T a, T b)
		{
			if (!m_Dirty && !a.Equals(b)) m_Dirty = true;
		}

		private Matrix4x4 m_Matrix;
		public Matrix4x4 matrix
		{
			get
			{
				if (m_Dirty)
					UpdateReference();
				return m_Matrix;
			}
		}

		private Vector3 m_Position;
		public Vector3 position
		{
			get => m_Position;
			set { m_Position = value; m_Dirty = true; }
		}

		private Quaternion m_Rotation;
		public Quaternion rotation
		{
			get
			{
				if (m_Rotation.IsInvalid())
				{
					m_Rotation = Quaternion.identity;
					m_Dirty = true;
				}
				return m_Rotation;
			}
			set { m_Rotation = value; m_Dirty = true; }
		}

		public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
		{
			m_Matrix.SetTRS(position, rotation, Vector3.one);
			m_Dirty = true;
		}

		public CapsuleData(CapsuleCollider colliderRef)
			: this(colliderRef.transform, colliderRef.center, colliderRef.radius, colliderRef.height, (eCapsuleDirection)colliderRef.direction)
		{
			if (colliderRef == null)
				throw new System.NullReferenceException();
		}

		public CapsuleData(Transform _transform, Vector3 center, float radius, float height, eCapsuleDirection direction = eCapsuleDirection.YAxis)
			: this(_transform.position, _transform.rotation, center, radius, height, direction)
		{
			if (_transform == null)
				throw new System.NullReferenceException();
		}

		public CapsuleData(Vector3 _pos, Quaternion _rot, Vector3 center,
			float radius, float height, eCapsuleDirection direction = eCapsuleDirection.YAxis)
		{
			m_Center = center;
			m_Radius = radius;
			m_Height = height;
			m_Direction = (int)direction; // 0,1,2
			m_Position = _pos;
			m_Rotation = _rot.IsInvalid() ? Quaternion.identity : _rot;
			m_Dirty = true;

			if (_pos.IsNaN())
				throw new UnityException("Invalid position - NaN");
			else if (_pos.IsInfinity())
				throw new UnityException("Invalid position - infinity");
			m_Matrix = Matrix4x4.TRS(_pos, _rot, Vector3.one);
			m_P0 = m_P1 = Vector3.zero;
		}

		private Vector3 m_P0, m_P1;
		public Vector3 p0
		{
			get
			{
				if (m_Dirty)
					UpdateReference();
				return m_P0;
			}
		}
		public Vector3 p1
		{
			get
			{
				if (m_Dirty)
					UpdateReference();
				return m_P1;
			}
		}

		public Vector3 p0Outter
		{
			get
			{
				if (m_Dirty)
					UpdateReference();
				var dir = direction switch
				{
					0 => Vector3.right,		// X-axis
					1 => Vector3.up,		// Y-axis
					2 => Vector3.forward,	// Z-axis
					_ => throw new System.NotImplementedException(),
				};
				return m_P0 + rotation * (dir * radius);
			}
		}

		public Vector3 p1Outter
		{
			get
			{
				if (m_Dirty)
					UpdateReference();
                var dir = direction switch
                {
                    0 => Vector3.left,		// X-axis
                    1 => Vector3.down,		// Y-axis
                    2 => Vector3.back,		// Z-axis
                    _ => throw new System.NotImplementedException(),
                };
                return m_P1 + rotation * (dir * radius);
			}
		}

		private void UpdateReference()
		{
			if (m_Dirty)
			{
				m_Matrix.SetTRS(position, rotation, Vector3.one);
				CalcPoints(m_Matrix, direction, center, height, radius, out m_P0, out m_P1);
			}
			m_Dirty = false;
		}

		/// <summary>Calculate Point 0,1 for capsule.</summary>
		/// <param name="matrix"></param>
		/// <param name="direction"><see cref="eCapsuleDirection"/></param>
		/// <param name="center"></param>
		/// <param name="height"></param>
		/// <param name="radius"></param>
		/// <param name="p0"></param>
		/// <param name="p1"></param>
		/// <exception cref="System.NotImplementedException"></exception>
		public static void CalcPoints(
			in Matrix4x4 matrix,
			int direction, 
			Vector3 center, float height, float radius, out Vector3 p0, out Vector3 p1)
        {
            // invalid setting :
            // matching Unity's Capsule Collider behavior,
            if (height < radius * 2f)
            {
                p0 = p1 = matrix.MultiplyPoint3x4(center);
				return;
            }
            
            float half = Mathf.Clamp(height * 0.5f - radius, 0f, float.PositiveInfinity);

			// math hack calculation for align Y-axis with direction == 1
            if (direction == 1 && matrix.rotation * Vector3.up == Vector3.up)
            {
				// When perfect align world up & it's Y-axis
				// 1) escape the matrix calculation since it's align world up.
				// 2) optimize/reduce the vector3 construction by doing Y-axis +/-
				p0 = matrix.GetPosition() + center; p0.y += half;
                p1 = p0; p1.y -= half + half;
				return;
            }

			// Full calculation
            switch (direction)
            {
                case 0: // X-axis
                    p0 = center; p0.x += half;
                    p1 = center; p1.x -= half;
                    break;
                case 1: // Y-axis
                    p0 = center; p0.y += half;
                    p1 = center; p1.y -= half;
                    break;
                case 2: // Z-axis
                    p0 = center; p0.z += half;
                    p1 = center; p1.z -= half;
                    break;
                default:
                    throw new System.NotImplementedException();
            }
            p0 = matrix.MultiplyPoint3x4(p0);
            p1 = matrix.MultiplyPoint3x4(p1);
        }

		public bool Equals(CapsuleData x, CapsuleData y)
		{
			return x.position == y.position &&
				x.rotation == y.rotation &&
				x.center == y.center &&
				x.height == y.height &&
				x.radius == y.radius &&
				x.direction == y.direction;
		}

		public int GetHashCode(CapsuleData obj)
		{
			return GenerateHashCode(
				center.GetHashCode(),
				height.GetHashCode(),
				radius.GetHashCode(),
				direction,
				position.GetHashCode(),
				rotation.GetHashCode());
		}

		/// <summary>
		/// generate effective hash code
		/// - Non unique combie hash. Same hashcode didn't mean same object.
		/// - quick collision checking.
		/// <see cref="https://stackoverflow.com/questions/11742593/what-is-the-hashcode-for-a-custom-class-having-just-two-int-properties"/>
		/// </summary>
		/// <param name="args">another hashcode for reference type fields</param>
		/// <returns>the hash code of the sum of all those args.</returns>
		private int GenerateHashCode(params int[] args)
		{
			// 17 & 31 are prime
			int hash = 17;
			int i = args.Length;
			while (i-- > 0)
			{
				hash = hash * 31 + args[i];
			}
			return hash;
		}

		public static explicit operator CapsuleData(CapsuleCollider collider)
		{
			return new CapsuleData(collider);
		}
        public static CapsuleData operator +(CapsuleData o, Vector3 v) => new CapsuleData(o.position + v, o.rotation, o.center, o.radius, o.height, (eCapsuleDirection)o.direction);
        public static CapsuleData operator -(CapsuleData o, Vector3 v) => new CapsuleData(o.position - v, o.rotation, o.center, o.radius, o.height, (eCapsuleDirection)o.direction);

		public static Vector3 operator - (CapsuleData a, CapsuleData b) => a.position - b.position;
    }
}
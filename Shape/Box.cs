using UnityEngine;
namespace Kit2.Shape
{
    [System.Serializable]
    public struct Box
    {
        #region Members
        public Vector3 origin { get; set; }
        private Quaternion m_Rotation;
        private Vector3 m_FTL, m_FTR, m_FBL, m_FBR;
        #endregion Members

        #region Constructor
        public Box(Vector3 origin, Vector3 halfExtents, Quaternion rotation) : this()
        {
            this.origin = origin;
            if (rotation == default(Quaternion))
                rotation = Quaternion.identity;
            m_Rotation = rotation;
            m_FTL = m_Rotation * new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
            m_FTR = m_Rotation * new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            m_FBL = m_Rotation * new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
            m_FBR = m_Rotation * new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
        }

        /// <summary>From vector between 2 points, use the vector as "Depth" of the box</summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Box(Vector3 from, Vector3 to, float width, float height)
            : this(Vector3.Lerp(to, from, 0.5f),
                new Vector3(width * 0.5f, height * 0.5f, (to - from).magnitude * 0.5f),
                (to == from ? Quaternion.identity : Quaternion.LookRotation(to - from)))
        { }

        public Box(Vector3 origin, Vector3 halfExtents)
            : this(origin, halfExtents, Quaternion.identity)
        { }

        /// <summary>Build from <see cref="Bounds"/></summary>
        /// <param name="b"></param>
        public Box(Bounds b) : this(b.center, b.extents) { }

        /// <summary>Build from <see cref="BoundsInt"/></summary>
        /// <param name="b"></param>
        public Box(BoundsInt b) : this(b.center, (Vector3)b.size * .5f) { }

        /// <summary>Build from <see cref="BoxCollider"/></summary>
        /// <param name="b"></param>
        public Box(BoxCollider b)
            : this(b.transform.TransformPoint(b.center), b.size * .5f, b.transform.rotation)
        { }
        #endregion Constructor

        public static implicit operator Box(BoxCollider b) => new Box(b);
        public static implicit operator Box(Bounds b) => new Box(b);
        public static implicit operator Box(BoundsInt b) => new Box(b);

        public Vector3 localFrontTopLeft        => m_FTL;
        public Vector3 localFrontTopRight       => m_FTR;
        public Vector3 localFrontBottomLeft     => m_FBL;
        public Vector3 localFrontBottomRight    => m_FBR;
        public Vector3 localBackTopLeft         => -m_FBR;
        public Vector3 localBackTopRight        => -m_FBL;
        public Vector3 localBackBottomLeft      => -m_FTR;
        public Vector3 localBackBottomRight     => -m_FTL;

        public Vector3 frontTopLeft             => localFrontTopLeft     + origin;
        public Vector3 frontTopRight            => localFrontTopRight    + origin;
        public Vector3 frontBottomLeft          => localFrontBottomLeft  + origin;
        public Vector3 frontBottomRight         => localFrontBottomRight + origin;
        public Vector3 backTopLeft              => localBackTopLeft      + origin;
        public Vector3 backTopRight             => localBackTopRight     + origin;
        public Vector3 backBottomLeft           => localBackBottomLeft   + origin;
        public Vector3 backBottomRight          => localBackBottomRight  + origin;
        
        public Quaternion rotation
        {
            get
            {
                if (m_Rotation == default(Quaternion))
                    m_Rotation = Quaternion.identity;
                return m_Rotation;
            }
            set
            {
                if (m_Rotation != value)
                {
                    if (value == default(Quaternion))
                        value = Quaternion.identity;
                    m_Rotation = value;
                    m_FTL = m_Rotation * m_FTL;
                    m_FTR = m_Rotation * m_FTR;
                    m_FBL = m_Rotation * m_FBL;
                    m_FBR = m_Rotation * m_FBR;
                }
            }
        }
        public Matrix4x4 localToWorldMatrix     => Matrix4x4.TRS(origin, rotation, Vector3.one);
        
        public float Width()    => (localFrontTopLeft - localFrontTopRight).magnitude;
        public float Height()   => (localFrontTopLeft - localFrontBottomLeft).magnitude;
        public float Depth()    => (localFrontTopLeft - localBackTopLeft).magnitude;
        public Vector3 Size()   => new Vector3(Width(), Height(), Depth());
        public Vector3 HalfExtends() => Size() * .5f;

        public Vector3 forward  => rotation * Vector3.forward;
        public Vector3 back     => rotation * Vector3.back;
        public Vector3 left     => rotation * Vector3.left;
        public Vector3 right    => rotation * Vector3.right;
        public Vector3 up       => rotation * Vector3.up;
        public Vector3 down     => rotation * Vector3.down;

        public Plane GetFrontPlane()    => new Plane(forward,   Depth()     * -.5f);
        public Plane GetBackPlane()     => new Plane(back,      Depth()     * -.5f);
        public Plane GetLeftPlane()     => new Plane(left,      Width()     * -.5f);
        public Plane GetRightPlane()    => new Plane(right,     Width()     * -.5f);
        public Plane GetUpPlane()       => new Plane(up,        Height()    * -.5f);
        public Plane GetDownPlane()     => new Plane(down,      Height()    * -.5f);
    }
}
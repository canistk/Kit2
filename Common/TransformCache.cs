using UnityEngine;

namespace Kit2
{
	[System.Flags]
	public enum eTransformCacheRef
	{
		// None = 0,
		LocalPosition = 1 << 0,
		LocalRotation = 1 << 1,
		LocalScale = 1 << 2,
		AnchorMin = 1 << 3,
		AnchorMax = 1 << 4,
		SizeDelta = 1 << 5,
		Pivot = 1 << 6,
		Parent = 1 << 7,
		Default = LocalPosition | LocalRotation | LocalScale | AnchorMin | AnchorMax | SizeDelta | Pivot,
		Full = Default | Parent,
		Lite = LocalPosition | LocalRotation,
	}

	[System.Serializable]
	public struct TransformCache
	{
		public static readonly TransformCache Default = new TransformCache
		{
			m_TransformRef = eTransformCacheRef.Default,
			m_Parent = null,
			m_LocalPosition = Vector3.zero,
			m_LocalRotation = Quaternion.identity,
			m_LocalScale = Vector3.one,

			// UI's
			m_AnchorMin = Vector3Half,
			m_AnchorMax = Vector3Half,
			m_SizeDelta = Vector3100,
			m_Pivot = Vector3Half,
		};
		private static readonly Vector3 Vector3Half = new Vector3(.5f, .5f, .5f);
		private static readonly Vector3 Vector3100 = new Vector3(100f, 100f, 100f);

		public eTransformCacheRef m_TransformRef; // = eTransformRef.Default;

		// Transform
		public Transform m_Parent; // = null;
		public Vector3 m_LocalPosition; // = Vector3.zero;
		public Vector3 position => m_Parent ? m_Parent.TransformPoint(m_LocalPosition) : m_LocalPosition;

		public Quaternion m_LocalRotation; // = Quaternion.identity;
		public Quaternion rotation => m_Parent ? m_Parent.rotation * m_LocalRotation : m_LocalRotation;
		public Vector3 m_LocalScale; // = Vector3.one;
		public Vector3 lossyScale
		{
			get
			{
				if (m_Parent)
				{
					/// <see cref="https://answers.unity.com/questions/456669/what-dose-lossyscale-actually-means.html"/>
					return new Vector3(m_LocalScale.x * m_Parent.lossyScale.x, m_LocalScale.y * m_Parent.lossyScale.y, m_LocalScale.z * m_Parent.lossyScale.z);
				}
				else
				{
					return m_LocalScale;
				}
			}
		}

		// RectTransform + Default values.
		public Vector2 m_AnchorMin;// = Vector3.one * .5f;
		public Vector2 m_AnchorMax;// = Vector3.one * .5f;
		public Vector2 m_SizeDelta;// = Vector3.one * 100f;
		public Vector2 m_Pivot;// = Vector3.one * .5f;

		public TransformCache(Transform sample, eTransformCacheRef transformRef = eTransformCacheRef.Full)
		{
			if (sample == null)
				throw new System.NullReferenceException();
			m_Parent = sample.parent;
			m_TransformRef = transformRef;
			if (sample is RectTransform)
			{
				RectTransform obj = (RectTransform)sample;
				m_LocalPosition = obj.anchoredPosition3D;
				m_LocalRotation = obj.localRotation;
				m_LocalScale = obj.localScale;
				m_AnchorMin = obj.anchorMin;
				m_AnchorMax = obj.anchorMax;
				m_SizeDelta = obj.sizeDelta;
				m_Pivot = obj.pivot;
			}
			else
			{
				m_LocalPosition = sample.localPosition;
				m_LocalRotation = sample.localRotation;
				m_LocalScale = sample.localScale;

				m_AnchorMin = Vector3Half;
				m_AnchorMax = Vector3Half;
				m_SizeDelta = Vector3100;
				m_Pivot = Vector3Half;
			}
		}

		public TransformCache(Vector3 pos, Quaternion rot)
		{
			m_TransformRef = eTransformCacheRef.Lite;
			m_LocalPosition = pos;
			m_LocalRotation = rot;

			m_Parent = null;
			m_LocalScale = Vector3.one;
			m_AnchorMin = Vector3Half;
			m_AnchorMax = Vector3Half;
			m_SizeDelta = Vector3100;
			m_Pivot = Vector3Half;
		}

		public void AssignTo(Transform target, eTransformCacheRef tranRef)
		{
			bool isRect = target.transform is RectTransform;
			if ((tranRef & eTransformCacheRef.Parent) != 0)
				target.SetParent(m_Parent, true);
			if (!isRect && (tranRef & eTransformCacheRef.LocalPosition) != 0)
				target.transform.localPosition = m_LocalPosition;
			if ((tranRef & eTransformCacheRef.LocalRotation) != 0)
				target.transform.localRotation = m_LocalRotation;
			if ((tranRef & eTransformCacheRef.LocalScale) != 0)
				target.transform.localScale = m_LocalScale;

			if (isRect)
			{
				RectTransform obj = (RectTransform)target.transform;
				if ((tranRef & eTransformCacheRef.AnchorMin) != 0)
					obj.anchorMin = m_AnchorMin;
				if ((tranRef & eTransformCacheRef.AnchorMax) != 0)
					obj.anchorMax = m_AnchorMax;
				if ((tranRef & eTransformCacheRef.SizeDelta) != 0)
					obj.sizeDelta = m_SizeDelta;
				if ((tranRef & eTransformCacheRef.Pivot) != 0)
					obj.pivot = m_Pivot;
				if ((tranRef & eTransformCacheRef.LocalPosition) != 0)
					obj.anchoredPosition3D = m_LocalPosition;
			}
		}

		public static TransformCache Lerp(TransformCache from, TransformCache to, float pt)
		{
			return new TransformCache
			{
				m_Parent = (pt < .5f) ? from.m_Parent : to.m_Parent, // TODO: test different parent.
				m_TransformRef = (pt < .5f) ? from.m_TransformRef : to.m_TransformRef,
				m_AnchorMin = Vector2.Lerp(from.m_AnchorMin, to.m_AnchorMin, pt),
				m_AnchorMax = Vector2.Lerp(from.m_AnchorMax, to.m_AnchorMax, pt),
				m_SizeDelta = Vector2.Lerp(from.m_SizeDelta, to.m_SizeDelta, pt),
				m_Pivot = Vector2.Lerp(from.m_Pivot, to.m_Pivot, pt),
				m_LocalPosition = Vector3.Lerp(from.m_LocalPosition, to.m_LocalPosition, pt),
				m_LocalRotation = Quaternion.Lerp(from.m_LocalRotation, to.m_LocalRotation, pt),
				m_LocalScale = Vector3.Lerp(from.m_LocalScale, to.m_LocalScale, pt)
			};
		}
	}
}
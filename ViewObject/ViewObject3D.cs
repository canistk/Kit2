using UnityEngine;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Kit2
{
	public class ViewObject3D : ViewObject
	{
		[Header("Config")]
		[SerializeField] bool m_IgnoreTimeScale = true;

		[Header("Disappear")]
		public TransformCache m_DisappearRect = TransformCache.Default;
		[SerializeField, RectRange(0f, 0f, 1f, 1f)] protected AnimationCurve m_DisappearCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		[SerializeField] protected float m_DisappearDuration = 0.25f;

		[Header("Appear")]
		public TransformCache m_AppearedRect = TransformCache.Default;
		[SerializeField, RectRange(0f, 0f, 1f, 1f)] protected AnimationCurve m_AppearedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		[SerializeField] protected float m_AppearDuration = 0.25f;

		protected override void Reset()
		{
			base.Reset();
			m_DisappearRect = new TransformCache(transform, m_AppearedRect.m_TransformRef);
			m_AppearedRect = new TransformCache(transform, m_DisappearRect.m_TransformRef);
		}

		protected override async void InternalStartAppearTask(CancellationToken cancelToken)
		{
			await _Animation();
		}

		protected override void ForceSetToAppeared()
		{
			m_AppearedRect.AssignTo(transform, m_AppearedRect.m_TransformRef);
			base.ForceSetToAppeared();
		}

		protected override async void InternalStartDisappear(CancellationToken cancelToken)
		{
			await _Animation();
		}

		protected override void ForceSetToDisappeared()
		{
			m_DisappearRect.AssignTo(transform, m_DisappearRect.m_TransformRef);
			base.ForceSetToDisappeared();
		}

		private async Task _Animation()
		{
			bool isAppearing = ViewStatus == eViewStatus.Appearing;
			bool isDisappearing = ViewStatus == eViewStatus.Disappearing;
			AnimationCurve curve = isAppearing ? m_AppearedCurve : m_DisappearCurve;
			float duration = isAppearing ? m_AppearDuration : m_DisappearDuration;
			float timeStart = Time.unscaledTime;

			if (m_ExecultType == eExecultType.EstimatePosition &&
				(isAppearing || isDisappearing))
			{
				// adjust current start time by widget position.
				float diff = LocateClosestStartTimeByPosition();
				timeStart -= diff;
			}

			float timeEndScaled = timeStart + (m_IgnoreTimeScale ? duration : duration * Time.timeScale);
			while (timeEndScaled >= Time.unscaledTime)
			{
				float pt = duration <= 0 ? 1f : Mathf.Clamp01((Time.unscaledTime - timeStart) / (timeEndScaled - timeStart));
				if (isDisappearing)
					pt = 1f - pt;
				/// Note: disappear curve(pt) start from right to left side (Curve),
				/// because we usually want to apply same curve on appear/disappear
				/// but expecting inverase behaviour

				float interpolate = curve.Evaluate(pt);
				InternalAnimationUpdate(m_DisappearRect, m_AppearedRect, interpolate);

				await Task.Yield(); // yield to next frame
				timeEndScaled = timeStart + (m_IgnoreTimeScale ? duration : duration * Time.timeScale);
			}

			EndOfAnimationUpdate(isAppearing ? m_AppearedRect : m_DisappearRect);
		}

		/// <summary>Extendable method for subclass to override behaviour <see cref="_Animation"/></summary>
		/// <param name="disappear"></param>
		/// <param name="appear"></param>
		/// <param name="interpolate"></param>
		protected virtual void InternalAnimationUpdate(TransformCache disappear, TransformCache appear, float interpolate)
		{
			TransformCache cache = TransformCache.Lerp(disappear, appear, interpolate);
			cache.AssignTo(transform, appear.m_TransformRef);
		}

		/// <summary>Extendable method for subclass to override behaviour <see cref="_Animation"/>
		/// call at the end of current animation</summary>
		/// <param name="final"></param>
		protected virtual void EndOfAnimationUpdate(TransformCache final)
		{
			final.AssignTo(transform, final.m_TransformRef);
		}

		/// <summary>Assume current postoAlphaition was right between start/end points.</summary>
		/// <returns>the closest position/time belong to path.</returns>
		private float LocateClosestStartTimeByPosition()
		{
			bool isAppearing = ViewStatus == eViewStatus.Appearing;
			bool isDisappearing = ViewStatus == eViewStatus.Disappearing;
			Debug.Assert(isAppearing || isDisappearing, "Invalid method call.");
			AnimationCurve curve = isAppearing ? m_AppearedCurve : m_DisappearCurve;
			Vector3 localPos = transform.localPosition; // assume UGUI only using local position.
			TransformCache from = m_DisappearRect;
			TransformCache to = m_AppearedRect;
			float duration = curve[curve.keys.Length - 1].time;
			float step = duration * 0.001f;
			float distanceSqr = (from.m_LocalPosition - to.m_LocalPosition).sqrMagnitude;
			float threshold = distanceSqr * 0.001f; // 1/100 percent based on distance.

			if (localPos == to.m_LocalPosition || localPos == from.m_LocalPosition)
				return 0f; // double call in same frame, common case Enable + SetActive(true)
			if (isAppearing && (localPos - from.m_LocalPosition).sqrMagnitude <= threshold)
				return 0f; // Too close to Disappear pos
			else if (isDisappearing && (localPos - to.m_LocalPosition).sqrMagnitude <= threshold)
				return 0f; // Too close to Appear pos
			else if (from.m_LocalPosition != to.m_LocalPosition)
			{
				// Where current point was not in between of start/end points.
				Vector3 v1 = from.m_LocalPosition - localPos;
				Vector3 v2 = localPos - to.m_LocalPosition;
				float dot = Vector3.Dot(v1.normalized, v2.normalized);
				if (Mathf.Abs(dot) < 0.9f)
				{
					Debug.Log("Searching start time from invalid position, aborted action. " +
						dot, this);

					return 0f;
				}
			}

			int iterCnt = 0;
			for (float time = 0f; time <= duration; time += step)
			{
				iterCnt++;
				TransformCache uiRect = TransformCache.Lerp(from, to, curve.Evaluate(time));
				if ((uiRect.m_LocalPosition - localPos).sqrMagnitude <= threshold)
				{
					if (isDisappearing)
						return duration - time;
					else
						return time;
				}
			}

			Debug.LogWarning("fail to locate start point in loop, IterCnt =" + iterCnt, this);
			return 0f;
		}
	}
}
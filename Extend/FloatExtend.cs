using System;
using System.Collections.Generic;

namespace Kit2
{
	public static class FloatExtend
	{
		/// <summary>Shortcut for <see cref="UnityEngine.Mathf.Approximately(float, float)"/></summary>
		/// <param name="self"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public static bool Approximately(this float self, float target)
		{
			return UnityEngine.Mathf.Approximately(self, target);
		}

		/// <summary>Roughly test for float,
		/// <see cref="http://floating-point-gui.de/errors/comparison/"/></summary>
		/// <param name="self"></param>
		/// <param name="target"></param>
		/// <param name="threshold"></param>
		/// <returns>return true when float's are close enough to each other.</returns>
		public static bool EqualRoughly(this float self, float target, float threshold = float.Epsilon)
		{
			return Math.Abs(self - target) < threshold;
		}
		/// <summary>Get Number after scale.</summary>
		/// <param name="self"></param>
		/// <param name="fromMin"></param>
		/// <param name="fromMax"></param>
		/// <param name="toMin"></param>
		/// <param name="toMax"></param>
		/// <returns></returns>
		/// <see cref="http://stackoverflow.com/questions/11121012/how-to-scale-down-the-values-so-they-could-fit-inside-the-min-and-max-values"/>
		[Obsolete("Remap")]
		public static float Fit(this float self, float fromMin, float fromMax, float toMin, float toMax)
		{
			self = Clamp(self, fromMin, fromMax);
			return toMin + ((toMax - toMin) / (fromMax - fromMin)) * (self - fromMin);
		}

        /// <summary>Faster equation for usually wanted to scale down to 0f~1f.</summary>
        /// <param name="self"></param>
        /// <param name="fromMin"></param>
        /// <param name="fromMax"></param>
        /// <returns></returns>
        [Obsolete("replace by InvLerp")]
		public static float Fit01(this float self, float fromMin, float fromMax)
		{
			return InvLerp(fromMin, fromMax, self);
		}

		public static float Lerp(float outMin, float outMax, float t)
        {
			t = Clamp(t, 0f, 1f);
			return LerpUnclamp(outMin, outMax, t);
		}

		public static float LerpUnclamp(float outMin, float outMax, float t)
        {
			return (1.0f - t) * outMin + outMax * t;
        }

        public static float InvLerp(float inMin, float inMax, float value)
        {
			value = Clamp(value, inMin, inMax);
            return InvLerpUnclamp(inMin, inMax, value);
        }

        public static float InvLerpUnclamp(float inMin, float inMax, float value)
        {
			return (value - inMin) / (inMax - inMin); // normalize
        }

		/// <summary>Mapping the number from one range to another.</summary>
		/// <remarks>
		/// <see cref="https://forum.unity.com/threads/re-map-a-number-from-one-range-to-another.119437/"/>
		/// <see cref="https://youtu.be/NzjF1pdlK7Y?t=1740"/>
		/// </remarks>
		/// <param name="value"></param>
		/// <param name="inMin"></param>
		/// <param name="inMax"></param>
		/// <param name="outMin"></param>
		/// <param name="outMax"></param>
		/// <returns></returns>
		public static float Remap(this float value, float inMin, float inMax, float outMin, float outMax)
        {
			float t = InvLerp(inMin, inMax, value);
			return LerpUnclamp(outMin, outMax, t);
        }

		private static KeyValuePair<float /*time*/, int/*hash*/> s_LastWarning = default;
		private static void WarningPeriod(string msg)
		{
			var hash = msg.GetHashCode();
			var now = UnityEngine.Time.realtimeSinceStartup;
            if (s_LastWarning.Value == hash && now - s_LastWarning.Key < 10f)
				return;

			s_LastWarning = new(now, hash);
            UnityEngine.Debug.LogWarning(msg);
        }

		private static float Clamp(float value, float min, float max)
		{
#if false // UNITY_EDITOR
			if (value < min)
			{
				WarningPeriod($"The input value {value}, are smaller then giving min {min}");
				value = min;
			}
			else if (value > max)
			{
                WarningPeriod($"The input value {value}, are larger then giving max {max}");
				value = max;
			}
#else
			value = value < min ? min : value > max ? max : value;
#endif
			return value;
		}
	}
}
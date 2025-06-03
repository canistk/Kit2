using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
    public static class ColorExtend
    {
        #region basic
        /// <summary>Clone & modify alpha value, This method alloc double memory.</summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns>return a new color with new alpha value.</returns>
        public static Color CloneAlpha(this Color self, float value)
        {
            self.a = value;
            return self;
        }

        public static bool Approximately(this Color self, Color target)
        {
            return
                Mathf.Approximately(self.r, target.r) &&
                Mathf.Approximately(self.g, target.g) &&
                Mathf.Approximately(self.b, target.b) &&
                Mathf.Approximately(self.a, target.a);
        }

        public static bool EqualRoughly(this Color self, Color target, float threshold = float.Epsilon)
        {
            return
                self.r.EqualRoughly(target.r, threshold) &&
                self.g.EqualRoughly(target.g, threshold) &&
                self.b.EqualRoughly(target.b, threshold) &&
                self.a.EqualRoughly(target.a, threshold);
        }

        public static Color TryParse(string RGBANumbers)
        {
            // clear up
            string[] param = RGBANumbers.Trim().Split(',');
            if (param == null || param.Length == 0)
                return Color.black;

            int pt = 0;
            int count = 0;
            bool Is255 = false;
            float[] rgba = new float[4] { 0f, 0f, 0f, 1f };

            while (param.Length > pt && count <= 4)
            {
                float tmp;
                if (float.TryParse(param[pt], out tmp))
                {
                    rgba[count] = tmp;
                    count++;
                    if (tmp > 1f) Is255 = true;
                }
                pt++;
            }

            // hotfix for 255
            if (Is255)
            {
                for (int i = 0; i < 3; i++) { rgba[i] /= 255f; }
                rgba[3] = Mathf.Clamp(rgba[3], 0f, 1f);
            }
            return new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
        }

        public static Color Random(this Color self)
        {
            return Random();
        }

        public static Color Random()
        {
            return RandomRange(Color.black, Color.white);
        }

        public static Color RandomRange(this Color self, Color min, Color max)
        {
            return RandomRange(min, max);
        }

        public static Color RandomRange(Color min, Color max)
        {
            return new Color(
                UnityEngine.Random.Range(min.r, max.r),
                UnityEngine.Random.Range(min.g, max.g),
                UnityEngine.Random.Range(min.b, max.b),
                UnityEngine.Random.Range(min.a, max.a));
        }
        #endregion

        #region Color map
        /// <summary><see cref="StringExtend.ToRichText(string, Color)"/></summary>
        /// <param name="color"></param>
        /// <param name="_string"></param>
        /// <returns></returns>
        public static string ToRichText(this Color color, string _string)
        {
            string rgba = ColorUtility.ToHtmlStringRGBA(color);
            return $"<color=#{rgba}>{_string}</color>";
        }

        /// <summary>Get the jet color (based on the Jet color map)</summary>
        /// <param name="val">normalized between 0f and 1f</param>
        /// <see cref="https://cn.mathworks.com/help/matlab/ref/jet.html"/>
        public static Color GetJetColor(float val)
        {
            float fourValue = 4.0f * val;
            float red = Mathf.Min(fourValue - 1.5f, -fourValue + 4.5f);
            float green = Mathf.Min(fourValue - 0.5f, -fourValue + 3.5f);
            float blue = Mathf.Min(fourValue + 0.5f, -fourValue + 2.5f);
            Color newColor = new Color();
            newColor.r = Mathf.Clamp01(red);
            newColor.g = Mathf.Clamp01(green);
            newColor.b = Mathf.Clamp01(blue);
            newColor.a = 1;
            return newColor;
        }

        private static Vector3[] s_Freq =
        {
            new(2f,  1f, 1f),
            new(0f,  2f, -2f),
            new(-2f, 1f, 2f),
            new(4f, 1f, 3f),
        };
        private static Vector3[] s_Phase =
        {
            new(60f, 0f, 120f),
            new(0f, 120f, 60f),
            new(180f, 0f, 180f),
            new(120f, 100f, 30f),
        };
        public static Color GetDimBlueColor(float val)
            => GetSinColor(val, s_Freq[0], s_Phase[0]);

        public static Color GetGreen2BlueColor(float val)
            => GetSinColor(val, s_Freq[1], s_Phase[1]);

        public static Color GetGreen2YellowColor(float val)
            => GetSinColor(val, s_Freq[2], s_Phase[2]);

        public static Color GetRed2BlueColor(float val)
            => GetSinColor(val, s_Freq[3], s_Phase[3]);

        public static Color GetSinColor(float val01, Vector3 freqRGB, Vector3 phaseRGB)
        {
            const float bias = 1e-6f;
            //val01 = val01 - Mathf.Floor(val01 + bias); // frac, 0 ~ 1 loop
            val01 = Mathf.Clamp(val01, bias, 1f - bias);
            
            return new Color
            {
                // sin = -1 ~ 1 -> 0 ~ 2 -> 0 ~ 1
                r = (Mathf.Sin(freqRGB.x * val01 + phaseRGB.x) + 1f) * 0.5f,
                g = (Mathf.Sin(freqRGB.y * val01 + phaseRGB.y) + 1f) * 0.5f,
                b = (Mathf.Sin(freqRGB.z * val01 + phaseRGB.z) + 1f) * 0.5f,
                a = 1f,
            };
        }

        /// <summary>
        /// Sine based gradient
        /// <see cref="https://en.wikibooks.org/wiki/Color_Theory/Color_gradient#Sine_based_gradient"/>
        /// <seealso cref="https://github.com/thi-ng/color/blob/master/src/gradients.org"/>
        /// <seealso cref="https://iquilezles.org/articles/palettes/"/>
        /// </summary>
        /// <param name="number"></param>
        /// <param name="maxAngle"></param>
        /// <param name="freqRGB">the Freq* are the frequencies of the respective RGB colors</param>
        /// <param name="phaseRGB">Phase* are the phase shift values.</param>
        /// <returns></returns>
        public static IEnumerable<Color> SineBasedGradient(
            int number, float maxAngle, Vector3 freqRGB, Vector3 phaseRGB)
        {
            float step = maxAngle / (float)number;
            for (int i = 0; i < number; ++i)
            {
                var color = new Color(
                    (Mathf.Sin(freqRGB.x * i * step + phaseRGB.x) + 1) * 0.5f,
                    (Mathf.Sin(freqRGB.y * i * step + phaseRGB.y) + 1) * 0.5f,
                    (Mathf.Sin(freqRGB.z * i * step + phaseRGB.z) + 1) * 0.5f);
                yield return color;
            }
        }
		#endregion

	}
}
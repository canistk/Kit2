using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2
{
	public class RangeExAttribute : PropertyAttribute
	{
		public readonly float max, min;
		public readonly string minName, maxName;

		[System.Flags]
		public enum eConfig
		{
			Normal = 0,
			MinNamed = 1 << 1,
			MaxNamed = 1 << 2,
			MinMaxNamed = MinNamed | MaxNamed,
		}
		public eConfig config { get; private set; } = eConfig.Normal;

		public RangeExAttribute(float min, float max)
		{
			this.min = min;
			this.max = max;
			config = eConfig.Normal;
		}

		public RangeExAttribute(string minName, float max)
		{
			this.minName = minName;
			this.max = max;
			config = eConfig.MinNamed;
		}

		public RangeExAttribute(float min, string maxName)
		{
			this.min = min;
			this.maxName = maxName;
			config = eConfig.MaxNamed;
		}

		public RangeExAttribute(string minName, string maxName)
		{
			this.minName = minName;
			this.maxName = maxName;
			config = eConfig.MinMaxNamed;
		}
	}
}
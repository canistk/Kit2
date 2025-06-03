using UnityEngine;
namespace Kit2
{
	public class MinMaxSliderAttribute : PropertyAttribute
	{
		public readonly float max;
		public readonly float min;

		public MinMaxSliderAttribute(float min, float max)
		{
			this.min = min;
			this.max = max;
		}
	}

	public class MinMaxSliderIntAttribute : PropertyAttribute
	{
		public readonly int max;
		public readonly int min;

		public MinMaxSliderIntAttribute(int min, int max)
		{
			this.min = min;
			this.max = max;
		}
	}
}
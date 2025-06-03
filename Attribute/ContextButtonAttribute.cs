using UnityEngine;

namespace Kit2
{
	public class ContextButtonAttribute : PropertyAttribute
	{
		public readonly string Callback;
		public ContextButtonAttribute(string callbackMethod)
		{
			Callback = callbackMethod;
		}
	}
}
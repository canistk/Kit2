using UnityEngine;

namespace Kit2
{
	[System.Obsolete("Use OnValueChange instead", true)]
	public class OnChangeAttribute : PropertyAttribute
	{
		public readonly string callbackMethodName;
		public OnChangeAttribute(string callbackMethodName)
		{
			this.callbackMethodName = callbackMethodName;
		}
	}
}
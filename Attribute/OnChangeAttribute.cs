using UnityEngine;

namespace Kit2
{
	public class OnChangeAttribute : PropertyAttribute
	{
		public readonly string callbackMethodName;
		public OnChangeAttribute(string callbackMethodName)
		{
			this.callbackMethodName = callbackMethodName;
		}
	}
}
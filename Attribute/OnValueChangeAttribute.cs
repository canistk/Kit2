using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
	public class OnValueChangeAttribute : PropertyAttribute
	{
		public string methodName;

		public OnValueChangeAttribute(string methodName)
		{
			this.methodName = methodName;
		}
	}
}

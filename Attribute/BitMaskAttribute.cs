using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class BitMaskAttribute : PropertyAttribute { }
}
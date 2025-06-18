using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace Kit2
{
	/// <summary>
	/// Editor : "Window/UI Toolkit > Sample" had sample of visualElement.
	/// </summary>
    public static class VisualElementExtend
    {
        public static VisualElement SplitVertical(float topHeight, out VisualElement top, float bottomHeight, out VisualElement bottom)
        {
			var vertical = new VisualElement()
			{
				style =
				{
					flexGrow = 1,
					flexDirection = FlexDirection.Column,
				},
			};
			top = new VisualElement();
			if (topHeight < 0f)
			{
				top.style.flexGrow = 1;
			}
			else
			{
				top.style.height = topHeight;
			}
			bottom = new VisualElement();
			if (bottomHeight < 0f)
			{
				bottom.style.flexGrow = 1;
			}
			else
			{
				bottom.style.height = bottomHeight;
			}
			vertical.Add(top);
			vertical.Add(bottom);
			return vertical;
		}

		public static VisualElement Splithorizontal(float leftWidth, out VisualElement left, float rightWidth, out VisualElement right)
		{
			var horizon = new VisualElement()
			{
				style =
				{
					flexGrow = 1,
					flexDirection = FlexDirection.Row,
				},
			};
			left = new VisualElement();
			if (leftWidth < 0f)
			{
				left.style.flexGrow = 1;
			}
			else
			{
				left.style.width = leftWidth;
			}
			right = new VisualElement();
			if (rightWidth < 0f)
			{
				right.style.flexGrow = 1;
			}
			else
			{
				right.style.height = rightWidth;
			}
			horizon.Add(left);
			horizon.Add(right);
			return horizon;
		}
	}
}
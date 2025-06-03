using UnityEngine;
using UnityEditor;

namespace Kit2
{
	public static class HandlesExtend
	{
		/// <summary>
		/// Wrap with <see cref="EditorGUI.BeginChangeCheck"/>
		/// </summary>
		/// <example>
		/// using (var checker = new EditorGUI.ChangeCheckScope())
		/// {
		///		DrawHandleBaseOnTools(ref p, ref r);
		///		if (checker.changed)
		///		{}
		///	}
		/// </example>
		/// <param name="pos"></param>
		/// <param name="rot"></param>
		public static void DrawHandleBaseOnTools(ref Vector3 pos, ref Quaternion rot)
		{
			if (Tools.current == Tool.Move)
			{
				pos = Handles.DoPositionHandle(pos, rot);
			}
			else if (Tools.current == Tool.Rotate)
			{
				rot = Handles.DoRotationHandle(rot, pos);
				if (rot.IsInvalid())
					rot = Quaternion.identity;
			}
		}

		public static void DrawSelectableDot(Vector3 position, Quaternion rotation, float size, 
			out bool isHover, out bool isSelected,
			Color? color = null, Color? hoverColor = null)
		{
			if (rotation.IsInvalid() || Tools.pivotRotation == PivotRotation.Global)
				rotation = Quaternion.identity;

			int id = GUIUtility.GetControlID(FocusType.Passive);
			isHover = false;
			isSelected = false;
			switch (Event.current.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (HandleUtility.nearestControl == id)
					{
						GUIUtility.hotControl = id;
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if ((Event.current.button == 0 || Event.current.button == 2) && GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0; // skip current event
						Event.current.Use(); // skip current event
						if (HandleUtility.nearestControl == id)
						{
							isSelected = true;
						}
					}
					break;
				case EventType.MouseMove:
					if ((HandleUtility.nearestControl == id && Event.current.button == 0) ||
						(GUIUtility.keyboardControl == id && Event.current.button == 2))
					{
						HandleUtility.Repaint();
					}
					break;
				case EventType.Repaint:
				{
					Color oldColor = Handles.color;
					if (HandleUtility.nearestControl == id && GUI.enabled)
					{
						Handles.color = hoverColor.HasValue ? hoverColor.Value : Color.cyan.CloneAlpha(0.8f);
						isHover = true;
					}
					else
					{
						Handles.color = color.HasValue ? color.Value : Color.cyan.CloneAlpha(0.4f);
					}
					Handles.SphereHandleCap(id, position, rotation, size, EventType.Repaint);
					Handles.color = oldColor;
					break;
				}
				case EventType.Layout:
					if (GUI.enabled)
					{
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, size));
					}
					break;
			}
		}

		public static void DrawLabel(Vector3 position, string text,
			SceneView sceneView, GUIStyle style = default(GUIStyle), Color color = default(Color),
			float offsetX = 0f, float offsetY = 0f)
		{
			Transform cam = sceneView != null ? sceneView.camera.transform :
				SceneView.currentDrawingSceneView != null ? SceneView.currentDrawingSceneView.camera.transform : // Scene View
				Camera.main.transform; // Only Game View
			if (Vector3.Dot(cam.forward, position - cam.position) > 0f)
			{
				Vector3 pos = position;
				if (offsetX != 0f || offsetY != 0f)
				{
					Vector3 camRightVector = cam.right * offsetX; // base on view
					pos += camRightVector + new Vector3(0f, offsetY, 0f); // base on target
				}

				if (style == default(GUIStyle))
				{
					if (color == default(Color))
						Handles.Label(pos, text, GUI.skin.textArea);
					else
					{
						style = new GUIStyle(GUI.skin.textArea);
						Color old = style.normal.textColor;
						style.normal.textColor = color;
						Handles.Label(pos, text, style);
						style.normal.textColor = old;
					}
				}
				else
				{
					if (color == default(Color))
						Handles.Label(pos, text, style);
					else
					{
						Color old = style.normal.textColor;
						style.normal.textColor = color;
						Handles.Label(pos, text, style);
						style.normal.textColor = old;
					}
				}
			}
		}

		public static bool Button(int id, Vector3 position, Quaternion direction, float size, float pickSize, bool useHandleSize, Handles.CapFunction capFunc, Color hoverCol, out bool isHovering)
		{
			Event current = Event.current;
			isHovering = false;
			if (useHandleSize)
			{
				float s = HandleUtility.GetHandleSize(position);
				size *= s;
				pickSize *= s;
			}
			switch (current.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (HandleUtility.nearestControl == id)
					{
						GUIUtility.hotControl = id;
						current.Use();
					}
					break;
				case EventType.MouseUp:
					if ((current.button == 0 || current.button == 2) && GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;
						current.Use();
						if (HandleUtility.nearestControl == id)
						{
							return true;
						}
					}
					break;
				case EventType.MouseMove:
					if ((HandleUtility.nearestControl == id && current.button == 0) ||
						(GUIUtility.keyboardControl == id && current.button == 2))
					{
						HandleUtility.Repaint();
					}
					break;
				case EventType.Repaint:
				{
					Color color = Handles.color;
					if (HandleUtility.nearestControl == id && GUI.enabled)
					{
						isHovering = true;
						Handles.color = hoverCol;
					}

#if UNITY_5_6_OR_NEWER
					capFunc(id, position, Quaternion.identity, size, EventType.Repaint);
#else
                    capFunc(id, position, direction, size);
#endif
					Handles.color = color;
					break;
				}
				case EventType.Layout:
					if (GUI.enabled)
					{
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, pickSize));
					}
					break;
			}
			return false;
		}

		//public static bool MouseHoverPosition(Event evt, SceneView sceneView, Vector3 _position, float radius)
		//{
		//	Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
		//	Vector3 normal = -sceneView.camera.transform.forward;
		//	RayHit hit = default;
		//	if (mouseRay.IntersectPointOnPlane(float.PositiveInfinity, _position, normal, ref hit))
		//		return 0f < hit.distance && hit.distance < radius;
		//	return false;
		//}
	}

	public struct HandlesMatrix : System.IDisposable
	{
		Matrix4x4 org;
		public HandlesMatrix(Matrix4x4 matrix)
		{
			org = Handles.matrix;
			Handles.matrix = matrix;
		}
		public void Dispose()
		{
			Handles.matrix = org;
		}
	}

	public struct HandlesColorScope : System.IDisposable
	{
		readonly Color oldColor;
		readonly bool hasValue;
		public HandlesColorScope(Color? color)
		{
			oldColor = Handles.color;
			hasValue = color.HasValue;
			if (color.HasValue && color.Value != default)
				Handles.color = color.Value;
		}

		public void Dispose()
		{
			if (hasValue)
				Handles.color = oldColor;
		}
	}

}
using UnityEngine;
using Kit2.Shape;
using Kit2.MeshUtils;
#if UNITY_EDITOR
using Handles = UnityEditor.Handles;
using HandleUtility = UnityEditor.HandleUtility;
#endif

namespace Kit2
{
	public struct ColorScope : System.IDisposable
	{
		readonly Color oldColor;
		readonly bool hasValue;
		public ColorScope(Color? color)
		{
			oldColor = Gizmos.color;
            hasValue = color.HasValue;
            if (color.HasValue && color.Value != default)
				Gizmos.color = color.Value;
		}

		public void Dispose()
		{
			if (hasValue)
				Gizmos.color = oldColor;
		}
	}

	public struct GizmosMatrix : System.IDisposable
	{
		Matrix4x4 org;
		public GizmosMatrix(Matrix4x4 matrix)
		{
			org = Gizmos.matrix;
			Gizmos.matrix = matrix;
		}
		public void Dispose()
		{
			Gizmos.matrix = org;
		}
	}

	/// <summary>Gizmo Extension - Static class that extends Unity's gizmo functionallity.</summary>
	public static class GizmosExtend
	{
		#region Point
		/// <summary>- Draws a point.</summary>
		/// <param name='position'>- The point to draw.</param>
		///  <param name='color'>- The color of the drawn point.</param>
		/// <param name='size'>- The size of the drawn point.</param>
		public static void DrawPoint(Vector3 position, Color? color = null, float size = 1.0f, bool fixScreenSize = false)
		{
#if UNITY_EDITOR
			float factor = fixScreenSize ? size * GetHandleSize(position) : size;
			DrawSphere(position, factor, color);
#endif
		}

		/// <summary>Draw transform position and rotaion</summary>
		/// <param name="obj"></param>
		/// <param name="fixScreenSize">remain same size based on scene view.</param>
		/// <param name="size">size of axis</param>
		public static void DrawTransform(Transform obj, bool fixScreenSize = true, float size = 1f)
		{
			DrawTransform(obj.position, obj.rotation, fixScreenSize, size);
		}
		public static void DrawTransform(Vector3 pos, Quaternion rot, bool fixScreenSize = true, float size = 1f)
		{
#if UNITY_EDITOR
			if (size <= 0f)
				size = 1f;
			float factor = fixScreenSize ? size * GetHandleSize(pos) : size;
			DrawRay(pos, rot * Vector3.forward * factor, Color.blue);
			DrawRay(pos, rot * Vector3.up * factor, Color.green);
			DrawRay(pos, rot * Vector3.right * factor, Color.red);
#endif
		}
		#endregion Point

		#region Rect
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawRectTransform(RectTransform rectTransform, Color? color = null)
		{
			if (rectTransform == null)
				return;
			DrawRect(rectTransform.rect, rectTransform.localToWorldMatrix, color);
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawRect(Rect rect, Matrix4x4 matrix, Color? color = null)
		{
			using (new ColorScope(color))
			{
				using (new GizmosMatrix(matrix))
				{
					var p0 = new Vector3(rect.x, rect.y, 0f);
					var p1 = new Vector3(rect.x, rect.yMax, 0f);
					var p2 = new Vector3(rect.xMax, rect.yMax, 0f);
					var p3 = new Vector3(rect.xMax, rect.y, 0f);

					Gizmos.DrawLine(p0, p1);
					Gizmos.DrawLine(p0, p3);
					Gizmos.DrawLine(p3, p2);
					Gizmos.DrawLine(p1, p2);
				}
			}
		}
		#endregion Rect

		#region Line
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawRay(Vector3 position, Vector3 direction, Color? color = null)
		{
#if UNITY_EDITOR
			using (new ColorScope(color))
			{
				Gizmos.DrawRay(position, direction);
			}
#endif
		}

		/// <summary>Override DrawLine</summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="color"></param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawLine(Vector3 from, Vector3 to, Color? color = null)
		{
#if UNITY_EDITOR
			using (new ColorScope(color))
			{
				Gizmos.DrawLine(from, to);
			}
#endif
		}

		/// <summary>- Draws an arrow.</summary>
		/// <param name='position'>- The start position of the arrow.</param>
		/// <param name='direction'>- The direction the arrow will point in.</param>
		/// <param name='color'>- The color of the arrow.</param>
		/// <param name="angle">- The angle of arrow head.0 ~ 90f</param>
		/// <param name="headLength">- The angle length of arrow head. 0 ~ 1 in percent</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawArrow(Vector3 position, Vector3 direction, Color? color = null, float headLength = 0.3f)
		{
#if UNITY_EDITOR
			if (direction == Vector3.zero)
				return; // can't draw a thing

			float length = direction.magnitude;
			float arrowLength = Mathf.Clamp01(headLength);

			using (new ColorScope(color))
			{
				Gizmos.DrawRay(position, direction);
			}
			DrawCone(position + direction, direction.normalized, arrowLength, arrowLength * 0.5f, color);
#endif
		}
		#endregion Line

		#region Circle
		/// <summary>- Draws a circle.</summary>
		/// <param name='position'>- Where the center of the circle will be positioned.</param>
		/// <param name='up'>- The direction perpendicular to the surface of the circle.</param>
		/// <param name='color'>- The color of the circle.</param>
		/// <param name='radius'>- The radius of the circle.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCircle(Vector3 position, Vector3? _up = null, Color? color = null, float radius = 1.0f)
		{
#if UNITY_EDITOR
			var up = (_up.HasValue ? _up.Value : Vector3.up).normalized;
			Vector3
				forward = Vector3.Slerp(up, -up, 0.5f) * radius,
				right = Vector3.Cross(up, forward).normalized * radius;

			Matrix4x4 matrix = new Matrix4x4()
			{
				m00 = right.x,
				m10 = right.y,
				m20 = right.z,

				m01 = up.x,
				m11 = up.y,
				m21 = up.z,

				m02 = forward.x,
				m12 = forward.y,
				m22 = forward.z
			};

			Vector3
				lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0))),
				nextPoint = Vector3.zero;

			using (new ColorScope(color))
			{
				for (int i = 0; i <= 90; ++i)
				{
					nextPoint = position + matrix.MultiplyPoint3x4(
						new Vector3(
							Mathf.Cos((i * 4) * Mathf.Deg2Rad),
							0f,
							Mathf.Sin((i * 4) * Mathf.Deg2Rad)
							)
						);
					Gizmos.DrawLine(lastPoint, nextPoint);
					lastPoint = nextPoint;
				}
			}
#endif
		}

		#endregion Circle

		#region Sphere
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawSphere(Transform self, Color? color = null)
		{
#if UNITY_EDITOR
			DrawSphere(self.position, self.localScale.x, color);
#endif
		}

		/// <summary>Draw Sphere</summary>
		/// <param name="position"></param>
		/// <param name="radius"></param>
		/// <param name="color"></param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawSphere(Vector3 position, float radius, Color? color = null)
		{
#if UNITY_EDITOR
			using (new ColorScope(color))
			{
				Gizmos.DrawSphere(position, radius);
			}
#endif
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireSphere(SphereCollider sphereCollider, Color? color = null)
		{
			Matrix4x4 matrix = sphereCollider.transform.localToWorldMatrix;
			using (new GizmosMatrix(matrix))
				DrawWireSphere(sphereCollider.center, sphereCollider.radius, color);
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireSphere(Vector3 position, float radius, Color? color = null)
		{
#if UNITY_EDITOR
			using (new ColorScope(color))
			{
				Gizmos.DrawWireSphere(position, radius);
			}
#endif
		}
		#endregion Sphere

		#region Shapes
		/// <summary>- Draws an axis-aligned bounding box.</summary>
		/// <param name='bounds'>- The bounds to draw.</param>
		/// <param name='color'>- The color of the bounds.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBounds(Bounds bounds, Color? color = null)
		{
#if UNITY_EDITOR
			DrawWireCube(bounds, color);
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBounds(Vector3 position, Quaternion rotation, Bounds bounds, Color? color = null)
		{
			using (new GizmosMatrix(Matrix4x4.TRS(position, rotation, Vector3.one)))
				DrawBounds(bounds, color);
		}

		/// <summary>- Draws a cylinder.</summary>
		/// <param name='start'>- The position of one end of the cylinder.</param>
		/// <param name='end'>- The position of the other end of the cylinder.</param>
		/// <param name='color'>- The color of the cylinder.</param>
		/// <param name='radius'>- The radius of the cylinder.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCylinder(Vector3 start, Vector3 end, float radius = 1.0f, Color? color = null)
		{
			var origin = Vector3.Lerp(start, end, 0.5f);
			var v = end - start;
			var height = v.magnitude;
			var up = v.normalized;
			if (up == Vector3.zero)
				up = Vector3.up;
			var orientation = Quaternion.LookRotation(Quaternion.LookRotation(up) * Vector3.up, up);
			DrawCylinder(origin, orientation, height, radius, color);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCylinder(Vector3 origin, Quaternion orientation, float height, float radius, Color? color = null)
		{
#if UNITY_EDITOR
			using (new ColorScope(color))
			{
				// since u3d cylinder height is 2unit, radius = 0.5unit
				var r = radius * 2f;
				var u3dScaleFix = new Vector3(r, height * 0.5f, r);
				var mesh = Primitive.GetUnityPrimitiveMesh(PrimitiveType.Cylinder);
				Gizmos.DrawMesh(mesh, origin, orientation, u3dScaleFix);
			}
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireCylinder(Vector3 start, Vector3 end, float radius = 1.0f, Color? color = null)
		{
			var origin = Vector3.Lerp(start, end, 0.5f);
			var v = end - start;
			var height = v.magnitude;
			var up = v.normalized;
			var forward = Vector3.SlerpUnclamped(up, -up, 0.5f);
			var orientation = Quaternion.LookRotation(forward, up);
			DrawWireCylinder(origin, orientation, height, radius, color);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireCylinder(Vector3 origin, Quaternion orientation, float height, float radius, Color? color = null)
		{
#if UNITY_EDITOR
#if false
			using (new ColorScope(color))
			{
				var mesh = Primitive.GetUnityPrimitiveMesh(PrimitiveType.Cylinder);
				// since u3d cylinder height is 2unit, radius = 0.5unit
				var r = radius * 2f;
				var u3dScaleFix = new Vector3(r, height * 0.5f, r);
				Gizmos.DrawWireMesh(mesh, origin, orientation, u3dScaleFix);
			}
#else
			Vector3 start = origin + orientation * (Vector3.up * height * 0.5f);
			Vector3 end = origin + orientation * (Vector3.down * height * 0.5f);
			Vector3
				up = (end - start).normalized * radius,
				forward = Vector3.Slerp(up, -up, 0.5f),
				right = Vector3.Cross(up, forward).normalized * radius;

			if ((start - end).magnitude < 0.0001f)
			{
				// when two are nearly stick together.
				using (new ColorScope(color))
				{
					DrawCircle(start, up, color, radius);
				}
				return;
			}

			using (new ColorScope(color))
			{
				//Radial circles
				DrawCircle(start, up, null, radius);
				DrawCircle(end, -up, null, radius);
				DrawCircle((start + end) * 0.5f, up, null, radius);

				//Side lines
				Gizmos.DrawLine(start + right, end + right);
				Gizmos.DrawLine(start - right, end - right);

				Gizmos.DrawLine(start + forward, end + forward);
				Gizmos.DrawLine(start - forward, end - forward);

				//Start endcap
				Gizmos.DrawLine(start - right, start + right);
				Gizmos.DrawLine(start - forward, start + forward);

				//End endcap
				Gizmos.DrawLine(end - right, end + right);
				Gizmos.DrawLine(end - forward, end + forward);
			}
#endif
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCone(Vector3 origin, Vector3 upward, float height, float radius, Color? color = null)
		{
#if UNITY_EDITOR
			var up = upward.normalized;
			var forward = Vector3.SlerpUnclamped(up, -up, 0.5f).normalized;
			var orientation = Quaternion.LookRotation(forward, up);
			var right = orientation * Vector3.right;
			var left = orientation * Vector3.left;
			var back = orientation * Vector3.back;

			DrawCircle(origin, up, color, radius);

			using (new ColorScope(color))
			{
				var apex = origin + up * height;
				Gizmos.DrawLine(apex, origin + forward * radius);
				Gizmos.DrawLine(apex, origin + left * radius);
				Gizmos.DrawLine(apex, origin + back * radius);
				Gizmos.DrawLine(apex, origin + right * radius);
			}
#endif
		}

		/// <summary>- Draws a cone.</summary>
		/// <param name='position'>- The position for the tip of the cone.</param>
		/// <param name='direction'>- The direction for the cone to get wider in.</param>
		/// <param name='color'>- The color of the cone.</param>
		/// <param name='angle'>- The angle of the cone.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Obsolete("Drawing backward", true)]
		public static void DrawCone(Vector3 position, Vector3 direction, Color? color = null, float angle = 45, bool flip = false)
		{
#if UNITY_EDITOR
			if (flip)
			{
				position += direction;
				direction *= -1f;
			}
			float length = direction.magnitude;
			angle = Mathf.Clamp(angle, 0f, 90f);
			Vector3
				forward = direction,
				up = Vector3.Slerp(forward, -forward, 0.5f),
				right = Vector3.Cross(forward, up).normalized * length,
				slerpedVector = Vector3.Slerp(forward, up, angle / 90.0f);

			Plane farPlane = new Plane(-direction, position + forward);
			Ray distRay = new Ray(position, slerpedVector);

			float dist;
			farPlane.Raycast(distRay, out dist);

			using (new ColorScope(color))
			{
				Gizmos.DrawRay(position, slerpedVector.normalized * dist);
				Gizmos.DrawRay(position, Vector3.Slerp(forward, -up, angle / 90.0f).normalized * dist);
				Gizmos.DrawRay(position, Vector3.Slerp(forward, right, angle / 90.0f).normalized * dist);
				Gizmos.DrawRay(position, Vector3.Slerp(forward, -right, angle / 90.0f).normalized * dist);

			}
			DrawCircle(position + forward, direction, color, (forward - (slerpedVector.normalized * dist)).magnitude);
			DrawCircle(position + (forward * 0.5f), direction, color, ((forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude);
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(CapsuleCollider capsuleCollider, Color? color = null)
		{
#if UNITY_EDITOR
			Vector3 center = capsuleCollider.center;
			float radius = capsuleCollider.radius;
			float height = capsuleCollider.height;
			int axisDir = capsuleCollider.direction; // x = 0, y = 1, z = 2
			float half = Mathf.Clamp(height / 2f - radius, 0f, float.PositiveInfinity);
			Matrix4x4 matrix = capsuleCollider.transform.localToWorldMatrix;
			Vector3 p0, p1;
			if (height < radius * 2f)
			{
				p0 = p1 = matrix.MultiplyPoint(center);
			}
			else
			{
				switch (axisDir)
				{
					case 0: // x-axis
						p0 = center; p0.x += half;
						p1 = center; p1.x -= half;
						break;
					case 1: // Y-axis
						p0 = center; p0.y += half;
						p1 = center; p1.y -= half;
						break;
					case 2: // Z-axis
						p0 = center; p0.z += half;
						p1 = center; p1.z -= half;
						break;
					default:
						throw new System.NotImplementedException();
				}
			}
			using (new GizmosMatrix(matrix))
				DrawCapsule(p0, p1, radius, color);
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(CapsuleData capsule, Color? color = null)
		{
			DrawCapsule(capsule.p0, capsule.p1, capsule.radius, color);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(Vector3 point1, Vector3 point2, float radius = 1f, Color? color = null, bool u3dMethod = false)
		{
#if UNITY_EDITOR
			using (new ColorScope(color))
			{
				var origin = Vector3.Lerp(point1, point2, 0.5f);
				var v = point2 - point1;
				var up = v.normalized;
				var height = v.magnitude;

				if (v == Vector3.zero)
				{
					DrawSphere(point1, radius, color);
					return;
				}
#if true
				var hh = u3dMethod ?
					Mathf.Max(0f, height * 0.5f - radius) : // respect U3D's method, capsule's outer
					Mathf.Max(0f, height * 0.5f); // point are sphere's center
				var p1 = origin + up * hh;
				var p2 = origin - up * hh;
				Gizmos.DrawSphere(p1, radius);
				Gizmos.DrawSphere(p2, radius);
				DrawCylinder(p1, p2, radius, color);
#else
				var orientation = Quaternion.LookRotation(Quaternion.LookRotation(up) * Vector3.up, up);
				var r = radius * 2f;
				var u3dScaleFix = new Vector3(r , height * 0.5f, r);
				var capsule = Primitive.GetUnityPrimitiveMesh(PrimitiveType.Capsule);
				Gizmos.DrawMesh(capsule, origin, orientation, u3dScaleFix);
#endif
			}
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(Vector3 origin, Quaternion orientation, float height, float radius, Color? color = null)
		{
#if UNITY_EDITOR
			var hh = orientation * Vector3.up * Mathf.Max(0f, height * 0.5f);
			var p0 = origin + hh;
			var p1 = origin - hh;
			DrawCapsule(p0, p1, radius, color);
#endif
		}

		public static void DrawWireCapsule(Vector3 origin, Quaternion orientation, float height, float radius, Color? color = null)
		{
#if UNITY_EDITOR
			var hh = orientation * Vector3.up * Mathf.Max(0f, height * 0.5f);
			var p0 = origin + hh;
			var p1 = origin - hh;
			DrawWireCapsule(p0, p1, radius, color);
#endif
		}
		/// <summary>- Draws a capsule.</summary>
		/// <param name='point1'>- The position of one end of the capsule.</param>
		/// <param name='point2'>- The position of the other end of the capsule.</param>
		/// <param name='color'>- The color of the capsule.</param>
		/// <param name='radius'>- The radius of the capsule.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius = 1f, Color? color = null)
		{
#if UNITY_EDITOR
			if (point1 == point2)
			{
				using (new ColorScope(color))
				{
					Gizmos.DrawWireSphere(point1, radius);
				}
			}
			else
			{
				float
					height = (point1 - point2).magnitude,
					sideLength = Mathf.Max(0, (height * 0.5f));

				Vector3
					up = (point2 - point1).normalized * radius,
					forward = Vector3.Slerp(up, -up, 0.5f),
					right = Vector3.Cross(up, forward).normalized * radius,
					middle = (point2 + point1) * 0.5f;
				
				point1 = middle + ((point1 - middle).normalized * sideLength);
				point2 = middle + ((point2 - middle).normalized * sideLength);


				using (new ColorScope(color))
				{
					//Radial circles
					DrawCircle(point1, up, null, radius);
					DrawCircle(point2, -up, null, radius);

					//Side lines
					Gizmos.DrawLine(point1 + right, point2 + right);
					Gizmos.DrawLine(point1 - right, point2 - right);

					Gizmos.DrawLine(point1 + forward, point2 + forward);
					Gizmos.DrawLine(point1 - forward, point2 - forward);

					for (int i = 1; i < 26; i++)
					{
						var f0 = (i - 1) / 25.0f;
						var f1 = i / 25.0f;
						//Start endcap
						Gizmos.DrawLine(Vector3.Slerp(right, -up, f1) + point1, Vector3.Slerp(right, -up, f0) + point1);
						Gizmos.DrawLine(Vector3.Slerp(-right, -up, f1) + point1, Vector3.Slerp(-right, -up, f0) + point1);
						Gizmos.DrawLine(Vector3.Slerp(forward, -up, f1) + point1, Vector3.Slerp(forward, -up, f0) + point1);
						Gizmos.DrawLine(Vector3.Slerp(-forward, -up, f1) + point1, Vector3.Slerp(-forward, -up, f0) + point1);

						//End endcap
						Gizmos.DrawLine(Vector3.Slerp(right, up, f1) + point2, Vector3.Slerp(right, up, f0) + point2);
						Gizmos.DrawLine(Vector3.Slerp(-right, up, f1) + point2, Vector3.Slerp(-right, up, f0) + point2);
						Gizmos.DrawLine(Vector3.Slerp(forward, up, f1) + point2, Vector3.Slerp(forward, up, f0) + point2);
						Gizmos.DrawLine(Vector3.Slerp(-forward, up, f1) + point2, Vector3.Slerp(-forward, up, f0) + point2);
					}
				}
			}
#endif
		}

		/// <summary>Draw Camera based on give reference.</summary>
		/// <param name="camera"></param>
		/// <param name="color"></param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawFrustum(Camera camera, Color? color = null)
		{
#if UNITY_EDITOR
			using (new GizmosMatrix(Matrix4x4.TRS(camera.transform.position, camera.transform.rotation, Vector3.one)))
			using (new ColorScope(color))
				Gizmos.DrawFrustum(Vector3.zero, camera.fieldOfView, camera.farClipPlane, camera.nearClipPlane, camera.aspect);
#endif
		}

		/// <summary>Candy tool to draw the giving collider</summary>
		/// <param name="collider"></param>
		/// <param name="color"></param>
		public static void DrawCollider(Collider collider, Color? color = null)
		{
			if (collider is BoxCollider box)
			{
				DrawCube(box, color);
			}
			else if (collider is CapsuleCollider capsule)
			{
				DrawCapsule(capsule, color);
			}
			else if (collider is SphereCollider sphere)
			{
				DrawWireSphere(sphere, color);
			}
			else if (collider is MeshCollider mc)
			{
				Transform t = collider.transform;
				using (new ColorScope(color))
					Gizmos.DrawWireMesh(mc.sharedMesh, 0, t.position, t.rotation, t.lossyScale);
			}
			// else throw new System.NotImplementedException();
		}
		#endregion Shapes

		#region Cube
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color? color = null)
		{
			DrawCube(new Box(origin, halfExtents, orientation), color);
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 from, Vector3 to, float width, float height, Color? color = null)
		{
			DrawCube(new Box(from, to, width, height), color);
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCube(Box box, Color? color = null)
		{
#if UNITY_EDITOR
			Vector3 size = box.Size();
			if (size.x == 0f || size.y == 0f || size.z == 0f)
				return;
			using (new ColorScope(color))
			{
				using (new GizmosMatrix(Matrix4x4.TRS(box.origin, box.rotation, Vector3.one)))
				{
					Gizmos.DrawCube(Vector3.zero, size);
				}
			}
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireCube(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color? color = null)
		{
			DrawWireCube(new Box(origin, halfExtents, orientation), color);
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireCube(Vector3 from, Vector3 to, float width, float height, Color? color = null)
		{
			DrawWireCube(new Box(from, to, width, height), color);
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireCube(Box box, Color? color = null)
		{
#if UNITY_EDITOR
			Vector3 size = box.Size();
			if (size.x == 0f || size.y == 0f || size.z == 0f)
				return;
			using (new ColorScope(color))
			{
				using (new GizmosMatrix(box.localToWorldMatrix))
				{
					Gizmos.DrawWireCube(Vector3.zero, size);
				}
			}
#endif
		}

		//Draws just the box at where it is currently hitting.
		public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color? color = null)
		{
#if UNITY_EDITOR
			//Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
			origin = origin + (direction.normalized * hitInfoDistance);
			DrawWireCube(origin, halfExtents, orientation, color);
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color? color = null)
		{
#if UNITY_EDITOR
			Box bottomBox = new Box(origin, halfExtents, orientation);
			Box topBox = new Box(origin + (direction.normalized * distance), halfExtents, orientation);

			using (new ColorScope(color))
			{
				Gizmos.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft);
				Gizmos.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight);
				Gizmos.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft);
				Gizmos.DrawLine(bottomBox.backTopRight, topBox.backTopRight);
				Gizmos.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft);
				Gizmos.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight);
				Gizmos.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft);
				Gizmos.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight);
			}

			DrawWireCube(bottomBox, color);
			DrawWireCube(topBox, color);
#endif
		}
		#endregion // Cube

		#region Handles
#if UNITY_EDITOR
		private static bool IsHandleHackAvailable =>
			UnityEditor.SceneView.currentDrawingSceneView != null ||
			(Application.isPlaying && Camera.main != null);
#else
		private const bool IsHandleHackAvailable = false;
#endif

		public static float GetHandleSize(Vector3 center)
		{
#if UNITY_EDITOR
			if (IsHandleHackAvailable)
				return HandleUtility.GetHandleSize(center);
			else
#endif
				return 1f;
		}

		/// <summary>DrawLabel on scene view</summary>
		/// <param name="position"></param>
		/// <param name="text"></param>
		/// <param name="offset"></param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawLabel(Vector3 position, string text, Vector2 offset)
		{
			DrawLabel(position, text, offsetX: offset.x, offsetY: offset.y);
		}

		/// <summary>Draw label on scene view</summary>
		/// <param name="position"></param>
		/// <param name="text"></param>
		/// <param name="offsetX"></param>
		/// <param name="offsetY"></param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawLabel(Vector3 position, string text, float offsetX = 0f, float offsetY = 0f, bool background = true)
		{
#if UNITY_EDITOR
			if (IsHandleHackAvailable)
			{
				Transform cam = UnityEditor.SceneView.currentDrawingSceneView != null ?
					UnityEditor.SceneView.currentDrawingSceneView.camera.transform : // Scene View
					Camera.main.transform; // Only Game View
				if (Vector3.Dot(cam.forward, position - cam.position) > 0)
				{
					Vector3 pos = position;
					if (offsetX != 0f || offsetY != 0f)
					{
						Vector3 camRightVector = cam.right * offsetX; // base on view
						pos += camRightVector + new Vector3(0f, offsetY, 0f); // base on target
					}

					if (background)
						Handles.Label(pos, text, GUI.skin.textArea);
					else
						Handles.Label(pos, text);
				}
			}
#endif
		}

		/// <summary>Draw a circular sector (pie piece) in 3D space.</summary>
		/// <param name="center">The center of the circle</param>
		/// <param name="normal">The normal of the circle</param>
		/// <param name="from">The direction of the point on the circumference, relative to the center, where the sector begins</param>
		/// <param name="angle">The angle of the sector, in degrees.</param>
		/// <param name="radius">The radius of the circle</param>
		/// <param name="constantScreenSize">Have constant screen-sized</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawArc(in Vector3 center, Vector3 normal, in Vector3 from, in float angle, float radius, in Color color, in bool constantScreenSize = false)
		{
#if UNITY_EDITOR
			if (IsHandleHackAvailable && radius > float.Epsilon && angle != 0f)
			{
				if (constantScreenSize)
					radius *= HandleUtility.GetHandleSize(center);
				using (new HandleColorScope(color))
				{
					Handles.DrawSolidArc(center, normal, from, angle, radius);
				}
			}
#endif
		}

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DrawWireArc(in Vector3 center, Vector3 normal, in Vector3 from, in float angle, float radius, in Color color, in bool constantScreenSize = false)
        {
#if UNITY_EDITOR
            if (IsHandleHackAvailable && radius > float.Epsilon && angle != 0f)
            {
                if (constantScreenSize)
                    radius *= HandleUtility.GetHandleSize(center);
                using (new HandleColorScope(color))
                {
                    Handles.DrawWireArc(center, normal, from, angle, radius);
                }
            }
#endif
        }

        /// <summary></summary>
        /// <param name="center">position</param>
        /// <param name="from">direction</param>
        /// <param name="to">direction</param>
        /// <param name="axis">A vector around, which the other vectors are rotated.</param>
        /// <param name="radius"></param>
        /// <param name="constantScreenSize"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawAngleBetween(Vector3 center, Vector3 from, Vector3 to, Vector3 axis, float radius, Color color, bool constantScreenSize = true, bool label = false)
		{
#if UNITY_EDITOR
			if (IsHandleHackAvailable)
			{
				float angle = Vector3.SignedAngle(from, to, axis);
				DrawArc(center, axis, from, angle, radius, color, constantScreenSize);
				if (label)
				{
					float factor = constantScreenSize ? HandleUtility.GetHandleSize(center) : 1f;
					Vector3 labelPos = center + (Vector3.Lerp(from, to, 0.5f) * factor);
					DrawLabel(labelPos, $"{angle:F2}");
				}
			}
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBezier(Vector3 start, Vector3 startTangent, Vector3 end, Vector3 endTangent, Color orgColor, Color behindColor, float width = 1f)
		{
#if UNITY_EDITOR
			Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
			Handles.DrawBezier(start, end, startTangent, endTangent, orgColor, Texture2D.whiteTexture, width);

			Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
			Handles.DrawBezier(start, end, startTangent, endTangent, behindColor, Texture2D.whiteTexture, width);
			Handles.zTest = UnityEngine.Rendering.CompareFunction.Disabled;
#endif
		}

		private struct HandleColorScope : System.IDisposable
		{
			Color oldColor;
			public HandleColorScope(Color? color)
			{
#if UNITY_EDITOR
				oldColor = Handles.color;
				if (color.HasValue)
					Handles.color = color.Value;
#else
				oldColor = Color.white;
#endif
			}

			public void Dispose()
			{
#if UNITY_EDITOR
				Handles.color = oldColor;
#endif
			}
		}
		#endregion // Handles

		#region Triangle
		public static void DrawTriangle(Triangle triangle, Color? color)
        {
			using (new ColorScope(color))
            {
                Gizmos.DrawLine(triangle[0], triangle[1]);
                Gizmos.DrawLine(triangle[1], triangle[2]);
                Gizmos.DrawLine(triangle[2], triangle[0]);
            }
        }

        public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color? color)
        {
            using (new ColorScope(color))
            {
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(b, c);
                Gizmos.DrawLine(c, a);
            }
        }
		#endregion Triangle

    }
}
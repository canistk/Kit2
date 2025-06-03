using UnityEngine;
using Kit2.Shape;
/// <summary>
/// Debug Extension
/// 	- Static class that extends Unity's debugging functionallity.
/// </summary>

namespace Kit2
{
	public static class DebugExtend
	{
		#region Point
		/// <summary>- Debugs a point.</summary>
		/// <param name='position'>- The point to debug.</param>
		/// <param name='color'>- The color of the point.</param>
		/// <param name='scale'>- The size of the point.</param>
		/// <param name='duration'>- How long to draw the point.</param>
		/// <param name='depthTest'>- Whether or not this point should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawPoint(Vector3 position, Color color = default(Color), float scale = 1.0f, float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			Debug.DrawRay(position + (Vector3.up * (scale * 0.5f)), -Vector3.up * scale, color, duration, depthTest);
			Debug.DrawRay(position + (Vector3.right * (scale * 0.5f)), -Vector3.right * scale, color, duration, depthTest);
			Debug.DrawRay(position + (Vector3.forward * (scale * 0.5f)), -Vector3.forward * scale, color, duration, depthTest);
#endif
		}

		/// <summary>Draw transform position and rotaion</summary>
		/// <param name="obj"></param>
		/// <param name="fixScreenSize">remain same size based on scene view.</param>
		/// <param name="size">size of axis</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawTransform(Transform obj, float size = 1f, float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			if (size <= 0f)
				size = 1f;
			DrawRay(obj.position, obj.forward * size, Color.blue, duration, depthTest);
			DrawRay(obj.position, obj.up * size, Color.green, duration, depthTest);
			DrawRay(obj.position, obj.right * size, Color.red, duration, depthTest);
#endif
		}

        /// <summary>Draw transform position and rotaion</summary>
        /// <param name="obj"></param>
        /// <param name="fixScreenSize">remain same size based on scene view.</param>
        /// <param name="size">size of axis</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DrawTransform(Vector3 pos, Quaternion rot, float size = 1f, float duration = 0, bool depthTest = false)
        {
#if UNITY_EDITOR
            if (size <= 0f)
                size = 1f;
            DrawRay(pos, rot * Vector3.forward * size,	Color.blue, duration, depthTest);
            DrawRay(pos, rot * Vector3.up * size,		Color.green, duration, depthTest);
            DrawRay(pos, rot * Vector3.right * size,	Color.red,	duration, depthTest);
#endif
        }
        #endregion Point

        #region Line
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawRay(Vector3 position, Vector3 direction, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			if (color == default(Color))
				color = Color.white;
			Debug.DrawRay(position, direction, color, duration, depthTest);
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawLine(Vector3 point1, Vector3 point2, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			if (color == default(Color))
				color = Color.white;
			Debug.DrawLine(point1, point2, color, duration, depthTest);
#endif
		}
		/// <summary>- Debugs an arrow.</summary>
		/// <param name='position'>- The start position of the arrow.</param>
		/// <param name='direction'>- The end position of the arrow.</param>
		/// <param name='color'>- The color of the arrow.</param>
		/// <param name='duration'>- How long to draw the arrow.</param>
		/// <param name='depthTest'>- Whether or not the arrow should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawArrow(Vector3 position, Vector3 direction, Color color = default(Color), float angle = 5f, float headLength = 0.2f, float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			if (direction == Vector3.zero)
				direction = Vector3.forward;
			if (angle < 0f)
				angle = Mathf.Abs(angle);
			if (angle > 0f)
			{
				if (headLength < 0f)
					headLength = Mathf.Abs(headLength);
				float length = direction.magnitude;
				if (length < headLength)
					headLength = length;
				Vector3 headDir = direction.normalized * (-length * headLength);
				DrawCone(position + direction, headDir, color, angle, duration, depthTest);
			}

			DrawRay(position, direction, color, duration, depthTest);
#endif
		}
		#endregion Line

		#region Circle
		/// <summary>- Debugs a circle.</summary>
		/// <param name='position'>- Where the center of the circle will be positioned.</param>
		/// <param name='up'>- The direction perpendicular to the surface of the circle.</param>
		/// <param name='color'>- The color of the circle.</param>
		/// <param name='radius'>- The radius of the circle.</param>
		/// <param name='duration'>- How long to draw the circle.</param>
		/// <param name='depthTest'>- Whether or not the circle should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCircle(Vector3 position, Vector3 up, Color color = default(Color), float radius = 1.0f, float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			up = ((up == default(Vector3)) ? Vector3.up : up).normalized * radius;
			Vector3
				forward = Vector3.Slerp(up, -up, 0.5f),
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

			for (int i = 0; i <= 90; i++)
			{
				nextPoint = position + matrix.MultiplyPoint3x4(
					new Vector3(
						Mathf.Cos((i * 4) * Mathf.Deg2Rad),
						0f,
						Mathf.Sin((i * 4) * Mathf.Deg2Rad)
						)
					);
				Debug.DrawLine(lastPoint, nextPoint, color, duration, depthTest);
				lastPoint = nextPoint;
			}
#endif
		}
		#endregion Circle

		#region Sphere
		/// <summary>- Debugs a wire sphere.</summary>
		/// <param name='position'>- The position of the center of the sphere.</param>
		/// <param name='color'>- The color of the sphere.</param>
		/// <param name='radius'>- The radius of the sphere.</param>
		/// <param name='duration'>- How long to draw the sphere.</param>
		/// <param name='depthTest'>- Whether or not the sphere should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawWireSphere(Vector3 position, float radius = 1.0f, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			float angle = 10.0f;

			Vector3
				x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0)),
				y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0)),
				z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z),
				new_x, new_y, new_z;

			color = (color == default(Color)) ? Color.white : color;

			for (int i = 1; i <= 36; i++)
			{
				new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad));
				new_y = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y, position.z + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad));
				new_z = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z);

				Debug.DrawLine(x, new_x, color, duration, depthTest);
				Debug.DrawLine(y, new_y, color, duration, depthTest);
				Debug.DrawLine(z, new_z, color, duration, depthTest);

				x = new_x;
				y = new_y;
				z = new_z;
			}
#endif
		}
		#endregion Sphere

		#region Shapes
		/// <summary>- Debugs an axis-aligned bounding box.</summary>
		/// <param name='bounds'>- The bounds to debug.</param>
		/// <param name='color'>- The color of the bounds.</param>
		/// <param name='duration'>- How long to draw the bounds.</param>
		/// <param name='depthTest'>- Whether or not the bounds should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBounds(Bounds bounds, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			Vector3
				ruf = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z),
				rub = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
				luf = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z),
				lub = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
				rdf = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
				rdb = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
				lfd = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z),
				lbd = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);

			Debug.DrawLine(ruf, luf, color, duration, depthTest);
			Debug.DrawLine(ruf, rub, color, duration, depthTest);
			Debug.DrawLine(luf, lub, color, duration, depthTest);
			Debug.DrawLine(rub, lub, color, duration, depthTest);

			Debug.DrawLine(ruf, rdf, color, duration, depthTest);
			Debug.DrawLine(rub, rdb, color, duration, depthTest);
			Debug.DrawLine(luf, lfd, color, duration, depthTest);
			Debug.DrawLine(lub, lbd, color, duration, depthTest);

			Debug.DrawLine(rdf, lfd, color, duration, depthTest);
			Debug.DrawLine(rdf, rdb, color, duration, depthTest);
			Debug.DrawLine(lfd, lbd, color, duration, depthTest);
			Debug.DrawLine(lbd, rdb, color, duration, depthTest);
#endif
		}

		/// <summary>- Debugs a cylinder.</summary>
		/// <param name='start'>- The position of one end of the cylinder.</param>
		/// <param name='end'>- The position of the other end of the cylinder.</param>
		/// <param name='color'>- The color of the cylinder.</param>
		/// <param name='radius'>- The radius of the cylinder.</param>
		/// <param name='duration'>- How long to draw the cylinder.</param>
		/// <param name='depthTest'>- Whether or not the cylinder should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCylinder(Vector3 start, Vector3 end, Color color = default(Color), float radius = 1, float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			Vector3
				up = (end - start).normalized * radius,
				forward = Vector3.Slerp(up, -up, 0.5f),
				right = Vector3.Cross(up, forward).normalized * radius;

			//Radial circles
			DrawCircle(start, up, color, radius, duration, depthTest);
			DrawCircle(end, -up, color, radius, duration, depthTest);
			DrawCircle((start + end) * 0.5f, up, color, radius, duration, depthTest);

			//Side lines
			Debug.DrawLine(start + right, end + right, color, duration, depthTest);
			Debug.DrawLine(start - right, end - right, color, duration, depthTest);

			Debug.DrawLine(start + forward, end + forward, color, duration, depthTest);
			Debug.DrawLine(start - forward, end - forward, color, duration, depthTest);

			//Start endcap
			Debug.DrawLine(start - right, start + right, color, duration, depthTest);
			Debug.DrawLine(start - forward, start + forward, color, duration, depthTest);

			//End endcap
			Debug.DrawLine(end - right, end + right, color, duration, depthTest);
			Debug.DrawLine(end - forward, end + forward, color, duration, depthTest);
#endif
		}

		/// <summary>- Debugs a cone.</summary>
		/// <param name='position'>- The position for the tip of the cone.</param>
		/// <param name='direction'>- The direction for the cone gets wider in.</param>
		/// <param name='angle'>- The angle of the cone.</param>
		/// <param name='color'>- The color of the cone.</param>
		/// <param name='duration'>- How long to draw the cone.</param>
		/// <param name='depthTest'>- Whether or not the cone should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCone(Vector3 position, Vector3 direction, Color color = default(Color), float angle = 45f, float duration = 0, bool depthTest = false, bool flip = false)
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

			Debug.DrawRay(position, slerpedVector.normalized * dist, color, duration, depthTest);
			Debug.DrawRay(position, Vector3.Slerp(forward, -up, angle / 90.0f).normalized * dist, color, duration, depthTest);
			Debug.DrawRay(position, Vector3.Slerp(forward, right, angle / 90.0f).normalized * dist, color, duration, depthTest);
			Debug.DrawRay(position, Vector3.Slerp(forward, -right, angle / 90.0f).normalized * dist, color, duration, depthTest);

			DrawCircle(position + forward, direction, color, (forward - (slerpedVector.normalized * dist)).magnitude, duration, depthTest);
			DrawCircle(position + (forward * 0.5f), direction, color, ((forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude, duration, depthTest);
#endif
		}

		/// <summary>- Debugs a capsule.</summary>
		/// <param name='start'>- The position of one end of the capsule.</param>
		/// <param name='end'>- The position of the other end of the capsule.</param>
		/// <param name='color'>- The color of the capsule.</param>
		/// <param name='radius'>- The radius of the capsule.</param>
		/// <param name='duration'>- How long to draw the capsule.</param>
		/// <param name='depthTest'>- Whether or not the capsule should be faded when behind other objects.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(Vector3 point1, Vector3 point2, float radius = 1f, Color color = default(Color), float duration = 0f, bool depthTest = false)
		{
#if UNITY_EDITOR
			if (point1 == point2)
			{
				DrawWireSphere(point1, radius, color, duration, depthTest);
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

				//Radial circles
				DrawCircle(point1, up, color, radius, duration, depthTest);
				DrawCircle(point2, -up, color, radius, duration, depthTest);

				//Side lines
				DrawLine(point1 + right, point2 + right, color, duration, depthTest);
				DrawLine(point1 - right, point2 - right, color, duration, depthTest);

				DrawLine(point1 + forward, point2 + forward, color, duration, depthTest);
				DrawLine(point1 - forward, point2 - forward, color, duration, depthTest);

				for (int i = 1; i < 26; i++)
				{
					//Start endcap
					DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + point1, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + point1, color, duration, depthTest);
					DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + point1, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + point1, color, duration, depthTest);
					DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + point1, Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + point1, color, duration, depthTest);
					DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + point1, Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + point1, color, duration, depthTest);

					//End endcap
					DrawLine(Vector3.Slerp(right, up, i / 25.0f) + point2, Vector3.Slerp(right, up, (i - 1) / 25.0f) + point2, color, duration, depthTest);
					DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + point2, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + point2, color, duration, depthTest);
					DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + point2, Vector3.Slerp(forward, up, (i - 1) / 25.0f) + point2, color, duration, depthTest);
					DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + point2, Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + point2, color, duration, depthTest);
				}
			}
#endif
		}
		#endregion Shapes

		#region Cube
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			DrawBox(new Box(origin, halfExtents, orientation), color, duration, depthTest);
#endif
		}
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBox(Box box, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color, duration, depthTest);
			Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color, duration, depthTest);
			Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color, duration, depthTest);
			Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color, duration, depthTest);

			Debug.DrawLine(box.backTopLeft, box.backTopRight, color, duration, depthTest);
			Debug.DrawLine(box.backTopRight, box.backBottomRight, color, duration, depthTest);
			Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color, duration, depthTest);
			Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color, duration, depthTest);

			Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color, duration, depthTest);
			Debug.DrawLine(box.frontTopRight, box.backTopRight, color, duration, depthTest);
			Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color, duration, depthTest);
			Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color, duration, depthTest);
#endif
		}
		//Draws just the box at where it is currently hitting.
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			//Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
			origin = origin + (direction.normalized * hitInfoDistance);
			DrawBox(origin, halfExtents, orientation, color, duration, depthTest);
#endif
		}

		//Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
#if UNITY_EDITOR
			direction.Normalize();
			Box bottomBox = new Box(origin, halfExtents, orientation);
			Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

			Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color, duration, depthTest);
			Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color, duration, depthTest);
			Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color, duration, depthTest);
			Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color, duration, depthTest);
			Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color, duration, depthTest);
			Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color, duration, depthTest);
			Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color, duration, depthTest);
			Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color, duration, depthTest);

			DrawBox(bottomBox, color, duration, depthTest);
			DrawBox(topBox, color, duration, depthTest);
#endif
		}
        #endregion // Box

        #region Handles
        /// <summary>DrawLabel on scene view</summary>
        /// <param name="position"></param>
        /// <param name="text"></param>
        /// <param name="offset"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DrawLabel(Vector3 position, string text, Vector2 offset)
        {
			GizmosExtend.DrawLabel(position, text, offsetX: offset.x, offsetY: offset.y);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DrawLabel(Vector3 position, string text, float offsetX = 0f, float offsetY = 0f, bool background = true)
        {
			GizmosExtend.DrawLabel(position, text, offsetX, offsetY, background);
        }
        #endregion Handles

        #region Triangle
        public static void DrawTriangle(Triangle triangle, Color color = default(Color), float duration = 0, bool depthTest = false)
        {
			Debug.DrawLine(triangle[0], triangle[1], color, duration, depthTest);
			Debug.DrawLine(triangle[1], triangle[2], color, duration, depthTest);
			Debug.DrawLine(triangle[2], triangle[0], color, duration, depthTest);
        }

		public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color = default(Color), float duration = 0, bool depthTest = false)
		{
			Debug.DrawLine(a, b, color, duration, depthTest);
			Debug.DrawLine(b, c, color, duration, depthTest);
			Debug.DrawLine(c, a, color, duration, depthTest);
		}
        #endregion Triangle
    }
}
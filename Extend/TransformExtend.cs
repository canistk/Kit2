using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Array = System.Array;

namespace Kit2
{
	public static class TransformExtend
    {
        #region Candy tools, relative position
        /*TransformPoint is used, as the name implies to transform a point from local space to global space. For example, if the collider of a player is offset by half their height so that the transform position is at the player's feet, to get the world space position of the collider center, it would be playerTransform.TransformPoint(collider.center) because that world-space position will change if any one of the player's position, rotation, or scale changes.
         */

        /// <summary>As same as TransformPoint</summary>
        /// <param name="transform"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <see cref="http://docs.unity3d.com/412/Documentation/ScriptReference/Transform.TransformPoint.html"/>
        /// <seealso cref="http://answers.unity3d.com/questions/154176/transformtransformpoint-vs-transformdirection.html"/>
        /// <seealso cref="http://answers.unity3d.com/questions/1021968/difference-between-transformtransformvector-and-tr.html"/>
        public static Vector3 PositionLocalToWorld(this Transform transform, Vector3 position)
        {
            return transform.TransformPoint(position);
        }
        /// <summary>As same as Inverse Transform Point</summary>
        /// <param name="transform"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <see cref="http://docs.unity3d.com/412/Documentation/ScriptReference/Transform.InverseTransformPoint.html"/>
        public static Vector3 PositionWorldToLocal(this Transform transform, Vector3 position)
        {
            return transform.InverseTransformPoint(position);
        }

        /*TransformDirection is used to transform a direction. For example, if there were a friendly AI that always wanted to face the same direction as the player, it would set its rotation to Quaternion.LookDirection(playerTransform.TransformDirection(Vector3.forward)). There's actually a convenience property on transform called .forward that does exactly this, but bear with me. This rotates Vector3.forward (which is 0,0,1) to face the direction of the player's forward direction. So if the player's rotation is in the default orientation, it will just return 0,0,1. But if the player were looking straight down, it would return 0,-1,0. Since TransformDirection only cares about the rotation, note that the magnitude is preserved.
         */
        /// <summary>As same as TransformDirection</summary>
        /// <param name="transform"></param>
        /// <param name="localDirection"></param>
        /// <returns></returns>
        /// <see cref="http://docs.unity3d.com/412/Documentation/ScriptReference/Transform.TransformDirection.html"/>
        public static Vector3 DirectionLocalToWorld(this Transform transform, Vector3 localDirection)
        {
            return transform.TransformDirection(localDirection);
        }
        /// <summary>As same as Inverse Transform Direction</summary>
        /// <param name="transform"></param>
        /// <param name="worldDirection"></param>
        /// <returns></returns>
        /// <see cref="http://docs.unity3d.com/412/Documentation/ScriptReference/Transform.InverseTransformDirection.html"/>
        public static Vector3 DirectionWorldToLocal(this Transform transform, Vector3 worldDirection)
        {
            return transform.InverseTransformDirection(worldDirection);
        }
        
        /*TransformVector, which seems to be the same as TransformDirection, but takes scale into account and will thus change the return value's magnitude accordingly (and probably the direction too if the scale is nonuniform). I'm not actually sure what this is useful for, since my use cases always fall under the first two, but clearly you've found a use for it!
         */
        public static Vector3 DirectionDistanceLocalToWorld(this Transform transform, Vector3 vector)
        {
            return transform.TransformVector(vector);
        }
        public static Vector3 DirectionDistanceWorldToLocal(this Transform transform, Vector3 vector)
        {
            return transform.InverseTransformVector(vector);
        }

		/// <summary>
		/// Change Pivot coordinate
		/// and then Re-apply child's coordinate before change.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="pos"></param>
		/// <param name="rot"></param>
		/// <param name="updatePos"></param>
		/// <param name="updateRot"></param>
		public static void ChangePivot(this Transform parent, Vector3 pos, Quaternion rot, bool updatePos = true, bool updateRot = true)
        {
			if (!updatePos && !updateRot)
				return; // do nothing.

			int cnt = parent.childCount;
			Transform[] c = new Transform[cnt];
			Vector3[] p = new Vector3[cnt];
			Quaternion[] r = new Quaternion[cnt];
			for (int i = 0; i < cnt; i++)
			{
				c[i] = parent.GetChild(i);
				p[i] = c[i].position;
				r[i] = c[i].rotation;
			}

			// DebugExtend.DrawLine(parent.position, pos, Color.magenta, 10f);
			if (updatePos && updateRot)
			{
				parent.SetPositionAndRotation(pos, rot);
				for (int i = 0; i < cnt; i++)
					c[i].SetPositionAndRotation(p[i], r[i]);
			}
			else
			{
				if (updatePos)
				{
					parent.position = pos;
					for (int i = 0; i < cnt; i++)
						c[i].position = p[i];
				}
				else if (updateRot)
				{
					parent.rotation = rot;
					for (int i = 0; i < cnt; i++)
						c[i].rotation = r[i];
				}
			}
		}
        #endregion

        #region Destroy
        /// <summary>Destroies all gameobject in children.</summary>
		/// <param name="gameobject">Gameobject.</param>
		/// <param name="immediate">If set to <c>true</c> destroy immediate.</param>
		public static void DestroyOnlyChildrens(this GameObject gameobject, bool immediate)
		{
			gameobject.transform.DestroyOnlyChildrens(immediate);
		}
		/// <summary>Destroies all gameobject in children.</summary>
		/// <param name="transform">_transform.</param>
		/// <param name="immediate">If set to <c>true</c> destroy immediate.</param>
		public static void DestroyOnlyChildrens(this Transform transform, bool immediate)
		{
			List<GameObject> children=new List<GameObject>();
			foreach(Transform child in transform)
			{
				children.Add(child.gameObject);
			}
			children.ForEach(delegate(GameObject obj)
			{
				if( immediate ) MonoBehaviour.DestroyImmediate(obj);
				else MonoBehaviour.Destroy(obj);
			});
		}
        #endregion

        #region SendMessage
        /// <summary>Sends the message in childrens.</summary>
		/// <param name="transform">_transform.</param>
		/// <param name="message">_message.</param>
		/// <param name="obj">_object.</param>
		/// <param name="sendMessageOptions">_send message options.</param>
		public static void SendMessageInChildrens(this Transform transform,
		                                          string message,
		                                          object obj=null,
		                                          SendMessageOptions sendMessageOptions = SendMessageOptions.RequireReceiver)
		{
			if( obj==null )
				transform.SendMessage(message,sendMessageOptions);
			else
				transform.SendMessage(message,obj,sendMessageOptions);
			foreach(Transform child in transform)
				child.SendMessageInChildrens(message,obj, sendMessageOptions);
		}
		/// <summary>Sends the message in childrens.</summary>
		/// <param name="gameobject">_gameobject.</param>
		/// <param name="message">_message.</param>
		/// <param name="obj">_object.</param>
		/// <param name="sendMessageOptions">_send message options.</param>
		public static void SendMessageInChildrens(this GameObject gameobject,
		                                          string message,
		                                          object obj=null,
		                                          SendMessageOptions sendMessageOptions = SendMessageOptions.RequireReceiver)
		{
			gameobject.transform.SendMessageInChildrens(message,obj,sendMessageOptions);
		}
        #endregion

        #region Get Or Add
        /// <summary>Gets or add a component.</summary>
        /// <example><code>BoxCollider boxCollider = transform.GetOrAddComponent/<BoxCollider/>();</code></example>
		/// <seealso cref="http://wiki.unity3d.com/index.php/Singleton"/>
        /// <seealso cref="http://wiki.unity3d.com/index.php/GetOrAddComponent"/>
		public static T GetOrAddComponent<T> (this Component component) where T: Component
		{
			return component.gameObject.GetOrAddComponent<T>();
		}

        public static T GetOrAddComponent<T> (this GameObject gameobject) where T: Component
        {
			T rst = gameobject.GetComponent<T>();
			if (rst == null)
				rst = gameobject.AddComponent<T>();
			return rst;
        }

		/// <summary>To return the target type component
		/// or Create a child gameobject which contain the target type component.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="childName"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static T GetOrAddChildObject<T>(this Component self, string childName) where T : Component
		{
			T rst = self.GetComponentInChildren<T>(true);
			if (rst == null)
			{
				GameObject go = new GameObject(childName);
				go.transform.SetParent(self.transform, false);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.identity;
				rst = go.AddComponent<T>();
			}
			return rst;
		}
		/// <summary>Get Component within child transform (extension)</summary>
		/// <typeparam name="T">Component class</typeparam>
		/// <param name="component">this</param>
		/// <param name="depth">searching depth, stop search when reching Zero.</param>
		/// <param name="includeInactive">include inactive gameobject.</param>
		/// <param name="includeSelf">include the caller itself.</param>
		/// <returns>Return first found component</returns>
		/// <remarks>The performance are much slower then the original, depend on how many level needed to drill down.</remarks>
		public static T GetComponentInChildren<T>(this Component component, int depth = 1, bool includeInactive = false, bool includeSelf = true) where T : Component
		{
			T rst = null;
			if (includeSelf)
				rst = component.GetComponent<T>();
			else if(depth < 0)
				Debug.LogError("Syntax Error: GetComponentInChildren<" + typeof(T).Name + "> You searching for nothing.", component.gameObject);
			if (depth > 0 && rst == null)
			{
				depth--;
				foreach (Transform child in component.transform)
				{
					if (includeInactive && !child.gameObject.activeSelf)
						continue;
					rst = child.GetComponentInChildren<T>(depth, includeInactive, true);
					if (rst != null)
						return rst;
				}
			}
			return rst;
		}

		/// <summary>Get Component within child transform</summary>
		/// <typeparam name="T">Component class</typeparam>
		/// <param name="gameobject">this</param>
		/// <param name="depth">searching depth, stop search when reching Zero.</param>
		/// <param name="includeInactive">include inactive gameobject.</param>
		/// <param name="includeSelf">include the caller itself.</param>
		/// <returns>Return first found component</returns>
		/// <remarks>The performance are much slower then the original, depend on how many level needed to drill down.</remarks>
		public static T GetComponentInChildren<T>(this GameObject gameobject, int depth = 1, bool includeInactive = false, bool includeSelf = true) where T : Component
		{
			return gameobject.transform.GetComponentInChildren<T>(depth, includeInactive, includeSelf);
		}

		#endregion

		#region Interface
		public static bool HasInterface<T>(this Component obj)
        {
            return obj.GetComponent(typeof(T)) != null;
        }
        public static T[] GetInterfacesInChildren<T>(this Component obj, bool includeInactive = false) where T : class
        {
			Component[] comps = obj.GetComponentsInChildren<Component>(includeInactive); // single API call.
			int cnt = comps.Length; // reduce property access time
			List<T> rst = new List<T>(cnt);
			for (int i = 0; i < cnt; i++)
			{
				if (comps[i] is T)
					rst.Add(comps[i] as T);
			}
			return rst.ToArray();
			
			// Linq
            //return obj
            //    .GetComponentsInChildren<Component>(includeInactive)
            //    .OfType<T>();
        }

		public static T GetInterface<T>(this Component obj)
		{
			//Component[] comps = obj.GetComponents<Component>(); // single API call.
			//int cnt = comps.Length; // reduce property access time
			//for (int i = 0; i < cnt; i++)
			//{
			//	if (comps[i] is T)
			//		return comps[i]; // How ?
			//}
			//return default(T);

			return obj.GetComponents<Component>().OfType<T>().FirstOrDefault();
		}
		
		#endregion

		#region Search
		/// <summary>Recursive allocate target transform in hierarchy ,depend on giving transform</summary>
		/// <remarks><see cref="http://forum.unity3d.com/threads/transform-find-doesnt-work.12949/"/></remarks>
		/// <param name="self"></param>
		/// <param name="name"></param>
		/// <returns>Null or Transform</returns>
		public static Transform FindInChildren(this Transform self, string name)
		{
			bool _condition(Transform t)
				=> t.name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase);

			foreach (var o in SearchChildrens(self, _condition))
				return o;
			return null;
		}

		public static IEnumerable<Transform> SearchChildrens(this Transform self, System.Func<Transform, bool> filter = null)
		{
			var queue = new Queue<Transform>();
			queue.Enqueue(self);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (filter == null || filter.Invoke(current))
					yield return current;
				for (int i = 0; i < current.childCount; ++i)
					queue.Enqueue(current.GetChild(i));
			}
		}

		/// <summary>Find the direct children right under the giving parent.</summary>
		/// <param name="self"></param>
		/// <param name="parent"></param>
		/// <param name="directNode"></param>
		/// <returns></returns>
		public static bool FindChildInParent(this Transform self, Transform parent, out Transform directNode)
		{
			if (self.IsChildOf(parent))
			{
				directNode = self;
				while (directNode.parent != parent)
					directNode = directNode.parent;
				return true;
			}
			directNode = null;
			return false;
		}

		/// <summary>Generate list of path from this transform to target parent.
		/// In order to sync hierarchy across server & client.</summary>
		/// <param name="self"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static string[] GetPathToParent(this Transform self, Transform parent)
        {
			if (parent == null || self == null)
				throw new System.NullReferenceException();
			if (!self.IsChildOf(parent))
				throw new System.Exception($"{self.name} aren't children from {parent.name}.");
			if (parent == self)
			{
				return new string[1] { self.name };
			}
			List<string> path = new List<string>(10);
			Transform pt = self;
			path.Add(pt.name);
			do
			{
				pt = pt.parent;
				path.Add(pt.name);
			}
			while (pt != parent);
			// path.Reverse(); it's a stack FILO
			return path.ToArray();
        }

		/// <summary>serialize hierarchy by gameobject's name,
		/// In order to sync hierarchy across server & client.</summary>
		/// <param name="self"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public static Transform FindChildByPath(this Transform self, IList<string> path, bool includeParent = false)
        {
			if (path == null || path.Count == 0)
			{
				Debug.LogError("Invalid Path");
				return null;
			}

			// Ensure this is parent
			int depth = path.Count - 1;
			Transform anchor = self;
			if (includeParent && !anchor.name.Equals(path[depth]))
            {
				Debug.LogError($"{nameof(FindChildByPath)}, Path parent missmatch, Expect {path[depth]} but giving {anchor.name}, ");
				return null;
            }

			while (depth--> 0)
            {
				// iter all child in this level.
				bool found = false;
				int cnt = anchor.childCount;
				for (int i = 0; i < cnt && !found; i++)
				{
					if (anchor.GetChild(i).name.Equals(path[depth]))
					{
						// Common case, Found child in this level.
						anchor = anchor.GetChild(i);
						//Debug.Log($"Found {anchor}", anchor);
						found = true;
					}
				}
				if (!found)
				{
					Debug.LogError($"{nameof(FindChildByPath)}, fail on <{anchor.name}>[{depth}]-[{path[depth]}] not found--\n{{ {string.Join(",", path)} }}", anchor);
					return null;
				}
			}
			return anchor;
        }

        #endregion

        #region Filter
		public static int FilterChildColliders(this Transform self, ref RaycastHit[] arr, int validCnt = -1)
        {
			RaycastHit[] tmp = new RaycastHit[arr.Length];
			int k = 0;
			int cnt = validCnt < 0 ? arr.Length : Mathf.Min(validCnt, arr.Length);
			for (int i = 0; i < cnt; i++)
			{
				if (arr[i].transform.IsChildOf(self))
					continue; // ignore body parts.
				else
				{
					tmp[k] = arr[i];
					k++;
				}
			}
			Array.Copy(tmp, arr, k);
			return k;
		}

		public static int FilterChildColliders<T>(this Transform self, ref T[] arr, int validCnt = -1)
			where T : Component
		{
			T[] tmp = new T[arr.Length];
			int k = 0;
			int cnt = validCnt < 0 ? arr.Length : Mathf.Min(validCnt, arr.Length);
			for (int i = 0; i < cnt; i++)
			{
				if (arr[i].transform.IsChildOf(self))
					continue; // ignore body parts.
				else
				{
					tmp[k] = arr[i];
					k++;
				}
			}
			Array.Copy(tmp, arr, k);
			return k;
		}
		#endregion Filter
	}
}

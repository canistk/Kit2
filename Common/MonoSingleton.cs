//#define SHOW_WARNING
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Kit2
{
	public class MonoSingletonConfigAttribute : System.Attribute
	{
		public bool DontDestroyOnLoad { get; set; }
		public string LoadPath { get; set; }
		public bool UseAddressable { get; set; }
	}

	/// <summary>SingleTon extend methods with <see cref="MonoBehaviour"/></summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="WhenDuplicates"><see cref="DoNothing"/>, <see cref="RemoveLateComer"/>, <see cref="RemoveExisting"/></typeparam>
	/// <typeparam name="InstanceBehavior"><see cref="Manually"/>, <see cref="SearchHierarchy"/>, <see cref="AutoCreate"/></typeparam>
	public class MonoSingleton<T, WhenDuplicates, InstanceBehavior> : MonoSingletonBase
		where T : MonoSingleton<T, WhenDuplicates, InstanceBehavior>
		where WhenDuplicates : DuplicateAction, new()
		where InstanceBehavior : InstanceBehaviorAction, new()
	{
		private static T m_Instance = null;

		/// <summary>When instance is null, depend on the instance behavior template this will TRY to return singleton instance.
		/// <see cref="MonoSingleton{T, WhenDuplicates, InstanceBehavior}"/>, <seealso cref="InstanceBehavior"/></summary>
		/// <remarks>bad performance while instance is null</remarks>
		public static T Instance
		{
			get
			{
				if (!IsAppQuit && m_Instance == null)
				{
					var attr = typeof(T).GetCustomAttribute(typeof(MonoSingletonConfigAttribute), false) as MonoSingletonConfigAttribute;
					if (attr != null && !string.IsNullOrEmpty(attr.LoadPath))
					{
						var prefab = attr.UseAddressable ?
							Addressables.LoadAssetAsync<GameObject>(attr.LoadPath).WaitForCompletion() :
							Resources.Load<GameObject>(attr.LoadPath);

						// spawn
						var token = GameObject.Instantiate(prefab, null, true);
						if (token == null)
						{
							throw new System.Exception("Fail to create new MonoSingleton.");
						}
						token.gameObject.name = GetTokenName();
						if (token.GetComponent<T>() == null)
						{
							throw new System.NullReferenceException($"Prefab {attr.LoadPath} doesn't contain {typeof(T).Name} component.");
						}
						// Awake & regist should being called on spawn
					}
					else if ((new InstanceBehavior()).Action == (new SearchHierarchy()).Action)
					{
						T searchObject = FindObjectOfType<T>();
						if (searchObject == null)
						{
							new GameObject(GetTokenName(), typeof(T));
							// Unity3D : awake MUST run here, otherwise throw null exception
						}
						else
						{
							searchObject.Awake(); // create multiple time on demand, trigger early awake, otherwise throw null exception
						}
					}
					else if ((new InstanceBehavior()).Action == (new Manually()).Action)
					{
						throw new System.NullReferenceException(typeof(T).Name + " : Singleton without instance.");
					}
					else if ((new InstanceBehavior()).Action == (new AutoCreate()).Action)
					{
						new GameObject(GetTokenName(), typeof(T));
						// Unity3D : awake MUST run here, otherwise throw null exception
					}
				}
				return m_Instance;

				string GetTokenName()
				{
					return $"{typeof(T).Name}(singleton)";
				}
			}
		}

		/// <summary>Standard normal instance, without any magic feature, only return instance, even it's null.</summary>
		public static T InstanceWithoutCreate { get { return m_Instance; } }
		protected virtual void Awake()
		{
			if (IsDestroying || IsAppQuit)
				return; // skip if destroying or app quit

			if (m_Instance != null)
			{
				if (m_Instance.GetInstanceID() != this.GetInstanceID())
				{
					WhenDuplicates duplicateAction = new WhenDuplicates();
					// when duplicate instance detected
					if (duplicateAction.Action == (new RemoveLateComer()).Action)
					{
#if SHOW_WARNING
						Debug.LogWarning("Destroying late singleton: "+this, this);
#endif
						enabled = false;
						Destroy(gameObject);
					}
					else if (duplicateAction.Action == (new RemoveExisting()).Action)
					{
#if SHOW_WARNING
						Debug.LogWarning("Destroying existing singleton: "+_instance, this);
#endif
						m_Instance.enabled = false;
						Destroy(m_Instance.gameObject);
						m_Instance = (T)this;
					}
				}
			}
			else
			{
				m_Instance = (T)this;
			}
		}

		protected bool IsDestroying { get; private set; } = false;
		protected virtual void OnDestroy()
		{
			IsDestroying = true;
			// unless the instance refers to a different object, set to null.
			// (NOTE: checking if (_instance == this) doesn't work,
			// since Unity play tricks with compare(==) operator.)
			if (ReferenceEquals(m_Instance, this))
			{
#if SHOW_WARNING
				Debug.LogWarning("Destroying singleton: "+this, this);
#endif
				m_Instance = null;
			}
		}

		protected static bool IsAppQuit { get; private set; } = false;
		protected virtual void OnApplicationQuit()
		{
			IsAppQuit = true;
		}
	}

	/// <summary>SingleTon extend methods</summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="WhenDuplicates"><see cref="DoNothing"/>, <see cref="RemoveLateComer"/>, <see cref="RemoveExisting"/></typeparam>
	public class MonoSingleton<T, WhenDuplicates> : MonoSingleton<T, WhenDuplicates, Manually>
		where T : MonoSingleton<T, WhenDuplicates>
		where WhenDuplicates : DuplicateAction, new()
	{ }

	/// <summary>SingleTon extend methods</summary>
	/// <typeparam name="T"></typeparam>
	public class MonoSingleton<T> : MonoSingleton<T, DoNothing, Manually> where T : MonoSingleton<T>
	{ }

	/// <summary>Helper class, so singletons can be found with GetComponent<Singleton<T>>, without knowing their specific type.</summary>
	public abstract class MonoSingletonBase : MonoBehaviour
	{ }

	public abstract class DuplicateAction { abstract public int Action { get; } }
	public class DoNothing : DuplicateAction { public override int Action { get { return 1; } } }
	public class RemoveLateComer : DuplicateAction { public override int Action { get { return 2; } } }
	public class RemoveExisting : DuplicateAction { public override int Action { get { return 3; } } }

	public abstract class InstanceBehaviorAction { abstract public int Action { get; } }
	public class Manually : InstanceBehaviorAction { public override int Action { get { return 1; } } }
	public class SearchHierarchy : InstanceBehaviorAction { public override int Action { get { return 2; } } }
	public class AutoCreate : InstanceBehaviorAction { public override int Action { get { return 3; } } }
}
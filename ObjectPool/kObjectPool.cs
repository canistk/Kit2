using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if USE_ADDRESSABLE
using UnityEngine.AddressableAssets;
#endif
using UnityEngine.ResourceManagement.AsyncOperations;
using Kit2.Task;

namespace Kit2.ObjectPool
{
	public interface ISpawnToken
	{
		public void OnSpawn(ISpawner pool);
		public void OnDespawn();
	}

	public interface ISpawner
	{
		public bool IsSpawned(GameObject token);

		public bool Despawn(GameObject go);
	}
	public class kObjectPool : MonoBehaviour, System.IDisposable, ISpawner
	{
		#region Event
		public delegate void TokenEvent(GameObject token);
		public event TokenEvent Event_Spawn;
		public event TokenEvent Event_Despawn;
		#endregion Event

		public static List<kObjectPool> Instances { get; } = new List<kObjectPool>(10);

		#region System
		protected virtual bool ShouldAutoRegisterPool() => false;
		protected virtual void Awake()
		{
			PreloadOnDemend();
			if (ShouldAutoRegisterPool())
			{
				Instances.Add(this);
			}
		}
		public bool IsDestroy { get; private set; } = false;
		protected virtual void OnDestroy()
		{
			if (IsDestroy)
				return;
			Instances.Remove(this);
			Dispose();
			IsDestroy = true; // last step, so we can reparent all child
		}

		private List<MyTaskBase> m_Tasks = null;
		protected List<MyTaskBase> tasks
		{
			get
			{
				if (m_Tasks == null)
					m_Tasks = new List<MyTaskBase>(8);
				return m_Tasks;
			}
		}
		protected virtual void Update()
		{
			MyTaskHandler.ManualParallelUpdate(tasks);
		}

		protected static bool IsAppQuit { get; private set; } = false;
		protected void OnApplicationQuit()
		{
			if (!IsAppQuit)
				IsAppQuit = true;
		}
		#endregion System

		#region Preload
		[System.Serializable]
		public struct PreloadInfo
		{
			public GameObject prefab;
			public float interval;
			public int count;
		}

		[SerializeField] protected PreloadInfo[] m_PreloadConfig = { };

		protected void PreloadOnDemend()
		{
			if (m_Category != null)
				return;
			m_Category = new Dictionary<GameObject, PrefabCategory>(10);
			if (m_PreloadConfig == null)
				m_PreloadConfig = new PreloadInfo[0];
			for (int i = 0; i < m_PreloadConfig.Length; ++i)
			{
				if (m_PreloadConfig[i].prefab == null)
					continue;
				Preload(m_PreloadConfig[i]);
			}
		}

		public void Preload(GameObject prefab, int preloadAmount, float interval)
			=> Preload(new PreloadInfo { prefab = prefab, interval = interval, count = preloadAmount });

		public void Preload(PreloadInfo preloadInfo)
		{
			var cat = GetOrAddCategory(preloadInfo.prefab, false);
			tasks.Add(new PreloadTask(cat, preloadInfo, transform));
		}

		private class PreloadTask : MyTaskWithState
		{
			private readonly Transform parent;
			private readonly PreloadInfo preloadInfo;
			private readonly PrefabCategory spawner;
			private float m_Last;
			public PreloadTask(PrefabCategory prefabInfo, PreloadInfo preloadInfo, Transform parent)
			{
				this.spawner = prefabInfo;
				this.preloadInfo = preloadInfo;
				this.parent = parent;
				m_Last = Time.realtimeSinceStartup;
			}

			protected override void OnEnter() { }
			protected override bool ContinueOnNextCycle()
			{
				if (spawner == null)
					return false; // fatel error

				if (spawner.total >= preloadInfo.count)
				{
					Debug.Log($"[ObjectPool] Preload complete {preloadInfo.prefab.name}, amount = {preloadInfo.count}");
					return false; // early end for enough token spawn.
				}

				var diff = Time.realtimeSinceStartup - m_Last;
				if (diff < preloadInfo.interval)
					return true; // wait for interval.

				m_Last = Time.realtimeSinceStartup;
				spawner.ReturnToken(spawner.NewToken(parent));
				return spawner.total < preloadInfo.count;
			}
			protected override void OnComplete() { }
		}

		#endregion Preload

		#region Pooling
		private class PrefabCategory : System.IDisposable
		{
			public readonly bool isAddressable;
			public readonly GameObject prefab;
			public AsyncOperationHandle<GameObject> handle;
			public readonly Transform parent;
			public HashSet<GameObject> activeObjs;
			public Queue<GameObject> deactiveObjs;
			private bool isDisposed;

			public PrefabCategory(bool _isAddressable, object stringOrPrefab, Transform _parent)
			{
				this.parent = _parent;
				this.activeObjs = new HashSet<GameObject>(10);
				this.deactiveObjs = new Queue<GameObject>(10);
				this.isAddressable = _isAddressable;
				this.handle = default;
				this.prefab = null;

				if (stringOrPrefab == null)
				{
					throw new System.ArgumentNullException(nameof(stringOrPrefab), "Prefab cannot be null");
				}
				else if (stringOrPrefab is GameObject _prefab)
				{
					this.prefab = _prefab;
				}
				if (stringOrPrefab is string path)
				{
#if USE_ADDRESSABLE
					if (_isAddressable)
					{
						this.handle = Addressables.LoadAssetAsync<GameObject>(path);
						this.prefab = this.handle.WaitForCompletion();
					}
					else
#endif
					{
						this.prefab = Resources.Load<GameObject>(path);
					}
				}
				else
				{
					throw new System.NotImplementedException($"Invalid Spawn cases. {stringOrPrefab}");
				}
			}

			public int total => activeObjs.Count + deactiveObjs.Count;

			public GameObject NewToken(Transform parent)
			{
				if (isDisposed)
					throw new System.Exception("kObjectPool was Disposed");
				bool oldState = prefab.activeSelf;
				prefab.SetActive(false);
				var token = Instantiate(prefab, parent);

				// U3D bug, enable this will also leave the token in scene
				//token.hideFlags = HideFlags.DontSave;

				token.name = token.name.Replace("Clone", $"#{total}");
				prefab.SetActive(oldState);
				return token;
			}

			public void GetOrAddToken(out GameObject token, in Transform parent, bool worldStay)
			{
				if (isDisposed)
					throw new System.Exception("kObjectPool was Disposed");
				token = null;
				while (deactiveObjs.Count > 0)
				{
					token = deactiveObjs.Dequeue();
					if (token != null)
					{
						if (token.transform.parent != parent)
							token.transform.SetParent(parent, worldStay);
						break;
					}
				}

				if (token == null)
				{
					// not enough
					token = NewToken(parent);
				}
				activeObjs.Add(token);
			}

			public void ReturnToken(GameObject token)
			{
				if (isDisposed)
					throw new System.Exception("kObjectPool was Disposed");
				if (token == null)
					return;
				try
				{
					if (token.activeSelf)
					{
						token.SetActive(false);
					}

					if (!token.transform.IsChildOf(parent))
						token.transform.SetParent(parent, true);
				}
				catch
				{ }
				finally
				{
					activeObjs.Remove(token);
					deactiveObjs.Enqueue(token);
				}
			}

			protected virtual void Dispose(bool disposing)
			{
				if (isDisposed)
					return;

				if (disposing)
				{
					// dispose managed state (managed objects)
					if (activeObjs.Count > 0)
					{
						var gos = activeObjs.ToArray();
						foreach (var go in gos)
						{
							if (go)
								ReturnToken(go);
						}
						gos = null;
					}
					activeObjs.Clear();
					while (deactiveObjs.Count > 0)
					{
						var token = deactiveObjs.Dequeue();
						if (token)
							GameObject.Destroy(token);
					}
					deactiveObjs.Clear();

					if (isAddressable)
					{
#if USE_ADDRESSABLE
						Addressables.Release(handle);
#endif
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				activeObjs = null;
				deactiveObjs = null;
				isDisposed = true;
			}

			// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
			~PrefabCategory()
			{
				// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
				Dispose(disposing: false);
			}

			public void Dispose()
			{
				// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
				Dispose(disposing: true);
				System.GC.SuppressFinalize(this);
			}
		}

		private Dictionary<GameObject /*token*/, GameObject /*prefab*/> m_ActiveTokens = new Dictionary<GameObject, GameObject>(100);
		private Dictionary<GameObject /*prefab*/, PrefabCategory> m_Category = null;
		private Dictionary<GameObject /*prefab*/, PrefabCategory> category
		{
			get
			{
				if (m_Category == null)
				{
					PreloadOnDemend();
				}
				return m_Category;
			}
		}

		public IEnumerable<GameObject> prefabs => category.Keys;
		protected IEnumerable<GameObject> spawned => m_ActiveTokens.Keys;

		private PrefabCategory GetOrAddCategory(object prefabOrString, bool isAddressable)
		{
			if (prefabOrString == null)
			{
				Debug.LogError("invalid prefab to spawn.", this);
				return null;
			}

			GameObject prefab = null;
			if (prefabOrString is string path)
			{
#if USE_ADDRESSABLE
				if (isAddressable)
				{
					var oper = Addressables.LoadAssetAsync<GameObject>(path);
					prefab = oper.WaitForCompletion();
				}
				else
#endif
				{
					prefab = Resources.Load<GameObject>(path);
				}
			}
			else if (prefabOrString is GameObject _prefab)
			{
				prefab = _prefab;
			}
			else
			{
				throw new System.NotImplementedException($"Invalid Spawn cases. {prefabOrString}");
			}

			if (!category.TryGetValue(prefab, out PrefabCategory info))
			{
				category.Add(prefab, info = new PrefabCategory(isAddressable, prefab, transform));
			}
			return info;
		}

		Dictionary<GameObject, ISpawnToken[]> m_TokenDict = new Dictionary<GameObject, ISpawnToken[]>(8);
		protected GameObject InternalSpawn(object prefabOrString, bool isAddressable, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
		{
			if (IsAppQuit)
				return null;
#if !USE_ADDRESSABLE
			if (isAddressable)
			{
				Debug.LogError($"[{nameof(kObjectPool)}] Addressable is not supported in this build, please enable USE_ADDRESSABLE define symbol.", this);
				return null;
			}
#endif
			var info = GetOrAddCategory(prefabOrString, isAddressable);
			if (info == null)
				return null;
			info.GetOrAddToken(out var token, parent, worldStay); //parent == null means scene root 
			m_ActiveTokens.Add(token, info.prefab);
			if (!worldStay)
			{
				token.transform.SetPositionAndRotation(position, rotation);
			}
			token.SetActive(true);
			Event_Spawn.TryCatchDispatchEventError(o => o?.Invoke(token));
			var arr = token.GetComponentsInChildCache<ISpawnToken>(m_TokenDict, true);
			foreach (var o in arr)
			{
				o.OnSpawn(this);
			}
			return token;
		}

		protected bool InternalDespawn(GameObject token)
		{
			if (IsAppQuit)
				return false;
			if (IsDestroy && token == null)
				return false;
			if (!m_ActiveTokens.TryGetValue(token, out var prefab))
			{
				Debug.LogWarning($"{token} isn't spawned by spawn pool :{name}.", token);
				// could be deactive in m_Category > PrefabInfo.deactiveObjs, skip search due to performance issue.
				return false;
			}

			var arr = token.GetComponentsInChildCache<ISpawnToken>(m_TokenDict, true);
			foreach (var o in arr)
			{
				o.OnDespawn();
			}
			Event_Despawn.TryCatchDispatchEventError(o => o?.Invoke(token));

			if (!IsDestroy)
			{
				m_ActiveTokens.Remove(token);
				category[prefab].ReturnToken(token);
			}
			return true;
		}

		private GameObject ResetLocalPosRot(bool worldStay_, GameObject go)
		{
			if (!worldStay_)
			{
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.identity;
			}
			return go;
		}
		#endregion Pooling

		#region Public API
		public GameObject Spawn(GameObject prefab, Transform parent, bool worldStay = false)
			=> ResetLocalPosRot(worldStay, InternalSpawn(prefab, false, Vector3.zero, Quaternion.identity, parent, true));
		
		public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
			=> InternalSpawn(prefab, false, position, rotation, parent, worldStay);

		public GameObject Spawn(string prefabPath, bool isAddressable, Transform parent, bool worldStay = false)
			=> InternalSpawn(prefabPath, isAddressable, Vector3.zero, Quaternion.identity, parent, worldStay);

		public GameObject Spawn(GameObject prefab, bool isAddressable, Transform parent, bool worldStay = false)
			=> ResetLocalPosRot(worldStay, InternalSpawn(prefab, isAddressable, Vector3.zero, Quaternion.identity, parent, true));

		public GameObject Spawn(string prefabPath, bool isAddressable, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
			=> InternalSpawn(prefabPath, isAddressable, position, rotation, parent, worldStay);

		public GameObject Spawn(GameObject prefab, bool isAddressable, Vector3 position, Quaternion rotation, Transform parent, bool worldStay)
			=> InternalSpawn(prefab, isAddressable, position, rotation, parent, worldStay);

		public T Spawn<T>(T prefab, Transform parent, bool worldStay = false) where T : Component
		{
			var obj = Spawn(prefab.gameObject, parent, worldStay);
			if (obj.TryGetComponent<T>(out var component))
			{
				return component;
			}
			else
			{
				throw new System.Exception($"[{nameof(kObjectPool)}] Spawned object contains no {typeof(T).Name}");
			}
		}

		public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent, bool worldStay) where T : Component
		{
			var obj = Spawn(prefab.gameObject, position, rotation, parent, worldStay);
			if (obj.TryGetComponent<T>(out var component))
			{
				return component;
			}
			else
			{
				throw new System.Exception($"[{nameof(kObjectPool)}] Spawned object contains no {typeof(T).Name}");
			}
		}

		public bool IsSpawned(GameObject token)
		{
			if (token == null || m_ActiveTokens == null) return false;
			return m_ActiveTokens.ContainsKey(token);
		}

		public bool Despawn(GameObject token)
			=> InternalDespawn(token);

		public IEnumerable<GameObject> GetSpawnedObjects()
			=> m_ActiveTokens.Keys;
		#endregion Public API

		#region Tools
		public static string GetPrefabPath(GameObject prefab)
		{
#if UNITY_EDITOR
			const string resourcesPath = "resources/";
			string path = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
			if (string.IsNullOrEmpty(path))
				throw new System.Exception($"{prefab} is not a prefab");
			int index = path.ToLower().IndexOf(resourcesPath);
			if (index == -1)
				throw new System.Exception($"{prefab} didn't contain \"{resourcesPath}\" path.\n{path}");
			else
				index += resourcesPath.Length;
			path = path.Substring(index, path.Length - index);
			return path;
#else
            throw new System.NotImplementedException("'GetPrefabPath' Feature not support in release.");
#endif
		}
		#endregion Tools

		#region Disposable
		public bool Isdisposed { get; private set; } = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!Isdisposed)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					m_Tasks.Clear();
					m_ActiveTokens.Clear();
					m_TokenDict.Clear();
					var arr = m_Category.Values.ToArray();
					for (int i = 0; i < arr.Length; ++i)
					{
						try
						{
							arr[i].Dispose();
						}
						catch (System.Exception ex)
						{
							Debug.LogWarning(ex, this);
							// ex.DeepLogInvocationException($"{nameof(kObjectPool)}-{name} : {nameof(Dispose)}");
						}
					}
					m_Category.Clear();
				}

				m_PreloadConfig = null;
				m_Category = null;
				m_Tasks = null;
				m_ActiveTokens = null;
				m_TokenDict = null;
				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				Isdisposed = true;
			}
		}

		~kObjectPool()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}
		#endregion Disposable
	}
}

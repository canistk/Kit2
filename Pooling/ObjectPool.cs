using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
namespace Kit2.Pooling
{
	using Task = System.Threading.Tasks.Task;
	public class ObjectPool : MonoBehaviour, System.IDisposable
	{
		[SerializeField] PrefabPreloadSetting[] m_PrefabPreloadSettings = { };

		#region Data structure
		/// <summary>Preload Setting</summary>
		[System.Serializable]
		public class PrefabPreloadSetting : PrefabSetting
		{
			[Header("Preload")]
			[Tooltip("After Awake(), trigger auto preload in ? second.")]
			public float m_PreloadDelay = 0f;
			[Tooltip("The interval between each preload elements, distribute the performace overhead during GameObject.Instantiate")]
			public int m_PreloadFramePeriod = 0;
			[Tooltip("Auto preload prefab(s) base on giving amount")]
			public int m_PreloadAmount = 1;
		}

		/// <summary>Setting for spawn/despawn prefab behavior</summary>
		[System.Serializable]
		public class PrefabSetting
		{
			[Header("Reference")]
			[Tooltip("Name used for spawning the preloaded prefab(s), e.g. across network")]
			public string m_Name = string.Empty;
			public GameObject m_Prefab;
			[Header("Rule")]
			[Tooltip("The maximum amount of the current pool.")]
			public int m_PoolLimit = 100;
			[Tooltip("Will block any further spawn request, if current pool was reach maximum limit.")]
			public bool m_CullOverLimit = true;

			internal void Validate()
			{
				if (m_Name.Length == 0 && m_Prefab != null)
					m_Name = m_Prefab.name;
			}
		}

		/// <summary>Internal memory cache to handle spawn flow and keep instance reference</summary>
		private class TokenCache
		{
			public PrefabSetting setting;
			public Queue<GameObject> deactiveObjs;
			public List<GameObject> activeOjbjs;
			public int activeCount => activeOjbjs.Count;
			public int totalCount => deactiveObjs.Count + activeCount;
			private readonly Transform transform;
			public TokenCache(PrefabSetting setting, Transform op_root)
			{
				this.setting = setting;
				this.transform = op_root;
				activeOjbjs = new List<GameObject>(setting.m_PoolLimit);
				deactiveObjs = new Queue<GameObject>(setting.m_PoolLimit);
			}
			internal void Clear()
			{
				activeOjbjs.Clear();
				deactiveObjs.Clear();
			}
			public GameObject RentOrCreateToken(Vector3 position, Quaternion rotation, Transform parent, bool useParentParam)
			{
				GameObject go = default;
				if (deactiveObjs.Count > 0)
				{
					// Reuse token
					go = deactiveObjs.Dequeue();
					activeOjbjs.Add(go);
					go.transform.SetPositionAndRotation(position, rotation);
					if (go.transform.parent != parent && (parent != null || useParentParam))
						go.transform.SetParent(parent);
					go.SetActive(true);
					return go;
				}
				else
				{
					// Create instance for prefab.
					go = useParentParam ?
						Instantiate(setting.m_Prefab, position, rotation, parent) :
						Instantiate(setting.m_Prefab, position, rotation, transform); // follow current pool as parent, if not using parent param
					go.name += $" #{totalCount + 1:0000}";
					activeOjbjs.Add(go);
				}
				return go;
			}
			public void ReturnToken(GameObject token)
			{
				if (!activeOjbjs.Remove(token))
					Debug.LogError("Fail to remove token from active list.", token);
				deactiveObjs.Enqueue(token);
				try
				{
					token.transform.SetParent(transform);
					token.SetActive(false);
				}
				catch (System.Exception ex)
				{
					Debug.LogError($"Fail to return token {token.name}, {ex.Message}", token);
				}

			}

			internal void PreloadDeactiveToken()
			{
				var orgState = setting.m_Prefab.activeSelf;
				// deactive spawn, due to for keep awake on first spawn
				setting.m_Prefab.SetActive(false);
				
				GameObject go = Instantiate(setting.m_Prefab, transform.position, transform.rotation, transform);
				go.name += $" #{totalCount + 1:0000}";
				if (go.activeSelf)
					go.SetActive(false);

				setting.m_Prefab.SetActive(orgState);
			}
		}

		private Dictionary<GameObject, TokenCache> _cacheDict = null;
		private Dictionary<GameObject /* prefab */, TokenCache> m_CacheDict
		{
			get
			{
				if (_cacheDict == null)
					_cacheDict = new Dictionary<GameObject, TokenCache>(10);
				return _cacheDict;
			}
		}

		/// <summary>The table to increase the speed to tracking the token and it's prefab group.</summary>
		private Dictionary<GameObject /* token */, GameObject /* prefab */> m_AllSpawnedObjs = new Dictionary<GameObject, GameObject>();
		#endregion // Data structure

		#region Mono
		private void OnValidate()
		{
			
		}
		private void Awake()
		{
			TriggerPreloadHandler();
			Initialize();
		}
		private void OnDestroy()
		{
			Dispose();
		}
		#endregion Mono


		#region Preload Token
		private void TriggerPreloadHandler()
		{
			for (int i = 0; i < m_PrefabPreloadSettings.Length; i++)
			{
				if (m_PrefabPreloadSettings[i].m_Prefab == null)
					Debug.LogError($"Fail to preload index {i}, missing prefab", this);
				else
				{
					System.Threading.Tasks.Task.Run(() =>
					{
						PreloadHandler(m_PrefabPreloadSettings[i]);
					});
				}
			}
		}

		private async System.Threading.Tasks.Task BackToMainThread()
		{
			await System.Threading.Tasks.Task.Delay(0).ConfigureAwait(false);
		}

		private async void PreloadHandler(PrefabPreloadSetting setting)
		{
			LocateOrCreateCache(setting.m_Prefab, setting, out TokenCache cache);
			if (setting.m_PreloadDelay > 0f)
				await System.Threading.Tasks.Task.Delay((int)(setting.m_PreloadDelay * 1000f));
			
			await BackToMainThread();

			int cnt = Mathf.Min(setting.m_PreloadAmount, setting.m_PoolLimit);
			while (cache.totalCount < cnt) // Async spawning parallel maintain preload amount 
			{
				await BackToMainThread();
				// Create instance for prefab.
				cache.PreloadDeactiveToken();
				int countFrame = setting.m_PreloadFramePeriod;

				while (countFrame-- > 0)
					await System.Threading.Tasks.Task.Yield();
			}
		}

		private void LocateOrCreateCache(GameObject prefab, PrefabSetting setting, out TokenCache cache)
		{
			if (prefab != null && setting != null && prefab != setting.m_Prefab)
				throw new UnityException($"Invalid prefab setting for this asset {prefab}");
			if (!m_CacheDict.TryGetValue(prefab, out cache))
			{
				// cache not found, create one.
				if (setting == null || setting.m_Prefab == null)
				{
					// spawn on demend, but setting not found.
					Debug.LogWarning($"Fail to locate {nameof(PrefabPreloadSetting)} for {prefab}, fallback default setting.", this);
					cache = new TokenCache(new PrefabPreloadSetting() { m_Prefab = prefab }, transform);
				}
				else
				{
					// spawn on demend, without using preload setting.
					cache = new TokenCache(setting, transform);
				}
				m_CacheDict.Add(prefab, cache);
			}
			else if (setting != null)
			{
				// override setting
				// Case : execution order issue. Spawn() call early then Awake();
				cache.setting = setting;
			}
		}
		#endregion Preload Token


		#region Pooling Core
		private bool m_Inited = false;
		public void Initialize()
		{
			if (!m_Inited)
			{
				m_Inited = true;
				// Force put setting into memory
				foreach (var setting in m_PrefabPreloadSettings)
					LocateOrCreateCache(setting.m_Prefab, setting, out TokenCache cache);
			}
		}


		public GameObject Spawn(string prefabName, Vector3 position, Quaternion rotation)
		{
			if (SpawnAfterDisposeError())
				return null;
			else if (!m_Inited)
				Initialize();
			foreach (var cache in m_CacheDict.Values)
				if (cache.setting.m_Name.Equals(prefabName))
					return InternalSpawn(cache, position, rotation, null, false);
			return null;
		}
		public GameObject Spawn(string prefabName, Vector3 position, Quaternion rotation, Transform parent)
		{
			if (SpawnAfterDisposeError())
				return null;
			else if (!m_Inited)
				Initialize();
			foreach (var cache in m_CacheDict.Values)
				if (cache.setting.m_Name.Equals(prefabName))
					return InternalSpawn(cache, position, rotation, parent, true);
			return null;
		}

		public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, PrefabSetting setting = null)
		{
			if (SpawnAfterDisposeError())
				return null;
			else if (prefab == null)
				throw new UnityException("Fail to spawn Null prefab.");
			else if (!m_Inited)
				Initialize();
			LocateOrCreateCache(prefab, setting, out TokenCache cache);
			return InternalSpawn(cache, position, rotation, null, false, setting);
		}
		public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, PrefabSetting setting = null)
		{
			if (SpawnAfterDisposeError())
				return null;
			else if (prefab == null)
				throw new UnityException("Fail to spawn Null prefab.");
			else if (!m_Inited)
				Initialize();
			LocateOrCreateCache(prefab, setting, out TokenCache cache);
			return InternalSpawn(cache, position, rotation, parent, true, setting);
		}

		public GameObject Spawn(GameObject prefab, Transform parent, PrefabSetting setting = null)
		{
			if (SpawnAfterDisposeError())
				return null;
			else if (prefab == null)
				throw new UnityException("Fail to spawn Null prefab.");
			else if (!m_Inited)
				Initialize();
			LocateOrCreateCache(prefab, setting, out TokenCache cache);
			var token = InternalSpawn(cache, parent.transform.position, parent.rotation, parent, true, setting);
			token.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			return token;
		}
		private GameObject InternalSpawn(
			TokenCache cache, Vector3 position, Quaternion rotation,
			Transform parent, bool useParentParam,
			PrefabSetting setting = null)
		{
			GameObject go = null;
			bool notEnoughToken = cache.deactiveObjs.Count == 0;
			if (notEnoughToken)
			{
				bool overLimit = cache.totalCount >= cache.setting.m_PoolLimit && cache.setting.m_CullOverLimit;
				if (overLimit)
				{
					Debug.LogWarning($"Pool limit reached for {cache.setting.m_Name}, unable to spawn more.", this);
					return null;
				}
			}

			// Spawn on demend
			go = cache.RentOrCreateToken(position, rotation, parent, useParentParam);
			if (go == null)
				throw new System.Exception("Fail to spawn token.");

			m_AllSpawnedObjs.Add(go, cache.setting.m_Prefab);
			go.SetActive(true);
			go.BroadcastMessage(nameof(ISpawnObject.OnSpawned), this, SendMessageOptions.DontRequireReceiver);
			return go;
		}

		/// <summary>Check if the giving gameobject was spawned by this spawn pool</summary>
		/// <param name="go">giving gameobject</param>
		/// <returns></returns>
		public bool IsSpawned(GameObject go)
		{
			return isDisposed ? false : m_AllSpawnedObjs.ContainsKey(go);
		}

		public void DespawnAll()
		{
			var arr = m_AllSpawnedObjs.ToArray();
			m_AllSpawnedObjs.Clear();
			var i = arr.Length;
			while (i-- > 0)
			{
				// Despawn(arr[i]); // skip checking.
				var token = arr[i].Key;
				var prefabKey = arr[i].Value;
				if (!m_CacheDict.TryGetValue(prefabKey, out var cache))
				{
					Debug.LogError("Fail to locate cache for prefab", token);
					continue;
				}

				// InternalDeactiveToken(token, cache);
				cache.ReturnToken(token);
				token.BroadcastMessage(nameof(ISpawnObject.OnDespawned), this, SendMessageOptions.DontRequireReceiver);
			}
		}

		public async void DespawnAllAsync()
		{
			var arr = m_AllSpawnedObjs.ToArray();
			m_AllSpawnedObjs.Clear();
			var i = arr.Length;
			while (i--> 0)
			{
				// Despawn(arr[i]); // skip checking.
				var token		= arr[i].Key;
				var prefabKey	= arr[i].Value;
				if (!m_CacheDict.TryGetValue(prefabKey, out var cache))
				{
					Debug.LogError("Fail to locate cache for prefab", token);
					continue;
				}

				//InternalDeactiveToken(token, cache);
				cache.ReturnToken(token);
				token.BroadcastMessage(nameof(ISpawnObject.OnDespawned), this, SendMessageOptions.DontRequireReceiver);
				await Task.Yield();
			}
		}

		public void Despawn(GameObject go)
		{
			if (isDisposed)
				return;
			else if (go == null)
			{
				Debug.LogError("Despawn null reference unit.", this);
				return;
			}
			if (m_AllSpawnedObjs.TryGetValue(go, out GameObject prefabKey))
			{
				InternalDeactiveToken(go, m_CacheDict[prefabKey]);
				go.BroadcastMessage(nameof(ISpawnObject.OnDespawned), this, SendMessageOptions.DontRequireReceiver);
			}
		}
		private void InternalDeactiveToken(GameObject token, TokenCache cache)
		{
			if (!m_AllSpawnedObjs.Remove(token))
				return;
			cache.ReturnToken(token);
		}
		#endregion // Pooling Core


		#region Disposable
		private bool SpawnAfterDisposeError()
		{
			if (isDisposed)
				Debug.LogError($"{GetType().Name} : Unable to {nameof(Spawn)} after disposed.", this);
			return isDisposed;
		}
		public bool isDisposed { get; private set; } = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					foreach (GameObject token in m_AllSpawnedObjs.Keys)
						if (token != null) token.SetActive(false);
					foreach (TokenCache cache in m_CacheDict.Values)
						cache.Clear();
				}
				m_AllSpawnedObjs.Clear();
				m_CacheDict.Clear();
				System.GC.SuppressFinalize(this);
				isDisposed = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~ObjectPool()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}
		#endregion Disposable
	}


	/// <summary>An interface to allow spawned object to receive the following callback during Spawn/Despawn flow.</summary>
	public interface ISpawnObject
	{
		/// <summary>Will boardcast to token(s) after spawn flow.</summary>
		/// <param name="pool">Handling pool reference</param>
		void OnSpawned(ObjectPool pool);

		/// <summary>Will boardcast to token(s) after despawn flow.</summary>
		/// <param name="pool">Handling pool reference</param>
		void OnDespawned(ObjectPool pool);
	}
}

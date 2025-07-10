using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using Kit2.Tasks;
using System.Threading;


namespace Kit2
{
	/// <summary>Develop for handling complex Appear/Disappear situation,
	/// aim for the animation delay and status control.
	/// allow nest hierarchy structure and Status pass <see cref="ViewObjectManager"/></summary>
	public abstract class ViewObject : MonoBehaviour
	{
		#region Variables
		[System.Flags]
		public enum eDebugDraw
		{
			Appear = 1 << 0,
			ForceSetToAppear = 1 << 1,
			Disappear = 1 << 2,
			ForceToDisappear = 1 << 3,
			FirstOnEnable = 1 << 4,
		}
		public enum eLogType
		{
			Log = 1,
			LogWarning = 2,
			LogError = 3,
		}
		[System.Serializable]
		public class DebugInfo
		{
			[MaskField(typeof(eDebugDraw))] public eDebugDraw draw = (eDebugDraw)0;
			public eLogType logType = eLogType.Log;
			public bool PauseDebug = false;
		}
		[SerializeField] DebugInfo m_Debug = new DebugInfo();

		public enum eViewStatus
		{
			Init = 0, // the very first status in scene. before Awake()
			Appearing = 1, // playing Appearing() Coroutine
			Appeared = 2,
			Disappearing = 10,  // playing Disappearing() Coroutine
			Disappeared = 11
		}
		// ViewStatus can never being init in code, used to identify the Disable object in scene.
		// case : when Appear() call faster then Awake().
		private eViewStatus _viewStatus = eViewStatus.Init;
		public eViewStatus ViewStatus
		{
			private set
			{
				if (_viewStatus != value)
				{
					_viewStatus = value;
					switch (_viewStatus)
					{
						case eViewStatus.Appearing:
						OnStartAppear.Invoke();
						break;
						case eViewStatus.Appeared:
						OnAppeared.Invoke();
						break;
						case eViewStatus.Disappearing:
						OnStartDisappear.Invoke();
						break;
						case eViewStatus.Disappeared:
						OnDisappeared.Invoke();
						break;
						default:
						break; // ignore
					}
				}
			}
			get { return _viewStatus; }
		}

		public enum eFirstStatus
		{
			None = 0,
			OnEnableAppearing,
			OnEnableAppeared,
			OnEnableDisappearing,
			OnEnableDisappeared,
		}
		/// <summary>
		/// Define the viewState when it's the first appear,
		/// case : when there are no request to appear/disappear, the init status of viewobject.
		/// in other words, any request from appear/disappear will override this setting.
		/// </summary>
		[SerializeField] protected eFirstStatus m_FirstOnEnable = eFirstStatus.None;
		private bool m_FirstEnabled = false;
		private bool m_Started = false;

		/// <summary>
		/// This will force to SetActive() during call Appear() of the gameobject,
		/// disable this option you'll need to handle SetActive() manually.
		/// </summary>
		/// <remarks>
		/// before OnStartAppear = SetActive(true),
		/// after OnDisappear = SetActive(false)
		/// </remarks>
		[SerializeField] private bool m_EnsureActiveState = true;

		public enum eExecultType
		{
			Queue = 0,  // queue up the action and trigger after the end of current.
			Immediately, // trigger the request immediately(snap to start/end directly), without queue up any action.
			EstimatePosition, // trigger the request immediately, but try to locate the correct timing based on position.
		}
		/// <summary>To define how to react, when new action are requested during animation.</summary>
		[SerializeField] protected eExecultType m_ExecultType = eExecultType.Queue;

		/// <summary>The max amount action can queue up in memory.</summary>
		[SerializeField] private int m_ActionQueueLength = 2;
		private List<System.Action> m_NextAction = new List<System.Action>();

		/// <summary>The event will dispatch in the same frame as Appear() being execult - before animation finish.</summary>
		public UnityEvent OnStartAppear;
		/// <summary>The event will dispatch after Appear() animation finished.</summary>
		public UnityEvent OnAppeared;
		/// <summary>The event will dispatch in the same frame as Disappear() being execult - before animation finish.</summary>
		public UnityEvent OnStartDisappear;
		/// <summary>The event will dispatch after Disappear() animation finished.</summary>
		public UnityEvent OnDisappeared;
		#endregion Variables

		#region System
		protected virtual void Reset() { }
		protected virtual void OnValidate() { }

		protected virtual void Awake()
		{
			if (m_ExecultType != eExecultType.Queue)
				m_ActionQueueLength = 0;
		}

		protected virtual void OnDestroy()
		{
		}

		protected virtual void Start()
		{
			if (!m_FirstEnabled)
			{
				FirstOnEnable(m_FirstOnEnable);
			}
			m_Started = true;
		}

		protected virtual void OnEnable()
		{
			if (m_Started && m_FirstEnabled)
			{
				// Allow to use SetActive() trigger Appear(), after first appear
				if (ViewStatus > eViewStatus.Appeared && ViewStatus <= eViewStatus.Disappeared)
					Appear();
			}
		}

		protected bool m_IsAppQuit { get; private set; } = false;
		private void OnApplicationQuit()
		{
			m_IsAppQuit = true;
		}

		protected virtual void OnDisable()
		{
			// case: another script force disable the gameObject during Coroutine session.
			// ViewStatus will stuck in disappearing/appearing status.
			if (!m_IsAppQuit && ViewStatus < eViewStatus.Disappeared)
			{
				// to handle this status, you might following method will be called.
				ForceSetToDisappeared();
			}
		}
		#endregion

		#region Internal API
		private void FirstOnEnable(eFirstStatus firstStatus)
		{
			if (!m_FirstEnabled)
			{
				m_FirstEnabled = true; // only happen once
									   // first time + !m_OnEnableAppear + no one change it, you want default action
									   // case: call appear/disappear before awake, state will change.
				if (ViewStatus == eViewStatus.Init)
				{
					if (m_Debug.draw.HasFlag(eDebugDraw.FirstOnEnable))
					{
						Log($"{gameObject.name} - {nameof(FirstOnEnable)} triggered - [{firstStatus}].");
					}

					switch (firstStatus)
					{
						case eFirstStatus.OnEnableAppearing: Appear(); break;
						case eFirstStatus.OnEnableAppeared: ForceSetToAppeared(); break;
						case eFirstStatus.OnEnableDisappearing: Disappear(); break;
						case eFirstStatus.OnEnableDisappeared: ForceSetToDisappeared(); break;
						case eFirstStatus.None:
						default:
						{
							// by default
							// sync first status, do nothing.
							if (gameObject.activeSelf)
							{
								// since we do nothing, don't play appear animation.
								ForceSetToAppeared();
							}
							else
							{
								// case, when disappear didn't mean "Disabled" the gameobject
								ForceSetToDisappeared();
							}
						}
						break;
					}
				}
				else if (firstStatus != eFirstStatus.None)
				{
					// Common case for : ViewObjectManager override child's first on enable state.
					// do nothing when we are being control.
					// throw new System.NotImplementedException("FIXME: non-handle state detected.");
				}
			}
		}

		private void _EnsureActiveState()
		{
			if (m_EnsureActiveState && gameObject && !gameObject.activeSelf)
			{
				gameObject.SetActive(true);
			}
		}

		private void _EnsureDeactiveState()
		{
			if (m_EnsureActiveState && gameObject.activeSelf)
				gameObject.SetActive(false);
		}

		/// <summary>To handle special status, which required to reset the status to Disappeared.
		/// override this function to clear up your mess.</summary>
		/// <remarks>This call will also clear up the NextActions queue.</remarks>
		protected virtual void ForceSetToDisappeared()
		{
			if (m_Debug.draw.HasFlag(eDebugDraw.ForceToDisappear))
			{
				Log($"{name} {nameof(ForceSetToDisappeared)}");
			}
			if (ViewStatus == eViewStatus.Init)
			{
			}
			else if (ViewStatus == eViewStatus.Disappeared)
			{
				return;
			}
			else
			{
				// To ensure the execution order
				if (ViewStatus == eViewStatus.Appearing)
					ViewStatus = eViewStatus.Appeared;
				if (ViewStatus == eViewStatus.Appeared)
					ViewStatus = eViewStatus.Disappearing;
			}

			ViewStatus = eViewStatus.Disappeared;
			Cancel();
			_EnsureDeactiveState();
			m_NextAction.Clear();
		}

		/// <summary>To handle special status, which required to reset the status to Appeared.
		/// override this function to clear up your mess.</summary>
		/// <remarks>This call will also clear up the NextActions queue.</remarks>
		protected virtual void ForceSetToAppeared()
		{
			if (m_Debug.draw.HasFlag(eDebugDraw.ForceSetToAppear))
			{
				Log($"{name} {nameof(ForceSetToAppeared)}");
			}

			if (ViewStatus == eViewStatus.Init)
			{
			}
			else if (ViewStatus == eViewStatus.Appeared)
			{
				return;
			}
			else
			{
				// To ensure the execution order
				if (ViewStatus == eViewStatus.Disappearing)
					ViewStatus = eViewStatus.Disappeared;
				if (ViewStatus == eViewStatus.Disappeared)
					ViewStatus = eViewStatus.Appearing;
			}

			ViewStatus = eViewStatus.Appeared;
			Cancel();
			_EnsureActiveState();
			m_NextAction.Clear();
		}

		private void Log(string msg)
		{
			switch (m_Debug.logType)
			{
				case eLogType.Log: Debug.Log(msg, this); break;
				case eLogType.LogWarning: Debug.LogWarning(msg, this); break;
				case eLogType.LogError: Debug.LogError(msg, this); break;
				default:
				throw new System.NotImplementedException();
			}

#if UNITY_EDITOR
			if (m_Debug.PauseDebug)
				Debug.Break();
#endif
		}
		#endregion

		#region API
		public void Appear(bool immediately = false)
		{
			if (immediately)
				ForceSetToAppeared();
			else
				Appear();
		}

		private void Appear()
		{
			if (m_Debug.draw.HasFlag(eDebugDraw.Appear))
			{
				Log($"{name} {nameof(Appear)}");
			}
			if (ViewStatus == eViewStatus.Init) // Not even awake yet,
			{
				// don't use m_FirstAppeared to override state, due to complex hierarchy setup between ViewObjectManager & it's childs

				// To avoid First OnEnable checking, and appear as normal
				// don't use ViewStatus{setter} to avoid trigger disappear event in this status.
				_viewStatus = eViewStatus.Disappeared;
			}
			if (ViewStatus == eViewStatus.Disappeared)
			{
				_EnsureActiveState();
				// Execute the appear animation.
				Cancel();
				m_Current = new CancellationTokenSource();
				InternalStartAppearTask(m_Current.Token);
			}
			else
			{
				switch (m_ExecultType)
				{
					case eExecultType.Queue:
					if ((m_NextAction.Count == 0 && ViewStatus == eViewStatus.Disappearing) || // not match with current status
						(m_NextAction.Count > 0 && m_NextAction[m_NextAction.Count - 1] != Appear)) // ignore double call
					{
						// handling frequent switching status between appear & disappear.
						// queue up the action.
						if (m_NextAction.Count < m_ActionQueueLength)
							m_NextAction.Add(Appear);
					}
					break;
					case eExecultType.Immediately:
					case eExecultType.EstimatePosition:
					if (ViewStatus == eViewStatus.Disappearing)
					{
						ForceSetToDisappeared();
						Appear();
					}
					break;
					default:
					throw new System.NotImplementedException();
				}
			}
		}
		public void Disappear(bool immediately = false)
		{
			if (immediately)
				ForceSetToDisappeared();
			else
				Disappear();
		}

		private void Disappear()
		{
			if (m_Debug.draw.HasFlag(eDebugDraw.Disappear))
			{
				Log($"{name} {nameof(Disappear)}");
			}
			if (ViewStatus == eViewStatus.Init) // Not even awake yet,
			{
				ForceSetToDisappeared();
			}
			else if (ViewStatus == eViewStatus.Appeared)
			{
				// Execute disappear animation.
				Cancel();
				m_Current = new CancellationTokenSource();
				InternalStartDisappear(m_Current.Token);
			}
			else
			{
				switch (m_ExecultType)
				{
					case eExecultType.Queue:
					if ((m_NextAction.Count == 0 && ViewStatus == eViewStatus.Appearing) || // not match with current status
						(m_NextAction.Count > 0 && m_NextAction[m_NextAction.Count - 1] != Disappear)) // ignore double call
					{
						// handling frequent switching status between appear & disappear.
						// queue up the action.
						if (m_NextAction.Count < m_ActionQueueLength)
							m_NextAction.Add(Disappear);
					}
					break;
					case eExecultType.Immediately:
					case eExecultType.EstimatePosition:
					if (ViewStatus == eViewStatus.Appearing)
					{
						ForceSetToAppeared();
						Disappear();
					}
					break;
					default:
					throw new System.NotImplementedException();
				}
			}
		}

		/// <summary>
		/// the internal method to start the appear task.
		/// should also handle the cancel token.
		/// assume cancel the appearing animation will jump to the end of animation,
		/// e.g. Appeared status.
		/// </summary>
		/// <param name="cancelToken"></param>
		protected abstract void InternalStartAppearTask(CancellationToken cancelToken);

		/// <summary>
		/// the internal method to start the disappear task.
		/// should also handle the cancel token.
		/// assume cancel the disappearing animation will jump to the end of animation,
		/// e.g. Disappeared status.
		/// </summary>
		/// <param name="cancelToken"></param>
		protected abstract void InternalStartDisappear(CancellationToken cancelToken);

		private CancellationTokenSource m_Current = null;
		private void Cancel()
		{
			if (m_Current == null)
				return;
			m_Current.Cancel();
			m_Current.Dispose();
			m_Current = null;
		}

		public void Toggle()
		{
			if (ViewStatus == eViewStatus.Appeared)
				Disappear();
			else
				Appear();
		}
		#endregion

		#region internal next action.
		public bool IsActionFinish()
		{
			return m_NextAction.Count == 0;
		}

		public int ActionCount()
		{
			return m_NextAction.Count;
		}

		private void NextAction()
		{
			if (m_NextAction.Count > 0)
			{
				m_NextAction[0]();
				if (m_NextAction.Count > 0)
				{
					/// must check again, since sub-class might call <see cref="ForceSetToDisappeared"/>...
					/// or anything else would call m_NextAction.Clear() in it's parent class.
					m_NextAction.RemoveAt(0);
				}
			}
		}

		/// <summary>
		/// only <see cref="ViewObjectManager"/> allow to use this to override the control.
		/// </summary>
		internal void _ClearAction()
		{
			m_NextAction.Clear();
		}
		#endregion

	}
}
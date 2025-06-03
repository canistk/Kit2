using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2
{
    public class UpdateManager
    {
        #region Singleton
        /// <summary><see cref="https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html"/></summary>
        [RuntimeInitializeOnLoadMethod]
        public static void AppStart()
        {
            instance.HijackU3DPlayloop();
        }

        private static UpdateManager m_instance = null;
        public static UpdateManager instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new UpdateManager();
                }
                return m_instance;
            }
        }
        #endregion Singleton

        #region Constructor
        private UpdateManager()
        {
            m_Handlers = new List<System.Action>(8);
        }
        ~UpdateManager()
        {
            m_Handlers.Clear();
            m_Handlers = null;
        }
        #endregion Constructor

        #region Core
        private void HijackU3DPlayloop()
        {
            /// Easy implementation idea :
            /// <see cref="https://forum.unity.com/threads/how-to-make-static-update-method.1326297/"/>
            /// more detail for this method.
            /// <see cref="https://medium.com/@thebeardphantom/unity-2018-and-playerloop-5c46a12a677"/>
            var tmp = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            tmp.subSystemList[5].updateDelegate += InternalUpdate; // sub system 5 is update.
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(tmp);
        }

        private List<System.Action> m_Handlers;
        private void InternalUpdate()
        {
            int cnt = m_Handlers.Count - 1;
            for (int i = cnt; i >= 0; --i)
            {
                if (m_Handlers[i] == null)
                {
                    m_Handlers.RemoveAt(i);
                    continue;
                }
                m_Handlers[i].TryCatchDispatchEventError(o => o?.Invoke());
            }
        }
        #endregion Core

        #region Public API
        public void Register(System.Action updateCallback)
        {
            if (m_Handlers.Contains(updateCallback))
                return;

            m_Handlers.Add(updateCallback);
        }

        public bool Deregister(System.Action updateCallback)
        {
            return m_Handlers.Remove(updateCallback);
        }
        #endregion Public API
    }
}
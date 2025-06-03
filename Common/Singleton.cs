using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
    public abstract class SingletonBase { }

    public abstract class Singleton<T> : SingletonBase
        where T : SingletonBase, new()
    {
        private static System.Lazy<T> m_Instance = null;

        public static T InstanceWithoutCreate => m_Instance?.Value;
		public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
					m_Instance = new System.Lazy<T>(() => new T());
				}
                return m_Instance.Value;
            }
        }
	}
}

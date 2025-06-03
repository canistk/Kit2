using System.Collections.Generic;
using UnityEngine;
using Action = System.Action;
using StringBuilder = System.Text.StringBuilder;
using IDisposable = System.IDisposable;

namespace Kit2
{
    public class HashLock<T>
    {
        private bool m_Strict;
        public HashLock(bool strict)
        {
            m_Strict = strict;
        }
        private HashSet<T> m_LockOwners = new HashSet<T>();
        public event Action Locked, Released;

        public bool IsLocked => m_LockOwners.Count > 0;
        public bool IsLockedBy(T obj) => m_LockOwners.Contains(obj);
        public int LockedCount => m_LockOwners.Count;

        /// <summary>To support auto release, such as try catch handle</summary>
        private class LockScope : IDisposable
        {
            private readonly HashLock<T> HashLock;
            private readonly T Caller;

            public LockScope(HashLock<T> hashLock, T caller)
            {
                HashLock = hashLock;
                Caller = caller;
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        HashLock.ReleaseLock(Caller);
                    }

                    disposedValue = true;
                }
            }
            public void Dispose() => Dispose(true);
            #endregion IDisposable Support
        }

        public IDisposable AcquireLock(T caller)
        {
            if (!m_LockOwners.Contains(caller))
            {
                m_LockOwners.Add(caller);
                if (Locked != null && m_LockOwners.Count == 1)
                {
                    Locked();
                }
            }
            else if (m_Strict)
            {
                throw new System.InvalidOperationException($"requesting double lock from {caller}");
            }
            else
            {
                Debug.Log($"requesting double lock from {caller}");
            }
            return new LockScope(this, caller);
        }

        public void ReleaseLock(T caller)
        {
            if (m_LockOwners.Contains(caller))
            {
                m_LockOwners.Remove(caller);
                if (!IsLocked)
                {
                    Released?.Invoke();
                }
            }
            else if (m_Strict)
            {
                throw new System.InvalidOperationException($"trying to release the non-exist lock from {caller}");
            }
            else
            {
                Debug.Log($"trying to release the non-exist lock from {caller}");
            }
        }

        public void Clear()
        {
            // fire event only it's locked
            if (IsLocked)
            {
                Released?.Invoke();
            }

            m_LockOwners.Clear();
        }

        public override string ToString()
        {
            return $"[{GetType().Name} : {(IsLocked ? "Locked" : "Unlock")}, count = {m_LockOwners.Count}]";
        }

        public string ToString(bool detail)
        {
            if (detail)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{ToString()}");
                foreach (T owner in m_LockOwners)
                {
                    sb.AppendLine($"- {owner.GetType().Name}");
                }

                return sb.ToString();
            }
            return ToString();
        }
    }
}

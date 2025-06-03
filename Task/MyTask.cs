using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2.Task
{
    public interface IDrawGizmos
    {
        public void DrawGizmos();
    }

    public abstract class MyTaskBase
    {
        /// <returns>
        /// true  = continue to execute on next cycle
        /// false = ending the task.
        /// </returns>
        public abstract bool Execute();
        public virtual void Reset() { }
	}

    /// <summary>
    /// A regular task for manage, support <see cref="Abort"/> with disposable pattern.
    /// </summary>
    public abstract class MyTask : MyTaskBase, IDisposable
    {
        public void Abort()
        {
            if (isDisposed)
                return;
            Dispose(disposing: true);
        }

        public sealed override bool Execute()
        {
            if (isDisposed || isCompleted)
                return false;

            isCompleted = !InternalExecute();
            return !isCompleted;
        }

        protected abstract bool InternalExecute();

        public bool isCompleted { get; private set; } = false;


        /// <summary>
        /// will be call during <see cref="Dispose(bool)"/>
        /// dispose managed state (managed objects)
        /// </summary>
        protected virtual void OnDisposing() { }
        /// <summary>
        /// TODO: free unmanaged resources (unmanaged objects) and override finalizer
        /// TODO: set large fields to null
        /// </summary>
        protected virtual void OnFreeMemory() { }

        #region Dispose
        public bool isDisposed { get; private set; } = false;

        protected void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    OnDisposing();
                }
                OnFreeMemory();
                isDisposed = true;
            }
        }

        ~MyTask()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void System.IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            // System.GC.SuppressFinalize(this);
        }
        #endregion Dispose
    }

    /// <summary>
    /// Design for debug usage,
    /// call <see cref="TryMoveToNextStep"/> to execute custom
    /// state machine. step by step.
    /// usually used with <see cref="IDrawGizmos"/> for editor visual result.
    /// </summary>
    public abstract class MyTaskDebug : MyTask
    {
        protected sealed override bool InternalExecute()
        {
            if (m_RequestToNextStep == 0)
                return true;
            --m_RequestToNextStep;
            return InternalStepExecute();
        }
        protected abstract bool InternalStepExecute();

        #region Step Ctrl
        protected int m_RequestToNextStep = 0;
        public bool TryMoveToNextStep()
        {
            if (m_RequestToNextStep == 0)
            {
                ++m_RequestToNextStep;
                return true;
            }
            return false;
        }
        #endregion Step Ctrl
    }

}
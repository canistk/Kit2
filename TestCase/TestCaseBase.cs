using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2.Testcase
{
    public abstract class TestCaseBase : ScriptableObject
    {
        /// <summary>
        /// Need to implement the custom 'test case' in sub class.
        /// more information on <see cref="TestOperation"/>
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<TestOperation> GetOperations();
    }

    /// <summary>
    /// Need to implement the custom test case task in child class.
    /// all exception will be store in <see cref="Exception"/>
    /// and progress will be store in <see cref="Duration"/>
    /// </summary>
    public abstract class TestOperation
    {
        public readonly bool ErrorIsExpected;
        public TestOperation(bool expectedError)
        {
            this.ErrorIsExpected = expectedError;
        }

        private int m_Step = 0;
        public int Step => m_Step;
        public DateTime StartTime { get; private set; } = DateTime.MinValue;
        public DateTime EndTime { get; private set; } = DateTime.MinValue;
        public TimeSpan Duration => m_Step == 0 ? TimeSpan.Zero : EndTime - StartTime;

        public bool hasException => Exception != null;
        public Exception Exception { get; private set; } = null;

        public bool Run(UnityEngine.Object target)
        {
            if (m_Step <= -1)
                return false;
            if (m_Step == 0)
            {
                StartTime = DateTime.UtcNow;
                ++m_Step;
                OnStart();
            }

            try
            {
                if (!InProgress())
                {
                    EndTime = DateTime.UtcNow;
                    m_Step = -1;
                    OnEnd(false);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(target);
#endif
                }
            }
            catch (Exception ex)
            {
                Exception = ex;
                EndTime = DateTime.UtcNow;
                m_Step = -1;
                OnEnd(true);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(target);
#endif
                return false;
            }
            return true;
        }

        protected virtual void OnStart() { }

        /// <summary>
        /// The progress will <see cref="Run"/>
        /// within try..catch, when error is throw will be cached in
        /// <see cref="Exception"/>
        /// </summary>
        /// <returns></returns>
        protected abstract bool InProgress();

        /// <summary>
        /// Override this for draw the debug information on inspector.
        /// </summary>
        /// <param name="debugText"></param>
        public virtual void OnInspecterDraw(out string debugText)
        {
            debugText = ToString();
        }

        protected virtual void OnEnd(bool error) { }

        public virtual string OnPassWithoutException()
        {
            return string.Empty;
        }

        public override string ToString()
        {
            return $"{GetState(false)}";
        }

        public bool IsEnd()
        {
            return Step < 0;
        }
        public bool IsPassed()
        {
            return
                ErrorIsExpected && hasException ||
                !ErrorIsExpected && !hasException;
        }

        public string GetState(bool richText)
        {
            bool pass = IsPassed();

            if (!richText)
            {
                var state = Step switch
                {
                    0 => "Idle",
                    1 => "Run",
                    _ => "End",
                };

                var passStr = Step >= 0 && Step <= 1 ? "-" : (pass ? "Pass" : "Fail");

                return $"[T:{state}:{passStr}]";
            }
            else
            {
                if (Step == 0)
                    return Color.gray.ToRichText("Idle");
                if (Step == 1)
                    return Color.yellow.ToRichText("Run");

                if (!pass)
                    return Color.red.ToRichText("Fail");

                return Color.green.ToRichText("Pass");
            }
        }
    }
}
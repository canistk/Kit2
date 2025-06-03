using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2.Testcase;
using System.Text;

namespace Kit2
{
    [CreateAssetMenu(fileName = "CircularBuffer_TestCase", menuName = "Kit/TestCase/CircularBuffer_TestCase")]
    public class CircularBuffer_TestCase : TestCaseBase
    {
        public override IEnumerable<TestOperation> GetOperations()
        {
            yield return new CBTest(false, true,
                (b) =>
                {
                    b.Enqueue("a");
                    b.Enqueue("b");
                    b.Enqueue("c");
                    b.Enqueue("d");
                    b.Enqueue("e");
                },
                (b) =>
                {
                    b.Enqueue("1");
                },
                (b) =>
                {

                    if (b.Count != 6)
                        throw new System.Exception("Not match buffer count");
                    var rst =
                        b.Dequeue() == "a" &&
                        b.Dequeue() == "b" &&
                        b.Dequeue() == "c" &&
                        b.Dequeue() == "d" &&
                        b.Dequeue() == "e" &&
                        b.Dequeue() == "1";
                    if (!rst)
                        throw new System.Exception("Order not match.");
                });
        }

        private class CBTest : TestOperation
        {
            private readonly CircularBuffer<string> m_Buffer;
            public delegate void BufferStep(CircularBuffer<string> action);
            private readonly BufferStep[] m_Actions;
            private StringBuilder m_Sb;
            private int m_Pt;
            public CBTest(bool expectedError, bool allowExpend, params BufferStep[] actions) : base(expectedError)
            {
                this.m_Buffer = new CircularBuffer<string>(5, allowExpend);
                this.m_Actions = actions;
                this.m_Pt = 0;
                this.m_Sb = new StringBuilder();
            }

            protected override bool InProgress()
            {
                if (m_Pt >= m_Actions.Length)
                    return false;

                var step = m_Actions[m_Pt];
                step.Invoke(m_Buffer);

                m_Sb.AppendLine($"Step {m_Pt + 1} : ");
                m_Sb.AppendLine(m_Buffer.ToString());
                return ++m_Pt < m_Actions.Length;
            }

            public override void OnInspecterDraw(out string debugText)
            {
                debugText = m_Sb.ToString();
            }
        }
    }
}
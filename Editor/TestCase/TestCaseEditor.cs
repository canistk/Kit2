using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace Kit2.Testcase
{
    [CustomEditor(typeof(TestCaseBase), editorForChildClasses: true)]
    public class TestCaseEditor : Editor
    {
        EditorCoroutine m_EditorProgress;

        List<TestOperation> m_Tasks = null;
        Queue<TestOperation> m_Jobs = new Queue<TestOperation>();
        private IEnumerator EditorLoop()
        {
            while (m_Jobs.Count > 0)
            {
                var job = m_Jobs.Peek();
                try
                {
                    if (!job.Run(target))
                    {
                        m_Jobs.Dequeue();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    m_Jobs.Dequeue();
                }
                yield return null;
            }
            EditorCoroutineUtility.StopCoroutine(m_EditorProgress);
            m_EditorProgress = null;
            m_Jobs = null;
        }

        private void StartTasks()
        {
            var self = target as TestCaseBase;
            var iter = self.GetOperations();

            m_Jobs = new Queue<TestOperation>();
            m_Tasks = new List<TestOperation>();
            foreach (var operation in iter)
            {
                m_Jobs.Enqueue(operation);
                m_Tasks.Add(operation);
            }
            m_EditorProgress = EditorCoroutineUtility.StartCoroutine(EditorLoop(), this);
        }


#if false

    public override VisualElement CreateInspectorGUI()
    {
        var self = target as TestCaseBase;
        if (m_Tasks == null)
        {
            StartTasks();
        }

        var listView = new ListView();
        listView.makeItem = _CreateItem;
        listView.bindItem = _BindItem;
        // listView.onSelectionChange += _OnSelectItemView;
        listView.itemsSource = m_Tasks;
        
        VisualElement _CreateItem()
        {
            return new VisualElement();
        }
        void _BindItem(VisualElement panel, int index)
        {
            var task    = m_Tasks[index];
            
            var split   = new TwoPaneSplitView(0, 80f, TwoPaneSplitViewOrientation.Horizontal);
            var detail  = new VisualElement();

            var state = new TextElement() { text = task.GetState(true), enableRichText = true };
            /// Ref : IBinding <see cref="IBindable"/> didn't work on custom struct ?
            
            split.Add(state);
            state.MarkDirtyRepaint();
            split.Add(detail);

            var txt = new TextElement() { text = task.ToString() };
            detail.Add(txt);
            txt.MarkDirtyRepaint();

            if (task.IsEnd() && !task.IsPassed())
            {
                if (!task.ErrorIsExpected)
                {
                    detail.Add(
                        new HelpBox(task.Exception.Message, HelpBoxMessageType.Warning)
                    );
                }
                else
                {
                    detail.Add(
                        new HelpBox("Error is expected", HelpBoxMessageType.Error)
                    );
                }
            }
            panel.Add(split);
        }
        void _OnSelectItemView(TestOperation oper)
        {

        }

        var scroll  = new ScrollView(ScrollViewMode.Vertical);
        scroll.Add(listView);
        return scroll;
    }

#else

        GUIStyle m_RichStyle = null;
        GUIStyle richStyle
        {
            get
            {
                if (m_RichStyle == null)
                {
                    m_RichStyle = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                    };
                }
                return m_RichStyle;
            }
        }
        Vector2 m_Scroll = Vector2.zero;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // var self = target as TestCaseBase;

            if (GUILayout.Button("Reload"))
            {
                m_Tasks = null;
                m_Jobs = null;
                if (m_EditorProgress != null)
                    EditorCoroutineUtility.StopCoroutine(m_EditorProgress);
                m_EditorProgress = null;
            }

            if (m_Tasks == null)
            {
                StartTasks();
            }

            using (var sc = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = sc.scrollPosition;


                foreach (var task in m_Tasks)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea))
                    {
                        var str = task.GetState(true);
                        task.OnInspecterDraw(out var txt);
                        EditorGUILayout.LabelField($"{str}", richStyle, GUILayout.Width(40f));
                        EditorGUILayout.TextArea($"{txt}", richStyle);
                    }

                    if (task.IsEnd())
                    {
                        if (task.IsPassed())
                        {
                            if (task.hasException)
                            {
                                EditorGUILayout.HelpBox(task.Exception.ToString(), MessageType.Info);
                            }
                            else
                            {
                                // no exception to display
                                var str = task.OnPassWithoutException();
                                if (!string.IsNullOrEmpty(str))
                                {
                                    EditorGUILayout.HelpBox(str, MessageType.Info);
                                }
                            }
                        }
                        else
                        {
                            if (task.hasException)
                            {
                                // shouldn't have error.
                                EditorGUILayout.HelpBox(task.Exception.ToString(), MessageType.Error);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("Error is expected (but no error)", MessageType.Error);
                            }
                        }
                    }
                }
            }
        }
#endif
    }
}
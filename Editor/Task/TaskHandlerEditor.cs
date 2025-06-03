using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Kit2.Task
{
    public class TaskHandlerEditor : EditorWindowBase
    {
        [MenuItem("Kit/Debug/TaskHandler")]
        private static void Init()
        {
            var window = GetWindow<TaskHandlerEditor>();
            window.titleContent = new GUIContent("TaskHandler Editor");
        }
        private System.Text.StringBuilder sb;

        private void OnEnable()
        {
            sb = new System.Text.StringBuilder();
        }

        private void OnDisable()
        {
            sb.Clear();
            sb = null;
        }

        private void OnGUI()
        {
            if (sb == null)
                return;
            sb.Clear();
            sb.Append(nameof(MyTaskHandler))
                .Append(" Task count :")
                .AppendLine(MyTaskHandler.TaskCount.ToString())
                .Append(" Executing :")
                .AppendLine(MyTaskHandler.Executing.ToString())
                .AppendLine();

            sb.Append(nameof(MyEditorTaskHandler))
                .Append(" Task count :")
                .AppendLine(MyEditorTaskHandler.TaskCount.ToString())
                .Append(" Executing :")
                .AppendLine(MyEditorTaskHandler.Executing.ToString());

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextArea(sb.ToString());
            }

            if (GUI.changed) Repaint();
        }
    }
}
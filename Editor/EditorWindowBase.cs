using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Kit2
{
    public class EditorWindowBase : EditorWindow
    {
#if false
        [MenuItem("Kit/Test/MyEditorWindow")]
        private static void Init()
        {
            EditorWindowBase window = GetWindow<EditorWindowBase>();
            window.titleContent = new GUIContent("Panel");
        }
#endif

        protected static GUIStyle m_ResizerStyle = null;
        protected GUIStyle resizerStyle
        {
            get
            {
                if (m_ResizerStyle == null)
                {
                    m_ResizerStyle = new GUIStyle();
                    m_ResizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
                }
                return m_ResizerStyle;
            }
        }

        protected static GUIStyle m_BoxStyle = null;
        protected GUIStyle boxStyle
        {
            get
            {
                if (m_BoxStyle == null)
                {
                    m_BoxStyle = new GUIStyle();
                    m_BoxStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                }
                return m_BoxStyle;
            }
        }

        protected static GUIStyle m_TextAreaStyle = null;
        protected GUIStyle textAreaStyle
        {
            get
            {
                if (m_TextAreaStyle == null)
                {
                    m_TextAreaStyle = new GUIStyle();
                    m_TextAreaStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                    m_TextAreaStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;
                }
                return m_TextAreaStyle;
            }
        }

        protected static Texture2D m_BoxBgOdd;
        protected Texture2D boxBgOdd
        {
            get
            {
                if (m_BoxBgOdd == null)
                {
                    m_BoxBgOdd = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
                }
                return m_BoxBgOdd;
            }
        }

        protected static Texture2D m_BoxBgEven = null;
        protected Texture2D boxBgEven
        {
            get
            {
                if (m_BoxBgEven == null)
                {
                    m_BoxBgEven = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
                }
                return m_BoxBgEven;
            }
        }

        protected static Texture2D m_BoxBgSelected = null;
        protected Texture2D boxBgSelected
        {
            get
            {
                if (m_BoxBgSelected == null)
                {
                    m_BoxBgSelected = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;
                }
                return m_BoxBgSelected;
            }
        }

        protected static Texture2D m_ErrorIcon = null;
        protected Texture2D errorIcon
        {
            get
            {
                if (m_ErrorIcon == null)
                {
                    m_ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
                }
                return m_ErrorIcon;
            }
        }

        protected static Texture2D m_ErrorIconSmall = null;
        protected Texture2D errorIconSmall
        {
            get
            {
                if (m_ErrorIconSmall == null)
                {
                    m_ErrorIconSmall = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
                }
                return m_ErrorIconSmall;
            }
        }

        protected static Texture2D m_WarningIcon = null;
        protected Texture2D warningIcon
        {
            get
            {
                if (m_WarningIcon == null)
                {
                    m_WarningIcon = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;
                }
                return m_WarningIcon;
            }
        }

        protected static Texture2D m_WarningIconSmall = null;
        protected Texture2D warningIconSmall
        {
            get
            {
                if (m_WarningIconSmall == null)
                {
                    m_WarningIconSmall = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
                }
                return m_WarningIconSmall;
            }
        }

        protected static Texture2D m_InfoIcon = null;
        protected Texture2D infoIcon
        {
            get
            {
                if (m_InfoIcon == null)
                {
                    m_InfoIcon = EditorGUIUtility.Load("icons/console.infoicon.png") as Texture2D;
                }
                return m_InfoIcon;
            }
        }

        protected static Texture2D m_InfoIconSmall = null;
        protected Texture2D infoIconSmall
        {
            get
            {
                if (m_InfoIconSmall == null)
                {
                    m_InfoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
                }
                return m_InfoIconSmall;
            }
        }

        /// <see cref="https://gram.gs/gramlog/creating-node-based-editor-unity/"/>

        /// <summary>
        /// <see cref="https://gram.gs/gramlog/creating-editor-windows-in-unity/"/>
        /// </summary>
        public class VerticalResizer : ResizerBase
        {
            public float    sizeRatio;
            public float    height;

            protected static GUIStyle resizerStyle = null;
            public VerticalResizer()
            {
                if (resizerStyle == null)
                {
                    resizerStyle = new GUIStyle();
                    resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
                }
            }

            public override void Resize(Event e, Rect position)
            {
                if (!isResizing)
                    return;
                sizeRatio = e.mousePosition.y / rect.height;
            }

            public override void Draw(Event e, Rect position)
            {
                rect = new Rect(0f, (position.height * sizeRatio) - height, position.width, height * 2f);

                GUILayout.BeginArea(new Rect(rect.position + (Vector2.up * height), new Vector2(position.width, 2)), resizerStyle);
                GUILayout.EndArea();

                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
            }
        }

        public abstract class ResizerBase
        {
            public Rect     rect;
            public bool     isResizing;
            public abstract void Resize(Event e, Rect position);
            public abstract void Draw(Event e, Rect position);
        }

        protected void ProcessEvents(ResizerBase resizer, Event e)
        {
            switch(e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && resizer.rect.Contains(e.mousePosition))
                    {
                        resizer.isResizing = true;
                    }
                    break;
                case EventType.MouseUp:
                    resizer.isResizing = false;
                    break;
            }

            resizer.Resize(e, position);
            Repaint();
        }
    }

}
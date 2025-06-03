using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Kit2.Testcase
{
    public class TestCaseViewer : EditorWindow
    {
        [MenuItem("Tools/Test Case Viewer")]
        public static void Init()
        {
            var wnd = GetWindow<TestCaseViewer>();
            wnd.minSize = new Vector2(800, 600);
        }


        private void CreateGUI()
        {
            var root = rootVisualElement;

            var group = new TwoPaneSplitView(0, 180f, TwoPaneSplitViewOrientation.Horizontal);
            var left = new VisualElement();
            var right = new VisualElement();

            group.Add(left);
            group.Add(right);
            rootVisualElement.Add(group);


            var tasks = Search<TestCaseBase>();
            var listView = new ListView();
            listView.makeItem = _CreateItem;
            listView.bindItem = _BindItem;
            listView.itemsSource = tasks;
            left.Add(listView);

            VisualElement _CreateItem()
            {
                return new Button();
            }
            void _BindItem(VisualElement item, int index)
            {
                var data = tasks[index];
                var btn = item as Button;
                btn.text = $"{data}";
                btn.clicked += () => _OnSelectItemView(data);
            }
            void _OnSelectItemView(object obj)
            {
                var data = obj as TestCaseBase;
                _LoadDetailOnRight(data, right);
            }
            void _LoadDetailOnRight(TestCaseBase task, VisualElement view)
            {
                var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
                scroll.Add(new InspectorElement(task));

                view.Clear();
                view.Add(scroll);
            }
        }

        private List<T> Search<T>()
            where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
            var list = new List<T>(guids.Length);
            foreach (var guid in guids)
            {
                var relative = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(relative))
                    list.Add(AssetDatabase.LoadAssetAtPath<T>(relative));
            }
            return list;
        }
    }
}
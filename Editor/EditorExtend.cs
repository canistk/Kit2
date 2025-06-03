using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using InternalEditorUtility = UnityEditorInternal.InternalEditorUtility;
using Type = System.Type;
using Path = System.IO.Path;
using File = System.IO.File;
using PropertyInfo = System.Reflection.PropertyInfo;
using BindingFlags = System.Reflection.BindingFlags;
using IDisposable = System.IDisposable;
using StringComparison = System.StringComparison;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Kit2
{
    public sealed class EditorExtend
    {
        #region SortingLayer
        public static string[] GetSortingLayerNames()
        {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }
        public static int[] GetSortingLayerUniqueIDs()
        {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
        }
        public static int SortingLayerField(Rect position, SerializedProperty property)
        {
            return SortingLayerField(position, property, property.displayName);
        }
        public static int SortingLayerField(Rect position, SerializedProperty property, string label)
        {
            int selectedIndex = property.intValue;
            string[] values = GetSortingLayerNames();
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(position, label, selectedIndex, values);
            if (selectedIndex >= values.Length)
            {
                selectedIndex = 0; // hotfix
                property.intValue = selectedIndex;
            }
            if(EditorGUI.EndChangeCheck())
            {
                property.intValue = selectedIndex;
            }
            return selectedIndex;
        }
		#endregion

		#region Tag
		/// <summary>lazy override for string, <see cref="TagFieldDrawer"/></summary>
		/// <param name="position"></param>
		/// <param name="property"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		public static string TagField(Rect position, SerializedProperty property, GUIContent label)
		{
			string layerName = property.stringValue;
			EditorGUI.BeginChangeCheck();
			if (string.IsNullOrEmpty(layerName))
			{
				layerName = "Untagged";
				property.stringValue = layerName;
			}
			layerName = EditorGUI.TagField(position, label, layerName);
			if (EditorGUI.EndChangeCheck())
			{
				property.stringValue = layerName;
			}
			return layerName;
		}
		#endregion

		#region Text AutoComplete
		/// <summary>The internal struct used for AutoComplete (Editor)</summary>
		private struct EditorAutoCompleteParams
		{
			public const string FieldTag = "AutoCompleteField";
			public static readonly Color FancyColor = new Color(.6f, .6f, .7f);
			public static readonly float optionHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			public const int fuzzyMatchBias = 3; // input length smaller then this letter, will not trigger fuzzy checking.
			public static List<string> CacheCheckList = null;
			public static string lastInput;
			public static string focusTag = "";
			public static string lastTag = ""; // Never null, optimize for length check.
			public static int selectedOption = -1; // record current selected option.
			public static Vector2 mouseDown;

			public static void CleanUpAndBlur()
			{
				selectedOption = -1;
				GUI.FocusControl("");
			}
		}

		/// <summary>A textField to popup a matching popup, based on developers input values.</summary>
		/// <param name="input">string input.</param>
		/// <param name="source">the data of all possible values (string).</param>
		/// <param name="maxShownCount">the amount to display result.</param>
		/// <param name="levenshteinDistance">
		/// value between 0f ~ 1f, (percent)
		/// - more then 0f will enable the fuzzy matching
		/// - 1f = 100% error threshold = anything thing is okay.
		/// - 0f = 000% error threshold = require full match to the reference
		/// - recommend 0.4f ~ 0.7f
		/// </param>
		/// <returns>output string.</returns>
		public static string TextFieldAutoComplete(string input, string[] source, int maxShownCount = 5, float levenshteinDistance = 0.5f)
		{
			return TextFieldAutoComplete(EditorGUILayout.GetControlRect(), input, source, maxShownCount, levenshteinDistance);
		}

		/// <summary>A textField to popup a matching popup, based on developers input values.</summary>
		/// <param name="position">EditorGUI position</param>
		/// <param name="input">string input.</param>
		/// <param name="source">the data of all possible values (string).</param>
		/// <param name="maxShownCount">the amount to display result.</param>
		/// <param name="levenshteinDistance">
		/// value between 0f ~ 1f, (percent)
		/// - more then 0f will enable the fuzzy matching
		/// - 1f = 100% error threshold = everything is okay.
		/// - 0f = 000% error threshold = require full match to the reference
		/// - recommend 0.4f ~ 0.7f
		/// </param>
		/// <returns>output string.</returns>
		public static string TextFieldAutoComplete(Rect position, string input, IEnumerable<string> source, int maxShownCount = 5, float levenshteinDistance = 0.5f)
		{
			// Text field
			int controlId = GUIUtility.GetControlID(FocusType.Passive);
			string tag = EditorAutoCompleteParams.FieldTag + controlId;
			GUI.SetNextControlName(tag);
			string rst = EditorGUI.TextField(position, input, EditorStyles.popup);

			// Matching with giving source
			if (input.Length > 0 && // have input
				(EditorAutoCompleteParams.lastTag.Length == 0 || EditorAutoCompleteParams.lastTag == tag) && // one frame delay for process click event.
				GUI.GetNameOfFocusedControl() == tag) // focus this control
			{
				// Matching
				if (EditorAutoCompleteParams.lastInput != input || // input changed
					EditorAutoCompleteParams.focusTag != tag) // switch focus from another field.
				{
					// Update cache
					EditorAutoCompleteParams.focusTag = tag;
					EditorAutoCompleteParams.lastInput = input;

					List<string> uniqueSrc = new List<string>(new HashSet<string>(source)); // remove duplicate
					int srcCnt = uniqueSrc.Count;
					EditorAutoCompleteParams.CacheCheckList = new List<string>(System.Math.Min(maxShownCount, srcCnt)); // optimize memory alloc
					// Start with - slow
					for (int i = 0; i < srcCnt && EditorAutoCompleteParams.CacheCheckList.Count < maxShownCount; i++)
					{
						if (uniqueSrc[i].ToLower().StartsWith(input.ToLower()))
						{
							EditorAutoCompleteParams.CacheCheckList.Add(uniqueSrc[i]);
							uniqueSrc.RemoveAt(i);
							srcCnt--;
							i--;
						}
					}

					// Contains - very slow
					if (EditorAutoCompleteParams.CacheCheckList.Count == 0)
					{
						for (int i = 0; i < srcCnt && EditorAutoCompleteParams.CacheCheckList.Count < maxShownCount; i++)
						{
							if (uniqueSrc[i].ToLower().Contains(input.ToLower()))
							{
								EditorAutoCompleteParams.CacheCheckList.Add(uniqueSrc[i]);
								uniqueSrc.RemoveAt(i);
								srcCnt--;
								i--;
							}
						}
					}

					// Levenshtein Distance - very very slow.
					if (levenshteinDistance > 0f && // only developer request
						input.Length > EditorAutoCompleteParams.fuzzyMatchBias && // bias on input, hidden value to avoid doing it too early.
						EditorAutoCompleteParams.CacheCheckList.Count < maxShownCount) // have some empty space for matching.
					{
						levenshteinDistance = Mathf.Clamp01(levenshteinDistance);
						string keywords = input.ToLower();
						for (int i = 0; i < srcCnt && EditorAutoCompleteParams.CacheCheckList.Count < maxShownCount; i++)
						{
							int distance = StringExtend.LevenshteinDistance(uniqueSrc[i], keywords, caseSensitive: false);
							bool closeEnough = (int)(levenshteinDistance * uniqueSrc[i].Length) > distance;
							if (closeEnough)
							{
								EditorAutoCompleteParams.CacheCheckList.Add(uniqueSrc[i]);
								uniqueSrc.RemoveAt(i);
								srcCnt--;
								i--;
							}
						}
					}
				}

				// Draw recommend keyword(s)
				if (EditorAutoCompleteParams.CacheCheckList.Count > 0)
				{
					Event evt = Event.current;
					int cnt = EditorAutoCompleteParams.CacheCheckList.Count;
					float height = cnt * EditorAutoCompleteParams.optionHeight;
					Rect area = new Rect(position.x, position.y - height, position.width, height);
					
					// Fancy color UI
					EditorGUI.BeginDisabledGroup(true);
					EditorGUI.DrawRect(area, EditorAutoCompleteParams.FancyColor);
					GUI.Label(area, GUIContent.none, GUI.skin.button);
					EditorGUI.EndDisabledGroup();

					// Click event hack - part 1
					// cached data for click event hack.
					if (evt.type == EventType.Repaint)
					{
						// Draw option(s), if we have one.
						// in repaint cycle, we only handle display.
						Rect line = new Rect(area.x, area.y, area.width, EditorAutoCompleteParams.optionHeight);
						EditorGUI.indentLevel++;
						for (int i = 0; i < cnt; i++)
						{
							EditorGUI.ToggleLeft(line, GUIContent.none, (input == EditorAutoCompleteParams.CacheCheckList[i]));
							Rect indented = EditorGUI.IndentedRect(line);
							if (line.Contains(evt.mousePosition))
							{
								// hover style
								EditorGUI.LabelField(indented, EditorAutoCompleteParams.CacheCheckList[i], GUI.skin.textArea);
								EditorAutoCompleteParams.selectedOption = i;

								GUIUtility.hotControl = controlId; // required for Cursor skin. (AddCursorRect)
								EditorGUIUtility.AddCursorRect(area, MouseCursor.ArrowPlus);
							}
							else if (EditorAutoCompleteParams.selectedOption == i)
							{
								// hover style
								EditorGUI.LabelField(indented, EditorAutoCompleteParams.CacheCheckList[i], GUI.skin.textArea);
							}
							else
							{
								EditorGUI.LabelField(indented, EditorAutoCompleteParams.CacheCheckList[i], EditorStyles.label);
							}
							line.y += line.height;
						}
						EditorGUI.indentLevel--;

						// when hover popup, record this as the last usein tag.
						if (area.Contains(evt.mousePosition) && EditorAutoCompleteParams.lastTag != tag)
						{
							// Debug.Log("->" + tag + " Enter popup: " + area);
							// used to trigger the clicked checking later.
							EditorAutoCompleteParams.lastTag = tag;
						}
					}
					else if (evt.type == EventType.MouseDown)
					{
						if (area.Contains(evt.mousePosition) || position.Contains(evt.mousePosition))
						{
							EditorAutoCompleteParams.mouseDown = evt.mousePosition;
						}
						else
						{
							// click outside popup area, deselected - blur.
							EditorAutoCompleteParams.CleanUpAndBlur();
						}
					}
					else if (evt.type == EventType.MouseUp)
					{
						if (position.Contains(evt.mousePosition))
						{
							// common case click on textfield.
							return rst;
						}
						else if (area.Contains(evt.mousePosition))
						{
							if (Vector2.Distance(EditorAutoCompleteParams.mouseDown, evt.mousePosition) >= 3f)
							{
								// Debug.Log("Click and drag out the area.");
								return rst;
							}
							else
							{
								// Click event hack - part 3
								// for some reason, this session only run when popup display on inspector empty space.
								// when any selectable field behind of the popup list, Unity3D can't reaching this session.
								_AutoCompleteClickhandle(position, ref rst);
								EditorAutoCompleteParams.focusTag = string.Empty; // Clean up
								EditorAutoCompleteParams.lastTag = string.Empty; // Clean up
							}
						}
						else
						{
							// click outside popup area, deselected - blur.
							EditorAutoCompleteParams.CleanUpAndBlur();
						}
						return rst;
					}
					else if (evt.isKey && evt.type == EventType.KeyUp)
					{
						switch (evt.keyCode)
						{
							case KeyCode.PageUp:
							case KeyCode.UpArrow:
								EditorAutoCompleteParams.selectedOption--;
								if (EditorAutoCompleteParams.selectedOption < 0)
									EditorAutoCompleteParams.selectedOption = EditorAutoCompleteParams.CacheCheckList.Count - 1;
								break;
							case KeyCode.PageDown:
							case KeyCode.DownArrow:
								EditorAutoCompleteParams.selectedOption++;
								if (EditorAutoCompleteParams.selectedOption >= EditorAutoCompleteParams.CacheCheckList.Count)
									EditorAutoCompleteParams.selectedOption = 0;
								break;

							case KeyCode.KeypadEnter:
							case KeyCode.Return:
								if (EditorAutoCompleteParams.selectedOption != -1)
								{
									_AutoCompleteClickhandle(position, ref rst);
									EditorAutoCompleteParams.focusTag = string.Empty; // Clean up
									EditorAutoCompleteParams.lastTag = string.Empty; // Clean up
								}
								else
								{
									EditorAutoCompleteParams.CleanUpAndBlur();
								}
								break;

							case KeyCode.Escape:
								EditorAutoCompleteParams.CleanUpAndBlur();
								break;

							default:
								// hit any other key(s), assume typing, avoid override by Enter;
								EditorAutoCompleteParams.selectedOption = -1;
								break;
						}
					}
				}
			}
			else if (EditorAutoCompleteParams.lastTag == tag &&
				GUI.GetNameOfFocusedControl() != tag)
			{
				// Click event hack - part 2
				// catching mouse click on blur
				_AutoCompleteClickhandle(position, ref rst);
				EditorAutoCompleteParams.lastTag = string.Empty; // reset
			}

			return rst;
		}

		/// <summary>calculate auto complete select option location, and select it.
		/// within area, and we display option in "Vertical" style.
		/// which line is what we care.
		/// </summary>
		/// <param name="rst">input string, may overrided</param>
		/// <param name="cnt"></param>
		/// <param name="area"></param>
		/// <param name="mouseY"></param>
		private static void _AutoCompleteClickhandle(Rect position, ref string rst)
		{
			int index = EditorAutoCompleteParams.selectedOption;
			Vector2 pos = EditorAutoCompleteParams.mouseDown; // hack: assume mouse are stay in click position (1 frame behind).

			if (0 <= index && index < EditorAutoCompleteParams.CacheCheckList.Count)
			{
				rst = EditorAutoCompleteParams.CacheCheckList[index];
				GUI.changed = true;
				// Debug.Log("Selecting index (" + EditorAutoCompleteParams.selectedOption + ") "+ rst);
			}
			else
			{
				// Fail safe, when selectedOption failure
				
				int cnt = EditorAutoCompleteParams.CacheCheckList.Count;
				float height = cnt * EditorAutoCompleteParams.optionHeight;
				Rect area = new Rect(position.x, position.y - height, position.width, height);
				if (!area.Contains(pos))
					return; // return early.

				float lineY = area.y;
				for (int i = 0; i < cnt; i++)
				{
					if (lineY < pos.y && pos.y < lineY + EditorAutoCompleteParams.optionHeight)
					{
						rst = EditorAutoCompleteParams.CacheCheckList[i];
						Debug.LogError("Fail to select on \"" + EditorAutoCompleteParams.lastTag + "\" selected = " + rst + "\ncalculate by mouse position.");
						GUI.changed = true;
						break;
					}
					lineY += EditorAutoCompleteParams.optionHeight;
				}
			}

			EditorAutoCompleteParams.CleanUpAndBlur();
		}
		#endregion

		#region Object Field for Project files
		/// <summary>Keep reference to target "extension" files.</summary>
		/// <param name="ObjectProp">The serializedProperty from UnityEngine.Object.</param>
		/// <param name="extension">The file extension, within project folder.</param>
		/// <param name="title">The message wanted to display when developer click on file panel button.</param>
		/// <param name="OnBecomeNull">The callback while ObjectField become Null.</param>
		/// <param name="OnSuccess">The callback while ObjectField assign correct file type.</param>
		/// <param name="OnSuccessReadText">
		/// The callback while ObjectField assign correct file type.
		/// this will try to read all text from target file.
		/// </param>
		/// <param name="OnSuccessReadBytes">
		/// The callback while ObjectField assign correct file type.
		/// this will try to read all data as bytes[] from target file.
		/// </param>
		public static void ProjectFileField(SerializedProperty ObjectProp,
			string extension = "txt",
			string title = "",
			System.Action OnBecomeNull = null,
			System.Action OnSuccess = null,
			System.Action<string> OnSuccessReadText = null,
			System.Action<byte[]> OnSuccessReadBytes = null)
		{
			if (ObjectProp.propertyType != SerializedPropertyType.ObjectReference)
			{
				EditorGUILayout.HelpBox("Only available for Object field.", MessageType.Error);
				return;
			}
			// necessary infos. 
			string oldAssetPath = ObjectProp.objectReferenceValue ? AssetDatabase.GetAssetPath(ObjectProp.objectReferenceValue) : null;
			extension = extension.TrimStart('.').ToLower();

			// Editor draw
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(ObjectProp, true);
			if (GUILayout.Button("*."+extension, GUILayout.Width(80f)))
			{
				// Locate file by file panel.
				title = string.IsNullOrEmpty(title) ?
						"Open *." + extension + " file" :
						title;
				oldAssetPath = string.IsNullOrEmpty(oldAssetPath) ?
						Application.dataPath :
						Path.GetDirectoryName(oldAssetPath);

				string path = EditorUtility.OpenFilePanel(title, oldAssetPath, extension);
				string assetPath = string.IsNullOrEmpty(path) ? null : FileUtil.GetProjectRelativePath(path);
				if (!string.IsNullOrEmpty(assetPath))
				{
					UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
					if (obj == null)
						throw new System.InvalidProgramException();
					ObjectProp.objectReferenceValue = obj;
					ObjectProp.serializedObject.ApplyModifiedProperties();
				}
				// else cancel
			}
			EditorGUILayout.EndHorizontal();

			// Change check.
			if (EditorGUI.EndChangeCheck())
			{
				UnityEngine.Object obj = ObjectProp.objectReferenceValue;
				string assetPath = obj ? AssetDatabase.GetAssetPath(obj) : null;
				string fileExt = string.IsNullOrEmpty(assetPath) ? null : Path.GetExtension(assetPath);
				
				// we got things, so what is that ?
				bool match = obj != null;

				// valid path
				if (match)
				{
					match &= !string.IsNullOrEmpty(assetPath);
					if (!match)
						throw new System.InvalidProgramException("Error: " + obj + " have invalid path.");
				}

				// valid extension
				if (match)
				{
					match &= fileExt.TrimStart('.').ToLower() == extension;
					if (!match)
						EditorUtility.DisplayDialog("Wrong file type",
							"Wrong file assigned !" +
							"\n\t"+ObjectProp.serializedObject.targetObject.name + " > " + ObjectProp.displayName +
							"\n\tCan only accept [*." + extension + "] asset.",
							"Ok !");
						// Debug.LogError("Wrong file type, only accept [*." + extension + "] asset.", ObjectProp.serializedObject.targetObject);
				}

				if (match)
				{
					// seem like we got what we needed.
					ObjectProp.serializedObject.ApplyModifiedProperties();
					if (OnSuccess != null)
						OnSuccess();
					if (OnSuccessReadText != null)
					{
						string txt = File.ReadAllText(assetPath);
						OnSuccessReadText(txt);
					}
					if (OnSuccessReadBytes!=null)
					{
						byte[] bytes = File.ReadAllBytes(assetPath);
						OnSuccessReadBytes(bytes);
					}
				}
				else
				{
					ObjectProp.objectReferenceValue = null;
					ObjectProp.serializedObject.ApplyModifiedProperties();
					if (OnBecomeNull != null)
						OnBecomeNull();
					return;
				}
			}
		}
		#endregion

		#region EditorGUI Draw List Template
		static readonly GUIContent g_AddButton = new GUIContent("+", "ADD Item");
		static readonly GUIContent g_DelButton = new GUIContent("-", "Delete Item");

        public delegate void EachElementFullCallback(Rect rect, int index, SerializedProperty childProperty);
		
		/// <summary>
		/// Draw fold able list view template 
		/// </summary>
		/// <param name="listProperty"></param>
		/// <param name="callback"></param>
		/// <param name="deleteButton"></param>
		/// <param name="addButton"></param>
		public static void DrawListView(SerializedProperty listProperty, EachElementFullCallback callback,
			bool deleteButton = false,
			bool addButton = false)
		{
			if (listProperty == null)
				return;
			int cnt = listProperty.arraySize;
			Rect rect = EditorGUILayout.GetControlRect();
			Rect[] cell = rect.SplitRight(50f);
			EditorGUI.BeginChangeCheck();
			listProperty.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(cell[0], listProperty.isExpanded, listProperty.displayName);
			int newCnt = EditorGUI.IntField(cell[1], cnt);
			if (EditorGUI.EndChangeCheck())
			{
				listProperty.arraySize = newCnt;
				listProperty.serializedObject.ApplyModifiedProperties();
				EditorGUI.EndFoldoutHeaderGroup();
				return;
			}
			if (!listProperty.isExpanded || callback == null)
			{
				EditorGUI.EndFoldoutHeaderGroup();
				return;
			}

			if (callback != null)
			{
				for (int i = 0; i < cnt; ++i)
				{
					rect = EditorGUILayout.GetControlRect();
					rect = EditorGUI.IndentedRect(rect);
					if (deleteButton)
					{
						cell = rect.SplitLeft(20f);
						if (GUI.Button(cell[0], g_DelButton))
						{
							EditorGUI.EndFoldoutHeaderGroup();
							listProperty.DeleteArrayElementAtIndex(i);
							return;
						}
						rect = cell[1];
					}
					if (addButton)
					{
						cell = rect.SplitLeft(20f);
						if (GUI.Button(cell[0], g_AddButton))
						{
							listProperty.InsertArrayElementAtIndex(i);
							++cnt;
						}
						rect = cell[1];
					}
					callback(rect, i, listProperty.GetArrayElementAtIndex(i));
				}
			}

			// bottom +/- buttons
			cell = EditorGUILayout.GetControlRect().SplitRight(60f)[1].SplitHorizontal(0.5f);
            if (GUI.Button(cell[0], g_AddButton))
            {
                listProperty.InsertArrayElementAtIndex(cnt);
            }
			using (new EditorGUI.DisabledScope(cnt == 0))
			{
				if (GUI.Button(cell[1], g_DelButton))
					listProperty.DeleteArrayElementAtIndex(cnt - 1);
			}

            EditorGUI.EndFoldoutHeaderGroup();
        }

		/// <summary>
		/// To easy apply searchableBlendShapeWeight
		/// </summary>
		/// <example>
		/// EditorExtend.DrawListView(property, (rect, index, prop) => {
		///		EditorExtend.SearchableBlendShapeWeight(smr, rect, prop, blendShape.Keys); },
		/// deleteButton: true, addButton: true);
		/// </example>
		/// <param name="smr"></param>
		/// <param name="rect"></param>
		/// <param name="prop"></param>
		/// <param name="bsNames"></param>
		public static void GUISearchableBlendShapeWeight(
			SkinnedMeshRenderer smr,
			Rect rect,
			SerializedProperty prop,
			ICollection<string> bsNames)
        {
            if (smr == null || smr.sharedMesh == null)
                return;

            Rect[] cell = rect.SplitHorizontal(0.7f, 100f, 800f);
			string oldFieldName = prop.stringValue;
            EditorGUI.BeginChangeCheck();
            string fieldName = EditorExtend.TextFieldAutoComplete(cell[0], oldFieldName, bsNames, 10, 0.5f);
            if (EditorGUI.EndChangeCheck() && !oldFieldName.Equals(fieldName))
            {
                Undo.RecordObject(smr, "Modify BlendShape Name");
                // Debug.Log($"Change keyword {fieldName}");
                prop.stringValue = fieldName;
                prop.serializedObject.ApplyModifiedProperties();
            }
            int idx = smr.sharedMesh.GetBlendShapeIndex(prop.stringValue);
            if (idx == -1)
                return;
			float oldW = smr.GetBlendShapeWeight(idx);
            EditorGUI.BeginChangeCheck();
            const float SPACE = 5f;
            if (cell[1].size.x > SPACE)
            {
                // add space in between
                cell = cell[1].SplitLeft(SPACE);
            }
            float newW = EditorGUI.Slider(cell[1], oldW, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(smr, "Modify BlendShape");
				smr.SetBlendShapeWeight(idx, newW);
				// smr.sharedMesh.MarkModified();
				// Debug.Log($"Modify {idx} value = {newW:F2}");
				prop.serializedObject.ApplyModifiedProperties();
            }
        }
		#endregion EditorGUI Draw List Template

		#region Project File Path
		/// <summary>
		/// Convert assetPath back to system's absolute path
		/// </summary>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		/// <exception cref="System.InvalidCastException"></exception>
		[System.Obsolete("Use ResolvePath instead.")]
		public static string ConvertAbsolutePath(string assetPath)
        {
			if (assetPath == null || assetPath.Length == 0)
				return string.Empty;
			const string assetsFolder = "Assets";
			if (assetPath.Length < assetsFolder.Length)
				throw new System.InvalidCastException($"Invalid path : {assetPath}");
			// bool valid = assetPath.StartsWith(assetsFolder, true, System.Globalization.CultureInfo.DefaultThreadCurrentCulture);
			bool valid = AssetDatabase.IsValidFolder(assetPath);
			if (!valid)
				throw new System.InvalidCastException($"Invalid path : {assetPath}");

			string absolutePath = assetPath.Replace(assetsFolder, Application.dataPath);
			// Debug.Log(absolutePath);
			return absolutePath;
		}

		/// <summary>
		/// The asset path that can used in <see cref="AssetDatabase"/>
		/// </summary>
		/// <param name="absolutePath">the system absolute file path.</param>
		/// <returns></returns>
		[System.Obsolete("Use ResolvePath instead.")]
		public static string ConvertAssetPath(string absolutePath)
        {
			if (absolutePath == null || absolutePath.Length == 0)
				return string.Empty;
            string fullPath = absolutePath.Replace(@"\", "/").TrimEnd('/');
            // Debug.Log(fullPath);
            string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
			// Debug.Log(assetPath.ToRichText(AssetDatabase.IsValidFolder(assetPath) ? Color.green : Color.red));
			return assetPath;
        }

		public static bool AssetFolderField(string title, ref string folder, string defaultName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Source Folder:");
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    GUILayout.TextField(folder, GUILayout.ExpandWidth(true));
                }
                if (GUILayout.Button("Set", GUILayout.Width(50f), GUILayout.Height(20f)))
                {
					if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                    {
						folder = ConvertAbsolutePath(folder);
                    }

					string rawPath = EditorUtility.OpenFolderPanel(title, folder, defaultName);
					//Debug.Log(rawPath.ToRichText(Color.green));
					if (string.IsNullOrEmpty(rawPath))
                    {
						// Cancel
						return false;
                    }
					else if (rawPath.StartsWith(Application.dataPath))
					{
						string assetPath = EditorExtend.ConvertAssetPath(rawPath);
						folder = assetPath;
						// Debug.Log(assetPath.ToRichText(AssetDatabase.IsValidFolder(assetPath) ? Color.green : Color.red));
						return true;
					}
					else
                    {
						Debug.LogError($"Invalid Folder: {rawPath.ToRichText(Color.yellow)}");
						return false;
                    }
                }
            }
			return false;
        }

		/// <summary></summary>
		/// <param name="path">Input absolute or relative convert into absolute and relative in project</param>
		/// <param name="absolutePath"></param>
		/// <param name="relativePath"></param>
		/// <exception cref="System.NullReferenceException"></exception>
		/// <exception cref="System.NotImplementedException"></exception>
		public static void ResolvePath(string path, out string absolutePath, out string relativePath)
		{
			if (path == null || path.Length == 0)
				throw new System.NullReferenceException();

#if true
			path = path.Replace("\\", "/");
			var isRelativePath = path.StartsWith("Assets/");
			var isAbsPath = path.StartsWith(Application.dataPath);
			if (isRelativePath)
			{
				var str = path.Substring(7);
				absolutePath = Path.Combine(Application.dataPath, str).Replace("\\", "/");
				relativePath = Path.Combine("Assets", str).Replace("\\", "/");
				return;
			}
			if (isAbsPath)
			{
				var str = path.Substring(Application.dataPath.Length + 1);
				absolutePath = Path.Combine(Application.dataPath, str).Replace("\\", "/");
				relativePath = Path.Combine("Assets", str).Replace("\\", "/");
				return;
			}
			throw new System.NotImplementedException($"Unknow path format : {path}");
#else
			// Only work on file.
			relativePath = FileUtil.GetProjectRelativePath(path);
			// absolutePath = FileUtil.GetLogicalPath(path);
			absolutePath = FileUtil.GetPhysicalPath(path);
#endif
		}

		public static void CreateOrWriteFile(string path, string content, System.Text.Encoding encoding = default)
		{
			if (path == null || path.Length == 0)
			{
				Debug.Log("Process suspended, user cancel.");
				return;
			}
			if (encoding == default)
				encoding = System.Text.Encoding.UTF8;
			
			ResolvePath(path, out var absolutePath, out var relativePath);
			var dir = Path.GetDirectoryName(relativePath);
			if (!AssetDatabase.IsValidFolder(dir))
			{
				Debug.Log($"Process suspended, invalid path = {path}");
				return;
			}
			if (File.Exists(absolutePath))
			{
				if (!EditorUtility.DisplayDialog("File exist", "File already exist, want to overwrite ?", "Process", "Cancel"))
				{
					Debug.Log("Process suspended, cancel by user.");
					return;
				}

			}
			File.WriteAllText(absolutePath, content, encoding);
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// return asset file path and project relative path.
		/// </summary>
		/// <param name="absolutePath">path for <see cref="System.IO.Path"/>absolute path.</param>
		/// <param name="relativePath">path for <see cref="AssetDatabase"/>relative path for project.</param>
		/// <returns>true = success access file's paths</returns>
		public static bool TryGetAssetPath(Object U3DAsset, out string absolutePath, out string relativePath)
		{
			absolutePath = null;
			relativePath = null;
			if (U3DAsset == null)
				return false;

			string assetFile = AssetDatabase.GetAssetPath(U3DAsset);
			if (string.IsNullOrEmpty(assetFile))
				return false;
			string assetPath = Path.GetDirectoryName(assetFile);
			if (string.IsNullOrEmpty(assetPath))
				return false;
			ResolvePath(assetPath, out absolutePath, out relativePath);
			return true;
		}

		/// <summary>Enumerate all files in the folder.</summary>
		/// <param name="path"></param>
		/// <param name="searchPattern"></param>
		/// <param name="searchOption"></param>
		/// <returns></returns>
		public static IEnumerable<string> ListFilesInFolder(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			if (path == null || path.Length == 0)
			{
				Debug.LogError("Invalid path.");
				yield break;
			}
			ResolvePath(path, out var absolutePath, out var relativePath);
			if (!Directory.Exists(absolutePath))
			{
				Debug.LogError("Path not exist.");
				yield break;
			}
			foreach (var file in Directory.EnumerateFiles(absolutePath, searchPattern, searchOption))
			{
				yield return file;
			}
		}
		#endregion Project File Path

		#region Icon
		[MenuItem("Kit/Generate Code/EditorGUI Icons")]
		private static void GenerateEditorGUIIcons()
		{
			var subs = AssetDatabase.GetSubFolders("Assets");
			var scriptFolder = string.Empty;
			for (int i = 0; i < subs.Length && scriptFolder.Length == 0; ++i)
			{
				if (!subs[i].Contains("script", System.StringComparison.OrdinalIgnoreCase))
					continue;
				scriptFolder = subs[i];
			}
			if (scriptFolder.Length == 0)
				scriptFolder = "Assets";

			var path = EditorUtility.SaveFolderPanel("EditorGUI Icons", scriptFolder, "");
			if (path == null || path.Length == 0)
			{
				Debug.Log("Process suspended, user cancel.");
				return;
			}
			ResolvePath(path, out var absolutePath, out var relativePath);
			var fullPath = Path.Join(absolutePath, "EditorGUIIcons.cs");

			var sb = new System.Text.StringBuilder(2048);
			GenerateEditorGUIIconsCode(ref sb);
			CreateOrWriteFile(fullPath, sb.ToString());
		}

		/// <summary>
		/// Mine the icon(s) from U3D folder
		/// ref : <see cref="https://github.com/halak/unity-editor-icons"/>
		/// </summary>
		private static void GenerateEditorGUIIconsCode(ref System.Text.StringBuilder sb)
		{
			sb.AppendLine("using UnityEngine;");
			sb.AppendLine("using UnityEditor;");
			sb.AppendLine("/************************************************");
			sb.AppendLine(" Generate Code : ");
			sb.AppendLine("************************************************/");
			EditorUtility.DisplayProgressBar("Fetch Icon(s)", "Mining...", 0f);
			sb.AppendLine("namespace Kit2 {");
			sb.AppendLine("public static class EditorGUIIcons {");
			sb.AppendLine("private static GUIContent Get(ref GUIContent inner, string builtInPath){if (inner == null) inner = EditorGUIUtility.IconContent(builtInPath); return inner;}");

			var varRename = new Regex(@"[^a-zA-Z0-9_]", RegexOptions.IgnoreCase);
			var failCase = new Regex(@"^[0-9]", RegexOptions.IgnoreCase);
			try
			{
				var editorAssetBundle = GetEditorAssetBundle();
				var iconsPath = GetIconsPath();
				var hashset = new HashSet<string>();
				foreach (var assetName in EnumerateIcons(editorAssetBundle, iconsPath))
				{
					var icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);
					if (icon == null)
						continue;
					var varName = varRename.Replace(icon.name, string.Empty);
					if (failCase.IsMatch(varName))
						varName = $"_{varName}";
					if (hashset.Contains(varName))
						continue; // skip duplicate cases for now.
					hashset.Add(varName);
					// var varName = icon.name.Replace("-","").Replace(".","").Replace(" ", "").Replace("@", "");

					sb
						.Append("private static GUIContent ")
						.Append($"s_{varName}")
						.Append(" = null;")
						.Append(" public static GUIContent ")
						.Append(varName)
						.Append("=> Get(ref ")
						.Append($"s_{varName}")
						.Append(", \"")
						.Append(icon.name)
						.Append("\");")
						.AppendLine();
				}

				sb.AppendLine("}}");

				Debug.Log($"Generate EditorGUI icons has been exported!");
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static AssetBundle GetEditorAssetBundle()
		{
			var editorGUIUtility = typeof(EditorGUIUtility);
			var getEditorAssetBundle = editorGUIUtility.GetMethod(
				"GetEditorAssetBundle",
				BindingFlags.NonPublic | BindingFlags.Static);

			return (AssetBundle)getEditorAssetBundle.Invoke(null, new object[] { });
		}

		private static IEnumerable<string> EnumerateIcons(AssetBundle editorAssetBundle, string iconsPath)
		{
			foreach (var assetName in editorAssetBundle.GetAllAssetNames())
			{
				if (assetName.StartsWith(iconsPath, StringComparison.OrdinalIgnoreCase) == false)
					continue;
				if (assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false &&
					assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == false)
					continue;

				yield return assetName;
			}
		}

		private static string GetIconsPath()
		{
#if UNITY_2018_3_OR_NEWER
			return UnityEditor.Experimental.EditorResources.iconsPath;
#else
            var assembly = typeof(EditorGUIUtility).Assembly;
            var editorResourcesUtility = assembly.GetType("UnityEditorInternal.EditorResourcesUtility");

            var iconsPathProperty = editorResourcesUtility.GetProperty(
                "iconsPath",
                BindingFlags.Static | BindingFlags.Public);

            return (string)iconsPathProperty.GetValue(null, new object[] { });
#endif
		}
		#endregion Icon

		/// <summary><see cref="http://stackoverflow.com/questions/720157/finding-all-classes-with-a-particular-attribute"/></summary>
		/// <typeparam name="TAttribute"></typeparam>
		/// <param name="inherit"></param>
		/// <returns></returns>
		public static IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit)
			where TAttribute : System.Attribute
		{
			return from assem in System.AppDomain.CurrentDomain.GetAssemblies()
				   from type in assem.GetTypes()
				   where type.IsDefined(typeof(TAttribute), inherit)
				   select type;
		}
	}

	[System.Obsolete("EditorGUI.ChangeCheckScope", true)]
	public struct ChangeCheckScope : IDisposable
	{
		SerializedObject serializedObject;
		public ChangeCheckScope(SerializedObject serializedObject)
		{
			this.serializedObject = serializedObject;
			EditorGUI.BeginChangeCheck();
		}

		public void Dispose()
		{
			if (EditorGUI.EndChangeCheck() && serializedObject != null)
				serializedObject.ApplyModifiedProperties();
		}
	}

	/// <summary>
	/// Use this when it must be EditorGUILayout rather than GUILayout to work correctly.
	/// </summary>
	// NOTE: this must be a class, not a struct. Otherwise using it gives weird UI errors!
	public class EditorVerticalScope : IDisposable
	{
		public Rect rect;

		/// <summary>
		/// </summary>
		/// <param name="options">
		///     An optional list of layout options that specify extra layout properties. Any
		///     values passed in here will override settings defined by the style.<br> See Also:
		///     GUILayout.Width, GUILayout.Height, GUILayout.MinWidth, GUILayout.MaxWidth, GUILayout.MinHeight,
		///     GUILayout.MaxHeight, GUILayout.ExpandWidth, GUILayout.ExpandHeight.
		/// </param>
		public EditorVerticalScope(params GUILayoutOption[] options)
		{
			rect = EditorGUILayout.BeginVertical(options);
		}

		/// <summary>
		/// </summary>
		/// <param name="options">
		///     An optional list of layout options that specify extra layout properties. Any
		///     values passed in here will override settings defined by the style.<br> See Also:
		///     GUILayout.Width, GUILayout.Height, GUILayout.MinWidth, GUILayout.MaxWidth, GUILayout.MinHeight,
		///     GUILayout.MaxHeight, GUILayout.ExpandWidth, GUILayout.ExpandHeight.
		/// </param>
		public EditorVerticalScope(GUIStyle style, params GUILayoutOption[] options)
		{
			rect = EditorGUILayout.BeginVertical(style, options);
		}

		public void Dispose()
		{
			EditorGUILayout.EndVertical();
		}
	}

	/// <summary>
	/// Use this when it must be EditorGUILayout rather than GUILayout to work correctly.
	/// </summary>
	// NOTE: this must be a class, not a struct. Otherwise using it gives weird UI errors!
	public class EditorHorizontalScope : IDisposable
	{
		public Rect rect;

		/// <summary>
		/// </summary>
		/// <param name="options">
		///     An optional list of layout options that specify extra layout properties. Any
		///     values passed in here will override settings defined by the style.<br> See Also:
		///     GUILayout.Width, GUILayout.Height, GUILayout.MinWidth, GUILayout.MaxWidth, GUILayout.MinHeight,
		///     GUILayout.MaxHeight, GUILayout.ExpandWidth, GUILayout.ExpandHeight.
		/// </param>
		public EditorHorizontalScope(params GUILayoutOption[] options)
		{
			rect = EditorGUILayout.BeginHorizontal(options);
		}


		/// <summary>
		/// </summary>
		/// <param name="options">
		///     An optional list of layout options that specify extra layout properties. Any
		///     values passed in here will override settings defined by the style.<br> See Also:
		///     GUILayout.Width, GUILayout.Height, GUILayout.MinWidth, GUILayout.MaxWidth, GUILayout.MinHeight,
		///     GUILayout.MaxHeight, GUILayout.ExpandWidth, GUILayout.ExpandHeight.
		/// </param>
		public EditorHorizontalScope(GUIStyle style, params GUILayoutOption[] options)
		{
			rect = EditorGUILayout.BeginHorizontal(style, options);
		}

		public void Dispose()
		{
			EditorGUILayout.EndHorizontal();
		}
	}

	/// <summary>
	/// Same as EditorGUILayout.ScrollViewScope, but with a nicer API (ref in the constructor).
	/// </summary>
	public class EditorScrollViewScope : EditorGUILayout.ScrollViewScope
	{
		public EditorScrollViewScope(ref Vector2 scrollPos, params GUILayoutOption[] options)
			: base(scrollPos, options)
		{
			scrollPos = base.scrollPosition;
		}
		public EditorScrollViewScope(
			ref Vector2 scrollPos, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
			: base(scrollPos, alwaysShowHorizontal, alwaysShowVertical, options)
		{
			scrollPos = base.scrollPosition;
		}
		public EditorScrollViewScope(
			ref Vector2 scrollPos, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
			: base(scrollPos, horizontalScrollbar, verticalScrollbar, options)
		{
			scrollPos = base.scrollPosition;
		}
		public EditorScrollViewScope(
			ref Vector2 scrollPos, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
			: base(scrollPos, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background, options)
		{
			scrollPos = base.scrollPosition;
		}
	}

	public struct EditorLabelWidthScope : IDisposable
	{
		float savedValue;
		public EditorLabelWidthScope(float value)
		{
			savedValue = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = value;
		}

		public void Dispose()
		{
			EditorGUIUtility.labelWidth = savedValue;
		}
	}
}
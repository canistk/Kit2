using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Kit2
{
    [CustomPropertyDrawer(typeof(AssetFileAttribute))]
    public class AssetFileDrawer : PropertyDrawer
    {
        const string ASSET_FOLDER = "Assets";
        System.Globalization.CultureInfo s_Culture => System.Globalization.CultureInfo.DefaultThreadCurrentCulture;
        AssetFileAttribute assetFile => (AssetFileAttribute)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                // base.OnGUI(position, property, label);
                Rect rest = EditorGUI.PrefixLabel(position, label);
                var cells = rest.SplitRight(50f);
                EditorGUI.TextField(cells[0], property.stringValue);
                if (GUI.Button(cells[1], "Set"))
                {
                    var file = property.stringValue;
                    if (!string.IsNullOrEmpty(file) &&
                        file.Length > ASSET_FOLDER.Length &&
                        file.StartsWith(ASSET_FOLDER, ignoreCase: true, s_Culture))
                    {
                        file = file.Replace(ASSET_FOLDER, Application.dataPath, System.StringComparison.OrdinalIgnoreCase);
                    }
                    var rawPath = EditorUtility.OpenFilePanel(assetFile.title, file, assetFile.extension);
                    if (string.IsNullOrEmpty(rawPath))
                    {
                        return;
                    }
                    else if (rawPath.StartsWith(Application.dataPath))
                    {
                        file = EditorExtend.ConvertAssetPath(rawPath);
                        var dir = System.IO.Path.GetDirectoryName(file);
                        if (!AssetDatabase.IsValidFolder(dir))
                        {
                            Debug.LogError($"invalid path : {file}");
                        }
                        property.stringValue = file;
                        property.serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        property.stringValue = rawPath;
                        property.serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }
}
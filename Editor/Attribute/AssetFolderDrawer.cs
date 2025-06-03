using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Kit2
{
    [CustomPropertyDrawer(typeof(AssetFolderAttribute))]
    public class AssetFolderDrawer : PropertyDrawer
    {
        const string ASSET_FOLDER = "Assets";
        System.Globalization.CultureInfo s_Culture => System.Globalization.CultureInfo.DefaultThreadCurrentCulture;

        AssetFolderAttribute assetFolder => (AssetFolderAttribute)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);
            Rect rest = EditorGUI.PrefixLabel(position, label);
            var cells = rest.SplitRight(50f);
            EditorGUI.TextField(cells[0], property.stringValue);
            if (GUI.Button(cells[1], "Set"))
            {
                var folder = property.stringValue;
                if (!string.IsNullOrEmpty(folder) &&
                    folder.Length > ASSET_FOLDER.Length &&
                    folder.StartsWith(ASSET_FOLDER, ignoreCase: true, s_Culture))
                {
                    folder = folder.Replace(ASSET_FOLDER, Application.dataPath, System.StringComparison.OrdinalIgnoreCase);
                }
                var rawPath = EditorUtility.OpenFolderPanel(assetFolder.title, folder, assetFolder.defaultName);
                if (string.IsNullOrEmpty(rawPath))
                {
                    // nothing
                }
                else if (rawPath.StartsWith(Application.dataPath))
                {
                    folder = EditorExtend.ConvertAssetPath(rawPath);
                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        Debug.LogError($"invalid path : {folder}");
                    }
                    property.stringValue = folder;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}

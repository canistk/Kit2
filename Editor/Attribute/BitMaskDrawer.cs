using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Kit2
{
    [CustomPropertyDrawer(typeof(BitMaskAttribute))]
    public class BitMaskDrawer : PropertyDrawer
    {
        BitMaskAttribute bitMask { get { return (BitMaskAttribute)attribute; } }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label, typeof(BitMaskAttribute).Name + " only allow to use with { integer }.");
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            const float w = 17f;
            const float intField = 50f;
            const float gap = 3f;
            // EditorGUI.showMixedValue = prop.hasMixedValue;
            using (var checker = new EditorGUI.ChangeCheckScope())
            {
                // int mask = prop.intValue
                int mask = property.intValue;
                var bodyWidth = w * 10f + gap + intField;
                var rect = position.SplitRight(bodyWidth);
                EditorGUI.LabelField(rect[0], label);
                var ch = rect[1].SplitHorizontalUnclamp(true, w, w, w, w, w, w, w, w, w, w, gap, intField);

                if (GUI.Button(ch[0], "-"))
                    mask = 0;

                if (GUI.Button(ch[9], "A"))
                    mask = 255;

                int value = 0;
                for (int i = 0; i < 8; ++i)
                {
                    var offset = 1 << i;
                    var bit = (mask & offset) != 0;
                    if (GUI.Button(ch[i + 1], (i + 1).ToString(), bit ? GUI.skin.button : GUI.skin.label))
                    {
                        bit = !bit;
                    }
                    if (bit)
                    {
                        value |= offset;
                    }
                }

                var finalValue = EditorGUI.IntField(ch[ch.Length - 1], value);
                value = Mathf.Clamp(finalValue, 0, 255);

                if (checker.changed)
                {
                    property.intValue = value; // why it didn't work ?
                }
            }
        }
    }
}
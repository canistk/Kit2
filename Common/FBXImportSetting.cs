using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Kit2
{
	[CreateAssetMenu(
		fileName = "FBXImportSetting",
		menuName = "Kit2/FBX Import Setting")]
	public class FBXImportSetting : ScriptableObject
	{
		[Header("Setting")]
		public bool m_DisableImporter = false;
		[SerializeField, ReadOnly] string m_FBXDirectory;
		public string FBXDirectory => Application.dataPath + "/" + m_FBXDirectory;

		[Space]
		[Header("Loop")]
		public bool m_Loop = false;
		public bool m_LoopPose = false;
		public float m_CycleOffset = 0f;

		[Header("Root Transform Rotation")]
		public bool BakeRotationIntoPose = false;
		public enum eRotationBaseUpon { Original = 0, BodyOrientation = 1, }
		public eRotationBaseUpon rotationBaseUpon = eRotationBaseUpon.Original;
		public float rotationOffset = 0f;

		[Header("Root Transform Position (Y)")]
		public bool BakePosYIntoPose = true;
		public enum ePositionYBaseUpon { Original, CenterOfMass, Feet }
		public ePositionYBaseUpon positionYBaseUpon = ePositionYBaseUpon.Original;
		public float positionYOffset = 0f;

		[Header("Root Transform Position (XZ)")]
		public bool BakePosXZIntoPose = false;
		public enum ePositionXZBaseUpon { Original, CenterOfMass }
		public ePositionXZBaseUpon positionXZBaseUpon = ePositionXZBaseUpon.Original;

		private void Reset()
		{
#if UNITY_EDITOR
			if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			var path = AssetDatabase.GetAssetPath(this);
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("Fail to fetch path of FBXImportSetting");
				return;
			}

			var fileName = System.IO.Path.GetFileName(path);
			this.m_FBXDirectory = path.Substring(0, path.Length - fileName.Length);
#endif
		}
	}

}
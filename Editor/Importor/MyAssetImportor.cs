using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace Kit2
{
    public class MyAssetImportor : AssetPostprocessor
	{
		private static List<KeyValuePair<string, FBXImportSetting>> m_CacheSettings = null;
		private const double s_AgreePeriod = 10f;
		private static double m_lastTimeAgree = 0f;
		private static int m_WasAgree = -1;


		void OnPreprocessModel()
		{
			//if (!assetImporter.importSettingsMissing)
			//	return; // already had meta data.
			if (string.IsNullOrEmpty(assetPath))
				return;
			if (!TryGetImportSetting(assetPath, out var setting))
				return;
			if (setting.m_DisableImporter)
				return;
			if (assetImporter is not ModelImporter modelImporter)
				return;
			if (!AskForDeveloperPermission(setting.FBXDirectory))
				return;

			modelImporter.animationType = ModelImporterAnimationType.Human;
			string path = Path.GetDirectoryName(modelImporter.assetPath);
			modelImporter.ExtractTextures(path);
			modelImporter.materialLocation = ModelImporterMaterialLocation.External;
			AssetDatabase.ImportAsset(modelImporter.assetPath);
		}

		void OnPreprocessAnimation()
		{
			//if (!assetImporter.importSettingsMissing)
			//	return; // avoid reimport
			if (string.IsNullOrEmpty(assetPath))
				return;
			if (!TryGetImportSetting(assetPath, out var setting))
				return;
			if (setting.m_DisableImporter)
				return;

			if (assetImporter is not ModelImporter modelImporter)
				return;

			//if (!AskForDeveloperPermission(setting.FBXDirectory))
			//	return;

			var animations = modelImporter.defaultClipAnimations;
			if (animations == null || animations.Length == 0)
				return;

			for (int i = 0; i < animations.Length; ++i)
			{
				DefineAnimationSetting(animations[i], setting, modelImporter);
			}

			modelImporter.clipAnimations = animations;
			modelImporter.animationType = ModelImporterAnimationType.Human;
		}

		private void DefineAnimationSetting(ModelImporterClipAnimation animation, FBXImportSetting importSetting, AssetImporter modelImporter)
		{
			animation.name = Path.GetFileNameWithoutExtension(modelImporter.assetPath);
			if (importSetting.m_Loop)
			{
				animation.loop = true;
				animation.loopTime = importSetting.m_Loop;
				animation.loopPose = importSetting.m_LoopPose;
				animation.cycleOffset = importSetting.m_CycleOffset;
			}

			// Root Transform Rotation
			animation.lockRootRotation = importSetting.BakeRotationIntoPose;
			animation.keepOriginalOrientation = importSetting.rotationBaseUpon == FBXImportSetting.eRotationBaseUpon.Original;
			animation.rotationOffset = importSetting.rotationOffset;

			// Root Transform Position (Y)
			animation.lockRootHeightY = importSetting.BakePosYIntoPose;
			switch (importSetting.positionYBaseUpon)
			{
				case FBXImportSetting.ePositionYBaseUpon.Original:
				animation.keepOriginalPositionY = true;
				animation.heightFromFeet = false;
				break;
				case FBXImportSetting.ePositionYBaseUpon.CenterOfMass:
				animation.keepOriginalPositionY = false;
				animation.heightFromFeet = false;
				break;
				case FBXImportSetting.ePositionYBaseUpon.Feet:
				animation.keepOriginalPositionY = false;
				animation.heightFromFeet = true;
				break;
				default: throw new System.NotImplementedException();
			}
			animation.heightOffset = importSetting.positionYOffset;

			// Root Transform Position (XZ)
			animation.lockRootPositionXZ = importSetting.BakePosXZIntoPose;
			animation.keepOriginalPositionXZ = importSetting.positionXZBaseUpon == FBXImportSetting.ePositionXZBaseUpon.Original;
		}

		private bool AskForDeveloperPermission(string path)
		{
			double duration = EditorApplication.timeSinceStartup - m_lastTimeAgree;
			bool isWithinSession = duration < s_AgreePeriod;
			if (m_WasAgree != -1 && isWithinSession)
				return m_WasAgree == 1;

			m_WasAgree = UnityEditor.EditorUtility.DisplayDialog(
				nameof(MyAssetImportor),
				$"Do you want to apply {nameof(MyAssetImportor)}\n" +
				$"under folder : {path}",
				"Apply"
				// ,$"Stop asking for {s_AgreePeriod:F0}sec", DialogOptOutDecisionType.ForThisSession, $"Don't ask for session"
				) ? 1 : 0;
			m_lastTimeAgree = EditorApplication.timeSinceStartup;
			return m_WasAgree == 1;
		}

		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
		static bool TryGetImportSetting(string assetPath, out FBXImportSetting setting)
		{
			if (m_CacheSettings == null)
			{
				var guids = AssetDatabase.FindAssets($"t:{nameof(FBXImportSetting)}");
				m_CacheSettings = new List<KeyValuePair<string, FBXImportSetting>>(guids.Length);
				for (int i = 0; i < guids.Length; i++)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[i]);
					var directory = Path.GetDirectoryName(path).Replace('\\','/');
					var obj = AssetDatabase.LoadAssetAtPath<FBXImportSetting>(path);
					if (obj != null)
						m_CacheSettings.Add(new KeyValuePair<string, FBXImportSetting>(directory, obj));
				}
			}

			var searchPath = assetPath.Replace('\\', '/');
			foreach ((var path, var obj) in m_CacheSettings)
			{
				if (!searchPath.StartsWith(path, IGNORE))
					continue;
				setting = obj;
				return true;
			}
			setting = default;
			return false;
		}

	}

}
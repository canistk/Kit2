using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.LowLevel;
#if UNITY_EDITOR
using UnityEditor.Compilation;
using UnityEditor;
#if UNITY_2023_1_OR_NEWER
using UnityEditor.Build;
#endif
#endif
namespace Kit2
{
    public static class PlayerSettingsExtend
    {
        public static void DefineSymbol(string _symbol)
        {
#if UNITY_2023_1_OR_NEWER
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] newDefines);
            if (newDefines.Contains(_symbol) == false)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines.Concat(new string[] { _symbol }).ToArray());
            }
#else
			var newDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');
			if (newDefines.Contains(_symbol) == false)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines.Concat(new string[] { _symbol }).ToArray());
			}
#endif
		}
	}

}
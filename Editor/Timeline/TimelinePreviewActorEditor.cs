using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;

namespace Kit2
{
    [CustomEditor(typeof(TimelinePreviewActor), true)]
    public class TimelinePreviewActorEditor : EditorBase
    {
        SerializedProperty actorProp, rootProp = null;
        TimelinePreviewActor self => serializedObject.targetObject as TimelinePreviewActor;

        private GameObject m_DebugActor
        {
            get => self ? self.actor : null;
            set => self.actor = value;
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            actorProp   = serializedObject.FindProperty(nameof(TimelinePreviewActor.m_ActorPrefab));
            rootProp    = serializedObject.FindProperty(nameof(TimelinePreviewActor.m_PreviewRoot));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearUp();
        }

        private void ClearUp()
        {
            if (m_DebugActor != null)
            {
                DestroyImmediate(m_DebugActor);
                m_DebugActor = null;
            }
        }

        protected override void OnBeforeDrawGUI()
        {
            base.OnBeforeDrawGUI();
            if (GUILayout.Button("Preview Actor"))
            {
                ClearUp();
                CreatePreviewActor();
            }
        }

        private void CreatePreviewActor()
        {
            //var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            //if (prefabStage == null ||
            //    prefabStage.mode != PrefabStage.Mode.InIsolation)
            //{
            //    return;
            //}

            if (actorProp == null)
            {
                Debug.LogError($"Missing preview Actor.");
                return;
            }
            var ani = actorProp.objectReferenceValue as Animator;
            if (ani == null)
            {
                Debug.LogError("Require animator.");
                return;
            }
            var go = ani.gameObject;
            if (go == null)
            {
                Debug.LogError("Target must be GameObject/Prefab");
                return;
            }

            var prefab = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (prefab == null && go != null)
            {
                prefab = go;
            }
            if (prefab == null)
            {
                Debug.LogError($"Invalid prefab");
                return;
            }
            if (m_DebugActor != null)
            {
                GameObject.DestroyImmediate(m_DebugActor);
                m_DebugActor = null;
            }
            m_DebugActor = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            m_DebugActor.hideFlags = HideFlags.DontSave;

            Transform _parent = rootProp.objectReferenceValue ? (rootProp.objectReferenceValue as Component).transform : self.transform;
            m_DebugActor.transform.SetParent(_parent);
            m_DebugActor.transform.localPosition = Vector3.zero;
            m_DebugActor.transform.localRotation = Quaternion.identity;
            var _animator = m_DebugActor.GetComponentInChildren<Animator>();

            if (self.playableDirector == null)
                return;

            var playableAsset = self.playableDirector.playableAsset;
            if (playableAsset == null)
                return;

            string filterName = self.m_ContainName;
            foreach (PlayableBinding binding in playableAsset.outputs)
            {
                if (binding.sourceObject is not UnityEngine.Timeline.AnimationTrack track)
                    continue;

                if (self.m_OnlyEmptyTrack)
                {
                    var current = self.playableDirector.GetGenericBinding(track);
                    if (current != null)
                    {
                        Debug.LogWarning($"Already assigned track : {track.name} NOT null");
                        continue;
                    }
                }

                if (filterName == null          ||
                    filterName.Length == 0      ||
                    track.name.Contains(filterName))
                {
                    self.playableDirector.SetGenericBinding(track, _animator);
                    Debug.Log($"Assign to track : {track.name}");
                }

                self.playableDirector.Play();
            }
        }
    }
}
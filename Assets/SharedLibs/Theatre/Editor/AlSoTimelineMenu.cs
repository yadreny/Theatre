//#if UNITY_EDITOR
//using System;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEditor.Timeline;
//using UnityEditorInternal;
//using UnityEngine;
//using UnityEngine.Playables;
//using UnityEngine.Timeline;

//namespace AlSo
//{
//    public static class AlSoTimelineHierarchyCreateGroup
//    {
//        [MenuItem("GameObject/AlSo/Timeline/Create Character Groups (Actor+Move+Action)", false, 49)]
//        private static void Create(MenuCommand command)
//        {
//            // Unity может вызвать этот MenuItem несколько раз (по одному на каждый выбранный объект).
//            // Выполняем реальную работу только один раз — на вызове, соответствующем activeObject.
//            if (command == null || command.context == null || command.context != Selection.activeObject)
//            {
//                return;
//            }

//            Transform[] selection = Selection.transforms;
//            if (selection == null || selection.Length == 0)
//            {
//                UnityEngine.Debug.LogWarning("[AlSoTimeline] No transforms selected.");
//                return;
//            }

//            PlayableDirector director = TimelineEditor.inspectedDirector ?? TimelineEditor.masterDirector;
//            if (director == null)
//            {
//                UnityEngine.Debug.LogWarning("[AlSoTimeline] No inspected/master PlayableDirector. Open Timeline and lock it to a Director.");
//                return;
//            }

//            if (director.playableAsset is not TimelineAsset timeline)
//            {
//                UnityEngine.Debug.LogWarning("[AlSoTimeline] Director.playableAsset is not a TimelineAsset.");
//                return;
//            }

//            string timelinePath = AssetDatabase.GetAssetPath(timeline);
//            if (!string.IsNullOrEmpty(timelinePath) && !AssetDatabase.IsOpenForEdit(timeline))
//            {
//                UnityEngine.Debug.LogWarning($"[AlSoTimeline] TimelineAsset is not open for edit: {timelinePath}");
//                return;
//            }

//            // Убираем дубликаты трансформов
//            var actors = new List<Transform>(selection.Length);
//            var used = new HashSet<int>();

//            for (int i = 0; i < selection.Length; i++)
//            {
//                Transform t = selection[i];
//                if (t == null)
//                {
//                    continue;
//                }

//                int id = t.GetInstanceID();
//                if (used.Add(id))
//                {
//                    actors.Add(t);
//                }
//            }

//            if (actors.Count == 0)
//            {
//                UnityEngine.Debug.LogWarning("[AlSoTimeline] Selection contains no valid transforms.");
//                return;
//            }

//            Undo.IncrementCurrentGroup();
//            int undoGroup = Undo.GetCurrentGroup();
//            Undo.SetCurrentGroupName("Create AlSo Character Groups");

//            Undo.RegisterCompleteObjectUndo(timeline, "Create AlSo Character Groups");
//            Undo.RecordObject(director, "Bind AlSo Actor Tracks");

//            int created = 0;

//            for (int i = 0; i < actors.Count; i++)
//            {
//                Transform actor = actors[i];
//                if (actor == null)
//                {
//                    continue;
//                }

//                string groupName = MakeUniqueGroupName(timeline, actor.name);

//                GroupTrack group = timeline.CreateTrack<GroupTrack>(null, groupName);
//                group.name = groupName;

//                // Развернуть группу сразу
//                TrackExtensions.SetCollapsed(group, false);

//                LocomotionActorBindingTrack actorTrack = timeline.CreateTrack<LocomotionActorBindingTrack>(group, "Actor");
//                actorTrack.name = "Actor";

//                LocomotionRunToTrack moveTrack = timeline.CreateTrack<LocomotionRunToTrack>(group, "Move");
//                moveTrack.name = "Move";

//                LocomotionActionTrack actionTrack = timeline.CreateTrack<LocomotionActionTrack>(group, "Action");
//                actionTrack.name = "Action";

//                // Каждый Actor трек -> свой Transform
//                director.SetGenericBinding(actorTrack, actor);

//                created++;
//            }

//            EditorUtility.SetDirty(director);
//            EditorUtility.SetDirty(timeline);

//            ForceTimelineRefreshNowAndNextGuiLoop();

//            Undo.CollapseUndoOperations(undoGroup);

//            UnityEngine.Debug.Log($"[AlSoTimeline] Created {created} character group(s) in Director '{director.name}'.");
//        }

//        [MenuItem("GameObject/AlSo/Timeline/Create Character Groups (Actor+Move+Action)", true)]
//        private static bool ValidateCreate(MenuCommand command)
//        {
//            // Чтобы пункт был доступен в контексте, но не “дублировался” по логике —
//            // оставляем видимым всегда, если есть selection и есть Timeline.
//            if (Selection.transforms == null || Selection.transforms.Length == 0)
//            {
//                return false;
//            }

//            PlayableDirector director = TimelineEditor.inspectedDirector ?? TimelineEditor.masterDirector;
//            if (director == null)
//            {
//                return false;
//            }

//            return director.playableAsset is TimelineAsset;
//        }

//        private static void ForceTimelineRefreshNowAndNextGuiLoop()
//        {
//            const RefreshReason reason =
//                RefreshReason.ContentsAddedOrRemoved |
//                RefreshReason.WindowNeedsRedraw |
//                RefreshReason.SceneNeedsUpdate;

//            TimelineEditor.Refresh(reason);
//            InternalEditorUtility.RepaintAllViews();

//            EditorApplication.delayCall += () =>
//            {
//                TimelineEditor.Refresh(reason);
//                InternalEditorUtility.RepaintAllViews();
//            };
//        }

//        private static string MakeUniqueGroupName(TimelineAsset timeline, string baseName)
//        {
//            int suffix = 0;
//            string name = baseName;

//            while (GroupNameExists(timeline, name))
//            {
//                suffix++;
//                name = $"{baseName} ({suffix})";
//            }

//            return name;
//        }

//        private static bool GroupNameExists(TimelineAsset timeline, string groupName)
//        {
//            foreach (TrackAsset t in timeline.GetOutputTracks())
//            {
//                if (t is GroupTrack && string.Equals(t.name, groupName, StringComparison.Ordinal))
//                {
//                    return true;
//                }
//            }

//            return false;
//        }
//    }
//}
//#endif

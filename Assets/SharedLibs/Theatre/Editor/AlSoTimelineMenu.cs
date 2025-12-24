#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    public static class AlSoTimelineHierarchyCreateGroup
    {
        [MenuItem("GameObject/AlSo/Timeline/Create Character Group (Actor+Move+Action)", false, 49)]
        private static void Create(MenuCommand command)
        {
            Transform actor = Selection.activeTransform;
            if (actor == null)
            {
                UnityEngine.Debug.LogWarning("[AlSoTimeline] No active Transform selected.");
                return;
            }

            // Важно: при lock Timeline окна inspectedDirector остаётся стабильным.
            PlayableDirector director = TimelineEditor.inspectedDirector ?? TimelineEditor.masterDirector;
            if (director == null)
            {
                UnityEngine.Debug.LogWarning("[AlSoTimeline] No inspected/master PlayableDirector. Open Timeline and lock it to a Director.");
                return;
            }

            TimelineAsset timeline = director.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                UnityEngine.Debug.LogWarning("[AlSoTimeline] Director.playableAsset is not a TimelineAsset.");
                return;
            }

            string timelinePath = AssetDatabase.GetAssetPath(timeline);
            if (!string.IsNullOrEmpty(timelinePath) && !AssetDatabase.IsOpenForEdit(timeline))
            {
                UnityEngine.Debug.LogWarning($"[AlSoTimeline] TimelineAsset is not open for edit: {timelinePath}");
                return;
            }

            string groupName = MakeUniqueGroupName(timeline, actor.name);

            Undo.RegisterCompleteObjectUndo(timeline, "Create AlSo Character Group");

            // Group
            GroupTrack group = timeline.CreateTrack<GroupTrack>(null, groupName);
            group.name = groupName;

            // Actor binding track (Transform)
            LocomotionActorBindingTrack actorTrack = timeline.CreateTrack<LocomotionActorBindingTrack>(group, "Actor");
            actorTrack.name = "Actor";

            // Move / Action tracks
            LocomotionRunToTrack moveTrack = timeline.CreateTrack<LocomotionRunToTrack>(group, "Move");
            moveTrack.name = "Move";

            LocomotionActionTrack actionTrack = timeline.CreateTrack<LocomotionActionTrack>(group, "Action");
            actionTrack.name = "Action";

            // Bind Actor track to selected Transform
            Undo.RecordObject(director, "Bind AlSo Actor Track");
            director.SetGenericBinding(actorTrack, actor);

            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(timeline);

            ForceTimelineRefreshNowAndNextGuiLoop();

            UnityEngine.Debug.Log($"[AlSoTimeline] Created group '{groupName}' in Director '{director.name}', bound Actor='{actor.name}'.");
        }

        [MenuItem("GameObject/AlSo/Timeline/Create Character Group (Actor+Move+Action)", true)]
        private static bool ValidateCreate(MenuCommand command)
        {
            if (Selection.activeTransform == null)
            {
                return false;
            }

            PlayableDirector director = TimelineEditor.inspectedDirector ?? TimelineEditor.masterDirector;
            if (director == null)
            {
                return false;
            }

            return director.playableAsset is TimelineAsset;
        }

        private static void ForceTimelineRefreshNowAndNextGuiLoop()
        {
            // Ты добавляешь треки -> ContentsAddedOrRemoved.
            // Плюс перерисовка окна и иногда апдейт сцены.
            const RefreshReason reason =
                RefreshReason.ContentsAddedOrRemoved |
                RefreshReason.WindowNeedsRedraw |
                RefreshReason.SceneNeedsUpdate;

            // 1) Попросили Timeline обновиться (происходит на следующем GUI loop). :contentReference[oaicite:3]{index=3}
            TimelineEditor.Refresh(reason);

            // 2) Иногда окно не репейнтится само — форсим репейнт всех views. :contentReference[oaicite:4]{index=4}
            InternalEditorUtility.RepaintAllViews();

            // 3) На следующий GUI loop — повторяем (это обычно убирает необходимость “тыкать другой объект”).
            EditorApplication.delayCall += () =>
            {
                TimelineEditor.Refresh(reason);
                InternalEditorUtility.RepaintAllViews();
            };
        }

        private static string MakeUniqueGroupName(TimelineAsset timeline, string baseName)
        {
            if (timeline == null)
            {
                return baseName;
            }

            int suffix = 0;
            string name = baseName;

            while (GroupNameExists(timeline, name))
            {
                suffix++;
                name = $"{baseName} ({suffix})";
            }

            return name;
        }

        private static bool GroupNameExists(TimelineAsset timeline, string groupName)
        {
            foreach (TrackAsset t in timeline.GetOutputTracks())
            {
                if (t is GroupTrack && string.Equals(t.name, groupName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif

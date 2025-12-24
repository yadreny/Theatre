#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AlSo
{
    internal static class AlSoTimelineScaffold
    {
        public static bool TryCreateCharacterGroupFromSelection(string groupNameOverride = null)
        {
            var timeline = TimelineEditor.inspectedAsset as TimelineAsset;
            if (timeline == null)
            {
                // иногда TimelineEditor.inspectedAsset = null, но masterAsset есть
                timeline = TimelineEditor.masterAsset as TimelineAsset;
            }

            var director = TimelineEditor.inspectedDirector;
            if (director == null)
            {
                director = TimelineEditor.masterDirector;
            }

            if (timeline == null)
            {
                UnityEngine.Debug.LogWarning("[AlSoTimeline] No TimelineAsset is opened/inspected.");
                return false;
            }

            // Проверка на read-only (частая причина “ничего не создаёт”)
            string path = AssetDatabase.GetAssetPath(timeline);
            if (!string.IsNullOrEmpty(path))
            {
                if (!AssetDatabase.IsOpenForEdit(timeline))
                {
                    UnityEngine.Debug.LogWarning($"[AlSoTimeline] TimelineAsset is not open for edit: {path}");
                    return false;
                }
            }

            Transform actor = Selection.activeTransform;
            string groupName = !string.IsNullOrEmpty(groupNameOverride)
                ? groupNameOverride
                : (actor != null ? actor.name : "Character");

            //TimelineUndo.PushUndo(timeline, "Create AlSo Character Group");

            // 1) Group
            GroupTrack group = timeline.CreateTrack<GroupTrack>(null, groupName);
            group.name = groupName;

            // 2) Actor binding track
            var actorTrack = timeline.CreateTrack<LocomotionActorBindingTrack>(group, "Actor");
            actorTrack.name = "Actor";

            // 3) Move / Action
            var moveTrack = timeline.CreateTrack<LocomotionRunToTrack>(group, "Move");
            moveTrack.name = "Move";

            var actionTrack = timeline.CreateTrack<LocomotionActionTrack>(group, "Action");
            actionTrack.name = "Action";

            // 4) Bind actor track to selected transform
            if (director != null && actor != null)
            {
                Undo.RecordObject(director, "Bind AlSo Actor Track");
                director.SetGenericBinding(actorTrack, actor);
                EditorUtility.SetDirty(director);
            }

            EditorUtility.SetDirty(timeline);

            // Обновляем UI Timeline
            TimelineEditor.Refresh(RefreshReason.ContentsModified | RefreshReason.WindowNeedsRedraw);

            UnityEngine.Debug.Log($"[AlSoTimeline] Created group '{groupName}' (Actor+Move+Action).");
            return true;
        }
    }

    /// <summary>
    /// Пункт в контекстном меню Timeline (ПКМ).
    /// </summary>
    [MenuEntry("AlSo/Create Character Group (Actor+Move+Action)", MenuPriority.defaultPriority)]
    public class CreateAlSoCharacterGroupTimelineAction : TimelineAction
    {
        public override ActionValidity Validate(ActionContext context)
        {
            // Если NotApplicable — пункта в меню НЕ будет.
            var asset = TimelineEditor.inspectedAsset ?? TimelineEditor.masterAsset;
            return asset != null ? ActionValidity.Valid : ActionValidity.NotApplicable;
        }

        public override bool Execute(ActionContext context)
        {
            return AlSoTimelineScaffold.TryCreateCharacterGroupFromSelection();
        }
    }

    public static class CreateAlSoCharacterGroupHierarchyMenu
    {
        /// <summary>
        /// Пункт в контекстном меню Hierarchy (ПКМ по GameObject).
        /// </summary>
        [MenuItem("GameObject/AlSo/Timeline/Create Character Group (Actor+Move+Action)", false, 49)]
        private static void CreateFromHierarchy(MenuCommand command)
        {
            // Selection.activeTransform уже будет правильным (клик по объекту в иерархии)
            AlSoTimelineScaffold.TryCreateCharacterGroupFromSelection();
        }

        [MenuItem("GameObject/AlSo/Timeline/Create Character Group (Actor+Move+Action)", true)]
        private static bool ValidateCreateFromHierarchy(MenuCommand command)
        {
            var asset = TimelineEditor.inspectedAsset ?? TimelineEditor.masterAsset;
            return asset != null && Selection.activeTransform != null;
        }
    }
}
#endif

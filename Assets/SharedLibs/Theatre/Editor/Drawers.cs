//#if UNITY_EDITOR
//using UnityEditor;
//using UnityEditor.Timeline;
//using UnityEngine;
//using UnityEngine.Timeline;

//namespace AlSo
//{
//    [CustomTimelineEditor(typeof(LocomotionRunToTrack))]
//    public class LocomotionRunToTrackEditor : TrackEditor
//    {
//        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
//        {
//            var o = base.GetTrackOptions(track, binding);
//            o.icon = LocomotionTimelineIconUtil.GetMoveIcon();
//            return o;
//        }
//    }

//    [CustomTimelineEditor(typeof(LocomotionActionTrack))]
//    public class LocomotionActionTrackEditor : TrackEditor
//    {
//        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
//        {
//            var o = base.GetTrackOptions(track, binding);
//            o.icon = LocomotionTimelineIconUtil.GetActionIcon();
//            return o;
//        }
//    }

//    internal static class LocomotionTimelineIconUtil
//    {
//        private const string MoveIconPath = "Assets/Editor/AlSo/Icons/locomotion_move.png";
//        private const string ActionIconPath = "Assets/Editor/AlSo/Icons/locomotion_action.png";

//        private static Texture2D _move;
//        private static Texture2D _action;

//        public static Texture2D GetMoveIcon()
//        {
//            if (_move != null) return _move;

//            _move = AssetDatabase.LoadAssetAtPath<Texture2D>(MoveIconPath);
//            if (_move != null) return _move;

//            // Fallback: встроенная иконка (если png не положили)
//            var c = EditorGUIUtility.IconContent("d_MoveTool");
//            _move = c != null ? c.image as Texture2D : null;
//            return _move;
//        }

//        public static Texture2D GetActionIcon()
//        {
//            if (_action != null) return _action;

//            _action = AssetDatabase.LoadAssetAtPath<Texture2D>(ActionIconPath);
//            if (_action != null) return _action;

//            // Fallback: встроенная иконка
//            var c = EditorGUIUtility.IconContent("d_PlayButton");
//            _action = c != null ? c.image as Texture2D : null;
//            return _action;
//        }
//    }
//}
//#endif

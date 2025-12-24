//#if UNITY_EDITOR
//using UnityEditor.Timeline;
//using UnityEditor.Timeline.Actions;
//using UnityEngine;
//using UnityEngine.Playables;
//using UnityEngine.Timeline;


//namespace AlSo
//{


//    [MenuEntry("AlSo/Create Character Group (Move + Action)", MenuPriority.defaultPriority)]
//    public class CreateAlSoCharacterGroupAction : TimelineAction
//    {
//        public override ActionValidity Validate(ActionContext context)
//        {
//            // Если вернуть NotApplicable — пункта в меню не будет вообще.
//            // Поэтому делаем Valid, когда реально открыт TimelineAsset.
//            return TimelineEditor.timelineAsset != null
//                ? ActionValidity.Valid
//                : ActionValidity.NotApplicable;
//        }

//        public override bool Execute(ActionContext context)
//        {
//            TimelineAsset timeline = TimelineEditor.timelineAsset;
//            if (timeline == null)
//                return false;

//            PlayableDirector director = TimelineEditor.inspectedDirector;

//            // Дальше твоя логика создания группы/треков...
//            UnityEngine.Debug.Log("[AlSo] Create Character Group action executed.");
//            return true;
//        }
//    }
//}

//#endif

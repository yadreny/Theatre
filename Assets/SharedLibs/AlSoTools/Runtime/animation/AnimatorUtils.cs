//using System.Collections.Generic;
//using UnityEditor.Animations;
//using UnityEngine;

//namespace AlSo
//{
//    public class AnimatorUtils
//    {


//        public static float GetCurrentClipNormalizedTimeByStateName(Animator animator, string stateName)
//        {
//            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
//            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);

//            if (stateInfo.IsName(stateName))
//            {
//                AnimationClip clip = GetCurrentClipByStateName(animator, stateName);
//                if (clip != null) return stateInfo.normalizedTime;
//            }

//            if (nextState.IsName(stateName))
//            {
//                AnimationClip clip = GetCurrentClipByStateName(animator, stateName);
//                if (clip != null) return nextState.normalizedTime;
//            }

//            Debug.LogWarning($"State '{stateName}' is not currently playing.");
//            return -1f;
//        }

//        private static AnimationClip GetCurrentClipByStateName(Animator animator, string stateName)
//        {
//            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
//            if (controller != null)
//            {
//                foreach (var layer in controller.layers)
//                {
//                    foreach (ChildAnimatorState state in layer.stateMachine.states)
//                    {
//                        if (state.state.name == stateName)
//                        {
//                            return state.state.motion as AnimationClip;
//                        }
//                    }
//                }
//            }
//            return null;
//        }


//    }
//}

////public static string[] GetAllAnimationStates(Animator animator)
////{
////    List<string> stateNames = new List<string>();

////    RuntimeAnimatorController controller = animator.runtimeAnimatorController;

////    if (controller is AnimatorController animatorController)
////    {
////        foreach (AnimatorControllerLayer layer in animatorController.layers)
////        {
////            AnimatorStateMachine stateMachine = layer.stateMachine;

////            foreach (ChildAnimatorState state in stateMachine.states)
////            {
////                stateNames.Add(state.state.name);
////            }
////        }
////    }

////    return stateNames.ToArray();
////}


////public static AnimationClip GetAnimationClipByStateName(Animator animator, string stateName, int layer = 0)
////{
////    AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
////    if (controller == null)
////    {
////        Debug.LogError("Animator does not have a valid AnimatorController.");
////        return null;
////    }

////    AnimatorStateMachine rootStateMachine = controller.layers[layer].stateMachine;
////    foreach (ChildAnimatorState state in rootStateMachine.states)
////    {
////        if (state.state.name == stateName)
////        {
////            return state.state.motion as AnimationClip;
////        }
////    }

////    Debug.LogWarning($"State '{stateName}' not found in the AnimatorController.");
////    return null;
////}
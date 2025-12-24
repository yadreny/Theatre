using UnityEngine;
using UnityEngine.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace AlSo
{
    public static class AnimatorTools
    {
        private static string BlendTreeSpeedVariablePostfix => "_Speed";
        private static string BlendTreeIsLinearPostfix => "_IsLinear";

        public static string BlendTreeSpeedVariableName(this string blendTreeName) => $"{blendTreeName}{BlendTreeSpeedVariablePostfix}";
        public static string BlendTreeIsLinearVariableName(this string blendTreeName) => $"{blendTreeName}{BlendTreeIsLinearPostfix}";


        public static AnimationClip GetClipByName(this Animator animator, string name)
        {
#if UNITY_EDITOR
            AnimatorController ac = animator.runtimeAnimatorController as AnimatorController;
            try
            {
                return ac.animationClips.Single(x => x.name == name);
            }
            catch
            {
                Debug.LogError($"cant find clip {animator.runtimeAnimatorController.name}.{name}");
            }
#endif
            return null ;
        }

        public static AnimatorOverrideController GetOverrided(this RuntimeAnimatorController current, AnimationClip newClip, string oldClipName)
        {
            AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController(current);
            animatorOverrideController[oldClipName] = newClip;
            return animatorOverrideController;
        }

        public static float GetBlendTreeSpeed(this Animator self, string blendTreeName)
        {
#if UNITY_EDITOR
            BlendTree tree = self.GetBlendTree(blendTreeName);
            return tree.GetSpeedModule();
#endif
            return self.GetFloat(blendTreeName.BlendTreeSpeedVariableName());
        }

        public static bool IsBlendTreeLinear(this Animator self, string blendTreeName)
        {
#if UNITY_EDITOR
            BlendTree tree = self.GetBlendTree(blendTreeName);
            return tree.blendType == BlendTreeType.Simple1D;
#endif
            return self.GetBool(blendTreeName.BlendTreeIsLinearVariableName());
        }
        

#if UNITY_EDITOR

        private static float GetSpeedModule(this BlendTree tree)
        {
            if (tree.blendType == BlendTreeType.Simple1D) return tree.maxThreshold;

            foreach (ChildMotion child in tree.children)
            {
                if (child.position == Vector2.zero) continue;
                float maxSpeed = child.position.magnitude; 
                return maxSpeed;
            }
            throw new Exception($"cant recognize speed module {tree.name}");
        }

        public static BlendTree GetBlendTree(this Animator self, string name, int layer = 0)
        {
            AnimatorController ac = self.runtimeAnimatorController as AnimatorController;
            ChildAnimatorState[] states = ac.layers[layer].stateMachine.states;

            for (int i = 0; i < states.Length; i++)
            {
                ChildAnimatorState state = states[i];
                if (state.state.name == name)
                {
                    BlendTree tree = state.state.motion as BlendTree;
                    return tree;
                }
            }
            throw new Exception($"cant find blendTree {name} on layer {layer}");
        }
#endif


        //public static AnimationClip GetAnimationClipByName(this RuntimeAnimatorController self, string name, int layer = 0)
        //{
        //    AnimatorController ac = self as AnimatorController;
        //    ChildAnimatorState[] states = ac.layers[layer].stateMachine.states;

        //    for (int i = 0; i < states.Length; i++)
        //    {
        //        ChildAnimatorState state = states[i];
        //        if (state.state.motion.name == name)
        //        {
        //            AnimationClip clip = state.state.motion as AnimationClip;
        //            return clip;
        //        }
        //    }
        //    throw new Exception($"cant find clip {name} on layer {layer}");
        //}


        //public static AnimationClip GetAnimationClipByStateName(this RuntimeAnimatorController self, string name, int layer=0)
        //{
        //    AnimatorController ac = self as AnimatorController;
        //    ChildAnimatorState[] states = ac.layers[layer].stateMachine.states;

        //    for (int i = 0; i < states.Length; i++)
        //    {
        //        ChildAnimatorState state = states[i];
        //        if (state.state.name == name)
        //        {
        //            AnimationClip clip = state.state.motion as AnimationClip;
        //            return clip;
        //        }
        //    }
        //    throw new Exception($"cant find clip {name} on layer {layer}");
        //}
    }
}
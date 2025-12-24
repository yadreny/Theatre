using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlSo
{
    public static class AnimatorExtension
    {
        public static AnimationState getByName(this Animation anim, string name)
        {
            foreach (AnimationState state in anim)
            {
                if (state.name == name)
                {
                    return state;
                }
            }
            throw new Exception(anim.gameObject.name + " doesnt contains " + name + " animation state");
        }

        public static List<AnimationState> getAnimations(this GameObject gobj)
        {
            List<AnimationState> res = new List<AnimationState>();
            Animation animation = gobj.GetComponent<Animation>();
            foreach (AnimationState state in animation)
            {
                res.Add(state);
            }
            return res;
        }

        public static void SetParam(this Animator animator, string name, int i)
        {
            //Debug.LogError(animator.gameObject.name + ") "+ name + " = " + i);
            animator.SetInteger(name, i);
        }

        public static void SetParam(this Animator animator, string name, float f)
        {
            //Debug.LogError(animator.gameObject.name + ") " + name + " = " + f);
            animator.SetFloat(name, f);
        }


        public static void SetParam(this Animator animator, string name, bool b)
        {
            //Debug.LogError(animator.gameObject.name + ") " + name + " = " + b);
            animator.SetBool(name, b);
        }

        public static void SetParam(this Animator animator, string name)
        {
            //Debug.LogError(animator.gameObject.name + ") " + name );
            animator.SetTrigger(name);
        }

        public static bool HasParam(this Animator animator, string paramName) => animator.parameters.Any(x => x.name == paramName);

    }
}
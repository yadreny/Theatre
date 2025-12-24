using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlSo
{
    public class AnimatorHelper : MonoBehaviour
    {
        private Animator _target;
        protected Animator Target
        {
            get
            {
                _target = _target == null ? gameObject.GetComponent<Animator>() : _target;
                return _target;
            }
        }

        public List<string> Log { get; private set; } = new List<string>();

        void Update()
        {
            string currentAnimation = GetCurrentAnimation();
            if (Log.Count == 0 || Log.Last() != currentAnimation)
            {
                Log.Add(currentAnimation);
            }
        }

        private string GetCurrentAnimation()
        {
            AnimatorClipInfo[] infos = Target.GetCurrentAnimatorClipInfo(0);
            string result = "";
            foreach (AnimatorClipInfo aci in infos)
            {
                result = result + " " + aci.clip.name;
            }
            return result;
        }
    }


    public abstract class AbsAnimParam
    {
        public string name;
        public abstract void Execute(Animator anim);
    }

    public class AnimIntParam : AbsAnimParam
    {
        public int param;

        public override void Execute(Animator anim)
        {
            anim.SetParam(name, param);
        }
    }

    public class AnimBoolParam : AbsAnimParam
    {
        public bool param;

        public override void Execute(Animator anim)
        {
            anim.SetParam(name, param);
        }
    }

    public class AnimFloatParam : AbsAnimParam
    {
        public float param;

        public override void Execute(Animator anim)
        {
            anim.SetParam(name, param);
        }
    }

    public class AnimTriggerParam : AbsAnimParam
    {
        public float param;

        public override void Execute(Animator anim)
        {
            anim.SetParam(name);
        }
    }

    public class AnimExec : AbsAnimParam
    {
        public override void Execute(Animator anim)
        {
            throw new NotImplementedException();
        }
    }

}
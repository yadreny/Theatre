#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlSo
{

    public class AnimatorLogger : MonoBehaviour
    {
        Animator _target;
        Animator target
        {
            get
            {
                _target = _target == null ? gameObject.GetComponent<Animator>() : _target;
                return _target;
            }
        }

        public List<string> log = new List<string>();
        string prevAnimation;

        void Update()
        {
            string currentAnimation = getCurrentAnimation();
            if (log.Count == 0 || log.Last() != currentAnimation)
            {
                log.Add(currentAnimation);
            }
        }

        string getCurrentAnimation()
        {
            AnimatorClipInfo[] infos = target.GetCurrentAnimatorClipInfo(0);
            string result = "";
            foreach (AnimatorClipInfo aci in infos)
            {
                result = result + " " + aci.clip.name;
            }
            return result;
        }
    }
     

}
#endif
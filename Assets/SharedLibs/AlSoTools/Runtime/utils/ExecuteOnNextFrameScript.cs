using System;
using UnityEngine;

namespace AlSo
{
    public static class ExecuteOnNextFrame
    {
        static GameObject _executor = new GameObject();

        public static void Do(Action handler)
        {
            ExecuteOnNextFrameScript scr = _executor.AddComponent<ExecuteOnNextFrameScript>();
            scr.Handler = handler;
        }

        public class ExecuteOnNextFrameScript : MonoBehaviour
        {
            public Action Handler { get; set; }

            private bool firstTime = true;

            private void Update()
            {
                if (firstTime)
                {
                    firstTime = false;
                    return;
                }

                Action toExecute = Handler;
                Handler = null;
                GameObject.Destroy(this);
                toExecute?.Invoke();
            }
        }
    }

}
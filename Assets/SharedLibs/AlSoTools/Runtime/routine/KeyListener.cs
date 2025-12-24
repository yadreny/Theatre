using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlSo
{

    public class KeyListener : MonoBehaviour
    {
        static KeyListener _instance;
        public static KeyListener Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject g = new GameObject("key listener");
                    _instance = g.AddComponent<KeyListener>();
                }
                return _instance;
            }
        }

        readonly Dictionary<KeyCode, List<Action>> handlersDown = new Dictionary<KeyCode, List<Action>>();
        readonly Dictionary<KeyCode, List<Action>> toRemoveDown = new Dictionary<KeyCode, List<Action>>();

        readonly Dictionary<KeyCode, List<Action>> handlersUp = new Dictionary<KeyCode, List<Action>>();
        readonly Dictionary<KeyCode, List<Action>> toRemoveUp = new Dictionary<KeyCode, List<Action>>();


        public void ListentToDown(KeyCode code, Action handler)
        {
            if (!handlersDown.ContainsKey(code)) handlersDown[code] = new List<Action>();
            handlersDown[code].Add(handler);
        }

        public void ListentToUp(KeyCode code, Action handler)
        {
            if (!handlersUp.ContainsKey(code)) handlersUp[code] = new List<Action>();
            handlersUp[code].Add(handler);
        }

        public void StopListentToUp(KeyCode code, Action handler)
        {
            if (!toRemoveUp.ContainsKey(code)) toRemoveUp[code] = new List<Action>();
            toRemoveUp[code].Add(handler);
        }

        public void StopListentToDown(KeyCode code, Action handler)
        {
            if (!toRemoveDown.ContainsKey(code)) toRemoveDown[code] = new List<Action>();
            toRemoveDown[code].Add(handler);
        }


        private void Update()
        {
            foreach (KeyCode code in handlersDown.Keys)
            {
                if (Input.GetKeyDown(code))
                {
                    foreach (Action handler in handlersDown[code]) { handler(); }
                }
            }

            foreach (KeyCode code in toRemoveDown.Keys)
            {
                foreach (Action handler in toRemoveDown[code])
                {
                    handlersDown[code].Remove(handler);
                }
            }

            foreach (KeyCode code in handlersUp.Keys)
            {
                if (Input.GetKeyUp(code))
                {
                    foreach (Action handler in handlersUp[code]) { handler(); }
                }
            }

            foreach (KeyCode code in toRemoveUp.Keys)
            {
                foreach (Action handler in toRemoveUp[code])
                {
                    handlersUp[code].Remove(handler);
                }
            }

        }

    }
}
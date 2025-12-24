using UnityEngine;

namespace AlSo
{
    public class DontDestory : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}

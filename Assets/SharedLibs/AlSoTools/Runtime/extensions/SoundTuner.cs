using UnityEngine;
using UnityEngine.Audio;

namespace AlSo
{
    public class SoundTuner : MonoBehaviour
    {
        private static string MasterChannel { get; } = "Master";
        private static string MasterVolume { get; } = "MasterVolume";
        public static SoundTuner Instance { get; private set; }

        [SerializeField]
        protected AudioMixer mixer;

        public AudioMixer Mixer => mixer;

        [SerializeField]
        protected float volume = 0.5f;

        private void Start()
        {
            if (Instance != null)
            {
                GameObject.Destroy(this);
                return;
                //throw new System.Exception("SoundTuner Instance always setuped");
            } 
            Instance = this;

            SetVolume(volume);
        }

        public float GetVolume()=> volume;
        public void SetVolume(float volume) => this.volume = volume;

        private void Update()
        {
            float db = volume <= 0.0001f ? -80 : Mathf.Log10(volume) * 20;
            //if (flo)
            Mixer.SetFloat(MasterVolume, db);
        }

        private AudioMixerGroup _masterGroup;
        public AudioMixerGroup MasterGroup
        {
            get
            { 
                if (_masterGroup == null) _masterGroup = Mixer.FindMatchingGroups(MasterChannel)[0];
                return _masterGroup;
            }
        }
        
    }
}
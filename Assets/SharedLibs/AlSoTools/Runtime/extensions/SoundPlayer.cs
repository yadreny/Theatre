using UnityEngine;
using static UnityEngine.Networking.UnityWebRequest;
using UnityEngine.SceneManagement;

namespace AlSo
{
    public static class SoundPlayer
    {
        public static void Play(AudioClip clip, float volume, Vector3 position)
        {
            if (clip == null) return;

            GameObject player = new GameObject("sound player");
            SceneManager.MoveGameObjectToScene(player, SceneUtils.RootScene);
            player.transform.position = position;

            bool isValidPosition = player.transform.position.x.IsValidFloat() && player.transform.position.y.IsValidFloat() && player.transform.position.z.IsValidFloat();

            if (!isValidPosition) return;

            AudioSource audioSource = player.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = SoundTuner.Instance.MasterGroup;

            try
            {
                audioSource.volume = volume.Clamp();
            }
            catch 
            {
                Debug.LogError("Volume problem");
                Debug.LogError("Volume problem");
                Debug.LogError("Volume problem");
                Debug.LogError("Volume problem");
            }

            try
            {
                audioSource.PlayOneShot(clip);
            }
            catch 
            {
                Debug.LogError("Weapon Clash problem");
                Debug.LogError("Weapon Clash problem");
                Debug.LogError("Weapon Clash problem");
                Debug.LogError("Weapon Clash problem");
            }
            
        }
    }

}
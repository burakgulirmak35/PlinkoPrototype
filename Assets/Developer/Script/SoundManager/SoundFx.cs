
using UnityEngine;

public class SoundFx : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    public void PlayDirect(AudioClip audioClip, float volume)
    {
        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();
    }
}

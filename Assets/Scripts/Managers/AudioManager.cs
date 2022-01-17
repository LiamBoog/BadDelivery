using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    #region Private Members

    [SerializeField] private GameObject newAudioSource = null;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates new instance of newAudioSource and returns its AudioSource component. newAudioSource object is destroyed after playing its sound.
    /// </summary>
    /// /// <param name="audioClip">The audio clip to play once</param>
    /// <returns></returns>
    public void CreateSingleUseAudioSource(AudioClip audioClip)
    {
        GameObject audioSourceObject = Instantiate(newAudioSource);
        AudioSource audioSource = audioSourceObject.GetComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.Play();
        StartCoroutine(DestroyAudioSource(audioSourceObject));
    }

    #endregion

    #region private Methods

    /// <summary>
    /// Destroys audioSource when it is done playing its sound
    /// </summary>
    /// <param name="audioSource"></param>
    /// <returns></returns>
    private IEnumerator DestroyAudioSource(GameObject audioSourceObject)
    {
        while (audioSourceObject.GetComponent<AudioSource>().isPlaying)
        {
            yield return null;
        }

        Destroy(audioSourceObject);
    }

    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioBlender : MonoBehaviour
{
    private float lastIntensity = -1f; // track previous intensity value.
    private bool isProcessing = true;
    public bool playOnAwake = false;

    [Range(0f, 1f)]
    public float intensity = 0f;

    public AudioEvent low;
    public AnimationCurve lowVolumeCurve; 

    public AudioEvent medium;
    public AnimationCurve medVolumeCurve;
   
    public AudioEvent high;
    public AnimationCurve highVolumeCurve;
    
    private void Start()
    {
        if (playOnAwake)
        {
            PlayAllAudioEvents();
        }
    }

    private void Update()
    {
        if (Mathf.Approximately(intensity, lastIntensity))
        {
            return; // No change in intensity, skip processing.
        }

        // check if intensity is zero, if so, stop all audio processing.
        if (Mathf.Approximately(intensity, 0f))
        {
            if (isProcessing)
            { 
                UpdateVolumes();
                isProcessing = false;
            }
            lastIntensity = intensity;
            return;
        }

        isProcessing = true;

        UpdateVolumes();
        lastIntensity = intensity;

    }

    private void PlayAllAudioEvents()
    {
        if (low != null)
        {
            low.PlayAudio();
        }

        if (medium != null)
        {
            medium.PlayAudio();
        }

        if (high != null)
        {
            high.PlayAudio();
        }
    }

    private void UpdateVolumes()
    {
        // Evaluate each curve based on current intensity value
        if (low != null)
        {
            float lowVolume = lowVolumeCurve.Evaluate(intensity);
            low.SetVolume(lowVolume);
            low.SetPlaybackState(lowVolume > 0f);
        }

        if (medium != null)
        {
            float medVolume = medVolumeCurve.Evaluate(intensity);
            medium.SetVolume(medVolume);
            medium.SetPlaybackState(medVolume > 0f);
        }

        if (high != null)
        {
            float highVolume = highVolumeCurve.Evaluate(intensity);
            high.SetVolume(highVolume);
            high.SetPlaybackState(highVolume > 0f);
        }
    }

    // Public method to control intensity from other scripts
    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
    }

    public void PlayAudioBlender()
    {
        PlayAllAudioEvents();
    }
}
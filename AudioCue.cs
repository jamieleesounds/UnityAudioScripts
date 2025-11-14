using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio Cue")]
public class AudioCue : ScriptableObject
{
    [System.NonSerialized] private int _lastPlayedIndex = -1;
    [System.NonSerialized] private int _sequenceIndex = 0;
    [SerializeField] public AudioClip[] soundList;

    // Play Types.
    public enum PlayType
    {
        Single, Sequence, Random
    }
    public PlayType playType = PlayType.Single;

    // Audio Source setting overrides.
    [Range(0f, 256f)]
    public int priority = 128;
    public bool loop;
    public bool randomizeStartPoint;

    public AudioMixerGroup audioMixerGroup;

    // Pitch and Volume randomization settings.
    [Header("Pitch Randomization")]
    public float pitchMin = 1f;
    public float pitchMax = 1f;
    [Header("Volume Randomization")]
    public float volumeMin = 1f;
    public float volumeMax = 1f;

    [Header("Spatial Settings")]
    [Range(0f, 1f)]
    [Tooltip("2D (0) to 3D (1)")]
    public float spatialBlend = 0f; // 2D by default.
    [Range(-1f, 1f)]
    public float pan = 0f;
    [Range(0f, 5f)]
    public float dopplerLevel = 0f;
    public AnimationCurve volumeRolloff;
    public float maxDistance = 50f;

    public float reverbZoneMix = 0f;

    
    public AudioSource PlayAudio(Vector3 position)
    {
        if (soundList == null || soundList.Length == 0)
        {
            return null;
        }

        // Create temporary GameObject at the specified position
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;

        // Add and configure AudioSource
        AudioSource source = tempGO.AddComponent<AudioSource>();
        ConfigureAudioSource(source);

        // Get the clip based on play type
        AudioClip clipToPlay = GetClipBasedOnPlayType();

        if (clipToPlay == null)
        {
            Object.Destroy(tempGO);
            return null;
        }

        source.clip = clipToPlay;


        // Randomize start point if enabled
        if (randomizeStartPoint && source.clip != null)
        {
            RandomizeStartPoint(source);
        }

        if (playType != PlayType.Single)
        {
            RandomizePitchVolume(source);
        }
        // Play the sound
        source.Play();

        // Destroy temporary GameObject after clip finishes (unless looping)
        if (!loop && source.clip != null)
        {
            float destroyTime = source.clip.length - source.time;
            Object.Destroy(tempGO, destroyTime + 0.1f);
        }

        return source;
    }

    public void ConfigureAudioSource(AudioSource audioSource)
    {
        if (audioSource == null) return;

        if (audioMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = audioMixerGroup;
        }

        audioSource.priority = priority;
        audioSource.dopplerLevel = dopplerLevel;
        audioSource.maxDistance = maxDistance;
        audioSource.loop = loop;
        audioSource.spatialBlend = spatialBlend;
        audioSource.panStereo = pan;
        audioSource.reverbZoneMix = reverbZoneMix;

        if (volumeRolloff != null && volumeRolloff.keys.Length > 0)
        {
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, volumeRolloff);
        }
        else
        {
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

    private AudioClip GetClipBasedOnPlayType()
    {
        if (playType == PlayType.Single)
        {
            return soundList[0];
        }
        else if (playType == PlayType.Sequence)
        {
            AudioClip clip = soundList[_sequenceIndex];
            _sequenceIndex = (_sequenceIndex + 1) % soundList.Length;
            return clip;
        }
        else // Random
        {
            int index;
            int attempts = 0;
            int maxAttempts = soundList.Length * 2;

            do
            {
                index = Random.Range(0, soundList.Length);
                attempts++;

                if (attempts >= maxAttempts) break;
            }
            while (soundList.Length > 1 && index == _lastPlayedIndex);

            _lastPlayedIndex = index;
            return soundList[index];
        }
    }

    public void SetVolume(AudioSource audioSource, float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void SetPlaybackState(AudioSource audioSource, bool shouldPlay)
    {
        if (audioSource == null) return;

        if (shouldPlay && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
        else if (!shouldPlay && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    private void RandomizePitchVolume(AudioSource audioSource)
    {
        if (audioSource == null) return;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = Random.Range(volumeMin, volumeMax);
    }

    private void RandomizeStartPoint(AudioSource audioSource)
    {
        if (audioSource == null || audioSource.clip == null) return;

        audioSource.time = Random.Range(0, audioSource.clip.length);
    }
}
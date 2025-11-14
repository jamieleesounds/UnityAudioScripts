using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;

public class AudioEvent : MonoBehaviour
{
    private int _lastPlayedIndex = -1;
    private int _sequenceIndex = 0;
    private AudioSource audioSource;
    private bool _isLowPassFilterActive = false;
    private Coroutine _currentFilterCoroutine = null;

    [Tooltip("Add audio clips here.")]
    [SerializeField] public AudioClip[] soundList;


    // Play Types.
    public enum PlayType
    {
        Single, Sequence, Random
    }

    [Tooltip("Choose play type. *Single: plays the first clip in the list. No random pitch and volume. *Sequence: plays clips in order. *Random: plays random clips without immediate repetition.")]
    public PlayType playType = PlayType.Single;
    [Tooltip("Check to loop the audio clip. Only available for single play type.")]
    public bool loop;
    [Tooltip("Use this to randomize start point of the audio clip.")]
    public bool randomizeStartPoint;

    // Audio Source setting overrides.
    public float pitch = 1;
    public float volume = 1;
    [Tooltip("Audio source priority. 0 is highest priority, 256 is lowest.")]
    [Range(0f, 256f)]
    public int priority = 128;

    [Tooltip("Choose the audio mixer group to route the audio through.")]
    public AudioMixerGroup audioMixerGroup;

    // Pitch and Volume randomization settings.
    [Header("Pitch Randomization")]
    [Tooltip("Pitch randomization minimum value. Choose from 0 to 3.")]
    public float pitchMin = 1f;
    [Tooltip("Pitch randomization maximum value. Choose from 0 to 3.")]
    public float pitchMax = 1f;

    [Header("Volume Randomization")]
    [Tooltip("Pitch randomization minimum value. Choose from 0 to 1.")]
    public float volumeMin = 1f;
    [Tooltip("Pitch randomization maximum value. Choose from 0 to 1.")]
    public float volumeMax = 1f;


    [Header("Spatial Settings")]
    [Tooltip("2D 0 to 3D 1.")]
    [Range(0f, 1f)]
    public float spatialBlend = 0f; // 2D by default. 
    [Range(-1f, 1f)]
    [Tooltip("Left -1 to Right 1. 0 is center.")]
    public float pan = 0f;
    [Range(0f, 5f)]
    [Tooltip("Doppler effect warps the pitch depending on distance. Think a race car passing by!")]
    public float dopplerLevel = 0f;
    [Tooltip("Custom volume rolloff curve. If none provided, defaults to logarithmic rolloff.")]
    public AnimationCurve volumeRolloff;
    [Tooltip("Volume rolloff only applies within this distance.")]
    public float maxDistance = 50f;

    [Header("Effects")]
    [Tooltip("Target frequency for the low pass filter effect.")]
    public float lowPassFilterTargetFrequency = 1000f;
    [Tooltip("How long the low pass filter will take to be in effect.")]
    public float lowPassFilterTransitionTime = 0f;
    [Tooltip("Transition time back to no filters.")]
    public float filterResetTransitionTime = 0f;
    [Tooltip("Fade in time for the audio clip.")]
    public float fadeInTime;
    [Tooltip("Reverb zone mix level. 0 is dry, 1 is wet.")]
    [Range(0f, 1f)]
    public float reverbZoneMix = 0f;


    // Test the audio files in the inspector.
    [Header("Test")]
    [Tooltip("Click to test the sound.")]
    public bool playSound;
    [Tooltip("Click to activate Low Pass Filter. Uncheck to reset filter.")]
    public bool lowPassFilter;
    [Tooltip("Click to activate Fade In effect before playback. Set fade in time in the effects tab.")]
    public bool fadeIn;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (volumeRolloff != null && volumeRolloff.keys.Length > 0)
        {
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, volumeRolloff);
        }
        else
        {
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // default to logarithmic if no curve provided.
        }

        audioSource.outputAudioMixerGroup = audioMixerGroup;
        audioSource.priority = (int)priority;
        audioSource.dopplerLevel = dopplerLevel;
        audioSource.maxDistance = maxDistance;
        audioSource.loop = loop;
        audioSource.reverbZoneMix = reverbZoneMix;
        audioSource.spatialBlend = spatialBlend;
        audioSource.panStereo = pan;

    }

    // testing methods.
    private void Update()
    {
        if (playSound)
        {
            PlayAudio();

            playSound = false;
        }

        // Low Pass Filter toggle handling.
        if (lowPassFilter && !_isLowPassFilterActive)
        {
            // stop any ongoing filter coroutine.
            if (_currentFilterCoroutine != null)
            {
                StopCoroutine(_currentFilterCoroutine);
            }

            // start the filter transition coroutine.
            _currentFilterCoroutine = StartCoroutine(LowPassFilterActivate());
            _isLowPassFilterActive = true;
        }
        else if (!lowPassFilter && _isLowPassFilterActive)
        {
            // stop any ongoing filter coroutine.
            if (_currentFilterCoroutine != null)
            {
                StopCoroutine(_currentFilterCoroutine);
            }

            // reset the filter.
            _currentFilterCoroutine = StartCoroutine(FilterReset());
            _isLowPassFilterActive = false;
        }
    }

    public void PlayAudio()
    {
        //Debug.Log($"PlayAudio called on {gameObject.name}");
        if (audioSource == null || soundList == null || soundList.Length == 0) return; // if there's no audio source or sound list or sound list length is 0, return.soundList.Length == 0) return;

        if (playType == PlayType.Single)
        {
            PlaySingle();
        }
        else if (playType == PlayType.Sequence)
        {
            PlaySequence();
        }
        else if (playType == PlayType.Random)
        {
            PlayRandomNoRepeat();
        }

        if (randomizeStartPoint)
        {
            RandomizeStartPoint();
        }

        if (fadeIn)
        {
            StartCoroutine(FadeIn());
        }
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    // used for audio blender script.
    public void SetPlaybackState(bool shouldPlay)
    {
        if (audioSource == null) return;

        if (shouldPlay && !audioSource.isPlaying)
        {
            //Debug.Log($"Unpausing audio on {gameObject.name}");
            audioSource.UnPause();
        }
        else if (!shouldPlay && audioSource.isPlaying)
        {
            //Debug.Log($"Pausing audio on {gameObject.name}");
            audioSource.Pause();
        }
    }

    private void PlaySingle()
    {
        if (soundList[0] == null) return;
        audioSource.clip = soundList[0];
        audioSource.Play();
    }

    private void PlaySequence()
    {
        if (soundList[_sequenceIndex] == null) return;
        audioSource.clip = soundList[_sequenceIndex];
        RandomizePitchVolume();
        audioSource.PlayOneShot(audioSource.clip);

        // Update index. % modulo for wrap around.
        _sequenceIndex = (_sequenceIndex + 1) % soundList.Length;
    }

    private void PlayRandomNoRepeat()
    {
        int index;
        int attempts = 0;
        int maxAttempts = soundList.Length * 2; // prevent infinite loop.
        do
        {
            index = Random.Range(0, soundList.Length);
            attempts++;

            if (attempts >= maxAttempts) break; // safety break.
        }
        while (soundList.Length > 1 && index == _lastPlayedIndex);

        if (soundList[index] == null) return;

        audioSource.clip = soundList[index];
        RandomizePitchVolume();
        audioSource.PlayOneShot(audioSource.clip);
        _lastPlayedIndex = index;

        // Debug.Log($"Playing random clip index {index} on {gameObject.name}");
    }


    // other small methods for randomizing.
    private void RandomizePitchVolume()
    {
        if (audioSource == null) return;
       
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = Random.Range(volumeMin, volumeMax);
    }

    private void RandomizeStartPoint()
    {
        if (audioSource == null || audioSource.clip == null) return;
        audioSource.time = Random.Range(0, audioSource.clip.length);
    }

    // fade in coroutine.
    private IEnumerator FadeIn()
    {
        if (audioSource == null) yield break;

        audioSource.volume = 0f;
        float elapsed = 0f;

        while (elapsed < fadeInTime)
        {   elapsed += Time.deltaTime;
            float t = elapsed / fadeInTime;
            audioSource.volume = Mathf.Lerp(0f, volume, t);
            yield return null;
        }
    }

    // lowpass filter coroutine.
    private IEnumerator LowPassFilterActivate()
    {
        AudioLowPassFilter lowpass = audioSource.GetComponent<AudioLowPassFilter>();

        if (lowpass == null)
        {
            lowpass = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
            lowpass.cutoffFrequency = 22000f; // set to default frequency initially.
        }

        float lowPassFilterStartFrequency = lowpass.cutoffFrequency;

        float elapsed = 0f;

        while (elapsed < lowPassFilterTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lowPassFilterTransitionTime;

            lowpass.cutoffFrequency = Mathf.Lerp(lowPassFilterStartFrequency, lowPassFilterTargetFrequency, t);
            yield return null;
        }

        lowpass.cutoffFrequency = lowPassFilterTargetFrequency; // making sure to set to target frequency at the end.
        _currentFilterCoroutine = null;
    }

    // resetting filter frequency to 22000 Hz.
    private IEnumerator FilterReset()
    {
        AudioLowPassFilter lowpass = audioSource.GetComponent<AudioLowPassFilter>();

        if (lowpass == null)
        {
            lowpass = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
        }

        float startFrequency = lowpass.cutoffFrequency;

        float elapsed = 0f;

        while (elapsed < filterResetTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / filterResetTransitionTime;

            lowpass.cutoffFrequency = Mathf.Lerp(startFrequency, 22000f, t);
            yield return null;
        }

        lowpass.cutoffFrequency = 22000f; // setting back to default frequency at the end.
        _currentFilterCoroutine = null;
    }
}
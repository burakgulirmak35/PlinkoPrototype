using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlinkoPrototype;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Sound FX")]
    [SerializeField] private List<SoundFx> soundFxList = new List<SoundFx>();
    private int _soundFxID;
    [SerializeField] private float soundFxVolume = 0.5f;

    private Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

    #region Unity

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameEvents.OnBallScored += HandleBallScored;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnBallScored -= HandleBallScored;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
    }

    private void OnDestroy()
    {
        ClearAudioCache();
    }

    #endregion

    #region Event Handlers

    private void HandleBallScored(int score)
    {
        PlaySound("Gameplay/BallScored");
    }

    private void HandleLevelCompleted()
    {
        PlaySound("Gameplay/LevelCompleted");
    }

    #endregion

    #region Public API

    public void PlaySound(string path)
    {
        string fullPath = $"Sounds/{path}";
        if (!_clipCache.TryGetValue(fullPath, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>(fullPath);
            if (clip == null)
                return;

            _clipCache.Add(fullPath, clip);
        }

        soundFxList[_soundFxID].PlayDirect(clip, soundFxVolume);
        _soundFxID = (_soundFxID + 1) % soundFxList.Count;
    }

    #endregion

    private void ClearAudioCache()
    {
        foreach (var clip in _clipCache.Values)
        {
            if (clip != null)
                Resources.UnloadAsset(clip);
        }
        _clipCache.Clear();
    }

}

using System.Collections.Generic;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Presentation;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Audio
{
    public sealed class BoardGameAudioManager : MonoBehaviour, IGameAudio
    {
        private const float BgmMaximumVolume = 0.1f;
        private const float ZeroVolumeThreshold = 0.01f;

        [Header("BGM")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField, Range(0f, 1f)] private float bgmNormalizedVolume = 1f;

        [Header("SFX")]
        [SerializeField] private AudioClip moveClip;
        [SerializeField] private AudioClip battleClip;
        [SerializeField] private AudioClip pieceDestroyedClip;
        [SerializeField] private AudioClip fusionClip;
        [SerializeField] private AudioClip fusionFailedClip;
        [SerializeField] private AudioClip gameEndedClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private bool isInitialized;

        public float BgmVolume => Mathf.Clamp01(bgmNormalizedVolume);
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            InitializeAudioSources();
        }

        private void Start()
        {
            PlayBgm();
        }

        public void InitializeAudioSources()
        {
            if (isInitialized) return;

            if (bgmSource == null)
            {
                bgmSource = CreateAudioSource("BGM Source", true);
            }
            if (sfxSource == null)
            {
                sfxSource = CreateAudioSource("SFX Source", false);
            }

            LoadDefaultClips();
            ApplyVolumeSettings();
            isInitialized = true;
        }

        public void SetBgmVolume(float volume)
        {
            bgmNormalizedVolume = NormalizeVolume(volume);
            if (bgmSource != null)
            {
                bgmSource.mute = bgmNormalizedVolume <= 0f;
                bgmSource.volume = bgmNormalizedVolume * BgmMaximumVolume;
                if (bgmNormalizedVolume <= 0f)
                {
                    bgmSource.Stop();
                    StopOtherBgmSources();
                }
                else if (!bgmSource.isPlaying)
                {
                    PlayBgm();
                }
            }
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = NormalizeVolume(volume);
            if (sfxSource != null)
            {
                sfxSource.mute = sfxVolume <= 0f;
                sfxSource.volume = sfxVolume;
            }
        }

        private static float NormalizeVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            return clampedVolume <= ZeroVolumeThreshold ? 0f : clampedVolume;
        }

        private void StopOtherBgmSources()
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source != sfxSource && source.loop)
                {
                    source.Stop();
                    source.mute = true;
                }
            }
        }

        private void ApplyVolumeSettings()
        {
            if (bgmSource != null)
            {
                bgmSource.mute = bgmNormalizedVolume <= 0f;
                bgmSource.volume = bgmNormalizedVolume * BgmMaximumVolume;
                if (bgmNormalizedVolume <= 0f)
                {
                    bgmSource.Stop();
                    StopOtherBgmSources();
                }
            }

            if (sfxSource != null)
            {
                sfxSource.mute = sfxVolume <= 0f;
                sfxSource.volume = sfxVolume;
            }
        }

        private void LoadDefaultClips()
        {
            if (bgmClip == null) bgmClip = Resources.Load<AudioClip>("Audio/超頭脳バトル");
            if (moveClip == null) moveClip = Resources.Load<AudioClip>("Audio/komaidou");
            if (pieceDestroyedClip == null) pieceDestroyedClip = Resources.Load<AudioClip>("Audio/koma_hakai");
            if (fusionClip == null) fusionClip = Resources.Load<AudioClip>("Audio/gattai");
            if (fusionFailedClip == null) fusionFailedClip = Resources.Load<AudioClip>("Audio/gattai_failed");
            if (battleClip == null) battleClip = Resources.Load<AudioClip>("Audio/koma_hakai");
        }

        public void PlayBgm()
        {
            InitializeAudioSources();

            if (bgmClip == null)
            {
                Debug.LogWarning("BGMの音源が見つかりません: Assets/Resources/Audio/超頭脳バトル を確認してください");
                return;
            }

            if (bgmNormalizedVolume <= 0f)
            {
                bgmSource.Stop();
                StopOtherBgmSources();
                return;
            }

            if (bgmSource.isPlaying && bgmSource.clip == bgmClip)
            {
                return;
            }

            bgmSource.clip = bgmClip;
            bgmSource.mute = bgmNormalizedVolume <= 0f;
            bgmSource.volume = bgmNormalizedVolume * BgmMaximumVolume;
            bgmSource.Play();
        }

        public void PlayEvents(IReadOnlyList<GameEvent> events)
        {
            if (events == null) return;

            foreach (AudioCue cue in GameEventAudioResolver.Resolve(events))
            {
                PlaySfx(GetClip(cue));
            }
        }

        public void PlayFusionFailed()
        {
            PlaySfx(fusionFailedClip);
        }

        private AudioClip GetClip(AudioCue cue)
        {
            return cue switch
            {
                AudioCue.Move => moveClip,
                AudioCue.Battle => battleClip,
                AudioCue.PieceDestroyed => pieceDestroyedClip,
                AudioCue.Fusion => fusionClip,
                AudioCue.FusionFailed => fusionFailedClip,
                AudioCue.GameEnded => gameEndedClip,
                _ => null
            };
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private AudioSource CreateAudioSource(string sourceName, bool loop)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
        }
    }
}

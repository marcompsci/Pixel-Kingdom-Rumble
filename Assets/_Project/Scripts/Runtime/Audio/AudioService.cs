using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Minimal music + SFX playback with volume from settings.
    /// Phase 1 ships with no audio clips (placeholders are silent); calls with a null clip are ignored.
    /// SFX use a small round-robin of sources so overlapping hits don't cut each other off.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        const int SfxVoices = 8;

        AudioSource _music;
        AudioSource[] _sfx;
        int _nextVoice;
        float _musicVolume = 0.8f;
        float _sfxVolume = 1f;

        void Awake()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;

            _sfx = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++)
            {
                _sfx[i] = gameObject.AddComponent<AudioSource>();
                _sfx[i].playOnAwake = false;
            }
        }

        public void ApplySettings(SettingsData s)
        {
            if (s == null) return;
            _musicVolume = s.musicVolume;
            _sfxVolume = s.sfxVolume;
            if (_music != null) _music.volume = _musicVolume;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (_music == null) return;
            if (clip == null) { _music.Stop(); return; }
            if (_music.clip == clip && _music.isPlaying) return;
            _music.clip = clip;
            _music.volume = _musicVolume;
            _music.Play();
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null || _sfx == null || _sfxVolume <= 0f) return;
            var src = _sfx[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sfx.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, _sfxVolume * volumeScale);
        }
    }
}

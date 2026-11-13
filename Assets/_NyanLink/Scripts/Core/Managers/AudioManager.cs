using NineMood.Data.Definitions;
using NineMood.Services.Core;
using UnityEngine;

namespace NineMood.Core.Managers
{
    /// <summary>
    /// SFX / BGM 재생 관리. GameEvents를 구독하여 자동으로 효과음을 트리거한다.
    /// 오디오 클립은 Inspector에서 할당 (클립이 없으면 해당 효과음 무음 처리).
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("SFX - Match")]
        public AudioClip sfxMatchShort;
        public AudioClip sfxMatchMiddle;
        public AudioClip sfxMatchLong;
        public AudioClip sfxItemEffect;

        [Header("SFX - Game State")]
        public AudioClip sfxGameOver;
        public AudioClip sfxStageClear;
        public AudioClip sfxHit;
        public int PlayedSfxCount {get;private set;}
        public AudioClip LastSfx {get;private set;}

        [Header("BGM")]
        public AudioClip bgmGame;

        [Header("Volume")]
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float bgmVolume = 0.4f;

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;

        protected override void Awake()
        {
            base.Awake();
            if(!IsCreated || Instance!=this)return;
            sfxMatchShort ??= Resources.Load<AudioClip>("Audio/Prototype/match-short");
            sfxMatchMiddle ??= Resources.Load<AudioClip>("Audio/Prototype/match-middle");
            sfxMatchLong ??= Resources.Load<AudioClip>("Audio/Prototype/match-long");
            sfxItemEffect ??= Resources.Load<AudioClip>("Audio/Prototype/item");
            sfxGameOver ??= Resources.Load<AudioClip>("Audio/Prototype/lose");
            sfxStageClear ??= Resources.Load<AudioClip>("Audio/Prototype/clear");
            sfxHit ??= Resources.Load<AudioClip>("Audio/Prototype/hit");

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = bgmVolume;
        }

        private void OnEnable()
        {
            GameEvents.OnMatchCompleted   += OnMatchCompleted;
            GameEvents.OnItemUseCompleted += OnItem;
            GameEvents.OnPlayerHit += OnHit;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            GameEvents.OnResultSet        += OnResultSet;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchCompleted   -= OnMatchCompleted;
            GameEvents.OnItemUseCompleted -= OnItem;
            GameEvents.OnPlayerHit -= OnHit;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            GameEvents.OnResultSet        -= OnResultSet;
        }

        private void OnItem(int score,int removed) {if(removed>0)PlaySFX(sfxItemEffect);}
        private void OnHit(float amount) {if(amount>0)PlaySFX(sfxHit);}

        private void OnMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, NineMood.Data.Enums.TileColor chainColor)
        {
            BalanceData balance = DataManager.Instance?.BalanceData;
            int tier = balance != null ? balance.GetEffectLevel(chainLength) : 0;

            AudioClip clip = tier >= 2 ? sfxMatchLong
                           : tier == 1 ? sfxMatchMiddle
                           : sfxMatchShort;
            PlaySFX(clip);

            // 아이템 효과로 추가 타일이 제거된 경우 효과음 추가 재생
            if (tilesRemoved > chainLength)
                PlaySFX(sfxItemEffect);
        }

        private void OnGameStateChanged(GameState state)
        {
            if(state==GameState.Paused || state==GameState.ColorSelection) _bgmSource?.Pause();
            if (state == GameState.RunnerPhase || state==GameState.BossPhase)
                PlayBGM(bgmGame);
        }

        private void OnResultSet(ResultType result)
        {
            StopBGM();
            switch (result)
            {
                case ResultType.Lose: PlaySFX(sfxGameOver);   break;
                case ResultType.Win:  PlaySFX(sfxStageClear); break;
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            PlayedSfxCount++;LastSfx=clip;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || _bgmSource == null) return;
            if (_bgmSource.clip == clip) { _bgmSource.UnPause();if(_bgmSource.isPlaying)return; }
            _bgmSource.clip = clip;
            _bgmSource.volume = bgmVolume;
            _bgmSource.Play();
        }

        public void StopBGM() => _bgmSource?.Stop();
    }
}

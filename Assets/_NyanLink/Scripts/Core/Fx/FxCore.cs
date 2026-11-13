using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NineMood.Core.Fx
{
    public enum FxPlayType
    {
        Triggered = 0,
        Sustained = 1,
    }

    public enum FxExtraTimeMode
    {
        AbsoluteSeconds = 0,
        RatioOfMinDuration = 1,
    }

    public abstract class FxCore : MonoBehaviour
    {
        [SerializeField] protected bool isPlaying;

        [SerializeField, HideInInspector] private bool foldoutParticles;
        [SerializeField] protected bool includeStartLifeTime; // particle
        [SerializeField] protected float particleDuration; // particle
                                                           // Duration + StartDelay + StartLifeTime

        #region Evnets

        protected Action onPlayAction;
        protected Action onUpdateAction;
        protected Action onCompleteAction;

        public UnityEvent onPlay = new();
        public UnityEvent onUpdate = new();
        public UnityEvent onComplete = new();
        public List<TimeEvent> onTimes = new();

        [SerializeField] private bool hasOnPlay;
        [SerializeField] private bool hasOnUpdate;
        [SerializeField] private bool hasOnComplete;

        [SerializeField] protected bool invokeCompleteWhenDisable = true;
        #endregion

        public bool IsPlaying => isPlaying;

        public virtual float ParticleDuration => particleDuration;

        public virtual Type cachedType { get; }
        // Ex) public override Type cachedType => typeof(EffectBase);

        public virtual bool UseAnimator { get; }
        public virtual bool UseDOTweenSequencer { get; }
        public virtual bool UseDOTweenSequence { get; }

        protected virtual void Awake()
        {
            InitComponent();

            onPlay.AddListener(() => { onPlayAction?.Invoke(); });
            onUpdate.AddListener(() => { onUpdateAction?.Invoke(); });
            onComplete.AddListener(() => { onCompleteAction?.Invoke(); });
        }

        protected virtual void OnEnable()
        {
            if (Application.isPlaying)
            {
                // Fx는 매니저의 중앙 업데이트를 구독해서 재생 중 이벤트를 구동한다.
                // (개별 Fx의 MonoBehaviour.Update를 강제하지 않기 위함)
                FxManager.Instance.onEffectUpdate += OnUpdate;
            }
        }

        protected virtual void OnDisable()
        {
            if (Application.isPlaying && FxManager.IsCreated)
            {
                // 풀링 반환(SetActive(false)) 경로에서도 구독 해제가 안전하게 수행되도록 한다.
                FxManager.Instance.onEffectUpdate -= OnUpdate;
            }

            if (isPlaying && invokeCompleteWhenDisable)
            {
                OnComplete();
            }
        }

        protected virtual void OnApplicationQuit()
        {
            if (isPlaying && invokeCompleteWhenDisable)
            {
                OnComplete();
            }
        }

        protected virtual void OnUpdate()
        {
            if (isPlaying)
                onUpdate?.Invoke();
        }

        protected virtual void InitComponent()
        {
            // if (UseDOTweenSequencer)
            // {
            //     var tweenSequencer = GetComponent<DOTweenSequencer>();
            //     if (tweenSequencer != null)
            //     {
            //         foreach (var kv in tweenSequencer.Sequences)
            //         {
            //             kv.Value.Events.onPlay.AddListener(() => { OnPlay(kv.Key); });
            //             kv.Value.Events.onComplete.AddListener(() => { OnComplete(kv.Key); });
            //         }
            //     }
            // }

            // if (UseDOTweenSequence)
            // {
            //     var tweenSequence = GetComponent<DOTweenSequence>();
            //     if (tweenSequence != null)
            //     {
            //         tweenSequence.onPlay.AddListener(() => { OnPlay(tweenSequence.ID); });
            //         tweenSequence.onComplete.AddListener(() => { OnComplete(tweenSequence.ID); });
            //     }
            // }
        }

        #region Animation Evnet

        public void OnPlayEvent()
        {
            if (!UseAnimator) return;
            OnPlay();
        }

        public void OnCompleteEvent()
        {
            if (!UseAnimator) return;
            OnComplete();
        }

        #endregion

        protected virtual void OnPlay(object obj = null)
        {
            // 반복 재생인 경우 이벤트는 호출되지만 isPlaying 가 true 여서 더 이상 FxManager.RegistFx()를 호출하지 않는다.
            if (isPlaying)
            {

            }
            else
            {
                // FxManager.GetFx()로 생성된 인스턴스는 "Play 호출 시점"에 매니저에 등록된다.
                // (호출자가 Play 전에 필요한 세팅을 할 수 있도록 하기 위함)
                if (Application.isPlaying && FxManager.IsCreated)
                {
                    if (!FxManager.Instance.RegistFx(this))
                    {
                        // 등록이 거부되면 재생을 시작하지 않는다.
                        // 호출자는 필요하면 ReleaseEffect(또는 Destroy) 처리할 수 있다.
                        return;
                    }
                }
                isPlaying = true;

            }

            onPlay?.Invoke();
        }

        /// <summary>
        /// 오버라이드 시 베이스보다 위쪽에 코드 작성
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void OnComplete(object obj = null)
        {
            isPlaying = false;
            onComplete?.Invoke();

            if (Application.isPlaying)
            {
                if (FxManager.IsCreated)
                {
                    // 완료 시점에서 매니저 업데이트 구독을 해제하고,
                    // 매니저에게 회수(풀링/Destroy)를 위임한다.
                    FxManager.Instance.onEffectUpdate -= OnUpdate;
                    FxManager.Instance.ReleaseEffect(this);
                }
            }
        }

        [SerializeField] private bool finalProperty;
    }

    public class HideInInspectorCustom : Attribute
    {
    }

    #region Events

    [Serializable]
    public class TimeEvent
    {
        [SerializeField] private bool ratioInvokeTime;
        [SerializeField] private float time;
        private Action onTimeAction;
        public UnityEvent onTime;

        public TimeEvent()
        {
            onTimeAction += () => { onTime?.Invoke(); };
        }

        public void Invoke()
        {
            onTimeAction?.Invoke();
        }
    }

    #endregion

    #region Sources

    [Serializable]
    public class BaseSource
    {
        protected float duration;
        public int relativeOrder;
    }

    [Serializable]
    public class ParticleSystemSource : BaseSource
    {
        public ParticleSystem particleSystem;
        public ParticleSystemRenderer particleSystemsRenderer;

        public float Duration(bool includeStartLifeTime = true)
        {
            var module = particleSystem.main;
            if (includeStartLifeTime)
            {
                duration = module.duration + module.startDelay.constant + module.startLifetime.constant;
            }
            else
            {
                duration = module.duration + module.startDelay.constant;
            }

            return duration;
        }
    }

    #endregion
}
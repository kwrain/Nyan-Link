using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NineMood.Core.Fx
{
    [CreateAssetMenu(menuName = "NyanLink/Fx/Fx Attribute", fileName = "FxAttribute_")]
    public sealed class FxAttribute : ScriptableObject
    {
        [Header("식별자")]
        [Tooltip("타입 이름(클래스명)로 사용됩니다. 예) \"FxGlow\"")]
        public string fxId;

        [Header("리소스")]
        [Tooltip("Addressables 주소 또는 Resources 하위 경로(확장자 없이). 예: Resources/Fx/Hit.prefab → \"Fx/Hit\"")]
        public string prefabPath;
        [Header("기본 동작")]
        public FxPlayType playType = FxPlayType.Triggered;
        [Tooltip("루프형(반복) 여부. true면 완료(Completed)는 자동으로 발생하지 않습니다.")]
        public bool isLooping = false;
        [Header("오브젝트 풀 제한")]
        [Min(0)]
        [Tooltip("동시에 존재 가능한 최대 수량. 0이면 제한 없음.")]
        public int maxAlive = 0;
        [Header("플레이타임 (Triggered)")]
        [Min(0f)]
        [Tooltip("파티클/애니/쉐이더로 지속시간을 판정할 수 없을 때 사용되는 기본 최소 재생시간(초).")]
        public float fallbackMinDurationSeconds = 0.2f;
        [Tooltip("추가 재생시간 적용 방식")]
        public FxExtraTimeMode extraTimeMode = FxExtraTimeMode.AbsoluteSeconds;
        [Min(0f)]
        [Tooltip("AbsoluteSeconds: 추가 초 / RatioOfMinDuration: 최소시간 * 비율")]
        public float extraTimeValue = 0f;
        [Header("레이어링(선택)")]
        [Tooltip("0이면 변경하지 않습니다. (Default=0과 구분이 어려워 -1 같은 값을 쓰고 싶으면 int?로 바꿔도 됩니다.)")]
        public int overrideLayer = 0;

        [NonSerialized] private Type cachedFxType;

        /// <summary>
        /// fxId(=클래스명)로부터 Fx 타입(<see cref="FxCore"/> 파생)을 런타임에 해석합니다.
        /// </summary>
        public Type FxType
        {
            get
            {
                if (cachedFxType != null) return cachedFxType;
                cachedFxType = ResolveFxType();
                return cachedFxType;
            }
        }

        public float ComputeExtraTime(float minDurationSeconds)
        {
            if (extraTimeValue <= 0f) return 0f;
            return extraTimeMode == FxExtraTimeMode.AbsoluteSeconds
                ? extraTimeValue
                : (minDurationSeconds * extraTimeValue);
        }

        private Type ResolveFxType()
        {
            if (string.IsNullOrEmpty(fxId)) return null;

            // fxId가 "FxGlow" 같은 클래스명이라고 가정하고, 로드된 어셈블리에서 검색한다.
            // (AOT/IL2CPP 환경에서 단순 GetType("FxGlow")는 실패할 확률이 높아 스캔 방식 사용)
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(x => x != null).ToArray();
                }

                foreach (var t in types)
                {
                    if (t == null) continue;
                    if (!typeof(FxCore).IsAssignableFrom(t)) continue;
                    if (t.Name != fxId) continue;
                    return t;
                }
            }

            return null;
        }
    }
}
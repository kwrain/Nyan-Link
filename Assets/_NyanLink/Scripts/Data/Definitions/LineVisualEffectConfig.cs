using System;
using System.Collections.Generic;
using UnityEngine;
using NyanLink.Data.Enums;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 연결선(LineRenderer) 시각 효과 설정.
    /// 체인 티어(Short/Middle/Long)별·선택 색상별로 연출을 지정 가능. 추후 전기→불꽃 등 연출 변경 시 이 에셋만 수정하면 됨.
    /// </summary>
    [Serializable]
    public class LineTierSettings
    {
        [Tooltip("연결선 색상")]
        public Color lineColor = new Color(0.7f, 0.9f, 1f, 0.8f);

        [Tooltip("연결선 두께")]
        [Range(0.005f, 0.15f)]
        public float lineWidth = 0.015f;

        [Tooltip("이펙트 강도 (0=없음, 1=최대). 셰이더/머티리얼에서 _EffectIntensity 등으로 사용 가능")]
        [Range(0f, 1f)]
        public float effectIntensity = 0f;

        [Tooltip("이 티어 전용 머티리얼 (비어있으면 기본 사용). 추후 전기/불꽃 등 연출 변경 시 여기서 교체")]
        public Material materialOverride;
    }

    /// <summary>
    /// 색상별 연결선 보정 (티어 설정 위에 추가 적용)
    /// </summary>
    [Serializable]
    public class LineColorOverride
    {
        public TileColor color;
        [Tooltip("티어 색상에 곱할 틴트 (흰색=변경 없음)")]
        public Color colorTint = Color.white;
        [Tooltip("이펙트 강도 배율 (1=변경 없음)")]
        [Min(0f)]
        public float effectIntensityMultiplier = 1f;
    }

    [CreateAssetMenu(fileName = "LineVisualEffectConfig", menuName = "NyanLink/Data/Line Visual Effect", order = 3)]
    public class LineVisualEffectConfig : ScriptableObject
    {
        [Header("체인 티어별 연출 (밸런스: Short / Middle / Long)")]
        [Tooltip("Short 체인 (아이템 미생성 구간)")]
        public LineTierSettings tierShort = new LineTierSettings();

        [Tooltip("Middle 체인 (Lv.1 아이템 구간) - 예: 약한 전기 이펙트")]
        public LineTierSettings tierMiddle = new LineTierSettings { effectIntensity = 0.4f };

        [Tooltip("Long 체인 (Lv.2 아이템 구간) - 예: 강한 전기 이펙트")]
        public LineTierSettings tierLong = new LineTierSettings { effectIntensity = 0.9f };

        [Header("색상별 이펙트 보정 (선택된 타일 색상에 따라 다르게)")]
        [Tooltip("색상별 틴트·강도 배율. 비어있으면 티어 설정만 사용")]
        public List<LineColorOverride> colorOverrides = new List<LineColorOverride>();

        /// <summary>
        /// 티어(0=Short, 1=Middle, 2=Long)에 해당하는 설정 반환
        /// </summary>
        public LineTierSettings GetTierSettings(int tier)
        {
            return tier switch
            {
                2 => tierLong,
                1 => tierMiddle,
                _ => tierShort
            };
        }

        /// <summary>
        /// 색상별 보정 반환 (없으면 null)
        /// </summary>
        public LineColorOverride GetColorOverride(TileColor color)
        {
            if (colorOverrides == null) return null;
            foreach (var o in colorOverrides)
            {
                if (o.color == color) return o;
            }
            return null;
        }

        /// <summary>
        /// 적용할 연결선 색상 (티어 + 색상 보정)
        /// </summary>
        public Color GetLineColor(int tier, TileColor chainColor)
        {
            LineTierSettings tierSettings = GetTierSettings(tier);
            Color c = tierSettings.lineColor;
            var over = GetColorOverride(chainColor);
            if (over != null)
                c *= over.colorTint;
            return c;
        }

        /// <summary>
        /// 적용할 연결선 두께
        /// </summary>
        public float GetLineWidth(int tier)
        {
            return GetTierSettings(tier).lineWidth;
        }

        /// <summary>
        /// 적용할 이펙트 강도 (티어 + 색상 배율)
        /// </summary>
        public float GetEffectIntensity(int tier, TileColor chainColor)
        {
            LineTierSettings tierSettings = GetTierSettings(tier);
            float intensity = tierSettings.effectIntensity;
            var over = GetColorOverride(chainColor);
            if (over != null)
                intensity *= over.effectIntensityMultiplier;
            return Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 적용할 머티리얼 (티어별 override 있으면 사용, 없으면 null = 기본 유지)
        /// </summary>
        public Material GetMaterial(int tier)
        {
            return GetTierSettings(tier).materialOverride;
        }
    }
}

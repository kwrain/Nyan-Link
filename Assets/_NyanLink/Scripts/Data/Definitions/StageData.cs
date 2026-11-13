using UnityEngine;
using System.Collections.Generic;
using NineMood.Data.Enums;

namespace NineMood.Data.Definitions
{
    /// <summary>
    /// 스테이지 설정 데이터 — 거리 기반 장애물 배치 (2026-06-24 확정)
    /// 러너는 runnerSpeed로 자동 전진. 장애물은 triggerDistance로 위치 고정.
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "NyanLink/Data/Stage", order = 7)]
    public class StageData : ScriptableObject
    {
        // ── 기본 정보 ───────────────────────────────────────────────────
        [Header("기본 정보")]
        public string stageId;
        public string stageName;
        [Min(1)] public int episodeNumber = 1;
        [Min(1)] public int stageNumber   = 1;

        // ── 그리드 ──────────────────────────────────────────────────────
        [Header("그리드")]
        [Tooltip("스테이지 쉐이프 마스크. null = 기본 6열×7행 전체 사용")]
        public GridShapeData gridShape;

        // ── 러너 ────────────────────────────────────────────────────────
        [Header("러너")]
        [Tooltip("이동 속도 (거리 단위/초). 0이면 BalanceData.runnerBaseSpeed 사용")]
        [Min(0f)] public float runnerSpeed = 0f;

        [Tooltip("스테이지 전체 길이 (거리 단위). Progress = currentDist / stageLength")]
        [Min(1f)] public float stageLength = 1000f;

        [Tooltip("BaseDecayRate 배율. 1.0 = BalanceData 기본값 그대로")]
        [Min(0f)] public float stageDecayMultiplier = 1.0f;

        [Tooltip("시간 경과에 따른 스태미나 감소율 증가량 (HP/sec²). 0이면 BalanceData.arcadeDecayIncreasePerSec 사용")]
        [Min(0f)] public float decayIncreasePerSecond = 0f;

        // ── 장애물 ──────────────────────────────────────────────────────
        [Header("장애물")]
        [Tooltip("장애물 목록. triggerDistance 오름차순 정렬 권장")]
        public List<ObstacleEntry> obstacles = new List<ObstacleEntry>();

        [Tooltip("장애물 충돌 기본 데미지")]
        [Min(0f)] public float obstacleBaseDamage = 20f;

        [Min(0.1f), Tooltip("장애물 접근 속도 (월드 단위/초)")]
        public float obstacleMoveSpeed = 4f;

        [Tooltip("챕터 난이도 계수. ActualDamage = obstacleBaseDamage × chapterCoefficient")]
        [Min(0f)] public float chapterCoefficient = 1.0f;

        // ── 보스 ────────────────────────────────────────────────────────
        [Header("보스")]
        public BossBattleConfig bossConfig;

        // ── 보상 ────────────────────────────────────────────────────────
        [Header("보상")]
        [Tooltip("클리어 보상 코인")]
        [Min(0)] public int clearRewardCoin = 100;

        [Tooltip("드롭 테이블 참조")]
        public LootTableData lootTable;

        // ── 편의 프로퍼티 ───────────────────────────────────────────────

        /// <summary>장애물이 하나라도 있는지</summary>
        public bool HasObstacles => obstacles != null && obstacles.Count > 0;

        /// <summary>실제 적용 데미지</summary>
        public float ActualObstacleDamage => obstacleBaseDamage * chapterCoefficient;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (obstacles == null) return;
            for (int i = 0; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                o.triggerDistance = Mathf.Clamp01(o.triggerDistance);
                obstacles[i] = o;
            }
        }
#endif
    }

    /// <summary>
    /// 개별 장애물 배치 정보
    /// 요구 액션은 ColorActionMapData.GetAction(requiredColor) 으로 조회
    /// </summary>
    [System.Serializable]
    public struct ObstacleEntry
    {
        [Range(0f, 1f)]
        [Tooltip("등장 위치 (0 = 출발점, 1 = 보스 직전)")]
        public float triggerDistance;

        [Tooltip("요구 타일 색상. 대응 러너 액션은 ColorActionMapData 에서 조회")]
        public TileColor requiredColor;
    }
}

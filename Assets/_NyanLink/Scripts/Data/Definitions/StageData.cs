using UnityEngine;
using System.Collections.Generic;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 스테이지 정보 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "NyanLink/Data/Stage", order = 7)]
    public class StageData : ScriptableObject
    {
        [Header("스테이지 기본 정보")]
        [Tooltip("스테이지 ID")]
        public string stageId;

        [Tooltip("스테이지 이름")]
        public string stageName;

        [Tooltip("에피소드 번호")]
        public int episodeNumber = 1;

        [Tooltip("스테이지 번호")]
        public int stageNumber = 1;

        [Header("그리드 설정")]
        [Tooltip("사용할 그리드 쉐이프")]
        public GridShapeData gridShape;

        [Header("러너 설정")]
        [Tooltip("스태미나 자연 감소율 (초당)")]
        public float staminaDecayRate = 1f;

        [Tooltip("스테이지 길이 (진행도 100%까지의 거리)")]
        public float stageLength = 1000f;

        [Header("장애물 설정")]
        [Tooltip("장애물 스폰 간격 (진행도 단위)")]
        public float obstacleSpawnInterval = 50f;

        [Tooltip("장애물 예고 시간 (초)")]
        public float obstacleWarningDuration = 3f;

        [Header("보스 설정")]
        [Tooltip("보스 전투 설정")]
        public BossBattleConfig bossConfig;

        [Tooltip("보스 등장 진행도 (0~1)")]
        [Range(0f, 1f)]
        public float bossAppearProgress = 1f;

        [Header("보상 설정")]
        [Tooltip("클리어 보상 코인")]
        public int clearRewardCoin = 100;

        [Tooltip("클리어 보상 경험치")]
        public int clearRewardExp = 50;
    }
}

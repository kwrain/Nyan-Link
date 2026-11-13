using System;
using System.IO;
using NineMood.Data.Definitions;
using NineMood.Runner;
using NineMood.Core;
using UnityEngine;

namespace NineMood.Core.Managers
{
    /// <summary>
    /// 체크포인트 저장/복원 — 기획서02 §3.2.
    ///
    /// 저장 타이밍: 장애물 클리어 직후 (ObstacleController.OnObstacleCleared).
    /// 복원 타이밍: GameManager.StartGame() → TryRestoreCheckpoint().
    /// 초기화:     RetryGame (명시적 재시작), TriggerStageClear (스테이지 완료).
    ///
    /// 파일 경로: Application.persistentDataPath/saves/checkpoint_{stageId}.json
    /// </summary>
    public class SaveManager : SceneSingleton<SaveManager>
    {
        [Header("스테이지 (null이면 저장 비활성)")]
        public StageData stageData;

        [Header("참조")]
        public RunnerProgressSystem progressSystem;
        public ObstacleSpawner      obstacleSpawner;

        private static string SaveDir => Path.Combine(Application.persistentDataPath, "saves");

        // ── 라이프사이클 ──────────────────────────────────────────

        private void OnEnable()  => ObstacleController.OnObstacleCleared += HandleObstacleCleared;
        private void OnDisable() => ObstacleController.OnObstacleCleared -= HandleObstacleCleared;

        // ── 저장 ─────────────────────────────────────────────────

        private void HandleObstacleCleared()
        {
            if (!IsConfigured()) return;

            var cp = new CheckpointData
            {
                stageId              = stageData.stageId,
                nextObstacleIndex    = obstacleSpawner?.NextObstacleIndex ?? 0,
                savedDistance        = progressSystem?.CurrentDistance    ?? 0f,
                savedStamina         = StaminaManager.Instance?.CurrentStamina        ?? 0f,
                savedElapsedRunTime  = StaminaManager.Instance?.ElapsedRunnerTime     ?? 0f,
                savedScore           = ScoreManager.Instance?.CurrentScore            ?? 0,
            };
            Write(cp);
            Debug.Log($"[SaveManager] 체크포인트 저장 — 장애물 {cp.nextObstacleIndex - 1} 클리어 " +
                      $"/ 거리 {cp.savedDistance:F0} / 스태미나 {cp.savedStamina:F1}");
        }

        // ── 공개 API ─────────────────────────────────────────────

        /// <summary>StartGame 직후 호출. 체크포인트가 있으면 복원한다.</summary>
        public void TryRestoreCheckpoint()
        {
            if (!IsConfigured()) return;
            var cp = Load(stageData.stageId);
            if (cp == null) return;
            Restore(cp);
            Debug.Log($"[SaveManager] 체크포인트 복원 — 거리 {cp.savedDistance:F0} " +
                      $"/ 스태미나 {cp.savedStamina:F1} / 점수 {cp.savedScore}");
        }

        /// <summary>현재 스테이지의 체크포인트가 있는지 확인. GameReadyPanel이 버튼 레이블 결정에 사용.</summary>
        public bool HasCurrentCheckpoint() =>
            IsConfigured() && File.Exists(GetPath(stageData.stageId));

        /// <summary>RetryGame, 스테이지 클리어 시 체크포인트 삭제.</summary>
        public void ClearCurrentCheckpoint()
        {
            if (!IsConfigured()) return;
            string path = GetPath(stageData.stageId);
            if (File.Exists(path)) File.Delete(path);
        }

        // ── 내부 ─────────────────────────────────────────────────

        private void Restore(CheckpointData cp)
        {
            progressSystem?.RestoreDistance(cp.savedDistance);
            obstacleSpawner?.RestoreCheckpoint(cp.nextObstacleIndex);
            StaminaManager.Instance?.RestoreStamina(cp.savedStamina, cp.savedElapsedRunTime);
            ScoreManager.Instance?.RestoreScore(cp.savedScore);
        }

        private void Write(CheckpointData cp)
        {
            Directory.CreateDirectory(SaveDir);
            File.WriteAllText(GetPath(cp.stageId), JsonUtility.ToJson(cp, prettyPrint: false));
        }

        private CheckpointData Load(string stageId)
        {
            string path = GetPath(stageId);
            if (!File.Exists(path)) return null;
            try   { return JsonUtility.FromJson<CheckpointData>(File.ReadAllText(path)); }
            catch { File.Delete(path); return null; }
        }

        private bool IsConfigured() =>
            stageData != null && !string.IsNullOrEmpty(stageData.stageId);

        private static string GetPath(string stageId) =>
            Path.Combine(SaveDir, $"checkpoint_{stageId}.json");
    }

    [Serializable]
    public class CheckpointData
    {
        public string stageId;
        public int    nextObstacleIndex;    // 다음에 스폰할 장애물 인덱스 (= 클리어한 수)
        public float  savedDistance;        // RunnerProgressSystem.CurrentDistance
        public float  savedStamina;
        public float  savedElapsedRunTime;  // StaminaManager 감소율 연속성 유지
        public int    savedScore;
    }
}

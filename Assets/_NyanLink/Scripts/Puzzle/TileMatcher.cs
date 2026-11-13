using System.Collections.Generic;
using UnityEngine;
using NineMood.Data.Enums;
using NineMood.Data.Definitions;
using NineMood.Core.Managers;
using NineMood.Boss;

namespace NineMood.Puzzle
{
    /// <summary>
    /// 타일 매칭 로직 처리
    /// ItemEffectType 기준 (2026-06-24 확정): Recovery/TimeFreeze/Blast/LineClear/PowerUp/ColorShift
    /// </summary>
    public class TileMatcher : MonoBehaviour
    {
        [Header("참조")]
        public PuzzleBoardManager boardManager;
        public BalanceData balanceData;

        [Header("매칭 설정")]
        [Tooltip("최소 매칭 개수")]
        public int minMatchCount = 2;

        [Header("디버그")]
        public bool debugLog = false;

        private void Awake()
        {
            if (boardManager == null)
                boardManager = FindFirstObjectByType<PuzzleBoardManager>();
        }

        // ── 공개 진입점 ────────────────────────────────────────────────

        public void ProcessMatch(List<Vector3Int> selectedChain)
        {
            if (GameManager.IsCreated && GameManager.Instance.CurrentState != NineMood.Core.GameState.RunnerPhase &&
                GameManager.Instance.CurrentState != NineMood.Core.GameState.BossPhase) return;
            if (selectedChain == null || selectedChain.Count < minMatchCount) return;
            if (!ValidateChain(selectedChain)) return;

            TileInstance firstTile = boardManager.GetTileAtOffset(selectedChain[0]);
            TileColor chainColor   = firstTile?.Color ?? TileColor.Red;
            int chainLength        = selectedChain.Count;
            bool preserveItems = GameManager.Instance?.CurrentState == NineMood.Core.GameState.BossPhase
                && !(BossController.Instance?.CanUseItems ?? false);

            BalanceData balance = ResolveBalance();
            int effectLevel = ChainEvaluator.GetEffectLevel(balance, chainLength);

            // 아이템 타일 생성 가능 여부 (타입별 1개 제한)
            bool canCreateItem      = false;
            TileState itemState     = TileState.Normal;
            Vector3Int itemPosition = selectedChain[chainLength - 1];
            ItemEffectType itemType = ResolveEffectType(chainColor);

            if (!preserveItems && balance != null && effectLevel > 0)
            {
                canCreateItem = CanPlaceItemTile(balance, itemType);
                if (canCreateItem)
                    itemState = ChainEvaluator.EffectLevelToTileState(effectLevel);

                if (debugLog)
                    Debug.Log($"[매칭] 체인 {chainLength}, 색상 {chainColor}, 효과 Lv{effectLevel}, 아이템 생성 {canCreateItem}");
            }

            // 제거 타일 수집 (체인 + 아이템 효과 연쇄)
            HashSet<Vector3Int> tilesToRemove = new HashSet<Vector3Int>();
            if (preserveItems)
            {
                foreach (var cell in selectedChain)
                    if (boardManager.GetTileAtOffset(cell).State == TileState.Normal) tilesToRemove.Add(cell);
            }
            else ApplyItemEffects(selectedChain, tilesToRemove, balance);

            int tilesRemoved = tilesToRemove.Count;
            RemoveMatchedTiles(tilesToRemove);

            ScoreManager.Instance?.AddScore(chainLength, effectLevel, tilesRemoved, chainColor);
            SpawnNewTiles(tilesToRemove, canCreateItem, itemPosition, chainColor, itemState);
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────

        private BalanceData ResolveBalance() =>
            balanceData ?? DataManager.Instance?.BalanceData;

        private bool ValidateChain(List<Vector3Int> chain)
        {
            if (chain == null || chain.Count < minMatchCount) return false;
            TileInstance first = boardManager.GetTileAtOffset(chain[0]);
            if (first == null) return false;
            TileColor color = first.Color;
            var unique = new HashSet<Vector3Int>();

            for (int i = 0; i < chain.Count; i++)
            {
                TileInstance t = boardManager.GetTileAtOffset(chain[i]);
                if (t == null || !t.IsActive || t.Color != color || !unique.Add(chain[i])) return false;
                if (i > 0 && !HexOffsetUtils.IsAdjacent(chain[i - 1], chain[i])) return false;
            }
            return true;
        }

        private bool CanPlaceItemTile(BalanceData balance, ItemEffectType type)
        {
            if (balance.maxItemsPerType <= 0) return true; // 0 = 무제한
            return boardManager.CountItemsOfType(type) < balance.maxItemsPerType;
        }

        private void RemoveMatchedTiles(IEnumerable<Vector3Int> offsets)
        {
            foreach (var o in offsets) boardManager.RemoveTile(o);
        }

        private void SpawnNewTiles(
            IEnumerable<Vector3Int> emptyPositions,
            bool createItem, Vector3Int itemPos,
            TileColor itemColor, TileState itemState)
        {
            foreach (var pos in emptyPositions)
            {
                if (createItem && pos == itemPos)
                    boardManager.SpawnTile(pos, itemColor, itemState);
                else
                    boardManager.SpawnTile(pos, boardManager.GetRandomTileColor(), TileState.Normal);
            }
            boardManager.EnsureRequiredColorPair();
        }

        // ── 색상 → 아이템 효과 매핑 ───────────────────────────────────

        private ItemEffectType ResolveEffectType(TileColor color)
        {
            ColorEffectMappingData mapping = DataManager.Instance?.ColorEffectMapping;
            if (mapping != null) return mapping.GetEffectForColor(color);

            return color switch
            {
                TileColor.Red    => ItemEffectType.Recovery,
                TileColor.Blue   => ItemEffectType.TimeFreeze,
                TileColor.Yellow => ItemEffectType.Blast,
                TileColor.Purple => ItemEffectType.LineClear,
                TileColor.Orange => ItemEffectType.PowerUp,
                TileColor.Cyan   => ItemEffectType.ColorShift,
                _                => ItemEffectType.None
            };
        }

        // ── 아이템 효과 연쇄 처리 ─────────────────────────────────────

        private void ApplyItemEffects(
            List<Vector3Int> chain,
            HashSet<Vector3Int> tilesToRemove,
            BalanceData balance)
        {
            var itemQueue             = new Queue<Vector3Int>();
            var processedItemSources  = new HashSet<Vector3Int>();

            foreach (var offset in chain)
                AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);

            while (itemQueue.Count > 0)
            {
                var offset = itemQueue.Dequeue();
                if (!processedItemSources.Add(offset)) continue;

                TileInstance tile = boardManager.GetTileAtOffset(offset);
                if (tile == null || tile.State == TileState.Normal) continue;

                int level = tile.State == TileState.ItemLv2 ? 2 : 1;
                ItemEffectType effect = ResolveEffectType(tile.Color);

                if (debugLog)
                    Debug.Log($"[아이템 효과] {effect} Lv{level} @ {offset}");

                switch (effect)
                {
                    case ItemEffectType.Recovery:
                        ApplyRecovery(level, balance);
                        break;
                    case ItemEffectType.TimeFreeze:
                        ApplyTimeFreeze(level, balance);
                        break;
                    case ItemEffectType.Blast:
                        ApplyBlast(offset, level, balance, tilesToRemove, itemQueue, processedItemSources);
                        break;
                    case ItemEffectType.LineClear:
                        ApplyLineClear(offset, level, tilesToRemove, itemQueue, processedItemSources);
                        break;
                    case ItemEffectType.PowerUp:
                        ApplyPowerUp(level, balance);
                        break;
                    case ItemEffectType.ColorShift:
                        ApplyColorShift(level);
                        break;
                }
            }
        }

        private void AddAffectedTile(
            Vector3Int offset,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue,
            HashSet<Vector3Int> processedItemSources)
        {
            TileInstance tile = boardManager.GetTileAtOffset(offset);
            if (tile == null || !tile.IsActive) return;
            if (tilesToRemove.Add(offset) && tile.State != TileState.Normal
                && !processedItemSources.Contains(offset))
                itemQueue.Enqueue(offset);
        }

        // ── 아이템 효과 구현 ──────────────────────────────────────────

        /// <summary>Red — 스태미나 즉시 회복 (% 기반, StaminaManager가 MaxStamina 적용)</summary>
        private void ApplyRecovery(int level, BalanceData balance)
        {
            float percent = level >= 2
                ? balance?.healPercentLv2 ?? 0.30f
                : balance?.healPercentLv1 ?? 0.10f;
            ItemEffectEvents.TriggerRecovery(percent);
        }

        /// <summary>Blue — 스태미나 감소 / 타이머 정지 (Phase 3 구현)</summary>
        private void ApplyTimeFreeze(int level, BalanceData balance)
        {
            float duration = level >= 2
                ? balance?.freezeDurationLv2 ?? 7f
                : balance?.freezeDurationLv1 ?? 3f;
            ItemEffectEvents.TriggerTimeFreeze(duration);
            // TODO(Phase3): StaminaManager.PauseDecay(duration) 연동
        }

        /// <summary>Yellow — 주변 N칸 범위 파괴</summary>
        private void ApplyBlast(
            Vector3Int center, int level, BalanceData balance,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue, HashSet<Vector3Int> processed)
        {
            int radius = level >= 2
                ? balance?.blastRangeLv2 ?? 2
                : balance?.blastRangeLv1 ?? 1;

            foreach (var o in HexOffsetUtils.GetRange(center, radius))
                AddAffectedTile(o, tilesToRemove, itemQueue, processed);
        }

        /// <summary>
        /// Purple — 라인 제거
        /// Lv1: 가로 1줄 / Lv2: 가로 + 좌대각 + 우대각 3방향
        /// </summary>
        private void ApplyLineClear(
            Vector3Int center, int level,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue, HashSet<Vector3Int> processed)
        {
            foreach (var offset in boardManager.GetAllTileOffsets())
                if (HexOffsetUtils.IsOnLine(center, offset, level >= 2))
                    AddAffectedTile(offset, tilesToRemove, itemQueue, processed);
        }

        /// <summary>Orange — 다음 보스 공격 데미지 버프 적용</summary>
        private void ApplyPowerUp(int level, BalanceData balance)
        {
            float bonus = balance?.GetPowerUpBonus(level) ?? (level >= 2 ? 1.0f : 0.5f);
            ItemEffectEvents.TriggerPowerUp(bonus);
            // TODO(Phase4): BossPhaseManager에서 OnPowerUp 구독 후 다음 공격에 버프 적용
        }

        /// <summary>
        /// Cyan — 보드 전체 색상 통일
        /// Lv1: 랜덤 1색 / Lv2: (UI로 색상 선택, Phase6 구현)
        /// </summary>
        private void ApplyColorShift(int level)
        {
            if (level >= 2)
            {
                // TODO(Phase6): 색상 선택 UI 표시 후 SetAllTileColor 호출
                Debug.Log("[ColorShift Lv2] 색상 선택 UI 미구현 — Phase6 예정");
                return;
            }
            TileColor[] colors = (TileColor[])System.Enum.GetValues(typeof(TileColor));
            TileColor target = colors[Random.Range(0, colors.Length)];
            ItemEffectEvents.TriggerColorShift(target);
            // TODO(Phase2): boardManager.SetAllTileColor(target) 구현 후 연동
        }
    }

    /// <summary>
    /// 아이템 효과 이벤트 집합
    /// 각 시스템(StaminaManager, BossPhaseManager 등)이 구독하여 처리
    /// </summary>
    public static class ItemEffectEvents
    {
        /// <summary>Red Recovery — 페이로드: 회복 비율 (0~1). StaminaManager가 MaxStamina 곱해 적용</summary>
        public static event System.Action<float> OnRecovery;
        public static void TriggerRecovery(float percent) => OnRecovery?.Invoke(percent);

        /// <summary>Blue TimeFreeze — 페이로드: 정지 시간(초)</summary>
        public static event System.Action<float> OnTimeFreeze;
        public static void TriggerTimeFreeze(float duration) => OnTimeFreeze?.Invoke(duration);

        /// <summary>Orange PowerUp — 페이로드: 버프 배율 (0.5 = +50%)</summary>
        public static event System.Action<float> OnPowerUp;
        public static void TriggerPowerUp(float bonus) => OnPowerUp?.Invoke(bonus);

        /// <summary>Cyan ColorShift Lv1 — 페이로드: 목표 색상</summary>
        public static event System.Action<TileColor> OnColorShift;
        public static void TriggerColorShift(TileColor color) => OnColorShift?.Invoke(color);
    }
}

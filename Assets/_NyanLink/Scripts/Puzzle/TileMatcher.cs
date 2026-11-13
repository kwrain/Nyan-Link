using System.Collections.Generic;
using NineMood.Boss;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.UI;
using UnityEngine;

namespace NineMood.Puzzle
{
    /// <summary>한 요청은 기존 타일만 수집·연쇄한 후 단일 집계/리필. 청록 선택은 커밋 후 수행.</summary>
    public class TileMatcher : MonoBehaviour
    {
        public PuzzleBoardManager boardManager;
        public BalanceData balanceData;
        public int minMatchCount = 2;
        public bool debugLog;
        public bool IsResolving { get; private set; }
        public int LastRemovedCount { get; private set; }
        public int LastDirectCount { get; private set; }
        public int LastActivatedCount { get; private set; }
        private readonly Queue<int> _colorShifts = new Queue<int>();
        private BalanceData Balance => balanceData != null ? balanceData : DataManager.Instance?.BalanceData;
        private bool IsPlaying => GameManager.Instance?.CurrentState == GameState.RunnerPhase || GameManager.Instance?.CurrentState == GameState.BossPhase;
        private bool ItemsAllowed => GameManager.Instance?.CurrentState == GameState.RunnerPhase || (BossController.Instance?.CanUseItems ?? false);
        private void Awake()
        {
            if (boardManager == null) boardManager = FindFirstObjectByType<PuzzleBoardManager>();
            if (GetComponent<ItemTileLabels>() == null) gameObject.AddComponent<ItemTileLabels>();
        }
        private void OnEnable() => GameEvents.OnGameStateChanged += OnState;
        private void OnDisable() { GameEvents.OnGameStateChanged -= OnState; Cancel(); }
        private void Cancel() { _colorShifts.Clear(); IsResolving = false; }
        private void OnState(GameState state)
        {
            if (state == GameState.Briefing || state == GameState.Result) Cancel();
        }
        public void ProcessMatch(List<Vector3Int> selectedChain)
        {
            if (!IsPlaying || IsResolving || !ValidateChain(selectedChain)) return;
            Resolve(new List<Vector3Int>(selectedChain),false);
        }
        public bool TryUseItem(Vector3Int cell)
        {
            var tile = boardManager.GetTileAtOffset(cell);
            if (!IsPlaying || !ItemsAllowed || IsResolving || tile == null || !tile.IsActive || tile.State == TileState.Normal) return false;
            Resolve(new List<Vector3Int> { cell },true);
            return true;
        }
        private bool ValidateChain(List<Vector3Int> chain)
        {
            if (chain == null || chain.Count < minMatchCount) return false;
            var first = boardManager.GetTileAtOffset(chain[0]);
            if (first == null) return false;
            var seen = new HashSet<Vector3Int>();
            for (int i=0;i<chain.Count;i++)
            {
                var tile=boardManager.GetTileAtOffset(chain[i]);
                if (tile==null || !tile.IsActive || tile.Color!=first.Color || !seen.Add(chain[i])) return false;
                if (i>0 && !HexOffsetUtils.IsAdjacent(chain[i-1],chain[i])) return false;
            }
            return true;
        }
        private void Resolve(List<Vector3Int> chain,bool solo)
        {
            IsResolving = true;
            _colorShifts.Clear();
            bool useItems = ItemsAllowed;
            TileColor chainColor=boardManager.GetTileAtOffset(chain[0]).Color;
            var removed=new HashSet<Vector3Int>();
            var sources=new Queue<Vector3Int>();
            var activated=new HashSet<Vector3Int>();
            void Affect(Vector3Int cell)
            {
                var tile=boardManager.GetTileAtOffset(cell);
                if (tile==null || !tile.IsActive || (!useItems && tile.State!=TileState.Normal)) return;
                if (removed.Add(cell) && tile.State!=TileState.Normal) sources.Enqueue(cell);
            }
            foreach (var cell in chain) Affect(cell);
            while (sources.Count>0)
            {
                var cell=sources.Dequeue();
                if (!activated.Add(cell)) continue;
                var tile=boardManager.GetTileAtOffset(cell);
                int level=tile.State==TileState.ItemLv2 ? 2 : 1;
                ItemEffectType type=tile.ItemType;
                // 기존 타일은 아직 제거/재색칠하지 않으므로 모든 연쇄가 같은 스냅샷을 본다.
                ItemEffectEvents.TriggerItemActivated(cell,type,level);
                switch(type)
                {
                    case ItemEffectType.Recovery:
                        ItemEffectEvents.TriggerRecovery(level==2 ? Balance?.healPercentLv2 ?? .3f : Balance?.healPercentLv1 ?? .1f); break;
                    case ItemEffectType.TimeFreeze:
                        ItemEffectEvents.TriggerTimeFreeze(level==2 ? Balance?.freezeDurationLv2 ?? 7f : Balance?.freezeDurationLv1 ?? 3f); break;
                    case ItemEffectType.PowerUp:
                        ItemEffectEvents.TriggerPowerUp(Balance?.GetPowerUpBonus(level) ?? (level==2 ? 1f : .5f)); break;
                    case ItemEffectType.ColorShift: _colorShifts.Enqueue(level); break;
                    case ItemEffectType.Blast:
                        int radius=level==2 ? Balance?.blastRangeLv2 ?? 2 : Balance?.blastRangeLv1 ?? 1;
                        foreach (var other in HexOffsetUtils.GetRange(cell,radius)) Affect(other); break;
                    case ItemEffectType.LineClear:
                        foreach (var other in boardManager.GetAllTileOffsets())
                            if (HexOffsetUtils.IsOnLine(cell,other,level==2)) Affect(other); break;
                }
            }
            LastRemovedCount=removed.Count;
            LastDirectCount=chain.Count;
            LastActivatedCount=activated.Count;
            foreach (var cell in removed) boardManager.RemoveTile(cell);
            int effectLevel=solo ? 0 : ChainEvaluator.GetEffectLevel(Balance,chain.Count);
            ItemEffectType newType=DataManager.Instance?.ColorEffectMapping?.GetEffectForColor(chainColor) ?? TileInstance.DefaultItemType(chainColor);
            // 기존 연쇄로 소비한 아이템을 제외한 최종 개수로 제한을 검사한다.
            bool createItem=useItems && !solo && effectLevel>0 &&
                ((Balance?.maxItemsPerType ?? 2)<=0 || boardManager.CountItemsOfType(newType)<(Balance?.maxItemsPerType ?? 2));
            ScoreManager.Instance?.AddScore(chain.Count,effectLevel,removed.Count,chainColor,solo);
            Vector3Int last=chain[chain.Count-1];
            foreach (var cell in removed)
                boardManager.SpawnTile(cell,createItem && cell==last ? chainColor : boardManager.GetRandomTileColor(),
                    createItem && cell==last ? ChainEvaluator.EffectLevelToTileState(effectLevel) : TileState.Normal);
            boardManager.EnsureRequiredColorPair();
            ContinueColorShifts();
        }
        private void ContinueColorShifts()
        {
            if (!IsPlaying && GameManager.Instance?.CurrentState!=GameState.ColorSelection) { Cancel(); return; }
            while (_colorShifts.Count>0)
            {
                if (_colorShifts.Dequeue()==1) ApplyColor((TileColor)Random.Range(0,6));
                else
                {
                    var panel=GameManager.Instance.GetComponent<ColorSelectionPanel>();
                    if (panel!=null && panel.Show(color=> { ApplyColor(color); ContinueColorShifts(); })) return;
                }
            }
            IsResolving=false;
            GameManager.Instance?.CompleteColorChoice();
        }
        private void ApplyColor(TileColor color)
        {
            boardManager.RecolorAll(color);
            ItemEffectEvents.TriggerColorShift(color);
        }
    }

    /// <summary>
    /// 아이템 효과 이벤트 집합
    /// 각 시스템(StaminaManager, BossPhaseManager 등)이 구독하여 처리
    /// </summary>
    public static class ItemEffectEvents
    {
        /// <summary>Red Recovery — 페이로드: 회복 비율 (0~1). StaminaManager가 MaxStamina 곱해 적용</summary>
        public static event System.Action<Vector3Int,ItemEffectType,int> OnItemActivated;
        public static void TriggerItemActivated(Vector3Int source,ItemEffectType type,int level) => OnItemActivated?.Invoke(source,type,level);
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

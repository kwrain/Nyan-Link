using System.Collections.Generic;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;

namespace NineMood.UI
{
    // Display strings only. Save IDs, object names, enums and transaction messages stay stable.
    public static class KoreanText
    {
        public static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            {"PLAY","게임 시작"},{"START","게임 시작"},{"EXIT","종료"},
            {"NEW GAME","새 게임"},{"CONTINUE","이어하기"},{"RETRY","다시 도전"},
            {"MAIN MENU","메인으로"},{"HOME","메인으로"},{"PAUSE","일시정지"},
            {"PAUSED","일시정지"},{"RESUME","계속하기"},{"RESTART","다시 시작"},
            {"EQUIPMENT","장비 관리"},{"CLOSE","닫기"},{"EQUIP","장착"},{"UNEQUIP","해제"},
            {"MAX LV.5","최고 5레벨"},{"EQUIPMENT  /  MAX LV.5","장비 관리  /  최고 5레벨"},
            {"NEW RUN SETUP (discard checkpoint)","새 게임 준비 (현재 진행 삭제)"},
            {"CHOOSE A COLOR","색상을 선택하세요"},{"Game and board are paused","선택하는 동안 게임과 퍼즐이 멈춥니다"},
            {"RED","빨강"},{"BLUE","파랑"},{"YELLOW","노랑"},{"PURPLE","보라"},{"ORANGE","주황"},{"CYAN","청록"},
            {"v1.0","버전 1.0"},{"HP","체력"},{"SCORE","점수"},{"BEST","최고 점수"},{"CLEAR!","클리어!"},
            {"GAME OVER","도전 종료"},{"GROGGY!","그로기!"},{"Phase 1","1단계"},
            {"Connect tiles to score!","같은 색 타일을 연결하세요!"},
            {"Clear the stage for 100 coins.\nFirst clear guarantees a Trail Hat.","스테이지를 클리어하면 코인 100개를 얻습니다.\n첫 클리어에는 여행자 모자를 반드시 얻습니다."},
            {"Select gear, then retry to use these stats.\nUpgrades: 100 / 150 / 200 / 250 coins","장비를 선택한 뒤 다시 도전하면 능력치가 적용됩니다.\n강화 비용: 코인 100 / 150 / 200 / 250개"},
            {"Saved run kept. Gear changes are locked.\nNEW RUN SETUP discards only the saved run.","진행이 저장되어 있어 장비를 변경할 수 없습니다.\n새 게임 준비를 선택하면 저장된 진행만 삭제됩니다."},
            {"SAVE UNAVAILABLE","저장 데이터를 사용할 수 없습니다"},
            {"TIME ATTACK","시간 내 순서 연결"},{"RHYTHM NOTE","리듬 노트"},{"COLOR QUOTA","색상 모으기"},{"MEMORY","순서 기억"},
        };
        static readonly Dictionary<string,string> Statuses = new Dictionary<string,string>
        {
            {"Saved","저장했습니다"},{"Checkpoint saved","진행을 저장했습니다"},
            {"Continued from last checkpoint","마지막 저장 구간에서 이어합니다"},
            {"Older checkpoint preserved. Start a new run.","이전 형식의 진행은 보존했습니다. 새 게임을 시작하세요."},
            {"Cannot inspect older checkpoints.","이전 진행을 확인할 수 없습니다. 원본은 보존됩니다."},
            {"Recovered from backup","백업에서 복구했습니다"},
            {"Recovered from backup; damaged original preserved","백업에서 복구했습니다. 손상된 원본은 보존했습니다."},
            {"Save version is unsupported; original preserved","지원하지 않는 저장 버전입니다. 원본은 보존됩니다."},
            {"Checkpoint is incompatible; original preserved","이 진행은 호환되지 않습니다. 원본은 보존됩니다."},
            {"Checkpoint boundary is incompatible; original preserved","이 저장 구간은 호환되지 않습니다. 원본은 보존됩니다."},
            {"Finish the saved run, or choose NEW RUN SETUP first.","저장된 도전을 끝내거나 새 게임 준비를 먼저 선택하세요."},
            {"Equipment was not found.","장비를 찾을 수 없습니다."},
            {"Unknown equipment preserved; cannot equip.","미등록 장비는 보존되지만 장착할 수 없습니다."},
            {"Unknown equipment preserved; cannot upgrade.","미등록 장비는 보존되지만 강화할 수 없습니다."},
            {"Equipment is already in that state.","이미 적용된 장비 상태입니다."},
            {"Maximum level is 5.","최고 레벨은 5입니다."},{"Not enough coins.","코인이 부족합니다."},
            {"Cannot change setup during play.","플레이 중에는 장비를 변경할 수 없습니다."},
            {"No saved run to discard.","삭제할 저장 진행이 없습니다."},
            {"Coin capacity exceeded. Reward remains pending.","코인 보유 한도를 넘었습니다. 보상은 대기 중입니다."},
            {"Reward already received.","이미 받은 보상입니다."},{"No pending clear.","대기 중인 클리어 보상이 없습니다."},
        };
        public static string UI(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (Labels.TryGetValue(value, out var result)) return result;
            if (value.StartsWith("UPGRADE  ")) return "강화  " + value.Substring(9);
            return value;
        }
        public static string Status(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (Statuses.TryGetValue(value, out var result)) return result;
            // Do not put raw exception/type diagnostics into player-facing text.
            return "데이터를 처리할 수 없습니다. 원본을 보존하고 다시 확인합니다.";
        }
        public static string ButtonRole(string localized)
        {
            if (localized.StartsWith("강화")) return "UPGRADE";
            foreach (var item in Labels) if (item.Value == localized) return item.Key;
            return localized;
        }
        public static string ColorName(TileColor color) => new[]{"빨강","파랑","노랑","보라","주황","청록"}[(int)color];
        public static string ItemName(ItemEffectType type) => type switch
        {
            ItemEffectType.Recovery=>"회복", ItemEffectType.TimeFreeze=>"정지", ItemEffectType.Blast=>"폭발",
            ItemEffectType.LineClear=>"라인", ItemEffectType.PowerUp=>"강화", ItemEffectType.ColorShift=>"변색", _=>"?"
        };
        public static string ActionName(RunnerState state) => state switch
        {
            RunnerState.Jump=>"점프",RunnerState.Slide=>"슬라이딩",RunnerState.Block=>"방어",RunnerState.Attack=>"공격",
            RunnerState.Stumble=>"피격",RunnerState.Invincible=>"무적",_=>""
        };
        public static string EquipmentName(EquipmentData data) => data.equipmentId switch
        {
            "prototype-hat-01"=>"여행자 모자", "prototype-outfit-01"=>"여행자 옷", "prototype-weapon-01"=>"여행자 지팡이", _=>data.equipmentName
        };
    }
}

using UnityEngine;
using NineMood.Data.Enums;

namespace NineMood.Data.Definitions
{
    /// <summary>
    /// 아이템 타일 비주얼 / 오디오 참조 데이터
    /// 수치(회복량, 지속시간 등)는 BalanceData에서 관리
    /// </summary>
    [CreateAssetMenu(fileName = "TileEffectData", menuName = "NyanLink/Data/Tile Effect", order = 2)]
    public class TileEffectData : ScriptableObject
    {
        [Header("아이템 타입")]
        [Tooltip("이 에셋이 담당하는 아이템 효과 타입 (ItemEffectType과 1:1 대응)")]
        public ItemEffectType effectType;

        [Tooltip("아이템 이름 (한글 표기)")]
        public string itemNameKr;

        [Header("아이템 타일 오버레이 스프라이트")]
        [Tooltip("Lv.1 아이템 상태 오버레이 (일반 타일 위에 표시)")]
        public Sprite overlaySpriteLv1;

        [Tooltip("Lv.2 아이템 상태 오버레이")]
        public Sprite overlaySpriteLv2;

        [Header("VFX 참조 (Polishing 단계 연결)")]
        [Tooltip("아이템 타일 생성 시 재생할 VFX 프리팹")]
        public GameObject createVfxPrefab;

        [Tooltip("아이템 발동 시 재생할 VFX 프리팹")]
        public GameObject activateVfxPrefab;
    }
}

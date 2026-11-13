using System.Collections.Generic;
using NineMood.Core;
using UnityEngine;

namespace NineMood.Runner
{
    /// <summary>
    /// 배경 패럴랙스 스크롤.
    /// 각 레이어마다 speedMultiplier를 다르게 줘서 원근감을 표현한다.
    /// 배경 오브젝트가 왼쪽 끝에 닿으면 오른쪽으로 순환(루프)된다.
    /// </summary>
    public class ParallaxController : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            public Transform target;

            [Tooltip("기준 속도 대비 배율 (0=정지, 1=동일 속도)")]
            [Range(0f, 1f)]
            public float speedMultiplier = 0.5f;

            [Tooltip("배경 너비 (루프 포인트 계산용)")]
            public float loopWidth = 20f;

            [HideInInspector] public float startX;
        }

        [Header("설정")]
        public float baseSpeed = 3f;
        public List<ParallaxLayer> layers = new List<ParallaxLayer>();

        private bool _running;

        private void Start()
        {
            foreach (var layer in layers)
            {
                if (layer.target != null)
                    layer.startX = layer.target.position.x;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!_running) return;

            foreach (var layer in layers)
            {
                if (layer.target == null) continue;

                // 레이어를 왼쪽으로 이동
                layer.target.position += Vector3.left * baseSpeed * layer.speedMultiplier * Time.deltaTime;

                // 루프: 왼쪽 끝 초과 시 오른쪽으로 재배치
                if (layer.target.position.x < layer.startX - layer.loopWidth)
                {
                    Vector3 pos = layer.target.position;
                    pos.x += layer.loopWidth;
                    layer.target.position = pos;
                }
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            _running = state == GameState.RunnerPhase || state == GameState.BossPhase;
        }
    }
}

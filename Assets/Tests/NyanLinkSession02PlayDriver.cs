#if UNITY_EDITOR
using System;
using UnityEngine;

/// <summary>Editor CLI 검사만 사용하는 임시 드라이버. Screen/Camera API를 게임 프레임 문맥에서 읽는다.</summary>
public sealed class NyanLinkSession02PlayDriver : MonoBehaviour
{
    public Action Tick;
    private void Awake() => DontDestroyOnLoad(gameObject);
    private void Update() => Tick?.Invoke();
}
#endif

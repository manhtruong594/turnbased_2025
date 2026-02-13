using System;
using UnityEngine;

/// <summary>
/// Reactive SO cho bool values. UI subscribe OnValueChanged để tự cập nhật.
/// Pattern giống IntReference.
/// </summary>
[CreateAssetMenu(fileName = "BoolReference", menuName = "TurnBasedGame/References/BoolReference")]
public class BoolReference : ScriptableObject
{
    [SerializeField] private bool _value;
    public Action<bool> OnValueChanged;

    public bool Value
    {
        get => _value;
        set
        {
            _value = value;
            OnValueChanged?.Invoke(_value);
        }
    }

    public void AddListener(Action<bool> listener) => OnValueChanged += listener;
    public void RemoveListener(Action<bool> listener) => OnValueChanged -= listener;
    public void RemoveAllListeners() => OnValueChanged = null;
}

using System;
using UnityEngine;

/// <summary>
/// Reactive SO cho string values. UI subscribe OnValueChanged để tự cập nhật.
/// Pattern giống IntReference.
/// </summary>
[CreateAssetMenu(fileName = "StringReference", menuName = "TurnBasedGame/References/StringReference")]
public class StringReference : ScriptableObject
{
    [SerializeField] private string _value;
    public Action<string> OnValueChanged;

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            OnValueChanged?.Invoke(_value);
        }
    }

    public void AddListener(Action<string> listener) => OnValueChanged += listener;
    public void RemoveListener(Action<string> listener) => OnValueChanged -= listener;
    public void RemoveAllListeners() => OnValueChanged = null;
}

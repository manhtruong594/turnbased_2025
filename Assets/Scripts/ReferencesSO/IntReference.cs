using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IntReference", menuName = "TurnBasedGame/References/IntReference")]
public class IntReference : ScriptableObject
{
    [SerializeField] private int _value;
    public Action<int> OnValueChanged;

    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            OnValueChanged?.Invoke(_value);
        }
    }

    public void AddListener(Action<int> listener)
    {
        OnValueChanged += listener;
    }

    public void RemoveListener(Action<int> listener)
    {
        OnValueChanged -= listener;
    }

    public void RemoveAllListeners()
    {
        OnValueChanged = null;
    }
}

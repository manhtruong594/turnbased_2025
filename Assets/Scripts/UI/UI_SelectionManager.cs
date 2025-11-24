using UnityEngine;
using UnityEngine.UI;

public class UI_SelectionManager : MonoBehaviour
{
    [SerializeField] private Toggle _unitsToggle;
    [SerializeField] private Toggle _spellsToggle;
    [SerializeField] private GameObject _unitsPanel;
    [SerializeField] private GameObject _spellsPanel;


    private void Start()
    {
        _unitsToggle.onValueChanged.AddListener(OnUnitsToggleChanged);
        _spellsToggle.onValueChanged.AddListener(OnSpellsToggleChanged);
    }
    private void OnUnitsToggleChanged(bool isOn)
    {
        if (isOn)
        {
            _spellsToggle.isOn = false;
            _spellsPanel.SetActive(false);
            _unitsPanel.SetActive(true);
        }
    }
    private void OnSpellsToggleChanged(bool isOn)
    {
        if (isOn)
        {
            _unitsToggle.isOn = false;
            _unitsPanel.SetActive(false);
            _spellsPanel.SetActive(true);
        }
    }
}

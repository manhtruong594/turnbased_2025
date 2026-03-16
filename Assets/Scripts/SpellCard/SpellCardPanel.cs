using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// UI Panel hiển thị danh sách Spell Card khả dụng trong trận đấu.
    /// Sinh button từ hand, cập nhật trạng thái enable/disable theo MP và action.
    /// </summary>
    public class SpellCardPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardButtonPrefab;
        [SerializeField] private IntReference _actionLefts;

        private List<SpellCardData> _spells = new();

        // private void OnEnable()
        // {
        //     if (SpellCardManager.Instance != null)
        //     {
        //         SpellCardManager.Instance.OnHandChanged += OnHandChanged;
        //         SpellCardManager.Instance.OnCardUsed += OnCardUsed;
        //     }
        //     RefreshCards();
        // }

        // private void OnDisable()
        // {
        //     if (SpellCardManager.Instance != null)
        //     {
        //         SpellCardManager.Instance.OnHandChanged -= OnHandChanged;
        //         SpellCardManager.Instance.OnCardUsed -= OnCardUsed;
        //     }
        // }

        public void Initialize(IReadOnlyList<SpellCardData> spells)
        {
            if (SpellCardManager.Instance != null)
            {
                SpellCardManager.Instance.OnHandChanged += OnHandChanged;
                SpellCardManager.Instance.OnCardUsed += OnCardUsed;
            }
            _spells = new List<SpellCardData>(spells);
            RefreshCards();
        }

        private void OnHandChanged(PlayerID player)
        {
            RefreshCards();
        }

        private void OnCardUsed(SpellCardData card, RedBjorn.ProtoTiles.Example.UnitMove target)
        {
            RefreshCards();
        }

        public void RefreshCards()
        {
            ClearButtons();

            foreach (var card in _spells)
            {
                CreateCardButton(card);
            }
            UpdateInteractable();
        }

        public void UpdateInteractable()
        {
            // foreach (var btn in _spells)
            // {
            //     bool canUse = SpellCardManager.Instance != null
            //         && SpellCardManager.Instance.CanUseCard(btn.Data, PlayerID.Player1); // TODO: dynamic player ID
            //     btn.SetInteractable(canUse);
            // }
        }

        private void CreateCardButton(SpellCardData card)
        {
            if (_cardButtonPrefab == null || _cardContainer == null) return;

            var obj = Instantiate(_cardButtonPrefab, _cardContainer);
            var btn = obj.GetComponent<SpellCardButton>();
            if (btn == null)
            {
                Destroy(obj);
                return;
            }

            btn.Bind(card, () => OnCardClicked(card));
        }

        private void OnCardClicked(SpellCardData card)
        {
            SpellCardManager.Instance?.SelectCard(card, PlayerID.Player1); // TODO: dynamic player ID
        }

        private void ClearButtons()
        {
            // foreach (var btn in _buttons)
            // {
            //     if (btn != null) Destroy(btn.gameObject);
            // }
            // _buttons.Clear();
        }
    }

    /// <summary>
    /// Button UI đại diện cho 1 spell card trong panel.
    /// Hiển thị icon, tên, MP cost, trạng thái khả dụng.
    /// </summary>
    public class SpellCardButton : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;

        public SpellCardData Data { get; private set; }

        public void Bind(SpellCardData data, System.Action onClick)
        {
            Data = data;
            // if (_nameText != null) _nameText.text = data.;
            // if (_costText != null) _costText.text = data.MpCost.ToString();
            // if (_iconImage != null && data.icon != null) _iconImage.sprite = data.icon;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
            if (_canvasGroup != null)
                _canvasGroup.alpha = interactable ? 1f : 0.5f;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatSlotUI : MonoBehaviour
{
    [Header("Привязки UI")]
    public Image portraitImage;
    public TMP_Text hpText;

    [Header("Цветовые индикаторы")]
    public Color normalColor = Color.white;
    public Color activeColor = Color.yellow; // Цвет хода
    public Color targetColor = Color.red;    // Цвет цели
    public Color deadColor = Color.gray;     // Цвет смерти

    private bool _isActive = false;
    private bool _isTargeted = false;
    private bool _isDead = false;

    // Метод обновления данных юнита
    public void UpdateSlot(CombatUnit unit)
    {
        if (unit == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _isDead = unit.IsDead;

        if (_isDead)
        {
            portraitImage.color = deadColor;
            hpText.text = "МЕРТВ";
        }
        else
        {
            hpText.text = $"EP: {unit.HealthyEP + unit.TiredEP}/{unit.BattleEndurance}";
            if (portraitImage != null && unit.Progress.Template.portrait != null)
                portraitImage.sprite = unit.Progress.Template.portrait;

            // Применяем цвета при обновлении данных
            ApplyColorState();
        }
    }

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
        ApplyColorState();
    }

    public void SetTargeted(bool isTargeted)
    {
        _isTargeted = isTargeted;
        ApplyColorState(); // <-- ЭТО БЫЛО КРИТИЧЕСКИ ВАЖНО
    }

    private void ApplyColorState()
    {
        if (_isDead)
        {
            portraitImage.color = deadColor;
            return;
        }

        // Логика приоритета: если юнит цель — он красный, если ходит — желтый
        if (_isTargeted) portraitImage.color = targetColor;
        else if (_isActive) portraitImage.color = activeColor;
        else portraitImage.color = normalColor;
    }
}
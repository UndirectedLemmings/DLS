using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatSlotUI : MonoBehaviour
{
    [Header("Привязки UI")]
    public Image portraitImage;
    public TMP_Text hpText; // Если используешь TextMeshPro, замени на TMP_Text
    // public Slider hpSlider; // Раскомментируй, если захочешь добавить полоску ХП

    /// <summary>
    /// Обновляет визуал слота на основе данных бойца
    /// </summary>
    public void UpdateSlot(CombatUnit unit)
    {
        // Если бойца нет или он мертв — выключаем слот
        if (unit == null || unit.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        // Включаем слот
        gameObject.SetActive(true);

        // Ставим портрет
        if (portraitImage != null && unit.BaseData.portrait != null)
        {
            portraitImage.sprite = unit.BaseData.portrait;
        }

        // ИСПРАВЛЕНО: Выводим состояния динамического пула EP вместо старого HP
        if (hpText != null)
        {
            // В настольном дизайне круче всего показывать состояние всего пула:
            int activeEP = unit.HealthyEP + unit.TiredEP;
            hpText.text = $"EP: {activeEP}/{unit.TotalEndurance} (Ран: {unit.WoundedEP})";
        }
    }
}

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
        // 1. Проверка на Null - критически важна
        if (unit == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 2. Визуал мертвого юнита (можно просто серый фильтр, а не скрывать объект)
        if (unit.IsDead)
        {
            portraitImage.color = Color.gray; // Например, затемняем портрет
            hpText.text = "МЕРТВ";
            return;
        }

        gameObject.SetActive(true);
        portraitImage.color = Color.white; // Возвращаем цвет

        // 3. Портрет через ссылку на шаблон в прогрессе
        if (portraitImage != null && unit.Progress.Template.portrait != null)
        {
            portraitImage.sprite = unit.Progress.Template.portrait;
        }

        // 4. Вывод ресурсов
        if (hpText != null)
        {
            // Теперь используем TotalEndurance из Progress (через BattleEndurance)
            // и актуальные HealthyEP, TiredEP, WoundedEP из Progress
            hpText.text = $"EP: {unit.HealthyEP + unit.TiredEP}/{unit.BattleEndurance} (Ран: {unit.WoundedEP})";
        }
    }
}

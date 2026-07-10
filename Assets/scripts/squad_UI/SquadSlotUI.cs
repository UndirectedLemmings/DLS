using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SquadSlotUI : MonoBehaviour
{
    [Header("Визуал слота")]
    public Image portraitImage;
    public Image highlightImage; // Рамка выделения

    [Header("Текст бонусов (опционально)")]
    public TextMeshProUGUI equipmentBonusText; // Текст для показа бонусов экипировки

    [Header("Кнопки")]
    public Button mainButton; // Главная невидимая кнопка на весь слот (для перестановки)
    public Button infoButton; // Маленькая кнопка [ i ] в углу

    private int logicIndex;
    private UnitProgress currentHero;

    // Вызывается из SquadUIManager
    public void SetupSlot(UnitProgress progress, int logicIndex, bool isSelected)
    {
        currentHero = progress;
        this.logicIndex = logicIndex; // ФИКС: Теперь индекс сохраняется в поле класса!

        if (progress != null && progress.Template != null)
        {
            portraitImage.sprite = progress.Template.portrait;
            portraitImage.color = Color.white;
            infoButton.gameObject.SetActive(true); // Показываем кнопку [i], т.к. герой есть

            // Обновляем бонусы экипировки
            if (equipmentBonusText != null)
            {
                equipmentBonusText.text = progress.GetFormattedEquipmentBonuses();
            }
        }
        else
        {
            portraitImage.sprite = null;
            portraitImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Пустой слот
            infoButton.gameObject.SetActive(false); // Прячем кнопку [i], если слот пуст

            // Очищаем текст бонусов для пустого слота
            if (equipmentBonusText != null)
            {
                equipmentBonusText.text = "";
            }
        }

        if (highlightImage != null)
        {
            highlightImage.enabled = isSelected;
        }

        // Очищаем старые подписки и вешаем новые
        mainButton.onClick.RemoveAllListeners();
        infoButton.onClick.RemoveAllListeners();

        mainButton.onClick.AddListener(OnMainButtonClicked);
        infoButton.onClick.AddListener(OnInfoButtonClicked);
    }

    private void OnMainButtonClicked()
    {
        // Безопасно передаем клик в менеджер для логики Свапа
        if (SquadUIManager.Instance != null)
        {
            SquadUIManager.Instance.OnSlotClicked(logicIndex);
        }
    }

    private void OnInfoButtonClicked()
    {
        // Проверяем существование синглтона и самой панели
        if (currentHero != null && SquadUIManager.Instance != null && SquadUIManager.Instance.heroInfoPanel != null)
        {
            SquadUIManager.Instance.heroInfoPanel.OpenPanel(currentHero);
        }
        else
        {
            Debug.LogWarning($"⚠️ [SquadSlotUI] Не удалось открыть панель! Проверь, привязан ли heroInfoPanel в скрипте SquadUIManager на сцене.");
        }
    }
}
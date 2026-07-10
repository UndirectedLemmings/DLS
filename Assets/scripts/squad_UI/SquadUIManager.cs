using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SquadUIManager : MonoBehaviour
{
    public static SquadUIManager Instance { get; private set; }

    [Header("UI Элементы")]
    public Button leaderButton;
    public TMP_Text leaderName;
    public SquadSlotUI[] uiSlots = new SquadSlotUI[4];

    [Header("Связь с окном героя")]
    public HeroInfoPanel heroInfoPanel;

    private readonly int[] gridToLogicMap = new int[] { 0, 1, 2, 3 };
    private int selectedLogicIndex = -1;

    [Header("Настройки инвентаря")]
    public Transform lootPanelContainer;
    public GameObject draggableItemPrefab;

    [Header("UI Статистики")]
    public TextMeshProUGUI roundCounterText; // Перетащи сюда текст со сцены

    // Добавь этот метод или вызови его внутри PopulateRosters
    private void OnEnable()
    {
        // 1. Сразу обновляем состояние (чтобы не ждать события)
        UpdateRoundCounter();

        // 2. Подписываемся на СТАТИЧЕСКИЕ события
        // Статика не требует доступа к Instance, что надежнее
        GameManager.OnInventoryChanged += RefreshInventoryUI;
        GameManager.OnRoundChanged += UpdateRoundCounter;
    }

    private void OnDisable()
    {
        // 3. ОБЯЗАТЕЛЬНО отписываемся от статических событий
        GameManager.OnInventoryChanged -= RefreshInventoryUI;
        GameManager.OnRoundChanged -= UpdateRoundCounter;
    }

    private void UpdateRoundCounter()
    {
        // Проверка через Instance нужна только здесь, чтобы получить данные
        if (GameManager.Instance != null && roundCounterText != null)
        {
            Debug.Log($"АУ! круг обновлен: {GameManager.Instance.currentExpeditionRound}");
            roundCounterText.text = $"Круг: {GameManager.Instance.currentExpeditionRound}";
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateSquadUI();
        UpdateRoundCounter();
    }

   

    public void UpdateSquadUI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ [SquadUI] GameManager не найден на сцене!");
            return;
        }

        // --- 1. РИСУЕМ ЛИДЕРА ---
        // ИСПРАВЛЕНО: Берем прогресс лидера (Убедись, что в GameManager есть поле leaderProgress!)
        // Если у тебя там остался currentLeader с типом UnitData/HeroData, замени его в GameManager на UnitProgress
        UnitProgress leaderProgress = GameManager.Instance.leaderProgress;
        leaderName.text = leaderProgress.Template != null ? leaderProgress.Template.unitName : "Неизвестный герой";
        if (leaderProgress != null && leaderButton != null)
        {
            Image leaderImage = leaderButton.GetComponent<Image>();
            if (leaderImage != null && leaderProgress.Template != null && leaderProgress.Template.portrait != null)
            {
                leaderImage.sprite = leaderProgress.Template.portrait;
                leaderImage.color = Color.white;
            }

            leaderButton.onClick.RemoveAllListeners();
            leaderButton.onClick.AddListener(() =>
            {
                // Теперь мы передаем правильный тип - UnitProgress
                if (heroInfoPanel != null) heroInfoPanel.OpenPanel(leaderProgress);
                else Debug.LogWarning("⚠️ [SquadUI] Панель HeroInfoPanel не назначена в инспекторе!");
            });
        }

        // --- 2. РИСУЕМ ФОРМАЦИЮ ---
        if (GameManager.Instance.combatFormation == null || GameManager.Instance.combatFormation.Length == 0)
        {
            Debug.LogError("❌ [SquadUI] combatFormation пуст или не инициализирован в GameManager!");
            return;
        }

        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] == null)
            {
                Debug.LogWarning($"⚠️ [SquadUI] Слот UI под номером {i} не назначен в инспекторе!");
                continue;
            }

            int logicIndex = gridToLogicMap[i];
            UnitProgress progressInSlot = GameManager.Instance.combatFormation[logicIndex];

            if (progressInSlot != null && progressInSlot.Template == null)
                Debug.LogWarning($"⚠️ [SquadUI] В слоте {logicIndex} есть прогресс, но у него потерян шаблон данных!");

            if (progressInSlot != null && progressInSlot.Template != null && progressInSlot.Template.portrait == null)
                Debug.LogWarning($"⚠️ [SquadUI] Герой {progressInSlot.Template.unitName} находится в отряде, но у него нет картинки в ScriptableObject!");

            bool isSelected = (logicIndex == selectedLogicIndex);

            uiSlots[i].SetupSlot(progressInSlot, logicIndex, isSelected);
        }

    }

    public void OnSlotClicked(int clickedLogicIndex)
    {
        if (selectedLogicIndex == -1) selectedLogicIndex = clickedLogicIndex;
        else if (selectedLogicIndex == clickedLogicIndex) selectedLogicIndex = -1;
        else
        {
            GameManager.Instance.SwapHeroesInFormation(selectedLogicIndex, clickedLogicIndex);
            selectedLogicIndex = -1;
        }

        UpdateSquadUI();
    }

    public void RefreshInventoryUI()
    {
        if (lootPanelContainer == null)
        {
            Debug.LogError("[UI DEBUG] КРИТИЧЕСКАЯ ОШИБКА: В инспекторе не назначен lootPanelContainer (куда спавнить предметы)!");
            return;
        }
        if (draggableItemPrefab == null)
        {
            Debug.LogError("[UI DEBUG] КРИТИЧЕСКАЯ ОШИБКА: В инспекторе не назначен draggableItemPrefabСтрочка предмета)!");
            return;
        }

        int removedCount = 0;
        foreach (Transform child in lootPanelContainer)
        {
            Destroy(child.gameObject);
            removedCount++;
        }
        Debug.Log($"[UI DEBUG] 3. Очищено старых UI-элементов: {removedCount}");

        List<ItemData> currentLoot = GameManager.Instance.expeditionInventory;
        Debug.Log($"[UI DEBUG] 4. Начинаем цикл отрисовки. В GameManager сейчас предметов: {currentLoot.Count}");

        foreach (ItemData item in currentLoot)
        {
            GameObject newItem = Instantiate(draggableItemPrefab, lootPanelContainer);
            Debug.Log($"[UI DEBUG] 5. Клон префаба под предмет {item.itemName} успешно создан в UI!");

            DraggableItemUI dragScript = newItem.GetComponent<DraggableItemUI>();
            if (dragScript != null)
            {
                dragScript.Setup(item);
            }
            else
            {
                Debug.LogError($"[UI DEBUG] На префабе {draggableItemPrefab.name} отсутствует скрипт DraggableItemUI!");
            }
        }
    }
}
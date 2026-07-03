using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject combatWindow;

    [Header("Timeline Elements")]
    public Transform timelineContainer;    // Сюда кидаем панель TimelineContainer
    public GameObject timelineIconPrefab;  // Сюда кидаем префаб TimelineIconPrefab

    [Header("Слоты Арены (Перетащи со сцены)")]
    public CombatSlotUI[] heroSlots = new CombatSlotUI[4];
    public CombatSlotUI[] enemySlots = new CombatSlotUI[4];

    [Header("Лог боя")]
    public TMP_Text combatLogText; // Если используешь TextMeshPro, замени Text на TMP_Text
    private List<string> logLines = new List<string>();
    private int maxLogLines = 10; // Сколько последних строк лога показывать

    [Header("Подкрепления")]
    public Transform reinforcementContainer; // Контейнер для иконок ожидающих врагов

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (combatWindow != null) combatWindow.SetActive(false);
    }

    public void ShowCombatWindow() { combatWindow.SetActive(true); }
    public void HideCombatWindow() { combatWindow.SetActive(false); }

    // --- ЛОГ БОЯ ---
    public void AddLogMessage(string message)
    {
        if (combatLogText == null) return;

        logLines.Add(message);
        if (logLines.Count > maxLogLines)
        {
            logLines.RemoveAt(0); // Удаляем самую старую запись, если строк слишком много
        }

        // Склеиваем строки с переносом
        combatLogText.text = string.Join("\n", logLines);
    }

    public void ClearLog()
    {
        logLines.Clear();
        if (combatLogText != null) combatLogText.text = "";
    }

    // --- ПОДКРЕПЛЕНИЯ ---
    public void DrawReinforcements(Queue<UnitData> backups)
    {
        if (reinforcementContainer == null) return;

        // Очищаем старые иконки
        foreach (Transform child in reinforcementContainer)
            Destroy(child.gameObject);

        // Рисуем очередь ожидающих мобов
        foreach (UnitData unit in backups)
        {
            GameObject iconObj = Instantiate(timelineIconPrefab, reinforcementContainer);
            Image img = iconObj.GetComponent<Image>();
            if (img != null && unit.portrait != null)
            {
                img.sprite = unit.portrait;
            }
        }
    }

    // --- НОВЫЙ МЕТОД ДЛЯ ТАЙМЛАЙНА ---
    public void DrawTimeline(List<CombatUnit> queue)
    {
        // 1. Очищаем старые иконки (удаляем всех детей контейнера)
        foreach (Transform child in timelineContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Рисуем новые иконки строго в порядке очереди
        foreach (CombatUnit unit in queue)
        {
            // Игнорируем мертвых
            if (unit.IsDead) continue;

            // Создаем иконку и кладем её в контейнер
            GameObject iconObj = Instantiate(timelineIconPrefab, timelineContainer);

            // Находим компонент Image и вставляем туда портрет бойца
            Image img = iconObj.GetComponent<Image>();

            // ИСПРАВЛЕНО: копаем глубже через Progress.Template вместо старого BaseData
            if (img != null && unit.Progress != null && unit.Progress.Template != null && unit.Progress.Template.portrait != null)
            {
                img.sprite = unit.Progress.Template.portrait;
            }
        }
    }

    public void UpdateArena(CombatUnit[] heroes, CombatUnit[] enemies)
    {
        for (int i = 0; i < 4; i++)
        {
            // Обновляем героев (проверяем границы массива на всякий случай)
            if (i < heroes.Length) heroSlots[i].UpdateSlot(heroes[i]);

            // Обновляем врагов
            if (i < enemies.Length) enemySlots[i].UpdateSlot(enemies[i]);
        }
    }
}
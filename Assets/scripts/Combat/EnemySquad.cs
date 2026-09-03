// Скрипт-контейнер для отряда врагов на карте.
// Хранит список врагов, обрабатывает стакинг и теперь реагирует на клик мышью для открытия UI-меню с составом отряда.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // Нужно для проверки кликов по UI

public class EnemySquad : MonoBehaviour
{
    [Header("Текущий состав отряда под этим флагом")]
    [Tooltip("Список всех мобов, которые накопились на этой клетке встречи")]
    public List<EnemyData> accumulatedEnemies = new List<EnemyData>();

    // Инициализация флага первым заспавнившимся мобом
    public void Initialize(EnemyData firstEnemy)
    {
        accumulatedEnemies.Clear();
        AddEnemy(firstEnemy);
    }

    // Добавление моба при срабатывании стакинга дороги
    public void AddEnemy(EnemyData extraEnemy)
    {
        if (extraEnemy != null)
        {
            accumulatedEnemies.Add(extraEnemy);
            UpdateVisualIndicator();
        }
    }


   

    // Метод, который будет дергать твоя система шагов (Character_move / GridGameController),
    public void EngageCombat()
    {
        if (accumulatedEnemies.Count == 0) return;

        Debug.Log($"DLS: Герой наступил на флаг! Передаем отряд из {accumulatedEnemies.Count} мобов в CombatManager.");
        // CombatManager.Instance.StartBattleWith(accumulatedEnemies);
    }

    private void UpdateVisualIndicator()
    {
        // Тут можно обновлять текст над флагом
    }
}
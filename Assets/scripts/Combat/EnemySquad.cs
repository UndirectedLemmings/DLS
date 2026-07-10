using System.Collections.Generic;
using UnityEngine;

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
    // когда герой наступает на эту клетку карты
    public void EngageCombat()
    {
        if (accumulatedEnemies.Count == 0) return;

        Debug.Log($"DLS: Герой наступил на флаг! Передаем отряд из {accumulatedEnemies.Count} мобов в CombatManager.");

        // Пример вызова (подставь имя своего метода старта боя из CombatManager):
        // CombatManager.Instance.StartBattleWith(accumulatedEnemies);
    }

    private void UpdateVisualIndicator()
    {
        // Тултип или микро-счетчик: тут ты можешь выводить циферку количества мобов над флагом,
        // чтобы игрок на карте видел, сколько врагов слиплось в один стак: 
        // "accumulatedEnemies.Count"
    }
}
using System.Collections.Generic;
using UnityEngine;

public class EnemySquad : MonoBehaviour, IMapInteractable
{
    [Header("Данные отряда")]
    // Список всех существ, которые сейчас находятся в этом отряде (стаке)
    public List<EnemyData> squadMembers = new List<EnemyData>();


    // Реализуем метод интерфейса
    public string GetDescription()
    {
        if (squadMembers.Count == 0) return "Пустой флаг (ошибка)";

        string name = squadMembers[0].enemyName;
        int count = squadMembers.Count;

        // Возвращаем красивую строку. Знак \n делает перенос на новую строку
        return $"Отряд: {name}\nЧисленность: {count}";
    }

    /// <summary>
    /// Инициализация отряда первым существом при первичном спавне
    /// </summary>
    public void Initialize(EnemyData firstEnemy)
    {
        squadMembers.Clear();
        AddEnemy(firstEnemy);
    }

    /// <summary>
    /// Добавление нового существа в отряд (механика усиления/стакинга)
    /// </summary>
    public void AddEnemy(EnemyData newEnemy)
    {
        squadMembers.Add(newEnemy);
        UpdateVisuals();
        Debug.Log($"DLS: В отряд добавлен {newEnemy.enemyName}. Теперь в отряде {squadMembers.Count} бойцов.");
    }

    /// <summary>
    /// Обновление визуала на карте (заглушка на будущее)
    /// </summary>
    private void UpdateVisuals()
    {
        // Здесь мы будем обновлять UI над отрядом.
        // Например, менять число в TextMeshPro, чтобы игрок видел размер угрозы.

        // Как бонус для тестов: можно слегка увеличивать размер модельки, если отряд растет
        // float scale = 1f + (squadMembers.Count * 0.05f);
        // transform.localScale = new Vector3(scale, scale, 1f);
    }
}
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameDatabase", menuName = "Game Data/Global Database")]
public class GameDatabase : ScriptableObject
{
    [Header("Глобальная библиотека игры")]
    [Tooltip("Полный ростер всех фракций, существующих в проекте.")]
    public List<FactionData> availableFactions = new List<FactionData>();

    // Задел на будущее: метод, который глобальный менеджер сможет вызывать 
    // перед стартом уровня, чтобы случайно выбрать участников сессии.
    public List<FactionData> GetRandomFactions(int count)
    {
        List<FactionData> result = new List<FactionData>();
        List<FactionData> pool = new List<FactionData>(availableFactions);

        // Защита от ошибок, если просят больше фракций, чем есть в игре
        if (count > pool.Count) count = pool.Count;

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex); // Удаляем, чтобы фракции не дублировались
        }

        return result;
    }
}
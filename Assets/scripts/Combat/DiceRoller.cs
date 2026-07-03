using System.Collections.Generic;
using UnityEngine;

public static class DiceRoller
{
    /// <summary>
    /// Бросает пул d10, считает успехи и возвращает красивую строку с результатами.
    /// </summary>
    public static int RollForSuccesses(int statValue, int bonusDice, out string rollDetails)
    {
        int dicePool = statValue + bonusDice;
        int successes = 0;

        if (dicePool <= 0)
        {
            rollDetails = "[Пул: 0]";
            return 0;
        }

        List<string> rollStrings = new List<string>();
        for (int i = 0; i < dicePool; i++)
        {
            int roll = Random.Range(1, 11); // От 1 до 10

            // Успех (зеленый) если бросок <= характеристике, иначе Провал (красный)
            if (roll <= statValue)
            {
                successes++;
                rollStrings.Add($"<color=#00FF00>{roll}</color>");
            }
            else
            {
                rollStrings.Add($"<color=#FF0000>{roll}</color>");
            }
        }

        // Формируем строчку вида: [3, 8, 2]
        rollDetails = $"[{string.Join(", ", rollStrings)}]";
        return successes;
    }

    // Оставляем старый метод для совместимости с другими скриптами
    public static int RollForSuccesses(int statValue, int bonusDice = 0)
    {
        return RollForSuccesses(statValue, bonusDice, out _);
    }
}
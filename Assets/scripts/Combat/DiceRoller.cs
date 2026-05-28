using UnityEngine;

public static class DiceRoller
{
    /// <summary>
    /// Бросает пул d10 и считает успехи (бросок <= значения характеристики).
    /// </summary>
    /// <param name="statValue">Значение характеристики (определяет порог успеха и базу пула)</param>
    /// <param name="bonusDice">Дополнительные кубики от экипировки, укрытий и т.д.</param>
    /// <returns>Количество успехов</returns>
    public static int RollForSuccesses(int statValue, int bonusDice = 0)
    {
        // Общий пул кубов (База + Бонусы)
        int dicePool = statValue + bonusDice;
        int successes = 0;

        // Если из-за штрафов пул упал ниже 1, можно считать это автоматическим провалом
        if (dicePool <= 0) return 0;

        for (int i = 0; i < dicePool; i++)
        {
            // Бросаем d10 (значения от 1 до 10 включительно)
            int roll = Random.Range(1, 11);

            // Успех: если выпавшее значение МЕНЬШЕ ИЛИ РАВНО самой характеристике
            if (roll <= statValue)
            {
                successes++;
            }
        }

        return successes;
    }
}
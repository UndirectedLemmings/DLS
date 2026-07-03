using UnityEngine;

public class Building_Start : MonoBehaviour, IBuildingLogic
{
    private Vector2Int myPosition;
    private bool isFirstSpawn = true;

    // ДОБАВЛЕНО: Память о последнем круге, прямо как в Сокровищнице
    private int lastVisitedLap = -1;

    public void InitializeAt(Vector2Int cellPosition)
    {
        myPosition = cellPosition;
    }

    public void OnHeroVisit(Character_move hero)
    {
        // Берем внутренний счетчик кругов самого героя
        int currentLap = hero.Round();

        // Если мы уже засчитали этот круг — игнорируем (защита от перекрестков и долгих остановок)
        if (lastVisitedLap >= currentLap) return;

        // Запоминаем, что на этом круге старт уже отработал
        lastVisitedLap = currentLap;

        if (isFirstSpawn)
        {
            isFirstSpawn = false;
            Debug.Log($"[Building_Start] Герой начал экспедицию с клетки {myPosition}");
            return;
        }

        // Если это уже не первый спавн, и круг действительно сменился:
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteExpeditionRound();
            Debug.Log($"[Building_Start] Герой вернулся в лагерь! Круг пройден.");
        }
    }
}
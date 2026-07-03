using UnityEngine;

public class FoundationBuilding : MonoBehaviour, IBuildingLogic
{
    public Vector2Int Position { get; private set; }

    // Предохранитель, чтобы фундамент давал карту только один раз
    private bool isVisited = false;

    public void InitializeAt(Vector2Int pos)
    {
        Position = pos;
    }

    // Герой наступает на клетку
    public void OnHeroVisit(Character_move hero)
    {
        if (!isVisited)
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.GiveRandomCardFromPool();
                isVisited = true; // Отмечаем, что лут собран

                Debug.Log($"[Foundation] Герой исследовал фундамент на {Position} и получил карту!");
            }
            else
            {
                Debug.LogWarning("[Foundation] Ошибка: HandManager.Instance не найден!");
            }
        }
        else
        {
            // Убираем этот лог, если он будет слишком спамить в консоль при каждом круге
            Debug.Log($"[Foundation] Герой прошел по фундаменту {Position}, но там уже пусто.");
        }
    }
}
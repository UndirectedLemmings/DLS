using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildManager : MonoBehaviour
{
    [Header("Ссылки")]
    public HandManager handManager; // Новая связь!
    public Tilemap Map;

    // Удаляем public List<CardData> playerHand;

    private CardData activeCard; // Карта, которую игрок выбрал для стройки


    // Метод специально для системы Drag-and-Drop
    public bool TryBuildFromDrag(CardData cardToBuild, Vector3 screenMousePos)
    {
        // ВЕТВЛЕНИЕ: Проверяем тип карты
        if (cardToBuild.type == CardType.Building)
        {
            // === СТАРАЯ ЛОГИКА ДЛЯ ЗДАНИЙ ===
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenMousePos);
            mouseWorldPos.z = 0;
            Vector3Int cellPos = Map.WorldToCell(mouseWorldPos);

            if (FILL_MAP_v4.FoundationCells.Contains(cellPos))
            {
                Vector3 spawnPos = Map.GetCellCenterWorld(cellPos);
                spawnPos.z = -0.1f;
                Instantiate(cardToBuild.buildingPrefab, spawnPos, Quaternion.identity);

                FILL_MAP_v4.FoundationCells.Remove(cellPos);
                if (handManager != null) handManager.RemoveCard(cardToBuild);

                Debug.Log($"DLS: Успех! Здание {cardToBuild.cardName} построено.");
                return true;
            }

            Debug.Log("DLS: Для постройки нужен свободный фундамент!");
            return false;
        }
        else if (cardToBuild.type == CardType.Effect)
        {
            // === НОВАЯ ЛОГИКА ДЛЯ ЭФФЕКТОВ ===
            // Пока что мы разрешаем кинуть заклинание просто в любое место экрана.
            // В будущем здесь можно добавить проверку: наведен ли курсор на врага/героя.

            Debug.Log($"DLS: Разыграно заклинание: {cardToBuild.cardName}! Сила эффекта: {cardToBuild.effectPower}");

            // Удаляем карту из руки
            if (handManager != null) handManager.RemoveCard(cardToBuild);

            return true; // Разрешаем карточке сгореть
        }

        return false;
    }
}
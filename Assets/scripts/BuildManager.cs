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
        // Переводим пиксели экрана в мировые координаты Unity
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenMousePos);
        mouseWorldPos.z = 0;
        Vector3Int cellPos = Map.WorldToCell(mouseWorldPos);

        // Проверяем, есть ли под мышкой свободный фундамент
        if (FILL_MAP_v4.FoundationCells.Contains(cellPos))
        {
            // Строим
            Vector3 spawnPos = Map.GetCellCenterWorld(cellPos);
            spawnPos.z = -0.1f;
            Instantiate(cardToBuild.buildingPrefab, spawnPos, Quaternion.identity);

            // Удаляем фундамент
            FILL_MAP_v4.FoundationCells.Remove(cellPos);

            // Говорим HandManager'у вычеркнуть карту из логического списка
            if (handManager != null)
            {
                handManager.RemoveCard(cardToBuild);
            }

            Debug.Log($"DLS: Успех! Здание {cardToBuild.cardName} построено перетаскиванием.");
            return true; // Говорим карточке, что она может самоуничтожиться
        }

        Debug.Log("DLS: Здесь нельзя строить. Возвращаю карту в руку.");
        return false; // Говорим карточке лететь обратно
    }
}
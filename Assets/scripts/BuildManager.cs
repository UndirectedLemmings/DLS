using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildManager : MonoBehaviour
{
    [Header("Ссылки")]
    public HandManager handManager;
    public Tilemap Map; // Твоя оригинальная переменная

    private CardData activeCard;

    // Сохраняем твою оригинальную сигнатуру метода!
    public bool TryBuildFromDrag(CardData cardToBuild, Vector3 screenMousePos)
    {
        if (cardToBuild.type == CardType.Effect)
        {
            // === ЛОГИКА ДЛЯ ЭФФЕКТОВ ===
            Debug.Log($"DLS: Разыграно заклинание: {cardToBuild.cardName}!");
            if (handManager != null) handManager.RemoveCard(cardToBuild);
            return true;
        }
        else if (cardToBuild.type == CardType.Building)
        {
            // === ЛОГИКА ДЛЯ ЗДАНИЙ ===
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenMousePos);
            mouseWorldPos.z = 0;
            Vector3Int cellPos = Map.WorldToCell(mouseWorldPos);

            // ПРОВЕРКА НАЛИЧИЯ ФУНДАМЕНТА
            if (!FILL_MAP_v4.FoundationCells.Contains(cellPos))
            {
                Debug.Log("DLS: Для постройки нужен свободный фундамент!");
                return false; // <--- ВАЖНЫЙ RETURN: Если фундамента нет, выходим сразу
            }

            // --- НОВАЯ СИСТЕМА ТЕРРИТОРИЙ ---
            ScriptableObject cellOwner = null;
            if (FILL_MAP_v4.cellOwners != null && FILL_MAP_v4.cellOwners.ContainsKey(cellPos))
            {
                cellOwner = FILL_MAP_v4.cellOwners[cellPos];
            }

            bool canBuild = false;

            // Проверяем права на застройку
            if (cardToBuild.alignment == CardAlignment.Universal)
            {
                canBuild = true;
            }
            else if (cardToBuild.alignment == CardAlignment.Hero && cellOwner is HeroData)
            {
                canBuild = true;
            }
            else if (cardToBuild.alignment == CardAlignment.Faction && cellOwner == cardToBuild.specificOwner)
            {
                canBuild = true;
            }

            // --- ФИНАЛЬНЫЙ ВЕРДИКТ ---
            if (canBuild)
            {
                Vector3 spawnPos = Map.GetCellCenterWorld(cellPos);
                spawnPos.z = -0.1f;

                GameObject newBuilding = Instantiate(cardToBuild.buildingPrefab, spawnPos, Quaternion.identity);

                IBuildingLogic buildingLogic = newBuilding.GetComponent<IBuildingLogic>();
                if (buildingLogic != null)
                {
                    Vector2Int logicalPos = new Vector2Int(cellPos.x, cellPos.y);
                    buildingLogic.InitializeAt(logicalPos);

                    // --- НОВОЕ: Запрещаем спавн на этой клетке ---
                    GridGameController.Instance.logic.buildingsOnMap.Add(logicalPos);
                }

                FILL_MAP_v4.FoundationCells.Remove(cellPos);
                if (handManager != null) handManager.RemoveCard(cardToBuild);

                string ownerName = cellOwner != null ? cellOwner.name : "Нейтральной";
                Debug.Log($"DLS: Успех! {cardToBuild.cardName} построено на территории {ownerName}.");
                return true;
            }

            Debug.Log("DLS: Отказ! Карта не предназначена для этой территории.");
            return false; // <--- ВАЖНЫЙ RETURN: Если фундамент есть, но условия не соблюдены
        }

        return false; // <--- ОБЯЗАТЕЛЬНЫЙ RETURN: Если тип карты не Effect и не Building
    }
}
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildManager : MonoBehaviour
{
    [Header("Ссылки")]
    public HandManager handManager;
    public Tilemap Map;

    public bool TryBuildFromDrag(CardData cardToBuild, Vector3 screenMousePos)
    {
        if (cardToBuild.type == CardType.Effect)
        {
            Debug.Log($"DLS: Разыграно заклинание: {cardToBuild.cardName}!");
            if (handManager != null) handManager.RemoveCard(cardToBuild);
            return true;
        }
        else if (cardToBuild.type == CardType.Building)
        {
            // --- ИСПРАВЛЕНИЕ: Учитываем дистанцию камеры для точного прицеливания ---
            Vector3 mousePosWithZ = screenMousePos;
            // Берем расстояние от камеры до нулевой плоскости (Z=0)
            mousePosWithZ.z = Mathf.Abs(Camera.main.transform.position.z);

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePosWithZ);
            mouseWorldPos.z = 0f; // Принудительно кладем точку на землю

            Vector3Int tilePos = Map.WorldToCell(mouseWorldPos);
            Vector2Int logicalPos = (Vector2Int)tilePos;

            // --- ДЕБАГ: Выводим точные координаты, чтобы поймать баг ---
            Debug.Log($"[BuildManager] Бросок карты! Мир: {mouseWorldPos} | Попытка найти фундамент в клетке сетки: {logicalPos}");

            // Подробная проверка реестра
            if (!GridGameController.Instance.logic.buildingInstances.TryGetValue(logicalPos, out var existingBuilding))
            {
                Debug.LogWarning($"[BuildManager] Отказ: В реестре нет НИКАКИХ зданий на координатах {logicalPos}!");
                return false;
            }

            if (!(existingBuilding is FoundationBuilding))
            {
                Debug.LogWarning($"[BuildManager] Отказ: На координатах {logicalPos} есть объект, но это {existingBuilding.GetType().Name}, а не фундамент!");
                return false;
            }

            // --- БАЗОВАЯ ЛОГИКА ПОСТРОЙКИ ---
            ScriptableObject cellOwner = null;
            if (FILL_MAP_v4.cellOwners != null && FILL_MAP_v4.cellOwners.TryGetValue(logicalPos, out var owner))
            {
                cellOwner = owner;
            }

            bool canBuild = cardToBuild.alignment == CardAlignment.Universal ||
                            (cardToBuild.alignment == CardAlignment.Hero && cellOwner is HeroData) ||
                            (cardToBuild.alignment == CardAlignment.Faction && cellOwner == cardToBuild.specificOwner);

            if (canBuild)
            {
                Vector3 spawnPos = Map.GetCellCenterWorld(tilePos);
                spawnPos.z = -0.1f;

                MonoBehaviour foundMb = existingBuilding as MonoBehaviour;
                if (foundMb != null) Destroy(foundMb.gameObject);

                GameObject newBuilding = Instantiate(cardToBuild.buildingPrefab, spawnPos, Quaternion.identity);

                IBuildingLogic buildingLogic = newBuilding.GetComponent<IBuildingLogic>();
                if (buildingLogic != null)
                {
                    buildingLogic.InitializeAt(logicalPos);
                    GridGameController.Instance.logic.buildingInstances[logicalPos] = buildingLogic;
                }

                FILL_MAP_v4.FoundationCells.Remove(logicalPos);
                if (handManager != null) handManager.RemoveCard(cardToBuild);

                string ownerName = cellOwner != null ? cellOwner.name : "Нейтральной";
                Debug.Log($"DLS: Успех! {cardToBuild.cardName} построено на территории {ownerName} (Клетка: {logicalPos}).");
                return true;
            }

            Debug.Log("[BuildManager] Отказ! Карта не предназначена для этой территории.");
            return false;
        }
        return false;
    }
}
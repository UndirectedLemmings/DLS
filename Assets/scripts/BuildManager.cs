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
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenMousePos);
            mouseWorldPos.z = 0;
            Vector3Int tilePos = Map.WorldToCell(mouseWorldPos);
            Vector2Int logicalPos = (Vector2Int)tilePos;

            if (!FILL_MAP_v4.FoundationCells.Contains(logicalPos))
            {
                Debug.Log("DLS: Для постройки нужен свободный фундамент!");
                return false;
            }

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

                GameObject newBuilding = Instantiate(cardToBuild.buildingPrefab, spawnPos, Quaternion.identity);

                IBuildingLogic buildingLogic = newBuilding.GetComponent<IBuildingLogic>();
                if (buildingLogic != null)
                {
                    buildingLogic.InitializeAt(logicalPos);
                    GridGameController.Instance.logic.buildingsOnMap.Add(logicalPos);
                }

                FILL_MAP_v4.FoundationCells.Remove(logicalPos);
                if (handManager != null) handManager.RemoveCard(cardToBuild);

                string ownerName = cellOwner != null ? cellOwner.name : "Нейтральной";
                Debug.Log($"DLS: Успех! {cardToBuild.cardName} построено на территории {ownerName}.");
                return true;
            }

            Debug.Log("DLS: Отказ! Карта не предназначена для этой территории.");
            return false;
        }
        return false;
    }
}
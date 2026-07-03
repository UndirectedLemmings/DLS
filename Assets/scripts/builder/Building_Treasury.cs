using System.Collections.Generic;
using UnityEngine;

public enum VisitRequirement
{
    None,
    Gold,
    KeyItem,
    Health
}

public class Building_Treasury : MonoBehaviour, IBuildingLogic
{
    [Header("Настройки Лута")]
    [Tooltip("Пул возможных предметов для этого здания")]
    public List<ItemData> buildingLootPool;
    [Tooltip("Сколько предметов выдавать за одно посещение")]
    public int itemsToDrop = 1;

    [Header("Условия и Оплата")]
    public VisitRequirement requirementType = VisitRequirement.None;
    public int costAmount = 0;
    public ItemData requiredKey;

    [Header("Состояние")]
    private int lastVisitedLap = -1;

    private Vector2Int myPosition;
    // ИСПРАВЛЕНО: Убрали переменную isVisited, из-за которой был warning
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void InitializeAt(Vector2Int logicalPos)
    {
        myPosition = logicalPos;
    }

    public void OnHeroVisit(Character_move hero)
    {
        int currentLap = hero.Round();
        Debug.Log($"[BUILDING DEBUG] Герой наступил на клетку здания {gameObject.name} на круг №{currentLap}");

        if (lastVisitedLap == currentLap)
        {
            Debug.Log($"[BUILDING DEBUG] Отказ! Здание {gameObject.name} уже посещалось на этом круге ({currentLap}).");
            return;
        }

        if (!CanAffordVisit())
        {
            Debug.Log($"[BUILDING DEBUG] Отказ! У героя не хватает средств/ресурсов для посещения {gameObject.name}.");
            return;
        }

        if (buildingLootPool == null || buildingLootPool.Count == 0)
        {
            Debug.LogError($"[BUILDING DEBUG] КРИТИЧЕСКАЯ ОШИБКА: У префаба {gameObject.name} ПУСТОЙ список лута в инспекторе!");
            return;
        }

        PayForVisit();
        GiveLoot();

        lastVisitedLap = currentLap;

        if (spriteRenderer != null) spriteRenderer.color = Color.gray;
    }

    private bool CanAffordVisit()
    {
        switch (requirementType)
        {
            case VisitRequirement.None:
                return true;
            case VisitRequirement.Gold:
                return GameManager.Instance.Gold >= costAmount;
            case VisitRequirement.KeyItem:
                return GameManager.Instance.expeditionInventory.Contains(requiredKey);
            default:
                return true;
        }
    }

    private void PayForVisit()
    {
        switch (requirementType)
        {
            case VisitRequirement.Gold:
                GameManager.Instance.Gold -= costAmount;
                Debug.Log($"DLS: Потрачено {costAmount} золота в {gameObject.name}.");
                break;
            case VisitRequirement.KeyItem:
                GameManager.Instance.expeditionInventory.Remove(requiredKey);
                Debug.Log($"DLS: Использован предмет: {requiredKey.itemName}.");
                break;
        }
    }

    private void GiveLoot()
    {
        if (buildingLootPool == null || buildingLootPool.Count == 0) return;

        for (int i = 0; i < itemsToDrop; i++)
        {
            int randomIndex = Random.Range(0, buildingLootPool.Count);
            ItemData droppedItem = buildingLootPool[randomIndex];

            GameManager.Instance.AddLootToInventory(droppedItem);
            Debug.Log($"DLS: Найдено в {gameObject.name}: {droppedItem.itemName}");
        }
    }
}
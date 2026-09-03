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
    [Header("Настройки Лута (Предметы)")]
    [Tooltip("Пул возможных предметов для этого здания")]
    public List<ItemData> buildingLootPool;
    [Tooltip("Сколько предметов выдавать за одно посещение")]
    public int itemsToDrop = 0;

    [Header("Настройки Лута (Карты)")]
    [Tooltip("Сколько карт выдавать за одно посещение")]
    public int cardsToDrop = 0;

    [Header("Настройки Лута (Ресурсы)")]
    [Tooltip("Сколько золота выдавать за посещение")]
    public int goldToDrop = 0;
    // Если в GameManager есть другие ресурсы (Дерево, Камень и т.д.), 
    // можешь добавить их сюда по аналогии с золотом.

    [Header("Условия и Оплата")]
    public VisitRequirement requirementType = VisitRequirement.None;
    public int costAmount = 0;
    public ItemData requiredKey;

    [Header("Состояние")]
    [Tooltip("Максимальное количество использований. -1 = неограниченно")]
    public int maxUses = -1;
    private int usesCount = 0;
    private int lastVisitedLap = -1;

    private Vector2Int myPosition;
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

        // Защита от повторного срабатывания в тот же круг
        if (lastVisitedLap == currentLap)
        {
            Debug.Log($"[BUILDING DEBUG] Отказ! Здание {gameObject.name} уже посещалось на этом круге ({currentLap}).");
            return;
        }

        // Проверяем лимит использований
        if (maxUses > -1 && usesCount >= maxUses)
        {
            Debug.Log($"[BUILDING DEBUG] Отказ! Здание {gameObject.name} исчерпало количество использований ({usesCount}/{maxUses}).");
            return;
        }

        if (!CanAffordVisit())
        {
            Debug.Log($"[BUILDING DEBUG] Отказ! У героя не хватает средств/ресурсов для посещения {gameObject.name}.");
            return;
        }

        // Проверяем, настроен ли вообще какой-либо лут
        if (itemsToDrop == 0 && cardsToDrop == 0 && goldToDrop == 0)
        {
            Debug.LogWarning($"[BUILDING DEBUG] Предупреждение: У префаба {gameObject.name} не настроена выдача лута, карт или ресурсов!");
        }

        PayForVisit();
        GiveLoot();

        usesCount++;
        lastVisitedLap = currentLap;

        // Если исчерпаны использования и лимит задан, затемняем спрайт
        if (maxUses > -1 && usesCount >= maxUses)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.gray;
        }
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
        // 1. Выдача предметов
        if (itemsToDrop > 0 && buildingLootPool != null && buildingLootPool.Count > 0)
        {
            for (int i = 0; i < itemsToDrop; i++)
            {
                int randomIndex = Random.Range(0, buildingLootPool.Count);
                ItemData droppedItem = buildingLootPool[randomIndex];

                GameManager.Instance.AddLootToInventory(droppedItem);
                Debug.Log($"DLS: Найдено в {gameObject.name}: {droppedItem.itemName}");
            }
        }
        else if (itemsToDrop > 0)
        {
            Debug.LogError($"[BUILDING DEBUG] ОШИБКА: itemsToDrop > 0, но список buildingLootPool пуст у {gameObject.name}!");
        }

        // 2. Выдача карт
        if (cardsToDrop > 0)
        {
            if (HandManager.Instance != null)
            {
                for (int i = 0; i < cardsToDrop; i++)
                {
                    HandManager.Instance.GiveRandomCardFromPool();
                }
                Debug.Log($"DLS: Получено карт в {gameObject.name}: {cardsToDrop}");
            }
            else
            {
                Debug.LogWarning($"[BUILDING DEBUG] ОШИБКА: HandManager.Instance не найден, карты не выданы!");
            }
        }

        // 3. Выдача ресурсов (например, золота)
        if (goldToDrop > 0)
        {
            GameManager.Instance.Gold += goldToDrop;
            Debug.Log($"DLS: Найдено золота в {gameObject.name}: {goldToDrop}");
        }
    }
}

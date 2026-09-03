using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Building_Trial : MonoBehaviour, IBuildingLogic, IMapInteractable
{
    [Header("Описание")]
    public string trialName = "Испытание";
    [TextArea(2, 4)]
    public string trialDescription = "Опасное место, где можно рискнуть ради награды.";

    [Header("Награды")]
    public List<ItemData> rewardItems = new List<ItemData>();
    public int itemsToDrop = 0;
    public int cardsToDrop = 0;
    public int goldToDrop = 0;

    [Header("Цена входа")]
    public VisitRequirement requirementType = VisitRequirement.None;
    public int costAmount = 0;
    public ItemData requiredKey;

    [Header("Состояние")]
    [Tooltip("Максимальное количество использований. -1 = неограниченно")]
    public int maxUses = 1;
    private int usesCount = 0;
    private int lastVisitedLap = -1;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void InitializeAt(Vector2Int cellPosition)
    {
    }

    public string GetDescription()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(trialName);
        sb.AppendLine(trialDescription);

        if (requirementType == VisitRequirement.Gold)
            sb.AppendLine($"Цена: {costAmount} золота");
        else if (requirementType == VisitRequirement.KeyItem && requiredKey != null)
            sb.AppendLine($"Цена: {requiredKey.itemName}");
        else if (requirementType == VisitRequirement.Health)
            sb.AppendLine($"Цена: {costAmount} здоровья лидера");

        List<string> rewards = new List<string>();
        if (goldToDrop > 0) rewards.Add($"золото x{goldToDrop}");
        if (cardsToDrop > 0) rewards.Add($"карты x{cardsToDrop}");
        if (itemsToDrop > 0) rewards.Add($"предметы x{itemsToDrop}");
        if (rewards.Count > 0) sb.AppendLine($"Награда: {string.Join(", ", rewards)}");

        return sb.ToString().Trim();
    }

    public void OnHeroVisit(Character_move hero)
    {
        int currentLap = hero.Round();
        if (lastVisitedLap == currentLap) return;

        if (maxUses > -1 && usesCount >= maxUses)
            return;

        if (!CanAffordVisit())
        {
            Debug.Log($"[TRIAL] Не хватает ресурсов для посещения {trialName}.");
            return;
        }

        PayForVisit();
        GiveRewards();

        usesCount++;
        lastVisitedLap = currentLap;

        if (maxUses > -1 && usesCount >= maxUses && spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }
    }

    private bool CanAffordVisit()
    {
        if (GameManager.Instance == null) return false;

        switch (requirementType)
        {
            case VisitRequirement.None:
                return true;
            case VisitRequirement.Gold:
                return GameManager.Instance.Gold >= costAmount;
            case VisitRequirement.KeyItem:
                return requiredKey != null && GameManager.Instance.expeditionInventory.Contains(requiredKey);
            case VisitRequirement.Health:
                return GameManager.Instance.leaderProgress != null && GameManager.Instance.leaderProgress.currentHealthyEP > costAmount;
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
                break;
            case VisitRequirement.KeyItem:
                if (requiredKey != null)
                    GameManager.Instance.expeditionInventory.Remove(requiredKey);
                break;
            case VisitRequirement.Health:
                if (GameManager.Instance.leaderProgress != null)
                    GameManager.Instance.leaderProgress.currentHealthyEP = Mathf.Max(0, GameManager.Instance.leaderProgress.currentHealthyEP - costAmount);
                break;
        }
    }

    private void GiveRewards()
    {
        if (itemsToDrop > 0 && rewardItems != null && rewardItems.Count > 0)
        {
            for (int i = 0; i < itemsToDrop; i++)
            {
                int randomIndex = Random.Range(0, rewardItems.Count);
                GameManager.Instance.AddLootToInventory(rewardItems[randomIndex]);
            }
        }

        if (cardsToDrop > 0 && HandManager.Instance != null)
        {
            for (int i = 0; i < cardsToDrop; i++)
            {
                HandManager.Instance.GiveRandomCardFromPool();
            }
        }

        if (goldToDrop > 0)
        {
            GameManager.Instance.Gold += goldToDrop;
        }

        Debug.Log($"[TRIAL] Пройдено испытание: {trialName}");
    }
}

using UnityEngine;

public class Building_Start : MonoBehaviour, IBuildingLogic
{
    private Vector2Int myPosition;

    public void InitializeAt(Vector2Int cellPosition)
    {
        myPosition = cellPosition;
    }

    public void OnHeroVisit(Character_move hero)
    {
        // Здание больше не проверяет lapsCount героя.
        // Оно просто говорит GameManager: "Герой пришел на старт, засчитай круг"
        if (GameManager.Instance != null)
        {
            // GameManager сам решит, прибавлять ли круг, основываясь на своей логике
            GameManager.Instance.CompleteExpeditionRound();
            Debug.Log("[Building_Start] Герой вернулся в лагерь!");
        }
    }
}
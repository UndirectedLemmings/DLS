// Создаем карточку Лидера прямо в Unity
using UnityEngine;

[CreateAssetMenu(fileName = "LeaderFeat", menuName = "Combat/Feats/Leader Feat")]
public class LeaderFeatData : FeatData
{
    public int cardsToDraw = 2;

    public override void ExecuteEffect(CombatUnit owner, CombatUnit target = null)
    {
        Debug.Log($"{owner.BaseData.unitName} использует черту Лидера!");
        // Вся грязная логика спрятана прямо в самом фите:
        // HandManager.Instance.AddLeaderCards(cardsToDraw);
    }
}
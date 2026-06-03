using UnityEngine;

[CreateAssetMenu(fileName = "Feat_Commander", menuName = "Combat/Feats/Commander")]
public class CommanderFeatData : FeatData
{
    [Header("Настройки Предводителя")]
    public int cardsToGive = 2;

    public override void ExecuteEffect(CombatUnit owner, CombatUnit target = null)
    {
        // Мы уже знаем, что владелец - лидер, так как метод вызван картой экспедиции
        Debug.Log($"<color=cyan>[ПРЕДВОДИТЕЛЬ]</color> {owner.BaseData.unitName} выступает лидером экспедиции! Выдаем {cardsToGive} стартовые карты.");

        if (HandManager.Instance != null)
        {
            for (int i = 0; i < cardsToGive; i++)
            {
                HandManager.Instance.GiveRandomCardFromPool();
            }
        }
        else
        {
            Debug.LogError("<color=red>Ошибка:</color> HandManager не найден на сцене! Проверь, висит ли скрипт на объекте.");
        }
    }
}
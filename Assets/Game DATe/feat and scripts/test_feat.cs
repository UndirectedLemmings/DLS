using UnityEngine;

[CreateAssetMenu(fileName = "Feat_LeaderLogTest", menuName = "Combat/Feats/Leader Log Test")]
public class LeaderLogTestFeatData : FeatData
{
    public override void ExecuteEffect(CombatUnit owner, CombatUnit target = null)
    {
        // Проверяем, есть ли у владельца тег "Leader"
        bool isLeader = owner.featController.HasFeatWithTag("Leader");

        if (isLeader)
        {
            // Условие выполнено: юнит — лидер
            Debug.Log($"<color=magenta>[ТЕСТ ФИТА]</color> Успех! Фит существует, и его владелец {owner.BaseData.unitName} — настоящий лидер экспедиции!");
        }
        else
        {
            // Условие не выполнено (можно закомментировать этот блок, если вообще не хочешь видеть спам от не-лидеров)
            Debug.Log($"<color=gray>[ТЕСТ ФИТА]</color> Фит сработал у {owner.BaseData.unitName}, но этот юнит не является лидером. Эффект проигнорирован.");
        }
    }
}
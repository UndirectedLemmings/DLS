/*
using UnityEngine;

[CreateAssetMenu(fileName = "Feat_Commander", menuName = "Combat/Feats/Commander")]
public class CommanderFeatData : FeatData
{
    [Header("Настройки Предводителя")]
    public int cardsToGive = 2;

    // ИСПРАВЛЕНО: Используем правильный метод для старта на глобальной карте!
    public override void ExecuteAdventureStartEffect(UnitProgress overworldProgress)
    {
        string leaderName = overworldProgress != null ? overworldProgress.heroName : "Неизвестный лидер";

        Debug.Log($"<color=cyan>[ПРЕДВОДИТЕЛЬ]</color> {leaderName} выступает лидером экспедиции! Выдаем {cardsToGive} стартовые карты.");

        if (HandManager.Instance != null)
        {
            for (int i = 0; i < cardsToGive; i++)
            {
                HandManager.Instance.GiveRandomCardFromPool();
            }
        }
        else
        {
            Debug.LogError("<color=red>Ошибка:</color> HandManager не найден на сцене! Проверь, включен ли UI и висит ли скрипт на объекте.");
        }
    }
}
*/
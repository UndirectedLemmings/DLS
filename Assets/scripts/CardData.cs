using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Game Data/Card")]
public class CardData : ScriptableObject
{
    [Header("Описание карты")]
    public string cardName = "Новая постройка";
    [TextArea(3, 5)] // Делает поле в инспекторе большим и удобным
    public string description = "Описание эффекта здания...";

    [Header("Логика")]
    public GameObject buildingPrefab;
}
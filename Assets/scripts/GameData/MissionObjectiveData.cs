using UnityEngine;

public enum ObjectiveType
{
    KillEnemies,  // Убить N врагов
    CollectGold,  // Собрать N золота
    ExploreNodes  // Посетить N построек/событий
}

[CreateAssetMenu(fileName = "NewMission", menuName = "Game Data/Mission Objective")]
public class MissionObjectiveData : ScriptableObject
{
    [Header("Описание миссии")]
    public string missionName = "Зачистка";
    [TextArea] public string description = "Убейте врагов, чтобы обезопасить сектор.";

    [Header("Условия")]
    public ObjectiveType type;
    public int targetValue = 5; // Сколько нужно убить/собрать
}
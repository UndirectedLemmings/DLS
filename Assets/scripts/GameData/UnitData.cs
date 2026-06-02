using System.Collections.Generic;
using UnityEngine;

public enum UnitSide { Hero, Enemy, Neutral }

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Game Data/Unit")]
public class UnitData : ScriptableObject
{
    [Header("Базовая информация")]
    public string unitName;
    public Sprite portrait;
    public UnitSide defaultSide = UnitSide.Enemy;

    [Header("Базовые характеристики (1-10)")]
    [Range(1, 10)] public int strength = 1;     // Сила
    [Range(1, 10)] public int endurance = 1;    // Выносливость
    [Range(1, 10)] public int will = 1;         // Воля
    [Range(1, 10)] public int wisdom = 1;       // Мудрость
    [Range(1, 10)] public int agility = 1;      // Ловкость (предполагаю, она есть для уклонения)
    [Range(1, 10)] public int perception = 1;   // Восприятие
                                                

    [HideInInspector] public int strengthXP = 0;
    [HideInInspector] public int enduranceXP = 0;
    [HideInInspector] public int willXP = 0;
    [HideInInspector] public int wisdomXP = 0;
    [HideInInspector] public int agilityXP = 0;
    [HideInInspector] public int perceptionXP = 0;


    [Header("Производные параметры")]
    public int maxHP = 10;
    public int armorValue = 0;

    [Header("Экипировка (Слоты)")]
    // Пока оставим их как GameObject или строки для примера, 
    // позже создадим отдельный класс ItemData
    public ScriptableObject weaponSlot;
    public ScriptableObject armorSlot;
    public ScriptableObject accessorySlot;

    [Header("Таланты и Особенности")]
    // Список всех активных фитов существа (можно пополнять прямо во время игры)
    public List<FeatData> activeFeats = new List<FeatData>();


}
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Class_New", menuName = "Combat/Feat/Class Configuration")]
public class ClassFeatData : FeatData
{
    [Header("⚙️ Настройки Визуализации Класса")]
    [Tooltip("RuleTile-тайлы окружения (фон вокруг дорог), которые принесет этот класс, если персонаж — лидер.")]
    public RuleTile[] classTerritoryVoidTiles;

    [Tooltip("RuleTile-тайлы самой центральной дороги героя.")]
    public RuleTile[] classTerritoryRoadTiles;

    [Header("🃏 Стартовая колода класса")]
    [Tooltip("Уникальные карты, которые добавляются ТОЛЬКО если герой выбран Лидером экспедиции.")]
    public List<CardData> LiderDeck = new List<CardData>();

    [Tooltip("Базовые карты класса, которые добавляются всегда (и Лидеру, и обычным спутникам).")]
    public List<CardData> HeroDeck = new List<CardData>();

    public override void ExecuteAdventureStartEffect(UnitProgress overworldProgress)
    {
        if (overworldProgress == null) return;

        // Определяем, является ли этот персонаж Лидером отряда
        bool isLeader = false;
        if (GameManager.Instance != null && GameManager.Instance.combatFormation.Length > 0)
        {
            isLeader = (GameManager.Instance.combatFormation[0] == overworldProgress);
        }

        // ==========================================================
        // --- 1. ЛОГИКА ДЛЯ ЛИДЕРА (ДОБАВЛЕНИЕ ЛИДЕРСКОЙ КОЛОДЫ)
        // ==========================================================
        if (isLeader)
        {
            if (LiderDeck != null && LiderDeck.Count > 0 && GameManager.Instance != null)
            {
                foreach (CardData card in LiderDeck)
                {
                    if (card != null) GameManager.Instance.sessionCardPool.Add(card);
                }
                Debug.Log($"[КЛАСС-ЛИДЕР] {overworldProgress.heroName} добавил {LiderDeck.Count} эксклюзивных карт Лидера в пул сессии.");
            }
        }

        // ==========================================================
        // --- 2. БАЗОВАЯ КОЛОДА (ДЛЯ ВСЕХ: И ЛИДЕРА, И СПУТНИКОВ)
        // ==========================================================
        if (HeroDeck != null && HeroDeck.Count > 0 && GameManager.Instance != null)
        {
            foreach (CardData card in HeroDeck)
            {
                if (card != null) GameManager.Instance.sessionCardPool.Add(card);
            }
            Debug.Log($"[КЛАСС-БАЗА] {overworldProgress.heroName} добавил {HeroDeck.Count} базовых карт класса в пул сессии.");
        }
    }
}
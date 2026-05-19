using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public GameObject enemyPrefab; // Префаб, который появится на карте

    // В будущем сюда добавим статы, например:
    // public int health;
    // public int damage;
}
using System.Collections.Generic;
using UnityEngine;

public class CoordinateSwitcher : MonoBehaviour
{
    // Списки, которые заполнит генератор
    public List<Vector3Int> pathA = new List<Vector3Int>();
    public List<Vector3Int> pathB = new List<Vector3Int>();

    private bool usePathA = true;

    // Метод для юнита: "дай мне текущий путь"
    public List<Vector3Int> GetActivePath() => usePathA ? pathA : pathB;

    private void OnMouseDown()
    {
        usePathA = !usePathA;
        Debug.Log("Направление изменено! Текущий путь: " + (usePathA ? "A" : "B"));

        // Здесь можно добавить визуальный эффект (например, поворот стрелки)
    }

    // Измени тип возвращаемого значения с void на List<Vector3Int>
}
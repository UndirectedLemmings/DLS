using System.Collections.Generic;
using UnityEngine;

public class CoordinateSwitcher : MonoBehaviour
{
    public List<Vector2Int> pathA = new List<Vector2Int>();
    public List<Vector2Int> pathB = new List<Vector2Int>();
    private bool usePathA = true;

    public List<Vector2Int> GetActivePath() => usePathA ? pathA : pathB;

    private void OnMouseDown()
    {
        usePathA = !usePathA;
        Debug.Log("Направление изменено! Текущий путь: " + (usePathA ? "A" : "B"));
    }
}
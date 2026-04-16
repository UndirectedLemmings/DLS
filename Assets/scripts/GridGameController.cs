using UnityEngine;
using UnityEngine.Tilemaps;

public class GridGameController : MonoBehaviour
{
    public Tilemap tilemap;
    public int width = 50;
    public int height = 50;
    public Vector3Int originCell = Vector3Int.zero;
    private LogicalGrid logic;

    void Awake()
    {
        logic = new LogicalGrid(width, height);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(world);

            Vector2Int lp = new Vector2Int(cell.x - originCell.x, cell.y - originCell.y);

            if (logic.InBounds(lp))
                Debug.Log($"Логическая клетка: {lp.x},{lp.y}");
            else
                Debug.Log("Вне игровой зоны");
        }
    }
}
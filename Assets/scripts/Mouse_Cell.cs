using UnityEngine;

public class TestGrid : MonoBehaviour
{
    private Grid grid;
    private void Awake()
    {
        grid = this.GetComponent<Grid>();
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = grid.WorldToCell(mousePos);

            Debug.Log($"Клик по ячейке {cellPos}");
        }
    }
} // короче что-то для координат и мышки
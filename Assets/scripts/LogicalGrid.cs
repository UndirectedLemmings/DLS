using UnityEngine;

public class LogicalGrid
{
    public readonly int width;
    public readonly int height;

    public LogicalGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public bool InBounds(Vector2Int p)
        => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
}

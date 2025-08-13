using UnityEngine;

public class BlockGridRegister : MonoBehaviour
{
    void Start()
    {
        RegisterInGrid();
    }

    public void RegisterInGrid()
    {
        Vector2Int gridPos = Vector2Int.RoundToInt(transform.position); // World Position direkt verwenden
        if (Field.IsInsideGrid(gridPos))
        {
            Field.grid[gridPos.x, gridPos.y] = transform;
        }
    }

    public void UnregisterFromGrid()
    {
        Vector2Int gridPos = Vector2Int.RoundToInt(transform.position); // World Position direkt verwenden
        if (Field.IsInsideGrid(gridPos) && Field.grid[gridPos.x, gridPos.y] == transform)
        {
            Field.grid[gridPos.x, gridPos.y] = null;
        }
    }
}
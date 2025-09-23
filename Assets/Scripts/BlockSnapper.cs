using UnityEngine;

public class BlockSnapper : MonoBehaviour
{
    /*
    [Header("Grid Einstellungen")]
    public int gridWidth = 10;
    public int gridHeight = 20;
    public float cellSize = 1f;
    public Vector2 gridOrigin = Vector2.zero; // linke untere Ecke in Weltkoordinaten

    // Speichert, ob eine Zelle belegt ist
    private bool[,] grid;*/

    public Vector2 gridOrigin = Vector2.zero;
    public static BlockSnapper Instance;

    private void Awake()
    {
        Instance = this;
        //grid = new bool[gridWidth, gridHeight];
    }


    /// <summary>
    /// Versucht den Block an die nächste freie Grid-Position zu setzen.
    /// pivotChildIndex ist der Index des Childs, das als Referenzzelle dient.
    /// </summary>
    public bool TrySnapBlockToNearestFreeCell(Transform block, int pivotChildIndex)
    {
        // Pivot-Offset berechnen
        if (pivotChildIndex < 0 || pivotChildIndex >= block.childCount)
        {
            Debug.LogError("PivotChildIndex ist ungültig.");
            return false;
        }

        Transform pivotChild = block.GetChild(pivotChildIndex);
        Vector3 pivotOffset = pivotChild.position - block.position;

        // Alle möglichen Grid-Positionen testen (einfaches Beispiel: von unten nach oben)
        for (int y = 0; y < Grid.Instance.height; y++)
        {
            for (int x = 0; x < Grid.Instance.width; x++)
            {
                if (IsPositionFree(block, x, y, pivotOffset))
                {
                    // Block setzen
                    Vector3 targetWorldPos = GridToWorld(new Vector2Int(x, y));
                    block.position = targetWorldPos - pivotOffset;

                    // Grid-Zellen als belegt markieren
                    MarkCells(block, true);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Prüft, ob alle Zellen, die der Block belegen würde, frei sind.
    /// </summary>
    private bool IsPositionFree(Transform block, int gridX, int gridY, Vector3 pivotOffset)
    {
        Vector3 testPos = GridToWorld(new Vector2Int(gridX, gridY)) - pivotOffset;

        foreach (Transform child in block)
        {
            // Auch hier Rotation beachten!
            Vector3 childWorldPos = testPos + block.rotation * child.localPosition;
            Vector2Int cell = WorldToGrid(childWorldPos);

            if (!IsInsideGrid(cell)) return false;
            if (Grid.Instance.grid[cell.x, cell.y]) return false;
        }
        return true;
    }

    /// <summary>
    /// Markiert die Zellen im Grid (belegt oder frei).
    /// </summary>
    public void MarkCells(Transform block, bool occupied)
    {
        foreach (Transform child in block)
        {
            // Berücksichtige die Rotation des Parents!
            Vector3 childWorldPos = block.position + block.rotation * child.localPosition;
            Vector2Int cell = WorldToGrid(childWorldPos);
            if (IsInsideGrid(cell))
            {
                Grid.Instance.grid[cell.x, cell.y] = occupied;
            }
        }
    }

    /// <summary>
    /// Wandelt Grid-Koordinaten in Weltposition um.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridOrigin.x + gridPos.x * Grid.Instance.cellSize,
            gridOrigin.y + gridPos.y * Grid.Instance.cellSize,
            0f
        );
    }

    /// <summary>
    /// Wandelt Weltposition in Grid-Koordinaten um.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - gridOrigin.x) / Grid.Instance.cellSize);
        int y = Mathf.RoundToInt((worldPos.y - gridOrigin.y) / Grid.Instance.cellSize);
        return new Vector2Int(x, y);
    }

    public bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < Grid.Instance.width &&
               cell.y >= 0 && cell.y < Grid.Instance.height;
    }

    // Add this method to your BlockSnapper class

    public void TryMarkCells(Transform blockTransform, bool mark)
    {
        // Implement logic to mark or unmark grid cells based on the block's transform.
        // This is a placeholder implementation.
        Debug.Log($"TryMarkCells called with mark={mark} on {blockTransform.name}");
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return true; // Zellen außerhalb gelten als belegt
        return Grid.Instance.grid[cell.x, cell.y];
    }

    public void SetCellOccupied(Vector2Int cell, bool occupied)
    {
        if (IsInsideGrid(cell))
            Grid.Instance.grid[cell.x, cell.y] = occupied;
    }

    public void ResetGrid()
    {
        Grid.Instance.grid = new bool[Grid.Instance.width, Grid.Instance.height];
    }
}
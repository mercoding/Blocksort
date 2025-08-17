using UnityEngine;

[ExecuteAlways]
public class GridDebugDrawerUnique : MonoBehaviour
{
    public Color gridColor = Color.gray;
    public Color filledColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    void OnDrawGizmos()
    {
        if (BlockSnapper.Instance == null) return;
        var snapper = BlockSnapper.Instance;

        for (int x = 0; x < Grid.Instance.width; x++)
        {
            for (int y = 0; y < Grid.Instance.height; y++)
            {
                // Hole die linke untere Ecke der Zelle
                Vector3 cellOrigin = Grid.Instance.gridOrigin + new Vector2(x * Grid.Instance.cellSize, y * Grid.Instance.cellSize);
                Vector3 size = new Vector3(Grid.Instance.cellSize, Grid.Instance.cellSize, 0.01f);
                Vector3 center = cellOrigin + new Vector3(Grid.Instance.cellSize, Grid.Instance.cellSize, 0) * 0.5f;

                // Fülle belegte Zellen
                if (snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    Gizmos.color = filledColor;
                    Gizmos.DrawCube(center, size * 0.95f);
                }

                // Zeichne Grid-Linien
                Gizmos.color = gridColor;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
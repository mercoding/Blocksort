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

        for (int x = 0; x < snapper.gridWidth; x++)
        {
            for (int y = 0; y < snapper.gridHeight; y++)
            {
                // Hole die linke untere Ecke der Zelle
                Vector3 cellOrigin = snapper.gridOrigin + new Vector2(x * snapper.cellSize, y * snapper.cellSize);
                Vector3 size = new Vector3(snapper.cellSize, snapper.cellSize, 0.01f);
                Vector3 center = cellOrigin + new Vector3(snapper.cellSize, snapper.cellSize, 0) * 0.5f;

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
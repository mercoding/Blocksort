using UnityEngine;

public class GridDebugDrawer : MonoBehaviour
{
    public int width = 10;
    public int height = 20;
    public float cellSize = 1f;
    public Vector2 gridOrigin = Vector2.zero;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;

        // Vertikale Linien
        for (int x = 0; x <= width; x++)
        {
            Vector3 start = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y, 0);
            Vector3 end = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y + height * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        // Horizontale Linien
        for (int y = 0; y <= height; y++)
        {
            Vector3 start = new Vector3(gridOrigin.x, gridOrigin.y + y * cellSize, 0);
            Vector3 end = new Vector3(gridOrigin.x + width * cellSize, gridOrigin.y + y * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}

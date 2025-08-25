using UnityEngine;
using System.Collections.Generic;

public enum TetrisShapeType
{
    I, O, T, L, J, S, Z
}

public class BlockGroupGrid : MonoBehaviour
{
    public TetrisShapeType shapeType;
    public bool[,] grid;


    private void Start()
    {
        // Shape auswählen und Grid setzen
        switch (shapeType)
        {
            case TetrisShapeType.I: grid = (bool[,])TetrisShapes.I.Clone(); break;
            case TetrisShapeType.O: grid = (bool[,])TetrisShapes.O.Clone(); break;
            case TetrisShapeType.T: grid = (bool[,])TetrisShapes.T.Clone(); break;
            case TetrisShapeType.L: grid = (bool[,])TetrisShapes.L.Clone(); break;
            case TetrisShapeType.J: grid = (bool[,])TetrisShapes.J.Clone(); break;
            case TetrisShapeType.S: grid = (bool[,])TetrisShapes.S.Clone(); break;
            case TetrisShapeType.Z: grid = (bool[,])TetrisShapes.Z.Clone(); break;
        }
    }

    public void SetBlock(int x, int y, bool value)
    {
        if (grid == null) return;
        grid[x, y] = value;
    }

    public bool GetBlock(int x, int y)
    {
        if (grid == null) return false;
        return grid[x, y];
    }

    public void UnregisterChild(Transform child)
    {
        if (grid == null) return;

        // Lokale Position OHNE Rotation berechnen
        Vector3 worldPos = child.position;
        Vector3 localPos = Quaternion.Inverse(transform.rotation) * (worldPos - transform.position);

        int x = Mathf.RoundToInt(localPos.x);
        int y = Mathf.RoundToInt(localPos.y);

        if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
        {
            grid[x, y] = false;
        }
    }

    public void RegisterChild(Transform child)
    {
        if (grid == null) return;

        Vector3 worldPos = child.position;
        Vector3 localPos = Quaternion.Inverse(transform.rotation) * (worldPos - transform.position);

        int x = Mathf.RoundToInt(localPos.x);
        int y = Mathf.RoundToInt(localPos.y);

        if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
            grid[x, y] = true;
    }

    public bool AreBlocksConnected()
    {
        if (grid == null) return true;

        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        // Finde einen Startpunkt (erstes belegtes Feld)
        Vector2Int? start = null;
        int totalBlocks = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y])
                {
                    if (!start.HasValue)
                        start = new Vector2Int(x, y);
                    totalBlocks++;
                }
            }
        }
        if (!start.HasValue) return true; // Kein Block vorhanden → gilt als verbunden

        // Flood-Fill (BFS)
        var visited = new bool[width, height];
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start.Value);
        visited[start.Value.x, start.Value.y] = true;
        int connected = 1;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            for (int dir = 0; dir < 4; dir++)
            {
                int nx = pos.x + dx[dir];
                int ny = pos.y + dy[dir];
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (grid[nx, ny] && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                        connected++;
                    }
                }
            }
        }

        // Sind alle belegten Felder verbunden?
        return connected == totalBlocks;
    }
    // Other methods for managing the grid would go here...


}
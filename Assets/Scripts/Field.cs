using UnityEngine;
using System.Collections.Generic;

public class Field : MonoBehaviour
{
    public static Field Instance;
    public static int width = 10;
    public static int height = 10; // sichtbar: 0–19, buffer: 20–39, vorrat: 40–59
    public static Transform[,] movableGrid = new Transform[10, 10];
    public static Transform[,] grid = new Transform[10, 10];
    public GameObject gridSquarePrefab; // Im Inspector zuweisen!



    public static int visibleStartY = 0;
    public static int visibleEndY = 9;

    public static int bufferStartY = 20;
    public static int bufferEndY = 39;

    public static int reserveStartY = 40;
    public int reserveEndY = 9;
    public static bool checkCells = false; // Reserviert für zukünftige Erweiterungen


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateVisualGrid();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static bool IsInsideGrid(Vector2 pos)
    {
        return (pos.x >= 0 &&
                pos.x < width &&
                pos.y >= 0 &&
                pos.y < height);
    }

    public static bool IsInsideGrid(Transform piece)
    {
        foreach (Transform block in piece)
        {
            Vector2 pos = RoundToGrid(block.position);
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
                return false;
        }
        return true;
    }

    public static bool IsAnyChildOccupied(Transform piece)
    {
        foreach (Transform block in piece)
        {
            Vector2 pos = RoundToGrid(block.position);
            int x = (int)pos.x;
            int y = (int)pos.y;
            if (grid[x, y] != null && IsInsideGrid(piece))
            {
                return true;
            }
        }
        return false;
    }

    public void CreateVisualGrid()
    {
        for (int y = visibleStartY; y <= visibleEndY; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject square = Instantiate(gridSquarePrefab, new Vector3(x, y, 1), Quaternion.identity, transform);
                // Optional: Sprite oder Farbe anpassen
                var sr = square.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(0.8f, 0.8f, 0.8f, 0.3f); // leicht transparent
                }
            }
        }
    }


    public static void AddToGrid(Transform piece)
    {
        foreach (Transform block in piece)
        {
            Vector2 pos = RoundToGrid(block.position);
            if (IsInsideGrid(pos))
            {
                grid[(int)pos.x, (int)pos.y] = block;
            }
        }
    }


    public static void RemoveFromGrid(Transform piece)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null && grid[x, y].parent == piece)
                {
                    grid[x, y] = null;
                }
            }
        }
    }

    public static void AddToMovableGrid(Transform piece)
    {
        foreach (Transform block in piece)
        {
            Vector2 pos = RoundToGrid(block.position);
            if (IsInsideGrid(pos))
            {
                movableGrid[(int)pos.x, (int)pos.y] = block;
            }
        }
    }


    public static void RemoveFromMovableGrid(Transform piece)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (movableGrid[x, y] != null && movableGrid[x, y].parent == piece)
                {
                    movableGrid[x, y] = null;
                }
            }
        }
    }

    public static Vector2 RoundToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x),
            Mathf.Round(pos.y)
        );
    }

    public static void SetAllBlocksTransparency(float alpha, Transform except = null)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var block = grid[x, y];
                if (block != null && (block.parent != except))
                {
                    var sr = block.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                }
            }
        }
    }

    public static void ResetAllBlocksSortingOrder()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var block = grid[x, y];
                if (block != null)
                {
                    var sr = block.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.sortingOrder = 0;

                    // Falls der Block weitere Kinder hat (z.B. bei zusammengesetzten Shapes)
                    foreach (Transform child in block)
                    {
                        var childSr = child.GetComponent<SpriteRenderer>();
                        if (childSr != null)
                            childSr.sortingOrder = 0;
                    }
                }
            }
        }
    }

    public static void ClearFullRows()
    {
        for (int y = visibleStartY; y <= visibleEndY; y++)
        {
            bool rowFull = true;
            // Prüfe, ob die Reihe voll ist
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] == null)
                {
                    rowFull = false;
                    break;
                }
            }

            if (rowFull)
            {
                // Merke dir alle Eltern, die evtl. gelöscht werden müssen
                HashSet<Transform> parentsToCheck = new HashSet<Transform>();

                // Lösche nur die Blöcke in dieser Reihe
                for (int x = 0; x < width; x++)
                {
                    var block = grid[x, y];
                    if (block != null)
                    {
                        Transform parent = block.parent;
                        Object.Destroy(block.gameObject); // löscht nur das Child
                        grid[x, y] = null;
                        if (parent != null && parent.childCount == 0)
                            Object.Destroy(parent.gameObject); // löscht Parent nur, wenn keine Childs mehr
                    }
                }

                // Lösche Elternobjekte, die keine Kinder mehr haben
                foreach (var parent in parentsToCheck)
                {
                    if (parent != null && parent.childCount == 0)
                    {
                        Object.Destroy(parent.gameObject);
                    }
                }
            }
        }
    }
}

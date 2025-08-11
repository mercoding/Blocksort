using UnityEngine;

public class Field : MonoBehaviour
{
    public static Field Instance;
    public static int width = 10;
    public static int height = 60; // sichtbar: 0–19, buffer: 20–39, vorrat: 40–59
    public static Transform[,] movableGrid = new Transform[10, 60];
    public static Transform[,] grid = new Transform[10, 60];
    public GameObject gridSquarePrefab; // Im Inspector zuweisen!



    public static int visibleStartY = 0;
    public static int visibleEndY = 19;

    public static int bufferStartY = 20;
    public static int bufferEndY = 39;

    public static int reserveStartY = 40;
    public int reserveEndY = 59;


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
}

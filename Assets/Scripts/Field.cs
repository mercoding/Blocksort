using UnityEngine;

public class Field : MonoBehaviour
{
    public static Field Instance;
    public static int width = 10;
    public static int height = 60; // sichtbar: 0–19, buffer: 20–39, vorrat: 40–59
    public static Transform[,] movableGrid = new Transform[10, 60];
    public static Transform[,] grid = new Transform[10, 60];


    public static int visibleStartY = 0;
    public static int visibleEndY = 19;

    public static int bufferStartY = 20;
    public static int bufferEndY = 39;

    public static int reserveStartY = 40;
    public int reserveEndY = 59;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

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

    public static Vector2 RoundToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x),
            Mathf.Round(pos.y)
        );
    }
}

using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    void Update()
    {
        if (transform.childCount == 0)
        {
            // Austragen aus dem Grid, falls BlockGroupGrid vorhanden
            var groupGrid = GetComponent<BlockGroupGrid>();
            if (groupGrid != null && groupGrid.grid != null)
            {
                int width = groupGrid.grid.GetLength(0);
                int height = groupGrid.grid.GetLength(1);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (groupGrid.grid[x, y])
                        {
                            BlockSnapper.Instance.SetCellOccupied(new Vector2Int(x, y), false);
                            groupGrid.grid[x, y] = false;
                        }
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}
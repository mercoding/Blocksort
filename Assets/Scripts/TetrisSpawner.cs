using UnityEngine;

public class TetrisSpawner : MonoBehaviour
{
    public GameObject[] tetrisPrefabs; // Prefabs für alle Tetris-Formen (Parent + 4 Blöcke als Childs)
    public int spawnY = 19; // Oben im Grid (bei gridHeight=20)
    public BlockSnapper snapper;

    void Start()
    {
        if (snapper == null) snapper = BlockSnapper.Instance;
        SpawnRandomTetrisBlock();
    }

    public void SpawnRandomTetrisBlock()
    {
        int spawnX = Grid.Instance.width / 2;
        GameObject prefab = tetrisPrefabs[Random.Range(0, tetrisPrefabs.Length)];

        // Zufällige Rotation (0, 90, 180, 270 Grad)
        int rotSteps = Random.Range(0, 4);
        Quaternion rotation = Quaternion.Euler(0, 0, rotSteps * 90);

        // Spawn-Position im Grid
        Vector3 spawnPos = snapper.GridToWorld(new Vector2Int(spawnX, spawnY));

        // Instanziiere das Shape
        GameObject block = Instantiate(prefab, spawnPos, rotation);

        // Prüfe, ob das Shape ins Grid passt
        if (!CanPlace(block.transform, snapper))
        {
            Destroy(block);
            Debug.Log("Game Over: Kein Platz für neues Shape!");
            return;
        }

        // Markiere belegte Zellen im Grid
        snapper.MarkCells(block.transform, true);
    }

    // Prüft, ob das Shape an seiner aktuellen Position/Rotation ins Grid passt
    bool CanPlace(Transform shape, BlockSnapper snapper)
    {
        foreach (Transform child in shape)
        {
            Vector3 childWorldPos = shape.position + shape.rotation * child.localPosition;
            Vector2Int cell = snapper.WorldToGrid(childWorldPos);
            if (!snapper.IsInsideGrid(cell) || snapper.IsCellOccupied(cell))
                return false;
        }
        return true;
    }
}
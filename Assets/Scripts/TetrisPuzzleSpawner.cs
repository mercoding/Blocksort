using UnityEngine;
using System.Collections.Generic;

public class TetrisPuzzleSpawner : MonoBehaviour
{
    public GameObject[] tetrisPrefabs; // Prefabs für alle Tetris-Formen (Parent + 4 Blöcke als Childs)
    public int shapeCount = 10;
    public BlockSnapper snapper;
    public static int nextGroupID = 1; // ID-Zähler für Blockgruppen
    public static TetrisPuzzleSpawner Instance;

    void Awake()
    {
        Instance = this;
        if (snapper == null) snapper = BlockSnapper.Instance;
        snapper.ResetGrid();
    }
    void Start()
    {
        SpawnPuzzleShapes();
    }

    public void SpawnPuzzleShapes()
    {
        int spawned = 0;
        int maxTries = 1000;
        int tries = 0;
        //int nextGroupID = 1; // ID-Zähler für Blockgruppen

        while (spawned < shapeCount && tries < maxTries)
        {
            tries++;
            GameObject prefab = tetrisPrefabs[Random.Range(0, tetrisPrefabs.Length)];
            int rotSteps = Random.Range(0, 4);
            Quaternion rotation = Quaternion.Euler(0, 0, rotSteps * 90);

            // Versuche zufällige Position im Grid
            int x = Random.Range(0, Grid.Instance.width);
            int y = Random.Range(0, Grid.Instance.height);
            Vector3 spawnPos = snapper.GridToWorld(new Vector2Int(x, y));
            GameObject block = Instantiate(prefab, spawnPos, rotation);

            if (CanPlace(block.transform, snapper))
            {
                // BlockGroupID vergeben
                var groupIDComp = block.AddComponent<BlockGroupID>();
                groupIDComp.groupID = nextGroupID;

                // Optional: Auch an alle Kinder vergeben (falls gewünscht)
                /*
                foreach (Transform child in block.transform)
                {
                    var childID = child.gameObject.GetComponent<BlockGroupID>();
                    if (childID == null)
                        childID = child.gameObject.AddComponent<BlockGroupID>();
                    childID.groupID = nextGroupID;
                }*/

                snapper.MarkCells(block.transform, true);
                spawned++;
                nextGroupID++;
            }
            else
            {
                Destroy(block);
            }
        }

        if (spawned < shapeCount)
            Debug.LogWarning("Nicht alle Shapes konnten platziert werden!");
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
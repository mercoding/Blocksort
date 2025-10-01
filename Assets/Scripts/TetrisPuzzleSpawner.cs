using UnityEngine;
using System.Collections.Generic;

public class TetrisPuzzleSpawner : MonoBehaviour
{
    public GameObject[] tetrisPrefabs; // Prefabs für alle Tetris-Formen (Parent + 4 Blöcke als Childs)
    public int shapeCount = 15;
    public BlockSnapper snapper;
    public static int nextGroupID = 1; // ID-Zähler für Blockgruppen
    public static TetrisPuzzleSpawner Instance;
    public int visibleHeight = 10; // Setze dies im Inspector oder Code passend!


    void Awake()
    {
        Instance = this;
        if (snapper == null) snapper = BlockSnapper.Instance;
        snapper.ResetGrid();
    }
    void Start()
    {
        //SpawnPuzzleShapes();
        //SpawnPuzzleShapesOnTop();
        SpawnPuzzleShapesWithGravity();
        //SpawnPuzzleShapesOnTop();

        //BlockDropper.Instance.ApplyGravityToAllBlocks();
        //BlockLineClearer.Instance.RebuildGridFromScene();

    }



    public void SpawnPuzzleShapesOnTop()
    {
        int spawned = 0;
        int maxTries = 1000;
        int tries = 0;

        int height = Grid.Instance.height;
        int width = Grid.Instance.width;

        while (spawned < shapeCount && tries < maxTries)
        {
            tries++;
            GameObject prefab = tetrisPrefabs[Random.Range(0, tetrisPrefabs.Length)];
            int rotSteps = Random.Range(0, 4);
            Quaternion rotation = Quaternion.Euler(0, 0, rotSteps * 90);

            // Spawn NUR im oberen unsichtbaren Bereich!
            int x = Random.Range(0, width);
            int y = Random.Range(visibleHeight, height); // Nur im unsichtbaren Bereich
            Vector3 spawnPos = snapper.GridToWorld(new Vector2Int(x, y));
            GameObject block = Instantiate(prefab, spawnPos, rotation);

            if (CanPlace(block.transform, snapper))
            {
                var groupIDComp = block.AddComponent<BlockGroupID>();
                groupIDComp.groupID = nextGroupID;

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

    public void SpawnPuzzleShapes()
    {
        int spawned = 0;
        int maxTries = 1000;
        int tries = 0;

        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        while (spawned < shapeCount && tries < maxTries)
        {
            tries++;
            GameObject prefab = tetrisPrefabs[Random.Range(0, tetrisPrefabs.Length)];
            int rotSteps = Random.Range(0, 4);
            Quaternion rotation = Quaternion.Euler(0, 0, rotSteps * 90);

            int x = Random.Range(0, width);
            int y = Random.Range(0, height);
            Vector3 spawnPos = snapper.GridToWorld(new Vector2Int(x, y));
            GameObject block = Instantiate(prefab, spawnPos, rotation);

            // Prüfe, ob ALLE Kinder im sichtbaren Bereich liegen
            bool allInVisible = true;
            foreach (Transform child in block.transform)
            {
                Vector3 childWorldPos = block.transform.position + block.transform.rotation * child.localPosition;
                Vector2Int cell = snapper.WorldToGrid(childWorldPos);
                if (cell.y < 0 || cell.y >= visibleHeight)
                {
                    allInVisible = false;
                    break;
                }
            }

            // Prüfe, ob durch diesen Block eine volle Reihe entstehen würde
            bool createsFullRow = false;
            if (allInVisible && CanPlace(block.transform, snapper))
            {
                // Temporär die Zellen markieren
                List<Vector2Int> occupiedCells = new List<Vector2Int>();
                foreach (Transform child in block.transform)
                {
                    Vector3 childWorldPos = block.transform.position + block.transform.rotation * child.localPosition;
                    Vector2Int cell = snapper.WorldToGrid(childWorldPos);
                    occupiedCells.Add(cell);
                }

                // Prüfe für jede betroffene Reihe
                HashSet<int> affectedRows = new HashSet<int>();
                foreach (var cell in occupiedCells)
                    affectedRows.Add(cell.y);

                foreach (int row in affectedRows)
                {
                    int count = 0;
                    for (int col = 0; col < width; col++)
                    {
                        // Zelle ist entweder schon belegt oder wird durch das neue Teil belegt
                        if (snapper.IsCellOccupied(new Vector2Int(col, row)) ||
                            occupiedCells.Contains(new Vector2Int(col, row)))
                        {
                            count++;
                        }
                    }
                    if (count == width)
                    {
                        createsFullRow = true;
                        break;
                    }
                }
            }

            if (allInVisible && CanPlace(block.transform, snapper) && !createsFullRow)
            {
                var groupIDComp = block.AddComponent<BlockGroupID>();
                groupIDComp.groupID = nextGroupID;

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


    public void SpawnPuzzleShapesWithGravity()
    {
        int spawned = 0;
        int maxTries = 2000;
        int tries = 0;

        int width = Grid.Instance.width;
        int height = Grid.Instance.height;

        // Temporäres Grid für die Simulation
        bool[,] tempGrid = new bool[width, height];

        while (spawned < shapeCount && tries < maxTries)
        {
            tries++;
            GameObject prefab = tetrisPrefabs[Random.Range(0, tetrisPrefabs.Length)];
            int rotSteps = Random.Range(0, 4);
            Quaternion rotation = Quaternion.Euler(0, 0, rotSteps * 90);

            int x = Random.Range(0, width);
            int y = height - 1; // Spawn ganz oben
            Vector3 spawnPos = snapper.GridToWorld(new Vector2Int(x, y));
            GameObject block = Instantiate(prefab, spawnPos, rotation);

            // Simuliere Gravity: Finde für das Shape die tiefste mögliche Position
            int maxDrop = height;
            List<Vector2Int> childCells = new List<Vector2Int>();
            foreach (Transform child in block.transform)
            {
                Vector3 childWorldPos = block.transform.position + block.transform.rotation * child.localPosition;
                Vector2Int cell = snapper.WorldToGrid(childWorldPos);
                childCells.Add(cell);
            }

            // Berechne für jeden Child die maximale Drop-Tiefe
            foreach (var cell in childCells)
            {
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
                {
                    maxDrop = 0;
                    break;
                }
                int drop = 0;
                for (int yTest = cell.y - 1; yTest >= 0; yTest--)
                {
                    if (cell.x < 0 || cell.x >= width || yTest < 0 || yTest >= height)
                        break;
                    if (tempGrid[cell.x, yTest])
                        break;
                    drop++;
                }
                maxDrop = Mathf.Min(maxDrop, drop);
            }

            // Verschiebe das Shape nach unten
            for (int i = 0; i < block.transform.childCount; i++)
            {
                Transform child = block.transform.GetChild(i);
                child.position += new Vector3(0, -maxDrop * Grid.Instance.cellSize, 0);
                var cell = childCells[i];
                cell.y -= maxDrop;
                childCells[i] = cell;
            }

            // Prüfe, ob alle Kinder im sichtbaren Bereich liegen und im Grid sind
            bool allInVisible = true;
            foreach (var cell in childCells)
            {
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= visibleHeight)
                {
                    allInVisible = false;
                    break;
                }
            }

            // Prüfe, ob durch diesen Block eine volle Reihe entstehen würde
            bool createsFullRow = false;
            if (allInVisible)
            {
                HashSet<int> affectedRows = new HashSet<int>();
                foreach (var cell in childCells)
                    affectedRows.Add(cell.y);

                foreach (int row in affectedRows)
                {
                    int count = 0;
                    for (int col = 0; col < width; col++)
                    {
                        if ((row >= 0 && row < height) &&
                            (tempGrid[col, row] || childCells.Contains(new Vector2Int(col, row))))
                            count++;
                    }
                    if (count == width)
                    {
                        createsFullRow = true;
                        break;
                    }
                }
            }

            // Prüfe, ob genug Platz bleibt (z.B. mindestens 2 freie Reihen im sichtbaren Bereich)
            int freeRows = 0;
            for (int yCheck = 0; yCheck < visibleHeight; yCheck++)
            {
                int count = 0;
                for (int xCheck = 0; xCheck < width; xCheck++)
                {
                    if (tempGrid[xCheck, yCheck])
                        count++;
                }
                if (count == 0)
                    freeRows++;
            }

            if (allInVisible && !createsFullRow && freeRows >= 2)
            {
                // Markiere die belegten Zellen im tempGrid
                foreach (var cell in childCells)
                {
                    if (cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height)
                        tempGrid[cell.x, cell.y] = true;
                }

                var groupIDComp = block.AddComponent<BlockGroupID>();
                groupIDComp.groupID = nextGroupID;

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

    /*
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
    /*
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
        }*/

    /*    public void SpawnPuzzleShapes()
        {
            int spawned = 0;
            int maxTries = 1000;
            int tries = 0;

            int width = Grid.Instance.width;
            int height = Grid.Instance.height;

            while (spawned < shapeCount && tries < maxTries)
            {
                tries++;
                GameObject prefab = tetrisPrefabs[Random.Range(0, tetrisPrefabs.Length)];
                int rotSteps = Random.Range(0, 4);
                Quaternion rotation = Quaternion.Euler(0, 0, rotSteps * 90);

                // ... wie gehabt ...
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                Vector3 spawnPos = snapper.GridToWorld(new Vector2Int(x, y));
                GameObject block = Instantiate(prefab, spawnPos, rotation);

                // Prüfe, ob ALLE Kinder im sichtbaren Bereich liegen
                bool allInVisible = true;
                foreach (Transform child in block.transform)
                {
                    Vector3 childWorldPos = block.transform.position + block.transform.rotation * child.localPosition;
                    Vector2Int cell = snapper.WorldToGrid(childWorldPos);
                    if (cell.y < 0 || cell.y >= visibleHeight)
                    {
                        allInVisible = false;
                        break;
                    }
                }

                if (allInVisible && CanPlace(block.transform, snapper))
                {
                    var groupIDComp = block.AddComponent<BlockGroupID>();
                    groupIDComp.groupID = nextGroupID;

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
*/
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


    public void SpawnIfUpperInvisibleGridFree(int visibleHeight)
    {
        int width = Grid.Instance.width;
        int height = Grid.Instance.height;
        int invisibleStart = visibleHeight;
        int invisibleEnd = height;
        int upperHalfStart = invisibleStart + (invisibleEnd - invisibleStart) / 2;

        // Prüfe, ob alle Zellen in der oberen Hälfte des unsichtbaren Bereichs frei sind
        bool allFree = true;
        for (int y = upperHalfStart; y < invisibleEnd; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (snapper.IsCellOccupied(new Vector2Int(x, y)))
                {
                    allFree = false;
                    break;
                }
            }
            if (!allFree) break;
        }

        if (allFree)
        {
            Debug.Log("Obere Hälfte des unsichtbaren Grids ist frei – Puzzle-Elemente werden gespawnt!");
            SpawnPuzzleShapesOnTop();
            StartCoroutine(BlockDropper.Instance.ApplyGravityToAllBlocksCoroutine(0.2f));
        }
    }
}

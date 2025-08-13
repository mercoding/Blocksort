using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShapeSpawner : MonoBehaviour
{
    public GameObject shapePrefab;
    public ShapeGroupMover.ShapeType selectedType = ShapeGroupMover.ShapeType.I;

    void Start()
    {
        // Beispiel: Erzeuge ein Shape in der Mitte des Feldes
        SpawnMultipleShapes(10);
        //SpawnPattern();
    }

    List<Vector2Int> Shuffle()
    {
        System.Random rnd = new System.Random();
        List<Vector2Int> allPositions = new List<Vector2Int>();
        for (int y = Field.visibleStartY; y <= Field.visibleEndY; y++)
            for (int x = 0; x < Field.width; x++)
                allPositions.Add(new Vector2Int(x, y));

        // Shuffle die Positionen für zufällige Verteilung
        for (int i = allPositions.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            var temp = allPositions[i];
            allPositions[i] = allPositions[j];
            allPositions[j] = temp;
        }

        return allPositions;
    }

    public void SpawnMultipleShapes(int count)
    {
        int spawned = 0;
        var shapeTypes = (ShapeGroupMover.ShapeType[])System.Enum.GetValues(typeof(ShapeGroupMover.ShapeType));
        System.Random rnd = new System.Random();
        List<Vector2Int> allPositions = Shuffle();

        foreach (var pos in allPositions)
        {
            var type = shapeTypes[rnd.Next(shapeTypes.Length)];

            for (int rot = 0; rot < 4; rot++)
            {
                Quaternion rotation = Quaternion.Euler(0, 0, rot * 90);
                List<Vector2Int> positions = GetShapePositions(type, pos, rotation);

                // Mathematische Prüfung (optional, für Performance)
                bool canPlace = true;
                foreach (var gridPos in positions)
                {
                    if (!Field.IsInsideGrid(gridPos) || Field.grid[gridPos.x, gridPos.y] != null)
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (!canPlace) continue;

                // Jetzt instanziieren und explizit die Form setzen!
                GameObject shape = Instantiate(shapePrefab, new Vector3(pos.x, pos.y, 0), rotation);
                var mover = shape.GetComponent<ShapeGroupMover>();
                mover.shapeType = type;
                mover.randomShape = false;
                mover.GenerateTetrisShape(type); // explizit die Form erzeugen

                // Jetzt die echten Block-Positionen prüfen!
                bool reallyFits = true;
                foreach (Transform block in shape.transform)
                {
                    Vector2Int gridPos = Vector2Int.RoundToInt(block.position);
                    if (!Field.IsInsideGrid(gridPos) || Field.grid[gridPos.x, gridPos.y] != null)
                    {
                        reallyFits = false;
                        break;
                    }
                }

                if (reallyFits)
                {
                    RegisterShapeInGrid(shape);
                    spawned++;
                    break;
                }
                else
                {
                    Destroy(shape);
                }
            }

            if (spawned >= count)
                return;
        }
    }

    void RegisterShapeInGrid(GameObject shape)
    {
        foreach (Transform block in shape.transform)
        {
            block.GetComponent<BlockGridRegister>()?.RegisterInGrid(); // Registriert sich im Grid
        }
    }

    void UnregisterShapeInGrid(GameObject shape)
    {
        foreach (Transform block in shape.transform)
        {
            block.GetComponent<BlockGridRegister>()?.UnregisterFromGrid(); // Entfernt sich aus dem Grid
        }
    }


    /*
        public void SpawnPattern()
        {
            var shapeTypes = (ShapeGroupMover.ShapeType[])System.Enum.GetValues(typeof(ShapeGroupMover.ShapeType));
            int spacing = 1; // Abstand zwischen Shapes

            // Beispiel: Erzeuge eine Zeile von Shapes
            for (int i = 0; i < Field.width; i += spacing)
            {
                var type = shapeTypes[i % shapeTypes.Length];
                Vector2Int pos = new Vector2Int(i, 2); // y=2 als Beispiel
                List<Vector2Int> positions = GetShapePositions(type, pos);

                if (IsValidPlacement(positions))
                {
                    SpawnShape(type, pos);
                    /*foreach (var gridPos in positions)
                    {
                        Field.grid[gridPos.x, gridPos.y] = new GameObject("TempBlock").transform;
                    }*/
    /* }
 }
}*/

    // Neue SpawnShape-Variante:
    public void SpawnShape(ShapeGroupMover.ShapeType type, Vector2Int spawnPosition, Quaternion rotation, List<Vector2Int> positions)
    {
        GameObject shape = Instantiate(shapePrefab, new Vector3(spawnPosition.x, spawnPosition.y, 0), rotation);
        var mover = shape.GetComponent<ShapeGroupMover>();
        mover.shapeType = type;
        mover.randomShape = false;
        mover.lastValidPosition = shape.transform.position;

        // Entferne ALLE vorhandenen Kinder (auch versehentliche)
        /*
        while (shape.transform.childCount > 0)
            DestroyImmediate(shape.transform.GetChild(0).gameObject);

        // Erzeuge die Blöcke neu und setze sie an die berechneten Positionen
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject block = Instantiate(mover.squarePrefab, shape.transform);
            Vector2Int localPosInt = positions[i] - spawnPosition;
            block.transform.localPosition = new Vector3(localPosInt.x, localPosInt.y, 0);
            block.GetComponent<BlockGridRegister>()?.RegisterInGrid();
        }*/
    }


    List<Vector2Int> GetShapePositions(ShapeGroupMover.ShapeType type, Vector2Int origin, Quaternion rotation)
    {
        List<Vector2Int> relPositions = ShapeGroupMover.GetRelativePositions(type);

        List<Vector2Int> absPositions = new List<Vector2Int>();
        foreach (var rel in relPositions)
            absPositions.Add(origin + Vector2Int.RoundToInt(rotation * (Vector2)rel));

        return absPositions;
    }

    // Prüft, ob das Prefab an der gewünschten Position/Rotation passt
    bool IsValidPlacement(ShapeGroupMover.ShapeType type, Vector2Int spawnPosition, Quaternion rotation)
    {
        List<Vector2Int> positions = GetShapePositions(type, spawnPosition, rotation);
        foreach (var gridPos in positions)
        {
            if (!Field.IsInsideGrid(gridPos) || Field.grid[gridPos.x, gridPos.y] != null)
            {
                return false;
            }
        }
        return true;
    }

    /*
        bool IsValidPlacement(ShapeGroupMover.ShapeType type, Vector2Int spawnPosition, Quaternion rotation)
        {
            List<Vector2Int> positions = GetShapePositions(type, spawnPosition, rotation);
            foreach (var gridPos in positions)
            {
                if (!Field.IsInsideGrid(gridPos) || Field.grid[gridPos.x, gridPos.y] != null)
                {
                    return false;
                }
            }
            return true;
        }*/

    bool HasSpaceForAnotherShape()
    {
        // Beispiel: Prüfe, ob irgendwo ein freier Bereich für ein I-Shape ist
        for (int y = 0; y < Field.height; y++)
        {
            for (int x = 0; x <= Field.width - 4; x++)
            {
                bool free = true;
                for (int i = 0; i < 4; i++)
                {
                    if (Field.grid[x + i, y] != null)
                    {
                        free = false;
                        break;
                    }
                }
                if (free) return true;
            }
        }
        return false;
    }

    // Liefert das Prefab für den jeweiligen ShapeType
    GameObject GetPrefabForType(ShapeGroupMover.ShapeType type)
    {
        // Hier kannst du eine Logik einbauen, um für jeden Typ ein anderes Prefab zu liefern.
        // Aktuell wird immer shapePrefab zurückgegeben.
        // Wenn du verschiedene Prefabs hast, kannst du z.B. ein Dictionary<ShapeType, GameObject> verwenden.
        return shapePrefab;
    }
}
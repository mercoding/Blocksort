using UnityEngine;
using System.Collections.Generic;

public class ShapeSpawner : MonoBehaviour
{
    public GameObject shapePrefab;
    public ShapeGroupMover.ShapeType selectedType = ShapeGroupMover.ShapeType.I;

    void Start()
    {
        // Beispiel: Erzeuge ein Shape in der Mitte des Feldes
        SpawnMultipleShapes(30);
        //SpawnPattern();
    }

    public void SpawnMultipleShapes(int count)
    {
        int spawned = 0;
        var shapeTypes = (ShapeGroupMover.ShapeType[])System.Enum.GetValues(typeof(ShapeGroupMover.ShapeType));
        System.Random rnd = new System.Random();

        // Erzeuge eine Liste aller möglichen Positionen im sichtbaren Bereich
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

        foreach (var pos in allPositions)
{
    var type = shapeTypes[rnd.Next(shapeTypes.Length)];
    List<Vector2Int> positions = GetShapePositions(type, pos);

    // Prüfe, ob ALLE Blöcke im Grid liegen und frei sind
    if (IsValidPlacement(positions))
    {
        SpawnShape(type, pos);
        foreach (var gridPos in positions)
            Field.grid[gridPos.x, gridPos.y] = new GameObject("TempBlock").transform;
        spawned++;
        if (spawned >= count) break;
    }
}

        if (spawned < count)
            Debug.LogWarning("Nicht genug Platz für alle Teile!");
    }



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
                foreach (var gridPos in positions)
                {
                    Field.grid[gridPos.x, gridPos.y] = new GameObject("TempBlock").transform;
                }
            }
        }
    }

    // Neue SpawnShape-Variante:
    public void SpawnShape(ShapeGroupMover.ShapeType type, Vector2Int spawnPosition)
    {
        List<Vector2Int> positions = GetShapePositions(type, spawnPosition);

        if (!IsValidPlacement(positions)) return;
        if (!HasSpaceForAnotherShape()) return;

        GameObject shape = Instantiate(shapePrefab, new Vector3(spawnPosition.x, spawnPosition.y, 0), Quaternion.identity);
        var mover = shape.GetComponent<ShapeGroupMover>();
        mover.shapeType = type;
        mover.randomShape = false;
        mover.lastValidPosition = shape.transform.position;
    }

    List<Vector2Int> GetShapePositions(ShapeGroupMover.ShapeType type, Vector2Int origin)
    {
        // Hole die relativen Positionen aus ShapeGroupMover
        var tempMover = new GameObject().AddComponent<ShapeGroupMover>();
        List<Vector2Int> relPositions = tempMover.GetShapePositions(type);
        Destroy(tempMover.gameObject);

        List<Vector2Int> absPositions = new List<Vector2Int>();
        foreach (var rel in relPositions)
            absPositions.Add(origin + rel);

        return absPositions;
    }

    bool IsValidPlacement(List<Vector2Int> positions)
{
    foreach (var pos in positions)
    {
        // Prüfe: Ist die Position im Grid und frei?
        if (!Field.IsInsideGrid(pos)) return false;
        if (Field.grid[pos.x, pos.y] != null) return false;
    }
    return true;
}

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
}
using UnityEngine;

public class PieceGhostCreator : MonoBehaviour
{
    [SerializeField] private GameObject ghostPrefab; // Halbdurchsichtiges Block-Prefab
    private GameObject ghostInstance;

    public void CreateGhost()
    {
        if (ghostInstance != null) return; // Nur einmal erzeugen

        // Ghost erzeugen
        ghostInstance = Instantiate(ghostPrefab, transform.position, transform.rotation);
        ghostInstance.name = gameObject.name + "_Ghost";

        // Position aller Kinder vom Original übernehmen
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform originalBlock = transform.GetChild(i);
            Transform ghostBlock = ghostInstance.transform.GetChild(i);

            ghostBlock.position = originalBlock.position;
            ghostBlock.rotation = originalBlock.rotation;
        }

        // Collider deaktivieren
        foreach (var col in ghostInstance.GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // Scripts deaktivieren
        foreach (var mover in ghostInstance.GetComponentsInChildren<MonoBehaviour>())
            mover.enabled = false;

        // Halbtransparenz setzen
        foreach (var sr in ghostInstance.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.GetComponent<SpriteRenderer>().sprite = ghostInstance.GetComponent<ShapeGroupMover>().shapeSprites[7];
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, 0.75f);
        }
    }

    public void RemoveGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
    }
}

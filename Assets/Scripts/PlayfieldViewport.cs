using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PlayfieldViewport : MonoBehaviour
{
    [Header("Ränder in Pixeln")]
    public int leftPx = 290;   // Platz für linke Sidebar
    public int rightPx = 290;  // Platz für rechte Sidebar
    public int topPx = 0;    // Platz für Topbar (Punktestand)
    public int bottomPx = 0;

    [Header("Ziel-Welt-Höhe des Spielfelds (Units)")]
    public float targetWorldHeight = 15f; // z. B. 20 Units für dein Tetris-Gitter

    Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        Apply();
    }

    void OnValidate() => Apply();

    void Update()
    {
        // In Edit Mode/Play bei Größenänderung (Resizing) neu anwenden
        if (!Application.isPlaying) Apply();
    }

    void Apply()
    {
        if (cam == null) return;

        float sw = Screen.width;
        float sh = Screen.height;

        // Viewport-Rect berechnen (0..1)
        float vx = leftPx / sw;
        float vy = bottomPx / sh;
        float vw = (sw - leftPx - rightPx) / sw;
        float vh = (sh - topPx - bottomPx) / sh;

        vw = Mathf.Clamp01(vw);
        vh = Mathf.Clamp01(vh);

        cam.rect = new Rect(vx, vy, vw, vh);

        // Orthographic Size so anpassen, dass targetWorldHeight innerhalb des *sichtbaren* Bereichs passt
        // In Orthographic gilt: sichtbare Welt-Höhe = 2 * orthoSize * vh (weil Rect die Kamera "beschneidet")
        // => orthoSize = targetWorldHeight / (2 * vh)
        if (vh > 0f)
        {
            cam.orthographicSize = targetWorldHeight / (2f * vh);
        }
    }
}

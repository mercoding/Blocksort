using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Global : MonoBehaviour
{
    public static Global Instance;
    public bool moved = false;
    public static bool DragLock = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (moved)
        {
            EndTurn();
            moved = false;
        }
        */
    }

    public void EndTurn()
    {

        BlockLineClearer.Instance.DropInvisibleBlocksByFreeRowsOnTop();

        //StartCoroutine(GravityAndClearParallel(0.5f, 0.2f));
        StartCoroutine(BlockDropper.Instance.GravityWithInterrupt(0.2f, 0.5f));

        TetrisPuzzleSpawner.Instance.SpawnIfUpperInvisibleGridFree(BlockLineClearer.Instance.visibleHeight);
        BlockLineClearer.Instance.RebuildGridFromScene();
    }

    public void EndTurn(BlockDragHandler lastBlock)
    {
        BlockLineClearer.Instance.DropInvisibleBlocksByFreeRowsOnTop();
        StartCoroutine(GravityAndSelectiveClearLoop(lastBlock, 0.2f));
    }

    public IEnumerator GravityAndSelectiveClearLoop(BlockDragHandler lastBlock, float gravityDelay = 0.2f)
    {
        Global.DragLock = true;
        bool somethingChanged;

        do
        {
            // 1. Gravity: Blöcke fallen lassen, bis keiner mehr fällt
            yield return StartCoroutine(BlockDropper.Instance.ApplyGravityToAllBlocksCoroutine(gravityDelay));
            BlockLineClearer.Instance.RebuildGridFromScene();

            // 2. Reihen prüfen und löschen
            somethingChanged = false;
            if (lastBlock != null)
            {
                var fullRows = BlockLineClearer.Instance.FindFullRows();
                if (fullRows.Count > 0)
                {
                    lastBlock.CheckAndClearAffectedRows(lastBlock.transform);
                    BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
                    BlockLineClearer.Instance.RebuildGridFromScene();
                    somethingChanged = true;
                }
            }

            // 3. Spawning etc.
            TetrisPuzzleSpawner.Instance.SpawnIfUpperInvisibleGridFree(BlockLineClearer.Instance.visibleHeight);

            // 4. Noch einmal alle Reihen prüfen und löschen
            var moreRows = BlockLineClearer.Instance.FindFullRows();
            if (moreRows.Count > 0)
            {
                BlockLineClearer.Instance.RemoveChildrenInRows(moreRows);
                BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
                BlockLineClearer.Instance.RebuildGridFromScene();
                somethingChanged = true;
            }

        } while (somethingChanged);

        Global.DragLock = false;
    }

    public IEnumerator GravityAndClearLoop(float animDelay = 0.5f, float gravityDelay = 0.2f)
    {
        Global.DragLock = true;
        bool somethingChanged;
        do
        {
            somethingChanged = false;

            // Gravity anwenden
            yield return StartCoroutine(BlockDropper.Instance.ApplyGravityToAllBlocksCoroutine(gravityDelay));
            BlockLineClearer.Instance.RebuildGridFromScene();

            // Volle Reihen prüfen
            var fullRows = BlockLineClearer.Instance.FindFullRows();
            if (fullRows.Count > 0)
            {
                somethingChanged = true;
                // Animierte Löschung
                yield return StartCoroutine(BlockLineClearer.Instance.RemoveFullRowBlocksAnimated(animDelay));
                BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
                BlockLineClearer.Instance.RebuildGridFromScene();
                TetrisPuzzleSpawner.Instance.SpawnIfUpperInvisibleGridFree(BlockLineClearer.Instance.visibleHeight);

                //BlockLineClearer.Instance.UpdateSingleBlockVisuals();
            }

        }
        while (somethingChanged);

        Global.DragLock = false;
    }

    public IEnumerator GravityAndClearParallel(float animDelay = 0.5f, float gravityDelay = 0.2f)
    {
        Global.DragLock = true;
        bool somethingChanged;
        do
        {
            somethingChanged = false;

            // Volle Reihen prüfen
            var fullRows = BlockLineClearer.Instance.FindFullRows();
            Coroutine clearCoroutine = null;
            if (fullRows.Count > 0)
            {
                somethingChanged = true;
                // Animierte Löschung parallel starten
                clearCoroutine = StartCoroutine(BlockLineClearer.Instance.RemoveFullRowBlocksAnimated(animDelay));
                BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
                BlockLineClearer.Instance.RebuildGridFromScene();
                BlockLineClearer.Instance.UpdateSingleBlockVisuals();
            }

            // Gravity parallel starten
            Coroutine gravityCoroutine = StartCoroutine(BlockDropper.Instance.ApplyGravityToAllBlocksCoroutine(gravityDelay));
            BlockLineClearer.Instance.RebuildGridFromScene();

            // Warten bis beide Animationen fertig sind
            if (clearCoroutine != null)
                yield return clearCoroutine;
            if (gravityCoroutine != null)
                yield return gravityCoroutine;

        }
        while (somethingChanged);

        Global.DragLock = false;
    }
}

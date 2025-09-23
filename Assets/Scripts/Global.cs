using UnityEngine;

public class Global : MonoBehaviour
{
    public static Global Instance;
    public bool moved = false;
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
        if (moved)
        {
            EndTurn();
            moved = false;
        }
    }

    public void EndTurn()
    {
        BlockLineClearer.Instance.RemoveFullRowBlocks();
        BlockLineClearer.Instance.SplitDisconnectedBlocksByGroupID();
        BlockLineClearer.Instance.RebuildGridFromScene();
        //Debug.Log("Turn Ended");
    }
}

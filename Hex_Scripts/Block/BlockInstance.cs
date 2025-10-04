using UnityEngine;

public class BlockInstance
{
    //--------------------------------------------------
    public BlockSkin Skin { get { return _skin; } set { _skin = value; } }
    private BlockSkin _skin;

    public int Life { get; set; }
    public Vector2Int Cell { get; set; }
}

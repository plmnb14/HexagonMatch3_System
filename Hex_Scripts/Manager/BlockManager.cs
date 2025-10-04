using System.Collections.Generic;
using UnityEngine;

#region Enum
public enum BlockCategory { Normal, Special, Obstacle, End }
public enum BlockDetailType 
{ 
    Normal_G, Normal_O, Normal_P, Normal_PP, Normal_R, Normal_Y,
    Specail_Bomb, Special_LineH, Special_LineV,
    Obstacle_Spin, Obstacle_JackBox,
    End
}
#endregion

public class BlockManager : Singleton<BlockManager>
{
    //--------------------------------------------------
    #region Fields
    // Serialize Fields
    [SerializeField] private BlockInfo _blockInfoSO;
    public BlockInfo BlockInfoSO => _blockInfoSO;

    [SerializeField] private BoardTile _boardTilePrefab;
    public BoardTile BoardTilePrefab => _boardTilePrefab;

    [SerializeField] private HexBoard _hexBoardInstance;
    [SerializeField] private int _normalBlockCreateCount;
    [SerializeField] private int _boardTileCreateCount;
    [SerializeField] private int _popVfxCreateCount;

    // Private Fields
    private readonly Queue<BlockBase> _normalBlockPooledQueue = new();
    private readonly Queue<BoardTile> _boardTilePooledQueue = new();
    private readonly Queue<VfxBase> _blockPopVfxPooledQueue = new();
    #endregion

    //--------------------------------------------------
    #region Awake Methods
    private void Awake()
    {
        CreateBlockInstance();
        CreateBoardTileInstnace();
        CreateBlockPopVfxInstance();
    }

    private void CreateBlockInstance()
    {
        for (int i = 0; i < _normalBlockCreateCount; i++)
            AddInstance(_normalBlockPooledQueue, _blockInfoSO.NormalBlockPrefab);
    }

    private void CreateBoardTileInstnace()
    {
        for (int i = 0; i < _boardTileCreateCount; i++)
            AddInstance(_boardTilePooledQueue, _boardTilePrefab);
    }

    private void CreateBlockPopVfxInstance()
    {
        for (int i = 0; i < _popVfxCreateCount; i++)
            AddInstance(_blockPopVfxPooledQueue, _blockInfoSO.PopVfxPrefab);
    }
    #endregion

    #region Instance Methods
    private void AddInstance<T>(Queue<T> instanceQueue, T prefab) where T : Component
    {
        var instance = Instantiate(prefab, transform);
        instance.gameObject.SetActive(false);

        instanceQueue.Enqueue(instance);
    }

    public BlockBase GetBlock(ref BlockSkin blockSkin, Vector2 pos)
    {
        if (_normalBlockPooledQueue.Count == 0)
            AddInstance(_normalBlockPooledQueue, _blockInfoSO.NormalBlockPrefab);

        var instance = _normalBlockPooledQueue.Dequeue();
        instance.transform.position = pos;
        instance.UpdateBlockSkin(ref blockSkin);
        instance.gameObject.SetActive(true);
        instance.OnDragged += _hexBoardInstance.CheckDirectionCell;
        instance.OnFoundTargetBlock += _hexBoardInstance.TrySwapBlock;

        return instance;
    }

    public BlockBase GetBlock(BlockDetailType detailType, Vector2 pos, bool objActive = true)
    {
        if (_normalBlockPooledQueue.Count == 0)
            AddInstance(_normalBlockPooledQueue, _blockInfoSO.NormalBlockPrefab);

        var skin = BlockInfoSO.Skins[(int)detailType];
        var instance = _normalBlockPooledQueue.Dequeue();

        instance.transform.position = pos;
        instance.UpdateBlockSkin(ref skin);
        instance.gameObject.SetActive(true);
        instance.OnDragged += _hexBoardInstance.CheckDirectionCell;
        instance.OnFoundTargetBlock += _hexBoardInstance.TrySwapBlock;

        return instance;
    }

    public BoardTile GetBoardTile(Vector2 pos, bool objActive = true)
    {
        if (_boardTilePooledQueue.Count == 0)
            AddInstance(_boardTilePooledQueue, _boardTilePrefab);

        var instance = _boardTilePooledQueue.Dequeue();
        instance.transform.position = pos;
        instance.gameObject.SetActive(true);

        return instance;
    }

    public VfxBase GetBlockPopVfx(BlockDetailType detailType, Vector2 pos, bool objActive = true)
    {
        if (_blockPopVfxPooledQueue.Count == 0)
            AddInstance(_blockPopVfxPooledQueue, _blockInfoSO.PopVfxPrefab);

        var skin = BlockInfoSO.Skins[(int)detailType];
        var instance = _blockPopVfxPooledQueue.Dequeue();

        instance.transform.position = pos;
        instance.UpdateParticleSprite(ref skin);
        instance.gameObject.SetActive(objActive);

        return instance;
    }

    //--------------------------------------------------

    public void ReleaseBlock(BlockBase blockBase)
    {
        blockBase.gameObject.SetActive(false);
        blockBase.ResetStatus();

        _normalBlockPooledQueue.Enqueue(blockBase);
    }

    public void ReleaseTile(BoardTile boardTile)
    {
        boardTile.gameObject.SetActive(false);
        boardTile.ResetStatus();

        _boardTilePooledQueue.Enqueue(boardTile);
    }

    public void ReleaseBlockPopVfx(VfxBase blockPopVfx)
    {
        blockPopVfx.gameObject.SetActive(false);
        blockPopVfx.ResetStatus();

        _blockPopVfxPooledQueue.Enqueue(blockPopVfx);
    }
    #endregion
}

//----------------------------------------------------------------------------------------------------
// 목적 : 보드 관리, 출력 (상태 관리/스폰/체인/팝/입력 결과 적용)
// 
// 주요 기능
// - 초기화 : 앵커 좌표 생성, 예외 셀 적용, 초기 배치 블록 생성
// - 체인 루프 : DropOld(수직) → 매칭/팝 → 스폰 → DropNew(대각허용) → 추가 수직 → Fallback(DropAll)
// - 스폰 정책 : 중앙 최상단 1칸만 생성(비었거나 outPassLast에 의해 ‘떠날 예정’이면 허용)
// - 팝 규칙 : 3매치 직선만 파괴, 스핀(장애물)은 직접 파괴되지 않고 인접 팝 2회 누적 시 파괴
// - 입력 처리 : 인접 스왑만 허용, 매칭 없으면 되돌리기
//----------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HexBoard : MonoBehaviour
{
    //--------------------------------------------------
    #region Consts
    private readonly Vector2Int[] nearCellOffsetsOdd =
    {
        new ( -1, 0 ), new ( 0, -1), new ( 1, -1 ),
        new ( 1, 0 ), new ( 1, 1 ), new ( 0, 1 )
    };

    private readonly Vector2Int[] nearCellOffsetsEven =
    {
        new ( -1, 0 ), new ( -1, -1), new ( 0, -1 ),
        new ( 1, 0 ), new ( 0, 1 ), new ( -1, 1 )
    };

    private const float DIST_EPSILON = 0.01f;
    #endregion

    #region Serialize Fields
    [SerializeField] private float _tileHeight;
    [SerializeField] private float _tileWidth;
    [SerializeField] private int _boardCountH;
    [SerializeField] private int _boardCountV;
    [Space(5)]
    [SerializeField] private List<Vector2Int> _exceptList;
    [SerializeField] private List<PlacedBlockInstance> _initialPlacedBlockList;
    #endregion

    #region Fields
    // Property Fields
    public Dictionary<Vector2Int, Vector2> AnchorDict { get; private set; } = new();

    // Private Fields
    private BlockBase[,] _blockGrid;
    private HashSet<Vector2Int> _exceptHashSet;
    private Vector2Int _topMidCell;

    private readonly HashSet<Vector2Int> _spawnTopHashSet = new();
    private readonly HashSet<Vector2Int> _outPassLast = new();

    private HexDropper _dropper;

    private readonly WaitForSeconds _waitForSec = new(0.1f);
    private WaitUntil _waitBoardSettled;
    #endregion

    //--------------------------------------------------
    #region Awake Methods
    private void Awake()
    {
        SetUpOnAwake();
        SetUpExceptHashSet();
        SetUpBlockGrid();
        GenerateBoardAncher();
        SetUpSpawnBlocks();
        SetUpDropper();
    }

    private void SetUpOnAwake()
    {
        _waitBoardSettled = new(CheckAllBoardSettled);
    }

    private void SetUpExceptHashSet() => _exceptHashSet = new(_exceptList);
    private void SetUpBlockGrid() => _blockGrid = new BlockBase[_boardCountV, _boardCountH];
    private void GenerateBoardAncher()
    {
        float vHalf = (_boardCountV - 1.0f) * 0.5f;
        float hHalf = (_boardCountH - 1.0f) * 0.5f;

        for(int i = 0; i < _boardCountV; i++) 
        {
            for(int j = 0; j < _boardCountH; j++)
            {
                float x = (j - hHalf) * _tileWidth;
                float y = (vHalf - i) * _tileHeight;

                if((j & 1) == 0)
                    y -= _tileHeight * 0.5f;

                var idx = new Vector2Int(i, j);
                var pos = new Vector2(x, y);

                AnchorDict.Add(idx, pos);
            }
        }
    }

    private void SetUpSpawnBlocks()
    {
        _spawnTopHashSet.Clear();

        int top = 0;
        int mid = _boardCountH / 2;

        var cell = new Vector2Int(top, mid);

        if (!CheckInBoard(cell) || _exceptHashSet.Contains(cell))
            return;

        _topMidCell = cell;
        _spawnTopHashSet.Add(cell);
    }

    private void SetUpDropper() => _dropper = new(CheckInBoard, GetCellOffset, cell => AnchorDict[cell], _boardCountH);
    #endregion

    #region Start Methods
    private void Start()
    {
        GenerateBoardTile();
        GenerateBlocks();
    }

    private void GenerateBoardTile()
    {
        for (int i = 0; i < _boardCountV; i++)
        {
            for (int j = 0; j < _boardCountH; j++)
            {
                var key = new Vector2Int(i, j);
                if (_exceptHashSet.Contains(key))
                    continue;

                BlockManager.Instance.GetBoardTile(AnchorDict[key]);
            }
        }
    }

    private void GenerateBlocks()
    {
        int loop = _initialPlacedBlockList.Count;
        for(int i = 0; i < loop; i++)
        {
            var blockInitData =  _initialPlacedBlockList[i];
            if (_exceptHashSet.Contains(blockInitData.cell))
                continue;

            var blockInstance = BlockManager.Instance.GetBlock(blockInitData.detailType, AnchorDict[blockInitData.cell]);
            blockInstance.CurrentBlock.Cell = blockInitData.cell;
            blockInstance.CurrentBlock.Life = blockInstance.CurrentBlock.Skin.detailType == BlockDetailType.Obstacle_Spin ? 2 : 1;

            _blockGrid[blockInstance.CurrentBlock.Cell.x, blockInstance.CurrentBlock.Cell.y] = blockInstance;
        }
    }
    #endregion

    #region Check Methods
    private bool CheckNeighbor(Vector2Int origin, Vector2Int target)
    {
        var offsets = GetCellOffset(origin);

        for (int i = 0; i < offsets.Length; i++)
        {
            if (origin + offsets[i] == target)
                return true;
        }

        return false;
    }

    private bool CheckInBoard(Vector2Int checkCell)
    {
        if (checkCell.x < 0 || checkCell.y < 0 || checkCell.x >= _boardCountV || checkCell.y >= _boardCountH ||
            _exceptHashSet.Contains(checkCell))
        {
            return false;
        }

        return true;
    }

    public BlockBase CheckDirectionCell(BlockBase blockBase, float angle)
    {
        float angle360 = (angle + 360.0f) % 360.0f;
        int sector = Mathf.RoundToInt(angle360 / 60.0f) % 6;

        var newCell = blockBase.CurrentBlock.Cell + GetCellOffset(blockBase.CurrentBlock.Cell)[sector];

        return !CheckInBoard(newCell) ? null : _blockGrid[newCell.x, newCell.y];
    }

    private bool CheckSettled(BlockBase block, Vector2 anchor)
    {
        if (block == null) return false;
        if (block.IsFalling || block.HasPendingPath || block.DropDelay > 0.0f) return false;

        return ((Vector2)block.transform.position - anchor).sqrMagnitude <= DIST_EPSILON;
    }

    private bool CheckAllBoardSettled()
    {
        for (int i = 0; i < _boardCountV; i++)
        {
            for (int j = 0; j < _boardCountH; j++)
            {
                var block = _blockGrid[i, j];
                if (block == null) continue;

                if (!CheckSettled(block, AnchorDict[block.CurrentBlock.Cell])) return false;
            }
        }

        return true;
    }

    private bool CheckEmptyCell()
    {
        for (int i = 0; i < _boardCountV; i++)
        {
            for (int j = 0; j < _boardCountH; j++)
            {
                if (!CheckInBoard(new Vector2Int(i, j)))
                    continue;

                if (_blockGrid[i, j] == null)
                    return true;
            }
        }

        return false;
    }

    private Vector2Int[] GetCellOffset(Vector2Int cell) => (((cell.y + 1) & 1) == 1) ? nearCellOffsetsOdd : nearCellOffsetsEven;
    #endregion

    #region Swap Methods
    public bool TrySwapBlock(BlockBase originBlock, BlockBase targetBlock)
    {
        if (!CheckNeighbor(originBlock.CurrentBlock.Cell, targetBlock.CurrentBlock.Cell))
            return false;

        SwapCell(originBlock, targetBlock);

        var matchedBlocks = new HashSet<Vector2Int>();

        matchedBlocks.UnionWith(HexMatchFinder.TryMatchBlock(
            originBlock, _blockGrid, CheckInBoard,
            (tmpBlock, _) => CheckSettled(tmpBlock, AnchorDict[tmpBlock.CurrentBlock.Cell]),
            StepCell, tmpCell => AnchorDict[tmpCell]));

        matchedBlocks.UnionWith(HexMatchFinder.TryMatchBlock(
            targetBlock, _blockGrid, CheckInBoard,
            (tmpBlock, _) => CheckSettled(tmpBlock, AnchorDict[tmpBlock.CurrentBlock.Cell]),
            StepCell, tmpCell => AnchorDict[tmpCell]));

        if (matchedBlocks.Count > 0)
        {
            originBlock.transform.position = AnchorDict[originBlock.CurrentBlock.Cell];
            targetBlock.transform.position = AnchorDict[targetBlock.CurrentBlock.Cell];

            PopBlock(matchedBlocks);

            StartCoroutine(BlockChainningProcess());

            return true;
        }

        else
        {
            SwapCell(originBlock, targetBlock);

            originBlock.transform.position = AnchorDict[originBlock.CurrentBlock.Cell];
            targetBlock.transform.position = AnchorDict[targetBlock.CurrentBlock.Cell];

            return false;
        }
    }

    private void SwapCell(BlockBase originBlock, BlockBase targetBlock)
    {
        var oldCell = originBlock.CurrentBlock.Cell;
        originBlock.CurrentBlock.Cell = targetBlock.CurrentBlock.Cell;
        targetBlock.CurrentBlock.Cell = oldCell;

        _blockGrid[originBlock.CurrentBlock.Cell.x, originBlock.CurrentBlock.Cell.y] = originBlock;
        _blockGrid[targetBlock.CurrentBlock.Cell.x, targetBlock.CurrentBlock.Cell.y] = targetBlock;
    }

    private Vector2Int StepCell(Vector2Int cell, int dir) => cell + GetCellOffset(cell)[dir];
    #endregion

    #region Channing/Pop/Create Methods
    private IEnumerator BlockChainningProcess()
    {
        BeginChainningLock();

        while (true)
        {
            while (_dropper.DropOldBlock(_blockGrid, _outPassLast)) { }
            yield return _waitBoardSettled;

            var matchedBlocks = HexMatchFinder.FindAllMatchedBlock(
                _blockGrid, CheckInBoard,
                (tmpBlock, _) => CheckSettled(tmpBlock, AnchorDict[tmpBlock.CurrentBlock.Cell]),
                StepCell, cell => AnchorDict[cell]);

            if (matchedBlocks.Count > 0)
            {
                // Pop 확인용 지연
                yield return _waitForSec;

                PopBlock(matchedBlocks);

                continue;
            }

            if (CheckEmptyCell())
            {
                if(CanSpawnBlockTop())
                {
                    yield return GenerateRandomBlockCascade();
                    _dropper.DropNewBlock(_blockGrid, _outPassLast);

                    while(true)
                    {
                        var moved = _dropper.DropOldBlock(_blockGrid, _outPassLast);
                        if (!moved) break;

                        yield return null;
                    }

                    continue;
                }

                bool verticalProgressed = false;
                while (true)
                {
                    var moved = _dropper.DropOldBlock(_blockGrid, _outPassLast);
                    verticalProgressed |= moved;

                    if (!moved) break;

                    yield return null;
                }

                if (verticalProgressed)
                {
                    yield return _waitBoardSettled;
                    continue;
                }

                bool allProgressed = false;
                while (true)
                {
                    var moved = _dropper.DropAllBlock(_blockGrid);
                    allProgressed |= moved;

                    if (!moved)
                        break;

                    yield return null;
                }

                if (allProgressed)
                {
                    yield return _waitBoardSettled;
                    continue;
                }
            }

            break;
        }

        EndChainningLock();
    }

    private bool CanSpawnBlockTop()
    {
        return _blockGrid[_topMidCell.x, _topMidCell.y] == null ||
            _outPassLast.Contains(_topMidCell);
    }

    private void PopBlock(HashSet<Vector2Int> matchedBlocks)
    {
        if(matchedBlocks == null || matchedBlocks.Count == 0) return;

        var popBlocks = new List<Vector2Int>();
        var affectedSpinBlock = new HashSet<Vector2Int>();

        foreach (var cell in matchedBlocks)
        {
            if(!CheckInBoard(cell)) continue;

            var block = _blockGrid[cell.x, cell.y];
            if (block == null
                || block.CurrentBlock.Skin.detailType == BlockDetailType.Obstacle_Spin) continue;

            popBlocks.Add(cell);

            var offsets = GetCellOffset(cell);
            for (int i = 0; i < offsets.Length; i++)
            {
                var nCell = cell + offsets[i];
                if (!CheckInBoard(nCell)) continue;

                var nBlock = _blockGrid[nCell.x, nCell.y];
                if (nBlock == null
                    || nBlock.CurrentBlock.Skin.detailType != BlockDetailType.Obstacle_Spin) continue;

                affectedSpinBlock.Add(nCell);
            }
        }

        foreach(var cell in popBlocks)
        {
            var curBlock = _blockGrid[cell.x, cell.y];
            if (curBlock == null) continue;

            BlockManager.Instance.GetBlockPopVfx(curBlock.CurrentBlock.Skin.detailType, curBlock.transform.position);

            curBlock.ReleaseBlock();
            _blockGrid[cell.x, cell.y] = null;
        }

        foreach(var spin in affectedSpinBlock)
        {
            var spinBlock = _blockGrid[spin.x, spin.y];
            if (spinBlock == null) continue;

            spinBlock.CurrentBlock.Life--;
            if (spinBlock.CurrentBlock.Life == 1)
                spinBlock.UpdateBlockSprite(1);

            else if (spinBlock.CurrentBlock.Life == 0)
            {
                spinBlock.UpdateBlockSprite(0);

                spinBlock.ReleaseBlock();
                _blockGrid[spin.x, spin.y] = null;
            }
        }
    } 

    private bool GenerateRandomBlock()
    {
        var startCell = _topMidCell;

        if (!CheckInBoard(startCell)) return false;
        if (_blockGrid[startCell.x, startCell.y])
            return false;

        var colorIdx = Random.Range(0, 6);
        var blockInstance = BlockManager.Instance.GetBlock((BlockDetailType)colorIdx, Vector2.zero);
        blockInstance.CurrentBlock.Cell = startCell;
        blockInstance.IsNewSpawn = true;

        var midAnchor = AnchorDict[_topMidCell];
        blockInstance.transform.position = midAnchor + Vector2.up * _tileHeight;

        blockInstance.SetUpDropPath(new[] { (Vector3)midAnchor }, 0.0f);

        _blockGrid[startCell.x, startCell.y] = blockInstance;

        _outPassLast.Clear();
        _outPassLast.Add(_topMidCell);

        return true;
    }

    private IEnumerator GenerateRandomBlockCascade()
    {
        while(true)
        {
            if (!CheckEmptyCell()) yield break;

            if (_blockGrid[_topMidCell.x, _topMidCell.y] == null || _outPassLast.Contains(_topMidCell))
            {
                if (GenerateRandomBlock())
                {
                    _dropper.DropNewBlock(_blockGrid, _outPassLast);
                    yield break;
                }
            }

            if(!CheckAllBoardSettled())
            {
                yield return null;
                continue;
            }

            if (_dropper.DropOldBlock(_blockGrid, _outPassLast))
            {
                yield return null;
                continue;
            }
            
            yield break;
        }
    }
    #endregion

    private bool CheckCanIntoPass(Vector2Int cell, HashSet<Vector2Int> movePass, HashSet<Vector2Int> outPass)
    {
        if (!CheckInBoard(cell)) return false;
        if(movePass.Contains(cell)) return false;

        var curBlock = _blockGrid[cell.x, cell.y];
        if(curBlock == null) return true;

        if (outPass.Contains(cell)) return true;

        return false;
    }
    #endregion

    #region Lock Methods
    private void ForceLockBlocks(bool locked)
    {
        for(int i = 0; i < _boardCountV; i++)
        {
            for(int j = 0; j < _boardCountH; j++)
            {
                var block = _blockGrid[i, j];
                if(block == null) 
                    continue;

                block.IsLocked = locked;
                if (locked)
                    block.CancelDrag();
            }
        }
    }

    private void BeginChainningLock() => ForceLockBlocks(true);
    private void EndChainningLock() => ForceLockBlocks(false);
    #endregion
}

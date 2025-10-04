//----------------------------------------------------------------------------------------------------
// 목적 : 블록 낙하 경로 계획/적용 (버퍼 재사용으로 GC 최소화, 성능 안정화)
// 
// 주요 기능
// - DropOld : 기존 블록 수직 우선 낙하(대각 비허용) 계획 → 적용
// - DropNew : 신규 블록 대각 허용 낙하(좌하/우하 바이어스), outPassLast 갱신
// - DropAll : Fallback 용도(신규/기존 모두 대각 허용)로 정체 해소
// - 경로 계획 시 예약(reservePass)과 통과(outPass) 집합으로 충돌/교차 방지
//----------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

public class HexDropper
{
    //--------------------------------------------------
    #region Fields
    private readonly Func<Vector2Int, bool> _inBoardFunc;
    private readonly Func<Vector2Int, Vector2Int[]> _offsetFunc;
    private readonly Func<Vector2Int, Vector3> _anchorFunc;

    private readonly HashSet<Vector2Int> _reservePass = new();
    private readonly HashSet<Vector2Int> _outPass = new();
    private readonly HashSet<Vector2Int> _outPassLast = new();

    private readonly List<(BlockBase block, Vector2Int from, Vector2Int to, List<Vector2Int> dropPath)> _moves = new();
    private readonly List<Vector2Int> _tmpPath = new();
    private readonly List<Vector3> _tmpAnchors = new();

    private readonly int _midH;
    #endregion
    //--------------------------------------------------

    #region Construct Methods
    public HexDropper(
        Func<Vector2Int, bool> inBoardFunc,
        Func<Vector2Int, Vector2Int[]> offsetFunc,
        Func<Vector2Int, Vector3> anchorFunc,
        int boardH)
    {
        _inBoardFunc = inBoardFunc;
        _offsetFunc = offsetFunc;
        _anchorFunc = anchorFunc;

        _midH = boardH / 2;
    }
    #endregion

    #region Methods
    public bool DropOldBlock(BlockBase[,] grid, HashSet<Vector2Int> outPassLast)
    {
        var plan = DropPlan(grid, false, false);
        var apply = ApplyDropBlock(grid, false);

        if(apply)
        {
            outPassLast.Clear();
            foreach (var cell in _outPass)
                outPassLast.Add(cell);
        }

        return plan && apply;
    }

    public bool DropNewBlock(BlockBase[,] grid, HashSet<Vector2Int> outPassLast)
    {
        var plan = DropPlan(grid, true, true);
        var apply = ApplyDropBlock(grid, true);

        if (apply)
        {
            _outPassLast.Clear();
            foreach (var pass in _outPass) 
                _outPassLast.Add(pass);
        }

        return plan && apply;
    }

    public bool DropAllBlock(BlockBase[,] grid)
    {
        var planNew = DropPlan(grid, true, true);
        var applyNew = ApplyDropBlock(grid, true);

        var planOld = DropPlan(grid, true, false);
        var applyOld = ApplyDropBlock(grid, false);

        return (planNew && applyNew) || (planOld && applyOld);
    }

    private bool _centerFlipToggle;
    private bool DropPlan(BlockBase[,] grid, bool allowCross, bool forNewSpawn)
    {
        _reservePass.Clear();
        _outPass.Clear();
        _moves.Clear();

        int boardV = grid.GetLength(0);
        int boardH = grid.GetLength(1);

        for (int i = boardV - 1; i >= 0; i--)
        {
            for (int j = 0; j < boardH; j++)
            {
                var startCell = new Vector2Int(i, j);

                var curBlock = grid[i, j];
                if (curBlock == null) continue;
                if (curBlock.IsFalling || curBlock.HasPendingPath || curBlock.DropDelay > 0.0f) continue;

                if (forNewSpawn && !curBlock.IsNewSpawn) continue;
                if (!forNewSpawn && curBlock.IsNewSpawn) continue;

                _tmpPath.Clear();
                var curCell = startCell;

                while (true)
                {
                    var offset = _offsetFunc(curCell);
                    var downCell = curCell + offset[3];

                    if (CheckCanIntoPass(grid, downCell))
                    {
                        _outPass.Add(curCell);
                        curCell = downCell;
                        _tmpPath.Add(curCell);
                        continue;
                    }

                    if (allowCross)
                    {
                        var dl = curCell + offset[2];
                        var dr = curCell + offset[4];

                        bool dlCheck = CheckCanIntoPass(grid, dl);
                        bool drCheck = CheckCanIntoPass(grid, dr);

                        if (dlCheck || drCheck)
                        {
                            Vector2Int nxtCell;

                            if (dlCheck && drCheck)
                            {
                                if (curCell.y > _midH)
                                    nxtCell = dl;
                                else if (curCell.y < _midH)
                                    nxtCell = dr;
                                else
                                {
                                    nxtCell = _centerFlipToggle ? dl : dr;
                                    _centerFlipToggle = !_centerFlipToggle;
                                }
                            }

                            else
                            {
                                nxtCell = dlCheck ? dl : dr;
                            }

                            _outPass.Add(curCell);
                            curCell = nxtCell;
                            _tmpPath.Add(curCell);

                            continue;
                        }
                    }

                    break;
                }

                if (_tmpPath.Count == 0) continue;

                var copyPath = new List<Vector2Int>(_tmpPath.Count);
                copyPath.AddRange(_tmpPath);

                _moves.Add((curBlock, startCell, curCell, copyPath));
                _reservePass.Add(curCell);
            }
        }

        return _moves.Count > 0;
    }

    private bool ApplyDropBlock(BlockBase[,] grid, bool notNewSpawn)
    {
        if (_moves.Count == 0) return false;

        for (int i = 0; i < _moves.Count; i++)
        {
            var move = _moves[i];

            grid[move.from.x, move.from.y] = null;
            grid[move.to.x, move.to.y] = move.block; ;
            move.block.CurrentBlock.Cell = move.to;

            _tmpAnchors.Clear();
            for (int j = 0; j < move.dropPath.Count; j++)
            {
                _tmpAnchors.Add(_anchorFunc(move.dropPath[j]));
            }

            move.block.SetUpDropPath(_tmpAnchors, 0.0f);

            if (notNewSpawn && move.block.IsNewSpawn)
                move.block.IsNewSpawn = false;
        }

        return true;
    }

    private bool CheckCanIntoPass(BlockBase[,] grid, Vector2Int cell)
    {
        if (!_inBoardFunc(cell)) return false;
        if (_reservePass.Contains(cell)) return false;

        var curBlock = grid[cell.x, cell.y];
        if (curBlock == null) return true;

        return _outPass.Contains(cell);
    }
    #endregion
}

//----------------------------------------------------------------------------------------------------
// ���� : ��Ī ����(���� ���) ? ���� ���� �Է� �׸���� 3��ġ ��ǥ�� ��ȯ
// 
// �ֿ� ���
// - TryMatchBlock : �ǹ� ���� ���� �����(0~2 / 3~5)���� ���� �� ���� ����
// - FindAllMatchedBlock : ���� ��ü���� ������ ��ϸ� ������� 3��ġ �̻� ���� ��ȯ
// - ��ֹ�(���� ��)�� ��Ī ��󿡼� ����
//----------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

public static class HexMatchFinder
{
    public static IEnumerable<Vector2Int> TryMatchBlock(
        BlockBase pivotBlock,
        BlockBase[,] grid,
        Func<Vector2Int, bool> inBoard,
        Func<BlockBase, Vector2, bool> isSettled,
        Func<Vector2Int, int, Vector2Int> step,
        Func<Vector2Int, Vector2> anchorOffset)
    {
        if (pivotBlock.CurrentBlock.Skin.category == BlockCategory.Obstacle)
            return Array.Empty<Vector2Int>();

        var result = new HashSet<Vector2Int>();

        for (int i = 0; i < 3; i++)
        {
            var tmp = new HashSet<Vector2Int>() { pivotBlock.CurrentBlock.Cell };

            tmp.UnionWith(FindBlock(pivotBlock.CurrentBlock.Cell, i, grid, inBoard, isSettled, step, anchorOffset));
            tmp.UnionWith(FindBlock(pivotBlock.CurrentBlock.Cell, i + 3, grid, inBoard, isSettled, step, anchorOffset));

            if (tmp.Count >= 3)
                result.UnionWith(tmp);
        }

        return result;
    }

    public static HashSet<Vector2Int> FindAllMatchedBlock(
        BlockBase[,] grid,
        Func<Vector2Int, bool> inBoard,
        Func<BlockBase, Vector2, bool> isSettled,
        Func<Vector2Int, int, Vector2Int> step,
        Func<Vector2Int, Vector2> anchorOffset)
    {
        var matchedBlocks = new HashSet<Vector2Int>();
        int boardV = grid.GetLength(0);
        int boardH = grid.GetLength(1);

        for (int x = boardV - 1; x >= 0; x--)
        {
            for (int y = 0; y < boardH; y++)
            {
                var curBlock = grid[x, y];
                if (curBlock == null
                    || curBlock.CurrentBlock.Skin.category == BlockCategory.Obstacle) continue;

                if (!isSettled(curBlock, anchorOffset(curBlock.CurrentBlock.Cell))) continue;

                matchedBlocks.UnionWith(TryMatchBlock(curBlock, grid, inBoard, isSettled, step, anchorOffset));
            }
        }

        return matchedBlocks;
    }

    public static IEnumerable<Vector2Int> FindBlock(
        Vector2Int pivotCell, 
        int checkIdx,
        BlockBase[,] grid,
        Func<Vector2Int, bool> inBoard,
        Func<BlockBase, Vector2, bool> isSettled,
        Func<Vector2Int, int, Vector2Int> step,
        Func<Vector2Int, Vector2> anchorOffset)
    {
        var result = new List<Vector2Int>();
        var startCell = pivotCell;
        var pivotBlock = grid[pivotCell.x, pivotCell.y];
        if (pivotBlock == null)
            return result;

        while (true)
        {
            startCell = step(startCell, checkIdx);

            if (!inBoard(startCell)) break;

            var curBlock = grid[startCell.x, startCell.y];
            if (curBlock == null || curBlock.CurrentBlock.Skin.category == BlockCategory.Obstacle) break;
            if (!isSettled(curBlock, Vector2.zero)) break;
            if (curBlock.CurrentBlock.Skin.detailType != pivotBlock.CurrentBlock.Skin.detailType) break;

            result.Add(startCell);
        }

        return result;
    }
}

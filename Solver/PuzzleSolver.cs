using Combinatorics.Collections;
using CommunityToolkit.HighPerformance;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Util;
using System.Numerics;

namespace Puzzle.ML.Solver;

internal class PuzzleSolver : IDisposable
{
    private readonly Accelerator accelerator;
    private readonly HostPuzzleData puzzleData;
    private readonly HostPuzzleCases puzzleCases;

    public PuzzleSolver(Accelerator accelerator, PuzzleData puzzle)
    {
        this.accelerator = accelerator;
        this.puzzleData = new HostPuzzleData(accelerator, puzzle);
        //this.puzzleCases = new HostPuzzleCases(
        //    accelerator,
        //    puzzle,
        //    new byte[,] { { 3, 6, 4, 0, 2, 1, 7, 5 } },
        //    new byte[,] { { 0, 0, 2, 3, 0, 3, 0, 2 } });
        this.puzzleCases = new HostPuzzleCases(accelerator, puzzle);
    }

    static void __kernel(Index2D index, DevicePuzzleData puzzle, DevicePuzzleCases puzzleCases, DeviceSolution solution)
    {
        if (solution.Found.Value == 1)
            return;

        var pieceShuffleData = puzzleCases.ShuffleCases
            .SubView((index.X, 0), (1, puzzle.NumPieces));

        var pieceVariationData = puzzleCases.VariationCases
            .SubView((index.Y, 0), (1, puzzle.NumPieces));

        var board = LocalMemory.Allocate2D<byte, Stride2D.DenseX>((7, 7), Stride2D.DenseX.FromExtent((7, 7)));
        for (int i = 0; i < puzzle.BoardData.BaseView.Length; i++)
        {
            var idx = puzzle.BoardData.Stride.ReconstructFromElementIndex(i);
            board[idx] = puzzle.BoardData[idx];
        }

        var coords = LocalMemory.Allocate1D<byte, Stride1D.Dense>(16, new Stride1D.Dense());

        for (int i = 0; i < puzzle.NumPieces; i++)
        {
            var pieceIdx = pieceShuffleData[0, i];
            var pieceVariationIdx = pieceVariationData[0, i];
            var variationIdx = (pieceIdx * 4) + pieceVariationIdx;
            var piece = puzzle.PieceData.SubView((variationIdx, 0, 0), (1, 5, 5));
            var pieceDimension = puzzle.PieceDimension[variationIdx];
            var (success, placedX, placedY) = __placePiece(piece, pieceDimension, board);
            if (!success)
                break;
            else
            {
                coords[2 * i] = placedX;
                coords[(2 * i) + 1] = placedY;
            }
        }

        if (__is_solution(board.BaseView))
        {
            solution.Found.Value = 1;
            for (int i = 0; i < puzzle.NumPieces; i++)
            {
                var pieceIdx = pieceShuffleData[0, i];
                solution.Shuffle[i] = pieceIdx;
            }

            for (int i = 0; i < puzzle.NumPieces; i++)
            {
                var pieceVariationIdx = pieceVariationData[0, i];
                solution.Variation[i] = pieceVariationIdx;
            }

            for (int i = 0; i < puzzle.NumPieces; i++)
            {
                solution.Coords[2 * i] = coords[2 * i];
                solution.Coords[(2 * i) + 1] = coords[(2 * i) + 1];
            }
        }
    }

    static (bool, byte x, byte y) __placePiece(
        ArrayView3D<byte, Stride3D.DenseXY> piece,
        Vector2 dimensions,
        ArrayView2D<byte, Stride2D.DenseX> board)
    {
        var boardshape = board.IntExtent;
        var shape = ((byte)dimensions.X, (byte)dimensions.Y);
        piece = piece.SubView((0, 0, 0), (1, (byte)dimensions.X, (byte)dimensions.Y));
        var after_placement = new byte[5, 5].AsArrayView().SubView((0, 0), shape);

        for (byte i = 0; i < boardshape.X; i++)
            for (byte j = 0; j < boardshape.Y; j++)
            {
                var isInside = __is_inside(board, (i, j), shape);
                if (!isInside) continue;
                var placement = board.SubView((i, j), shape);
                //__print(placement);
                //__print(piece);
                for (int x = 0; x < placement.IntExtent.X; x++)
                    for (int y = 0; y < placement.IntExtent.Y; y++)
                        after_placement[x, y] = (byte)(placement[x, y] + piece[0, x, y]);
                //__print(after_placement);
                var success = __is_placement_valid(after_placement.BaseView);
                if (success)
                {
                    for (int x = 0; x < placement.IntExtent.X; x++)
                        for (int y = 0; y < placement.IntExtent.Y; y++)
                        {
                            placement[x, y] = after_placement[x, y];
                        }
                    //__print(puzzle.BoardData);

                    return (true, i, j);
                }
            }

        return (false, 0, 0);
    }

    static bool __is_inside<TStride>(ArrayView2D<byte, TStride> m, Index2D index,
            Index2D extent)
        where TStride : struct, IStride2D
    {
        var inBoundsX = Bitwise.And(
            index.X >= 0,
            Bitwise.Or(
                index.X + extent.X <= (int)m.Extent.X,
                Bitwise.And(index.X == 0, extent.X == 0)));

        var inBoundsY = Bitwise.And(
            index.Y >= 0,
            Bitwise.Or(
                index.Y + extent.Y <= (int)m.Extent.Y,
                Bitwise.And(index.Y == 0, extent.Y == 0)));
        return Bitwise.And(inBoundsX, inBoundsY);
    }

    static bool __is_solution(ArrayView<byte> m)
    {
        bool flag = false;
        for (int i = 0; i < m.Length; i++)
        {
            flag = m[i] == 1;
            if (!flag) break;
        }

        return flag;
    }

    static bool __is_placement_valid(ArrayView<byte> m)
    {
        bool flag = false;
        for (int i = 0; i < m.Length; i++)
        {
            flag = m[i] >= 0 && m[i] < 2;
            if (!flag) break;
        }

        return flag;
    }

    static void __print<TStride>(ArrayView2D<byte, TStride> m)
        where TStride : struct, IStride2D
    {
        for (int i = 0; i < m.IntExtent.X; i++)
        {
            for (int j = 0; j < m.IntExtent.Y; j++)
                Interop.Write("{0} ", m[i, j]);
            Interop.WriteLine("");
        }

        Interop.WriteLine("");
    }

    static void __print<TStride>(ArrayView3D<byte, TStride> m)
        where TStride : struct, IStride3D
    {
        for(int i = 0; i < m.IntExtent.Y; i++)
        {
            for (int j = 0; j < m.IntExtent.Z; j++)
                Interop.Write("{0} ", m[0, i, j]);
            Interop.WriteLine("");
        }

        Interop.WriteLine("");
    }

    public (bool, byte[], byte[], Memory2D<byte>) StartSolver()
    {
        using HostSolution hostSolution = new(accelerator, puzzleData);

        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, DevicePuzzleData, DevicePuzzleCases, DeviceSolution>(__kernel);
        var size = (puzzleCases.ShuffleCases.IntExtent.X, puzzleCases.VariationCases.IntExtent.X);

        var maxSizeX = accelerator.MaxGridSize.X;
        var maxSizeY = accelerator.MaxGridSize.Y;
        size = (40320, 65535);
        kernel(size, puzzleData.ToDevice(), puzzleCases.ToDevice(), hostSolution.ToDevice());
        accelerator.Synchronize();

        var found = hostSolution.Found.GetAsArray1D();
        var shuffle = hostSolution.Shuffle.GetAsArray1D();
        var variation = hostSolution.Variation.GetAsArray1D();
        var coords = hostSolution.Coords.GetAsArray1D().AsMemory().AsMemory2D(puzzleData.NumPieces, 2);
        // Reorders/transposes data on copying to CPU
        return (found[0] == 1, shuffle, variation, coords);
    }

    public void Dispose()
    {
        this.puzzleData?.Dispose();
        this.puzzleCases.Dispose();
    }
}

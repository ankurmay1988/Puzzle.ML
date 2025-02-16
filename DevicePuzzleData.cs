﻿using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Helpers;
using ILGPU;
using ILGPU.Algorithms.Vectors;
using ILGPU.Runtime;
using ILGPU.Util;
using System.Numerics;

namespace Puzzle.ML;

public struct HostPuzzleData : IDisposable
{
    public int NumPieces;
    public MemoryBuffer3D<byte, Stride3D.DenseXY> PieceData;
    public MemoryBuffer1D<Vector2, Stride1D.Dense> PieceDimension;
    public MemoryBuffer2D<byte, Stride2D.DenseX> BoardData;
    public int BoardWidth;
    public int BoardHeight;

    public HostPuzzleData(Accelerator accelerator, PuzzleData puzzle)
    {
        BoardHeight = puzzle.BoardHeight;
        BoardWidth = puzzle.BoardWidth;
        NumPieces = puzzle.Pieces.Count;
        PieceData = default!;
        PieceDimension = default!;
        BoardData = default!;
        Initialize(accelerator, puzzle);
    }

    private void Initialize(Accelerator accelerator, PuzzleData puzzle)
    {
        BoardData = accelerator.Allocate2DDenseX(puzzle.BoardDataArray);
        //VectorView<bool> a = new VectorView<bool>();
        //UInt64x8 b = UInt64x8.FromScalar(1);
        //BitHelper.ExtractRange;
        //a.SliceVector(1);

        var pieceData = puzzle.Pieces
            .SelectMany(p => p.Variations)
            .Select(x => new
            {
                data = new Span2D<byte>(x.Data, x.Height, x.Width).ToArray(),
                dim = Vector2.Create(x.Height, x.Width)
            })
            .ToList();
        
        var pieceVariations = NumPieces * puzzle.NumVariations;

        var pieceBuffer = new byte[pieceVariations, 5, 5];
        pieceBuffer.AsSpan().Clear();
        var idx = Enumerable.Range(0, pieceVariations)
            .SelectMany(i => Enumerable.Range(0, 5)
                .SelectMany(j => Enumerable.Range(0, 5)
                    .Select(k => (i, j, k))));
        idx = idx.Where(x => x.j < pieceData[x.i].dim.X && x.k < pieceData[x.i].dim.Y);

        foreach (var (i, j, k) in idx)
            pieceBuffer[i, j, k] = pieceData[i].data[j, k];

        PieceData = accelerator.Allocate3DDenseXY(pieceBuffer);

        var dims = pieceData.Select(p => p.dim).ToArray();
        PieceDimension = accelerator.Allocate1D(dims);
    }

    private void ExtractBitVector<TVec, TElem>(
        VectorView<TVec> vector,
        int index,
        VectorView<bool> dest)
            where TVec : unmanaged, IVectorType<TVec, byte>
    {
        for (long i = 0; i < vector.NumVectors; i++)
        {
            var v = vector.SliceVector(i);
            for (int j = 0; j < v.Dimension.X; j++)
            {
                var val = v[j].AsSpan();
                dest[i, j] = BitHelper.HasFlag(val[0], index);
            }
        }
    }

    public DevicePuzzleData DeviceView()
    {
        return new()
        {
            BoardHeight = BoardHeight,
            BoardWidth = BoardWidth,
            NumPieces = NumPieces,
            BoardData = BoardData.View,
            PieceData = PieceData.View,
            PieceDimension = PieceDimension.View
        };
    }

    public void Dispose()
    {
        BoardData?.Dispose();
        PieceData?.Dispose();
        PieceDimension?.Dispose();
    }
}

public struct DevicePuzzleData
{
    public int NumPieces;
    public int BoardWidth;
    public int BoardHeight;
    public ArrayView3D<byte, Stride3D.DenseXY> PieceData;
    public ArrayView1D<Vector2, Stride1D.Dense> PieceDimension;
    public ArrayView2D<byte, Stride2D.DenseX> BoardData;
}

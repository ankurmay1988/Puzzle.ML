﻿using Combinatorics.Collections;
using ILGPU;
using ILGPU.Runtime;

namespace Puzzle.ML;

public struct HostPuzzleCases : IDisposable
{
    private readonly Accelerator accelerator;
    public MemoryBuffer2D<byte, Stride2D.DenseX> ShuffleCases;
    public MemoryBuffer2D<byte, Stride2D.DenseX> VariationCases;
    private readonly int NumPieces;
    private int NumVariationCases;
    private int NumShuffleCases;
    public HostPuzzleCases(Accelerator accelerator, PuzzleData puzzle, byte[,]? shuffleCases = null, byte[,]? variationCases = null)
    {
        this.accelerator = accelerator;
        ShuffleCases = default!;
        VariationCases = default!;
        NumPieces = puzzle.Pieces.Count;
        Initialize(puzzle, shuffleCases, variationCases);
    }

    private void Initialize(PuzzleData puzzle, byte[,]? shuffled, byte[,]? variations)
    {
        if (shuffled == null)
        {
            var cases_Perm = new Permutations<byte>(Enumerable.Range(0, puzzle.Pieces.Count).Select(x => (byte)x)).ToList();
            var arrShuffleCases = new byte[cases_Perm.Count, puzzle.Pieces.Count];
            var casesIdx = Enumerable.Range(0, cases_Perm.Count).SelectMany(i => Enumerable.Range(0, puzzle.Pieces.Count).Select(j => (i, j)));

            Parallel.ForEach(casesIdx, idx =>
            {
                arrShuffleCases[idx.i, idx.j] = cases_Perm[idx.i][idx.j];
            });

            ShuffleCases = accelerator.Allocate2DDenseX(arrShuffleCases);
            NumShuffleCases = cases_Perm.Count;
        }
        else
        {
            ShuffleCases = accelerator.Allocate2DDenseX(shuffled);
            NumShuffleCases = shuffled.GetLength(0);
        }

        if (variations == null)
        {
            var variationCases = new Variations<byte>(Enumerable.Range(0, puzzle.NumVariations).Select(x => (byte)x), puzzle.Pieces.Count, GenerateOption.WithRepetition).ToList();
            var arrVariationCases = new byte[variationCases.Count, puzzle.Pieces.Count];
            var variationIdx = Enumerable.Range(0, variationCases.Count).SelectMany(i => Enumerable.Range(0, puzzle.Pieces.Count).Select(j => (i, j)));

            Parallel.ForEach(variationIdx, idx =>
            {
                arrVariationCases[idx.i, idx.j] = variationCases[idx.i][idx.j];
            });

            VariationCases = accelerator.Allocate2DDenseX(arrVariationCases);
            NumVariationCases = variationCases.Count;
        }
        else
        {
            VariationCases = accelerator.Allocate2DDenseX(variations);
            NumVariationCases = variations.GetLength(0);
        }
    }

    public DevicePuzzleCases DeviceView()
    {
        return new()
        {
            ShuffleCases = ShuffleCases.View,
            VariationCases = VariationCases.View
        };
    }

    public void Dispose()
    {
        ShuffleCases?.Dispose();
        VariationCases?.Dispose();
    }
}

public struct DevicePuzzleCases
{
    // (Index, VariationIndex, Pieces)
    public ArrayView2D<byte, Stride2D.DenseX> ShuffleCases;
    public ArrayView2D<byte, Stride2D.DenseX> VariationCases;
}
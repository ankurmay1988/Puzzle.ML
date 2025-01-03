﻿using Combinatorics.Collections;
using ILGPU;
using ILGPU.Runtime;

namespace Puzzle.ML;

public struct HostPuzzleCases : IDisposable
{
    private readonly Accelerator accelerator;
    public MemoryBuffer2D<int, Stride2D.DenseX> ShuffleCases;
    public MemoryBuffer2D<int, Stride2D.DenseX> VariationCases;
    public HostPuzzleCases(Accelerator accelerator, PuzzleData puzzle, int[,]? shuffleCases = null, int[,]? variationCases = null)
    {
        this.accelerator = accelerator;
        ShuffleCases = default!;
        VariationCases = default!;
        Initialize(puzzle, shuffleCases, variationCases);
    }

    private void Initialize(PuzzleData puzzle, int[,]? shuffled, int[,]? variations)
    {
        if (shuffled == null)
        {
            var cases_Perm = new Permutations<int>(Enumerable.Range(0, puzzle.Pieces.Count)).ToList();
            var arrShuffleCases = new int[cases_Perm.Count, puzzle.Pieces.Count];
            var casesIdx = Enumerable.Range(0, cases_Perm.Count).SelectMany(i => Enumerable.Range(0, puzzle.Pieces.Count).Select(j => (i, j)));

            Parallel.ForEach(casesIdx, idx =>
            {
                arrShuffleCases[idx.i, idx.j] = cases_Perm[idx.i][idx.j];
            });

            ShuffleCases = accelerator.Allocate2DDenseX(arrShuffleCases);
        }
        else
        {
            ShuffleCases = accelerator.Allocate2DDenseX(shuffled);
        }

        if (variations == null)
        {
            var variationCases = new Variations<int>(Enumerable.Range(0, puzzle.NumVariations), puzzle.Pieces.Count, GenerateOption.WithRepetition).ToList();
            var arrVariationCases = new int[variationCases.Count, puzzle.Pieces.Count];
            var variationIdx = Enumerable.Range(0, variationCases.Count).SelectMany(i => Enumerable.Range(0, puzzle.Pieces.Count).Select(j => (i, j)));

            Parallel.ForEach(variationIdx, idx =>
            {
                arrVariationCases[idx.i, idx.j] = variationCases[idx.i][idx.j];
            });

            VariationCases = accelerator.Allocate2DDenseX(arrVariationCases);
        }
        else
        {
            VariationCases = accelerator.Allocate2DDenseX(variations);
        }
    }

    public DevicePuzzleCases ToDevice() => new()
    {
        ShuffleCases = ShuffleCases,
        VariationCases = VariationCases
    };

    public void Dispose()
    {
        ShuffleCases?.Dispose();
        VariationCases?.Dispose();
    }
}

public struct DevicePuzzleCases
{
    // (Index, VariationIndex, Pieces)
    public ArrayView2D<int, Stride2D.DenseX> ShuffleCases;
    public ArrayView2D<int, Stride2D.DenseX> VariationCases;
}
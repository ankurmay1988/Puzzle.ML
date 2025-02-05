using Combinatorics.Collections;
using CommunityToolkit.HighPerformance;
using System.Buffers;

namespace Puzzle.ML;

public struct PuzzleCases : IDisposable
{
    public readonly int NumPieces;
    public int NumShuffleCases;
    public Memory2D<byte> ShuffleCases;
    public int NumVariationCases;
    public Memory2D<byte> VariationCases;

    private readonly PuzzleData puzzle;

    private IMemoryOwner<byte> ownerShuffleCases;
    private IMemoryOwner<byte> ownerVariationCases;

    public PuzzleCases(PuzzleData puzzle)
    {
        ShuffleCases = default!;
        VariationCases = default!;
        NumPieces = puzzle.Pieces.Count;
        this.puzzle = puzzle;
        ownerShuffleCases = default!;
        ownerVariationCases = default!;
    }

    public void Generate(byte[,]? shuffled = null, byte[,]? variations = null)
    {
        var numPieces = puzzle.Pieces.Count;
        if (shuffled == null)
        {
            var cases_Perm = new Permutations<byte>(Enumerable.Range(0, numPieces).Select(x => (byte)x)).ToList();
            var casesIdx = Enumerable.Range(0, cases_Perm.Count).SelectMany(i => Enumerable.Range(0, numPieces).Select(j => (i, j)));
            this.ownerShuffleCases = MemoryPool<byte>.Shared.Rent(cases_Perm.Count * numPieces);
            var arrShuffleCases = ownerShuffleCases.Memory.AsMemory2D(cases_Perm.Count, numPieces);

            Parallel.ForEach(casesIdx, idx =>
            {
                arrShuffleCases.Span[idx.i, idx.j] = cases_Perm[idx.i][idx.j];
            });

            ShuffleCases = arrShuffleCases;
            NumShuffleCases = cases_Perm.Count;
        }
        else
        {
            ShuffleCases = shuffled;
            NumShuffleCases = shuffled.GetLength(0);
        }

        if (variations == null)
        {
            var variationCases = new Variations<byte>(Enumerable.Range(0, puzzle.NumVariations).Select(x => (byte)x), numPieces, GenerateOption.WithRepetition).ToList();
            var variationIdx = Enumerable.Range(0, variationCases.Count).SelectMany(i => Enumerable.Range(0, numPieces).Select(j => (i, j)));
            this.ownerVariationCases = MemoryPool<byte>.Shared.Rent(variationCases.Count * numPieces);
            var arrVariationCases = ownerVariationCases.Memory.AsMemory2D(variationCases.Count, numPieces);

            Parallel.ForEach(variationIdx, idx =>
            {
                arrVariationCases.Span[idx.i, idx.j] = variationCases[idx.i][idx.j];
            });

            VariationCases = arrVariationCases;
            NumVariationCases = variationCases.Count;
        }
        else
        {
            VariationCases = variations;
            NumVariationCases = variations.GetLength(0);
        }
    }

    public void Dispose()
    {
        ownerShuffleCases?.Dispose();
        ownerVariationCases?.Dispose();
    }
}

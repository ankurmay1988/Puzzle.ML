using Combinatorics.Collections;
using CommunityToolkit.HighPerformance;

namespace Puzzle.ML;

public struct PuzzleCases
{
    public readonly int NumPieces;
    public int NumShuffleCases;
    public Memory2D<byte> ShuffleCases;
    public int NumVariationCases;
    public Memory2D<byte> VariationCases;
    private readonly PuzzleData puzzle;

    public PuzzleCases(PuzzleData puzzle)
    {
        ShuffleCases = default!;
        VariationCases = default!;
        NumPieces = puzzle.Pieces.Count;
        this.puzzle = puzzle;
    }

    public void Generate(byte[,]? shuffled = null, byte[,]? variations = null)
    {
        var numPieces = puzzle.Pieces.Count;
        if (shuffled == null)
        {
            var cases_Perm = new Permutations<byte>(Enumerable.Range(0, numPieces).Select(x => (byte)x)).ToList();
            var arrShuffleCases = new byte[cases_Perm.Count, numPieces];
            var casesIdx = Enumerable.Range(0, cases_Perm.Count).SelectMany(i => Enumerable.Range(0, numPieces).Select(j => (i, j)));

            Parallel.ForEach(casesIdx, idx =>
            {
                arrShuffleCases[idx.i, idx.j] = cases_Perm[idx.i][idx.j];
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
            var arrVariationCases = new byte[variationCases.Count, numPieces];
            var variationIdx = Enumerable.Range(0, variationCases.Count).SelectMany(i => Enumerable.Range(0, numPieces).Select(j => (i, j)));

            Parallel.ForEach(variationIdx, idx =>
            {
                arrVariationCases[idx.i, idx.j] = variationCases[idx.i][idx.j];
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
}

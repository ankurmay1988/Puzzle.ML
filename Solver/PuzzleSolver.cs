using CommunityToolkit.HighPerformance;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.IR.Types;
using ILGPU.Runtime;
using ILGPU.Util;
using System.Numerics;

namespace Puzzle.ML.Solver;

internal class PuzzleSolver : IDisposable
{
    private readonly Accelerator accelerator;
    private readonly HostPuzzleData puzzleData;
    private readonly HostPuzzleCases puzzleCases;
    private readonly HostSolution hostSolution;

    public PuzzleSolver(Accelerator accelerator, PuzzleData puzzle)
    {
        this.accelerator = accelerator;
        this.puzzleData = new HostPuzzleData(accelerator, puzzle);
        var puzzleCases = new PuzzleCases(puzzle);
        puzzleCases.Generate();
        //puzzleCases.Generate(
        //    new byte[,] { { 3, 6, 4, 0, 2, 1, 7, 5 } },
        //    new byte[,] { { 0, 0, 2, 3, 0, 3, 0, 2 } });
        this.puzzleCases = new HostPuzzleCases(accelerator, puzzleCases);
        this.hostSolution = new HostSolution(accelerator, puzzleData);
    }

    public (bool, byte[], byte[], Memory2D<byte>) StartSolver()
    {
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, Index2D, DevicePuzzleData, DevicePuzzleCases, DeviceSolution>(PuzzleKernel.__kernel);
        var size = (puzzleCases.ShuffleCases.IntExtent.X, puzzleCases.VariationCases.IntExtent.X);

        int workSize = accelerator.WarpSize * 256;
        var gridDim = (workSize, workSize);

        var workRows = (size.Item1 + workSize - 1) / workSize;
        var workColumns = (size.Item2 + workSize - 1) / workSize;

        for (int i = 0; i < workRows; i++)
            for (int j = 0; j < workColumns; j++)
                kernel(gridDim, (i * workSize, j * workSize), puzzleData.DeviceView(), puzzleCases.DeviceView(), hostSolution.DeviceView());

        return SolutionIfFound();
    }

    private (bool, byte[], byte[], Memory2D<byte>) SolutionIfFound()
    {
        accelerator.Synchronize();
        var found = hostSolution.Found.GetAsArray1D();
        if (found[0] == 1)
        {
            var shuffle = hostSolution.Shuffle.GetAsArray1D();
            var variation = hostSolution.Variation.GetAsArray1D();
            var coords = hostSolution.Coords.GetAsArray1D().AsMemory().AsMemory2D(puzzleData.NumPieces, 2);
            // Reorders/transposes data on copying to CPU
            return (true, shuffle, variation, coords);
        }

        return (false, Array.Empty<byte>(), Array.Empty<byte>(), Memory2D<byte>.Empty);
    }

    public void Dispose()
    {
        this.puzzleData.Dispose();
        this.puzzleCases.Dispose();
        this.hostSolution.Dispose();
    }
}

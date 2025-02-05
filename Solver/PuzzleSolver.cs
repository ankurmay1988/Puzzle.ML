using CommunityToolkit.HighPerformance;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.IR.Types;
using ILGPU.Runtime;
using ILGPU.Util;
using System.Diagnostics;
using System.Numerics;

namespace Puzzle.ML.Solver;

internal class PuzzleSolver : IDisposable
{
    private readonly Accelerator accelerator;
    private readonly HostPuzzleData puzzleData;
    private readonly HostPuzzleCases puzzleCases;
    private readonly HostSolution hostSolution;

    public PuzzleSolver(Accelerator accelerator, PuzzleData puzzle, PuzzleCases puzzleCases)
    {
        this.accelerator = accelerator;
        this.puzzleData = new HostPuzzleData(accelerator, puzzle);
        this.puzzleCases = new HostPuzzleCases(accelerator, puzzleCases);
        this.hostSolution = new HostSolution(accelerator, puzzleData);
    }

    public (bool, byte[], byte[], Memory2D<byte>, TimeSpan) StartSolver()
    {
        var kernel = accelerator.LoadStreamKernel<Index2D, DevicePuzzleData, DevicePuzzleCases, DeviceSolution>(PuzzleKernel.__kernel);
        Index2D size = (puzzleCases.ShuffleCases.IntExtent.X, puzzleCases.VariationCases.IntExtent.X);

        var grpSize = accelerator.EstimateGroupSize(kernel.GetKernel());
        Index2D groupDim = (grpSize, 1);
        int workSize = groupDim.Size;
        var workRows = (size.X + workSize - 1) / workSize;
        var workColumns = (size.Y + workSize - 1) / workSize;
        Index2D gridDim = (workRows, workColumns);

        var sw = Stopwatch.StartNew();
        var config = new KernelConfig(gridDim, groupDim);
        for (int i = 0; i < workRows; i++)
            for (int j = 0; j < workColumns; j++)
                kernel(config, (i * workSize, j * workSize), puzzleData.DeviceView(), puzzleCases.DeviceView(), hostSolution.DeviceView());

        var result = SolutionIfFound();
        sw.Stop();
        return (result.Item1, result.Item2, result.Item3, result.Item4, sw.Elapsed);
    }

    public (bool, byte[], byte[], Memory2D<byte>, TimeSpan) StartSolver_Auto()
    {
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, Index2D, DevicePuzzleData, DevicePuzzleCases, DeviceSolution>(PuzzleKernel.__kernel_auto);
        var size = (puzzleCases.ShuffleCases.IntExtent.X, puzzleCases.VariationCases.IntExtent.X);

        int workSize = accelerator.WarpSize * 256;
        var gridDim = (workSize, workSize);

        var workRows = (size.Item1 + workSize - 1) / workSize;
        var workColumns = (size.Item2 + workSize - 1) / workSize;

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < workRows; i++)
            for (int j = 0; j < workColumns; j++)
                kernel(gridDim, (i * workSize, j * workSize), puzzleData.DeviceView(), puzzleCases.DeviceView(), hostSolution.DeviceView());

        var result = SolutionIfFound();
        sw.Stop();
        return (result.Item1, result.Item2, result.Item3, result.Item4, sw.Elapsed);
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

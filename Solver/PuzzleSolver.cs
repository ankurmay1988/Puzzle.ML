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
        //this.puzzleCases = new HostPuzzleCases(
        //    accelerator,
        //    puzzle,
        //    new byte[,] { { 3, 6, 4, 0, 2, 1, 7, 5 } },
        //    new byte[,] { { 0, 0, 2, 3, 0, 3, 0, 2 } });
        this.puzzleCases = new HostPuzzleCases(accelerator, puzzle);
        this.hostSolution = new HostSolution(accelerator, puzzleData);
    }

    public (bool, byte[], byte[], Memory2D<byte>) StartSolver()
    {
        //var kernel = accelerator.LoadGridStrideKernel<PuzzleKernelBody>();
        //var kernelBody = new PuzzleKernelBody(accelerator, puzzleData.ToDevice(), puzzleCases.ToDevice(), hostSolution.ToDevice());

        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, Index2D, DevicePuzzleData, DevicePuzzleCases, DeviceSolution>(PuzzleKernel.__kernel);
        var size = (puzzleCases.ShuffleCases.IntExtent.X, puzzleCases.VariationCases.IntExtent.X);

        Index2D blockDim = (accelerator.WarpSize, accelerator.WarpSize);
        if ((accelerator.WarpSize ^ 2) > accelerator.MaxNumThreadsPerGroup)
            blockDim = (accelerator.MaxNumThreadsPerGroup, 1);
        
        var threadsPerWork = blockDim.X * blockDim.Y;
        //BigInteger totalCasesLinearIdx = BigInteger.Multiply(size.Item1, size.Item2);
        //var totalBlocks = totalCasesLinearIdx / threadsPerWork;
        //var (totalBlocksPerWork, remaining) = BigInteger.DivRem(totalBlocks, workSize ^ 2);
        int workSize = 8096;
        var gridDim = (workSize, workSize);

        //var workRows = Enumerable.Chunk(Enumerable.Range(0, size.Item1), workSize);
        //var workColumns = Enumerable.Chunk(Enumerable.Range(0, size.Item2), workSize);
        var workRows = (size.Item1 + workSize - 1) / workSize;
        var workColumns = (size.Item2 + workSize - 1) / workSize;
        for (int i = 0; i < workRows; i++)
        {
            for (int j = 0; j < workColumns; j++)
            {
                Range slice = new(i * workSize, (i + 1) * workSize);
                if (i == workRows - 1)
                    slice = Range.StartAt(i * workSize);
                
                Range variationSlice = new(j * workSize, (j + 1) * workSize);
                if (j == workColumns - 1)
                    variationSlice = Range.StartAt(j * workSize);

                //var config = new KernelConfig(gridDim, blockDim);
                kernel(gridDim, (slice.Start.Value, variationSlice.Start.Value), puzzleData.DeviceView(), puzzleCases.DeviceView(), hostSolution.DeviceView());

                var (found, shuffle, variation, coords) = SolutionIfFound();
                if (found)
                    return (found, shuffle, variation, coords);
            }
        }

        return (false, Array.Empty<byte>(), Array.Empty<byte>(), Memory2D<byte>.Empty);
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
        this.puzzleData?.Dispose();
        this.puzzleCases.Dispose();
        this.hostSolution.Dispose();
    }
}

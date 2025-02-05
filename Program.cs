using ILGPU;
using ILGPU.Runtime.CPU;
using Puzzle.ML;
using Puzzle.ML.Solver;
using System.Diagnostics;

using (Context context = Context.Create(builder =>
    builder.EnableAlgorithms()
        .AllAccelerators()
#if DEBUG
        .Profiling()
        .DebugConfig(true, true, enableIRVerifier: true, forceDebuggingOfOptimizedKernels: true)
#else
        .Optimize(OptimizationLevel.O2)
#endif
    ))
{
    //using var accelerator = context.CreateCPUAccelerator(0);
    using var accelerator = context.GetPreferredDevice(false).CreateAccelerator(context);
    Console.WriteLine(accelerator);
    accelerator.Device.PrintInformation(Console.Out);
    
    PuzzleData puzzleData = new("30Jan");
    await puzzleData.Initialize();

    using var puzzleCases = new PuzzleCases(puzzleData);
    puzzleCases.Generate();
    //puzzleCases.Generate(
    //    new byte[,] { { 3, 6, 4, 0, 2, 1, 7, 5 } },
    //    new byte[,] { { 0, 0, 2, 3, 0, 3, 0, 2 } });

    using PuzzleSolver solver = new(accelerator, puzzleData, puzzleCases);
    var (found, shuffle, variation, coords, timeTaken) = solver.StartSolver();

    Console.WriteLine($"Found: {found} in {timeTaken.TotalSeconds} seconds");
    if (found)
    {
        Console.WriteLine($"Shuffled: {shuffle.Aggregate("", (p, n) => $"{p} {n}")}");
        Console.WriteLine($"Variation: {variation.Aggregate("", (p, n) => $"{p} {n}")}");
        Console.WriteLine($"Coord: {Enumerable.Range(0, puzzleData.Pieces.Count).Aggregate("", (p, n) => $"{p} ({coords.Span[n, 0]}, {coords.Span[n, 1]})")}");
        Utils.DrawImagePieces(puzzleData, shuffle, variation, coords);
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "solution.png"
        });
    }
}


Console.ReadKey();

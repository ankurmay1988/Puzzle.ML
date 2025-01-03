using CommunityToolkit.HighPerformance;
using ILGPU;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.OpenCL;
using Puzzle.ML;
using Puzzle.ML.Solver;
using System.Diagnostics;

using (Context context = Context.Create(builder => builder.AllAccelerators()))
{
    //foreach (var d in context)
    //{
    //    Console.WriteLine(d);
    //}

    //using var accelerator = context.CreateCPUAccelerator(0);
    using var accelerator = context.CreateCLAccelerator(0);
    Console.WriteLine(accelerator);
    PuzzleData puzzleData = new("03Jan");
    using PuzzleSolver solver = new(accelerator, puzzleData);
    var (found, shuffle, variation, coords) = solver.StartSolver();
    if (found)
    {
        Console.WriteLine($"Found: {found}");
        Console.WriteLine($"Shuffled: {shuffle.Aggregate("", (p, n) => $"{p} {n}")}");
        Console.WriteLine($"Variation: {variation.Aggregate("", (p, n) => $"{p} {n}")}");
        Console.WriteLine($"Coord: {Enumerable.Range(0, puzzleData.Pieces.Count).Aggregate("", (p, n) => $"{p} ({coords.Span[n,0]}, {coords.Span[n, 1]})")}");
        Utils.DrawImagePieces(puzzleData, shuffle, variation, coords);
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "solution.png"
        });
    }
}


Console.ReadKey();
Environment.Exit(0);

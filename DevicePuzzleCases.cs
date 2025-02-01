using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;

namespace Puzzle.ML;

public struct HostPuzzleCases : IDisposable
{
    public MemoryBuffer2D<byte, Stride2D.DenseX> ShuffleCases;
    public MemoryBuffer2D<byte, Stride2D.DenseX> VariationCases;
    public HostPuzzleCases(Accelerator accelerator, PuzzleCases puzzleCases)
    {
        ShuffleCases = default!;
        VariationCases = default!;
        ShuffleCases = accelerator.Allocate2DDenseX(puzzleCases.ShuffleCases.ToArray());
        VariationCases = accelerator.Allocate2DDenseX(puzzleCases.VariationCases.ToArray());
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
using ILGPU;
using ILGPU.Runtime;

namespace Puzzle.ML;

public struct HostSolution(Accelerator accelerator, HostPuzzleData puzzleData) : IDisposable
{
    public MemoryBuffer1D<byte, Stride1D.Dense> Found = accelerator.Allocate1D<byte>(1);
    public MemoryBuffer1D<byte, Stride1D.Dense> Shuffle = accelerator.Allocate1D<byte>(puzzleData.NumPieces);
    public MemoryBuffer1D<byte, Stride1D.Dense> Variation = accelerator.Allocate1D<byte>(puzzleData.NumPieces);
    public MemoryBuffer1D<byte, Stride1D.Dense> Coords = accelerator.Allocate1D<byte>(puzzleData.NumPieces * 2);
    public DeviceSolution ToDevice()
    {
        return new()
        {
            Found = Found.View.VariableView(0),
            Shuffle = Shuffle.View,
            Variation = Variation.View,
            Coords = Coords.View,
        };
    }

    public void Dispose()
    {
        Found.Dispose();
        Shuffle.Dispose();
        Variation.Dispose();
        Coords.Dispose();
    }
}
public struct DeviceSolution
{
    public VariableView<byte> Found;
    public ArrayView1D<byte, Stride1D.Dense> Shuffle;
    public ArrayView1D<byte, Stride1D.Dense> Variation;
    public ArrayView1D<byte, Stride1D.Dense> Coords;
}

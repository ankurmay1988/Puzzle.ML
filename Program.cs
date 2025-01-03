using CommunityToolkit.HighPerformance;
using ILGPU;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.OpenCL;
using Puzzle.ML;
using Puzzle.ML.Solver;
using SkiaSharp;
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
        DrawImagePieces(puzzleData, shuffle, variation, coords);
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "solution.png"
        });
    }
}

void DrawImagePieces(PuzzleData puzzleData, int[] shuffle, int[] variation, int[,] coords)
{
    // unit size
    var s = 20;
    var p = puzzleData.Pieces.Count;
    // width includes padding and gaps between shapes
    var w = (5 * s * p) + (9 * s);
    var h = (12 * s) + (3 * s);
    SKColor[] palette = [ SKColors.IndianRed, SKColors.HotPink, SKColors.OrangeRed, SKColors.BlueViolet,
                          SKColors.Cornsilk, SKColors.Green, SKColors.Goldenrod, SKColors.Olive ];
    using var image = new SKBitmap(w, h);
    using var canvas = new SKCanvas(image);
    canvas.SetMatrix(SKMatrix.CreateTranslation(s, s));
    var fillPaint = new SKPaint()
    {
        Style = SKPaintStyle.Fill
    };

    for (int i = 0; i < p; i++)
    {
        var piece = puzzleData.Pieces[shuffle[i]].Variations[variation[i]];
        DrawPiece(piece, palette[shuffle[i]], canvas);
        canvas.Translate((piece.Width * s) + s, 0);
    }


    for (int i = 0; i < puzzleData.Pieces.Count; i++)
    {
        var c = (coords[i, 0], coords[i, 1]);
        canvas.SetMatrix(SKMatrix.CreateTranslation(5 * s, 7 * s));
        canvas.Translate(s * c.Item2, s * c.Item1);
        var piece = puzzleData.Pieces[shuffle[i]].Variations[variation[i]];
        DrawPiece(piece, palette[shuffle[i]], canvas);
    }

    using var fs = new FileStream("solution.png", FileMode.Create);
    image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);
}

void DrawPiece(PieceData piece, SKColor color, SKCanvas canvas)
{
    // unit size
    var s = 20;
    var fillPaint = new SKPaint()
    {
        Style = SKPaintStyle.Fill
    };

    var pieceData = piece.Data.AsSpan().AsSpan2D(piece.Height, piece.Width);

    for (int j = 0; j < piece.Height; j++)
    {
        for (int k = 0; k < piece.Width; k++)
        {
            if (pieceData[j, k] == 1)
            {
                fillPaint.Color = color;
                canvas.DrawRect(s + (k * s), j * s, s, s, fillPaint);
            }
        }
    }
}

Console.ReadKey();
Environment.Exit(0);

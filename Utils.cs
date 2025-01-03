using CommunityToolkit.HighPerformance;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puzzle.ML;

internal static class Utils
{
    public static void DrawImagePieces(PuzzleData puzzleData, byte[] shuffle, byte[] variation, Memory2D<byte> coords)
    {
        // unit size
        var s = 20;
        var p = puzzleData.Pieces.Count;
        // width includes padding and gaps between shapes
        var w = 5 * s * p + 9 * s;
        var h = 12 * s + 3 * s;
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
            canvas.Translate(piece.Width * s + s, 0);
        }


        for (int i = 0; i < puzzleData.Pieces.Count; i++)
        {
            var c = (coords.Span[i, 0], coords.Span[i, 1]);
            canvas.SetMatrix(SKMatrix.CreateTranslation(5 * s, 7 * s));
            canvas.Translate(s * c.Item2, s * c.Item1);
            var piece = puzzleData.Pieces[shuffle[i]].Variations[variation[i]];
            DrawPiece(piece, palette[shuffle[i]], canvas);
        }

        using var fs = new FileStream("solution.png", FileMode.Create);
        image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);
    }

    private static void DrawPiece(PieceData piece, SKColor color, SKCanvas canvas)
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
                    canvas.DrawRect(s + k * s, j * s, s, s, fillPaint);
                }
            }
        }
    }

    public static string PrettyPrint<T>(T[,] matrix) where T : unmanaged
    {
        // When building string, better use StringBuilder instead of String
        StringBuilder matrixView = new StringBuilder();

        // Note r < matrix.GetLength(0), matrix is zero based [0..this.rows - 1]
        // When printing array, let's query this array - GetLength(0)
        for (int r = 0; r < matrix.GetLength(0); r++)
        {
            // Starting new row we should add row delimiter 
            if (r > 0)
                matrixView.AppendLine();

            // New line starts from [
            matrixView.Append("[");

            // Note r < matrix.GetLength(1), matrix is zero based [0..this.rows - 1]
            for (int c = 0; c < matrix.GetLength(1); c++)
            {
                // Starting new column we should add column delimiter 
                if (c > 0)
                    matrixView.Append(' ');

                matrixView.Append(matrix[r, c]);
            }

            // New line ends with [
            matrixView.Append("]");
        }

        return matrixView.ToString();
    }
}

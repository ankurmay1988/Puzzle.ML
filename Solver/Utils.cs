using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puzzle.ML.CustomOperations;

internal static class Utils
{
    public static IEnumerable<int> GetCaseArr(double num, int radix, int pieces)
    {
        for (int i = 0; i < pieces; i++)
        {
            yield return (int)(num % radix);
            num /= radix;
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

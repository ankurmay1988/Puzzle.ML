namespace Puzzle.ML;

internal class Constants
{
    public static int[,] Piece1 = {
        { 1, 1, 1 },
        { 0, 1, 0 },
        { 0, 1, 0 } };

    public static int[,] Piece2 = {
        {1, 1, 1 },
        {1, 0, 1}};

    public static int[,] Piece3 = {
        {1, 1},
        {1, 1},
        {1, 1}};

    public static int[,] Piece4 = {
        {1, 1, 1, 0},
        {0, 0, 1, 1}};

    public static int[,] Piece5 = {
        {1, 1, 1, 1},
        {1, 0, 0, 0}};

    public static int[,] Piece6 = {
        {1, 1, 1, 1},
        {0, 1, 0, 0}};

    public static int[,] Piece7 = {
        {1, 1, 1},
        {1, 1, 0}};

    public static int[,] Piece8 = {
        {1, 1, 1},
        {1, 0, 0},
        {1, 0, 0}};

    public static int[,] Board = {
        {0, 0, 0, 0, 0, 0, 1},
        {0, 0, 0, 0, 0, 0, 1},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 1}};
}

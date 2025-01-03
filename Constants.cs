namespace Puzzle.ML;

internal class Constants
{
    public static byte[,] Piece1 = {
        { 1, 1, 1 },
        { 0, 1, 0 },
        { 0, 1, 0 } };

    public static byte[,] Piece2 = {
        {1, 1, 1 },
        {1, 0, 1}};

    public static byte[,] Piece3 = {
        {1, 1},
        {1, 1},
        {1, 1}};

    public static byte[,] Piece4 = {
        {1, 1, 1, 0},
        {0, 0, 1, 1}};

    public static byte[,] Piece5 = {
        {1, 1, 1, 1},
        {1, 0, 0, 0}};

    public static byte[,] Piece6 = {
        {1, 1, 1, 1},
        {0, 1, 0, 0}};

    public static byte[,] Piece7 = {
        {1, 1, 1},
        {1, 1, 0}};

    public static byte[,] Piece8 = {
        {1, 1, 1},
        {1, 0, 0},
        {1, 0, 0}};

    public static byte[,] Board = {
        {0, 0, 0, 0, 0, 0, 1},
        {0, 0, 0, 0, 0, 0, 1},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 1, 1, 1, 1}};
}

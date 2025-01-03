﻿using CommunityToolkit.HighPerformance;
using Numpy;

namespace Puzzle.ML;

public struct PieceData
{
    public int VariationIndex;
    public int Width;
    public int Height;
    public int[] Data;
}

public struct Piece
{
    public string Name;
    public List<PieceData> Variations;

    public Piece()
    {
        Name = string.Empty;
        Variations = [];
    }
}

public struct PuzzleData
{
    public List<Piece> Pieces = [];
    public int BoardWidth = Constants.Board.GetLength(1);
    public int BoardHeight = Constants.Board.GetLength(0);
    public int[] BoardData;
    public int NumVariations = 4;
    private readonly DateOnly dateOnly;

    public PuzzleData()
        : this(DateOnly.FromDateTime(DateTime.Today))
    { }

    public PuzzleData(string dateStr)
        : this(DateOnly.ParseExact(dateStr, "ddMMM"))
    { }

    public PuzzleData(DateOnly dateOnly)
    {
        BoardData = PuzzleData.NewBoard;
        this.dateOnly = dateOnly;
        Initialize();
    }

    public void Initialize()
    {
        int[] monthIdx = [(dateOnly.Month - 1) / BoardWidth, (dateOnly.Month - 1) % BoardWidth];
        int[] dayIdx = [2 + ((dateOnly.Day - 1) / BoardWidth), (dateOnly.Day - 1) % BoardWidth];

        int[][] fixedPos = [monthIdx, dayIdx];
        var board = BoardData.AsSpan().AsSpan2D(BoardHeight, BoardWidth);
        foreach (var pos in fixedPos)
            board[pos[0], pos[1]] = 1;

        List<NDarray<int>> pieces = [
            np.array(Constants.Piece1),
            np.array(Constants.Piece2),
            np.array(Constants.Piece3),
            np.array(Constants.Piece4),
            np.array(Constants.Piece5),
            np.array(Constants.Piece6),
            np.array(Constants.Piece7),
            np.array(Constants.Piece8),
        ];

        IEnumerable<NDarray> PieceVariations(NDarray piece)
        {
            yield return piece;
            var p = np.rot90(piece, 1)!;
            yield return p;
            p = np.rot90(piece, 2)!;
            yield return p;
            p = np.rot90(piece, 3)!;
            yield return p;
        }

        foreach (var (piece, idx) in pieces.Select((item, idx) => (item, idx)))
        {
            var pieceObj = new Piece()
            {
                Name = $"Piece{idx + 1}"
            };

            foreach (var (v, vidx) in PieceVariations(piece).Select((item, idx) => (item, idx)))
            {
                var data = v.GetData<int>();
                var pData = new PieceData()
                {
                    Data = data,
                    Height = v.shape[0],
                    Width = v.shape[1],
                    VariationIndex = vidx
                };

                pieceObj.Variations.Add(pData);
            }

            Pieces.Add(pieceObj);
        }
    }

    public static int[] NewBoard
    {
        get
        {
            return Constants.Board.AsSpan().ToArray();
        }
    }

    public readonly int[,] BoardDataArray
    {
        get
        {
            var s = new Span2D<int>(BoardData, BoardHeight, BoardWidth);
            return s.ToArray();
        }
    }
}
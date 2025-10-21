using Gdk;
using Gtk;
using System.Collections.Generic;
using System.IO;
using Controller_namespace;
using System.Net.Sockets;
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Pieces_namespace
{
    public abstract class Piece
    {
        public abstract void Move_to((int, int) new_position, Board board);

        public abstract List<Move> Show_Possible_Moves(Board board);

        public abstract Piece Copy();

        public abstract string Address { get; }

        public abstract (int, int) Pos { get; set; }

        public abstract int Color { get; }

        public abstract string Type { get; }

        protected bool is_valid_move(int i, int j)
        {
            return (i >= 0 && i < 8 && j >= 0 && j < 8);
        }

        public static string FindSourceDirectory(string fileName)
        {
            string exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string projectDirectory = exeDirectory;
            while (!Directory.GetFiles(projectDirectory, "*.csproj").Any())
            {
                string parentDirectory = Directory.GetParent(projectDirectory)?.FullName;
                if (parentDirectory == projectDirectory || parentDirectory == null)
                {
                    return null;
                }
                projectDirectory = parentDirectory;
            }

            string[] sourceFiles = Directory.GetFiles(projectDirectory, fileName, SearchOption.AllDirectories);

            string sourceDirectory = Path.GetDirectoryName(sourceFiles.FirstOrDefault());

            return sourceDirectory;
        }
    }

    class Pawn : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        public bool FirstMove = true;
        private string type = "Pawn";

        public Pawn(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");

            string BlackImage = Path.Combine(fullPath, "pieces\\black_pawn.svg.png");
            string WhiteImage = Path.Combine(fullPath, "pieces\\white_pawn.svg.png");

            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        public override List<Move> Show_Possible_Moves(Board board)
        {
            List<Move> Moves = new List<Move>();
            int from_x = position.Item1;
            int from_y = position.Item2;

            // Single move forward
            int one_step = from_y - color;
            if (is_valid_move(from_x, one_step) && board[from_x, one_step] == null)
            {
                Moves.Add(new Move(from_x, from_y, from_x, one_step));

                // Double move forward (only if single is possible)
                if (FirstMove)
                {
                    int two_steps = from_y - 2 * color;
                    if (is_valid_move(from_x, two_steps) && board[from_x, two_steps] == null)
                    {
                        Moves.Add(new Move(from_x, from_y, from_x, two_steps));
                    }
                }
            }

            // Captures
            int[] capture_dirs = { -1, 1 };
            foreach (int dir in capture_dirs)
            {
                int to_x = from_x + dir;
                int to_y = from_y - color;
                if (is_valid_move(to_x, to_y) && board[to_x, to_y] != null && board[to_x, to_y].Color != color)
                {
                    Moves.Add(new Move(from_x, from_y, to_x, to_y));
                }
            }

            // TODO: Implement En Passant logic here

            return Moves;
        }

        public override void Move_to((int, int) new_position, Board board)
        {
            if (FirstMove) FirstMove = false;
            
            board[new_position.Item1, new_position.Item2] = this;
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }
        
        public override Piece Copy()
        {
            return new Pawn(this.Color, this.position) { FirstMove = this.FirstMove };
        }

        public override string Address => address;
        public override (int, int) Pos { get => position; set => position = value; }
        public override int Color => color;
        public override string Type => type;
    }

    class Knight : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Knight";

        public Knight(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");
            string BlackImage = Path.Combine(fullPath, "pieces\\black_knight.svg.png");
            string WhiteImage = Path.Combine(fullPath, "pieces\\white_knight.svg.png");
            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        public override List<Move> Show_Possible_Moves(Board board)
        {
            List<Move> moves = new List<Move>();
            (int, int)[] vectors = { (2, 1), (2, -1), (-2, 1), (-2, -1), (1, 2), (1, -2), (-1, 2), (-1, -2) };
            foreach ((int, int) vector in vectors)
            {
                int to_x = position.Item1 + vector.Item1;
                int to_y = position.Item2 + vector.Item2;
                if (is_valid_move(to_x, to_y))
                {
                    if (board[to_x, to_y] == null || board[to_x, to_y].Color != color)
                    {
                        moves.Add(new Move(position.Item1, position.Item2, to_x, to_y));
                    }
                }
            }
            return moves;
        }

        public override void Move_to((int, int) new_position, Board board)
        {
            board[new_position.Item1, new_position.Item2] = this;
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override Piece Copy() => new Knight(this.color, this.position);
        public override string Address => address;
        public override (int, int) Pos { get => position; set => position = value; }
        public override int Color => color;
        public override string Type => type;
    }

    class Bishop : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Bishop";

        public Bishop(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");
            string BlackImage = Path.Combine(fullPath, "pieces\\black_bishop.svg.png");
            string WhiteImage = Path.Combine(fullPath, "pieces\\white_bishop.svg.png");
            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        public override List<Move> Show_Possible_Moves(Board board)
        {
            List<Move> moves = new List<Move>();
            (int, int)[] vectors = { (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach ((int, int) vector in vectors)
            {
                (int, int) new_position = (position.Item1 + vector.Item1, position.Item2 + vector.Item2);
                while (is_valid_move(new_position.Item1, new_position.Item2))
                {
                    if (board[new_position.Item1, new_position.Item2] != null)
                    {
                        if (board[new_position.Item1, new_position.Item2].Color != color)
                        {
                            moves.Add(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
                        }
                        break;
                    }
                    moves.Add(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
                    new_position = (new_position.Item1 + vector.Item1, new_position.Item2 + vector.Item2);
                }
            }
            return moves;
        }

        public override void Move_to((int, int) new_position, Board board)
        {
            board[new_position.Item1, new_position.Item2] = this;
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override Piece Copy() => new Bishop(this.color, this.position);
        public override string Address => address;
        public override (int, int) Pos { get => position; set => position = value; }
        public override int Color => color;
        public override string Type => type;
    }

    class Rook : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Rook";
        public bool hasMoved = false;

        public Rook(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");
            string BlackImage = Path.Combine(fullPath, "pieces\\black_rook.svg.png");
            string WhiteImage = Path.Combine(fullPath, "pieces\\white_rook.svg.png");
            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        public override List<Move> Show_Possible_Moves(Board board)
        {
            List<Move> moves = new List<Move>();
            (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1) };
            foreach ((int, int) vector in vectors)
            {
                (int, int) new_position = (position.Item1 + vector.Item1, position.Item2 + vector.Item2);
                while (is_valid_move(new_position.Item1, new_position.Item2))
                {
                    if (board[new_position.Item1, new_position.Item2] != null)
                    {
                        if (board[new_position.Item1, new_position.Item2].Color != color)
                        {
                            moves.Add(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
                        }
                        break;
                    }
                    moves.Add(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
                    new_position = (new_position.Item1 + vector.Item1, new_position.Item2 + vector.Item2);
                }
            }
            return moves;
        }
        
        public override void Move_to((int, int) new_position, Board board)
        {
            if (!hasMoved) hasMoved = true;
            board[new_position.Item1, new_position.Item2] = this;
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override Piece Copy() => new Rook(this.color, this.position) { hasMoved = this.hasMoved };
        public override string Address => address;
        public override (int, int) Pos { get => position; set => position = value; }
        public override int Color => color;
        public override string Type => type;
    }

    class Queen : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Queen";

        public Queen(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");
            string BlackImage = Path.Combine(fullPath, "pieces\\black_queen.svg.png");
            string WhiteImage = Path.Combine(fullPath, "pieces\\white_queen.svg.png");
            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        public override List<Move> Show_Possible_Moves(Board board)
        {
            List<Move> moves = new List<Move>();
            (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach ((int, int) vector in vectors)
            {
                (int, int) new_position = (position.Item1 + vector.Item1, position.Item2 + vector.Item2);
                while (is_valid_move(new_position.Item1, new_position.Item2))
                {
                    if (board[new_position.Item1, new_position.Item2] != null)
                    {
                        if (board[new_position.Item1, new_position.Item2].Color != color)
                        {
                            moves.Add(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
                        }
                        break;
                    }
                    moves.Add(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
                    new_position = (new_position.Item1 + vector.Item1, new_position.Item2 + vector.Item2);
                }
            }
            return moves;
        }
        
        public override void Move_to((int, int) new_position, Board board)
        {
            board[new_position.Item1, new_position.Item2] = this;
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override Piece Copy() => new Queen(this.color, this.position);
        public override string Address => address;
        public override (int, int) Pos { get => position; set => position = value; }
        public override int Color => color;
        public override string Type => type;
    }

    class King : Piece
{
    private int color;
    private (int, int) position;
    private string address;
    private string type = "King";
    public bool hasMoved = false;

    public King(int color, (int, int)position)
    {
        this.color = color;
        this.position = position;
        string fullPath = FindSourceDirectory("Pieces.cs");
        string BlackImage = Path.Combine(fullPath, "pieces\\black_king.svg.png");
        string WhiteImage = Path.Combine(fullPath, "pieces\\white_king.svg.png");
        this.address = color == 1 ? WhiteImage : BlackImage;
    }

    // NEW METHOD: Generates only the 8 adjacent moves without castling checks. This is safe to call from anywhere.
    public List<Move> GetStandardMoves()
    {
        List<Move> moves = new List<Move>();
        (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
        foreach ((int, int)vector in vectors)
        {
            int to_x = position.Item1 + vector.Item1;
            int to_y = position.Item2 + vector.Item2;
            if (is_valid_move(to_x, to_y))
            {
                // In this context, we don't check board state, just the potential squares.
                moves.Add(new Move(position.Item1, position.Item2, to_x, to_y));
            }
        }
        return moves;
    }

    public override List<Move> Show_Possible_Moves(Board board)
    {
        List<Move> moves = new List<Move>();
        // Get standard moves and validate them against the board state
        (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
        foreach ((int, int) vector in vectors)
        {
            int to_x = position.Item1 + vector.Item1;
            int to_y = position.Item2 + vector.Item2;
            if (is_valid_move(to_x, to_y))
            {
                if (board[to_x, to_y] == null || board[to_x, to_y].Color != color)
                {
                    moves.Add(new Move(position.Item1, position.Item2, to_x, to_y));
                }
            }
        }


        // --- Castling Logic --- (This part remains recursive, but is no longer called by IsSquareAttacked)
        if (!hasMoved)
        {
            // Kingside
            if (board.CanCastle(color, isKingside: true))
            {
                if (board[position.Item1 + 1, position.Item2] == null && board[position.Item1 + 2, position.Item2] == null)
                {
                    if (!board.IsSquareAttacked(position, -color) && !board.IsSquareAttacked((position.Item1 + 1, position.Item2), -color) && !board.IsSquareAttacked((position.Item1 + 2, position.Item2), -color))
                    {
                        moves.Add(new Move(position.Item1, position.Item2, position.Item1 + 2, position.Item2) { isCastling = true });
                    }
                }
            }
            // Queenside
            if (board.CanCastle(color, isKingside: false))
            {
                if (board[position.Item1 - 1, position.Item2] == null && board[position.Item1 - 2, position.Item2] == null && board[position.Item1 - 3, position.Item2] == null)
                {
                    if (!board.IsSquareAttacked(position, -color) && !board.IsSquareAttacked((position.Item1 - 1, position.Item2), -color) && !board.IsSquareAttacked((position.Item1 - 2, position.Item2), -color))
                    {
                        moves.Add(new Move(position.Item1, position.Item2, position.Item1 - 2, position.Item2) { isCastling = true });
                    }
                }
            }
        }
        return moves;
    }

    public override void Move_to((int, int) new_position, Board board)
    {
        if (!hasMoved) hasMoved = true;
        board[new_position.Item1, new_position.Item2] = this;
        board[position.Item1, position.Item2] = null;
        position = new_position;
    }

    public override Piece Copy() => new King(this.color, this.position) { hasMoved = this.hasMoved };
    public override string Address => address;
    public override (int, int) Pos { get => position; set => position = value; }
    public override int Color => color;
    public override string Type => type;
}
}
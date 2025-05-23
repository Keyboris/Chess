﻿using Gdk;
using Gtk;
using System.Collections.Generic;
using System.IO;
using Controller_namespace;
using System.Net.Sockets;
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Runtime.CompilerServices;

namespace Pieces_namespace
{

    public abstract class Piece
    {
        public abstract void Move_to((int, int) new_position, Board board);

        public abstract void Unmove(Board board);
        public abstract (int, int)[] Show_Possible_Moves(Board board);

        public abstract Piece Copy();

        public abstract string Address { get; }

        public abstract (int, int) Pos { get; set; }

        public abstract int Color { get; }

        public abstract string Type { get; }

        public static string FindSourceDirectory(string fileName)
        {
            string exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string projectDirectory = exeDirectory;
            while (!Directory.GetFiles(projectDirectory, "*.csproj").Any())
            {
                string parentDirectory = Directory.GetParent(projectDirectory).FullName;
                if (parentDirectory == projectDirectory)
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
        private (int, int) position;   //x, y
        private string address;
        public bool FirstMove = true;
        private string type = "Pawn";
        private Piece? lastCaptured;
        private (int, int) lastPos;
        private bool lastFirstMove;
        private bool promoted;
        private Stack<Move> lastMoves = new Stack<Move>();

        public Pawn (int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");

            string BlackImage = Path.Combine(fullPath, "pieces\\black_pawn.svg.png");

            string WhiteImage = Path.Combine(fullPath, "pieces\\white_pawn.svg.png");

            this.address = color == 1 ? WhiteImage : BlackImage;
        }


        private bool is_valid_move(int i, int j)
        {
            return (i < 8 && j < 8 && i > -1 && j > -1);
        }

        public override (int, int)[] Show_Possible_Moves(Board board)
        {
            List<(int, int)> Moves = new List<(int, int)>();
            if (FirstMove)
            {
                for (int j = 1; j < 3; j++)
                {
                    if (board[position.Item1, position.Item2 + j * -color] == null)  //do not need to check for the validity of the move here, this is the first move of the pawn
                    {
                        Moves.Add((position.Item1, position.Item2 + j * -color));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                for (int j = 1; j < 2; j++)
                {
                    if (is_valid_move(position.Item1, position.Item2 + j * -color))  //god damn it does not use lazy evaluation(((
                    {
                        if (board[position.Item1, position.Item2 + j * -color] == null)
                        {
                            Moves.Add((position.Item1, position.Item2 + j * -color));
                        }
                    }
                }
            }

            for (int j = -1; j < 2; j++)
            {
                if (j == 0) continue;
                if (is_valid_move(position.Item1 + j, position.Item2 + 1 * -color))
                {
                    if (board[position.Item1 + j, position.Item2 + 1 * -color] != null && board[position.Item1 + j, position.Item2 + 1 * -color].Color != color)
                    {
                        Moves.Add((position.Item1 + j, position.Item2 + 1 * -color));
                    }
                    
                }

            }

            return Moves.ToArray();
        }

        public override void Move_to((int, int) new_position, Board board)
        {
            if (FirstMove)
            {
                lastFirstMove = true;
            }
            else
            {
                lastFirstMove = false;
            }
            FirstMove = false;
            {
                if (board[new_position.Item1, new_position.Item2] is not null)
                {
                    lastCaptured = board[new_position.Item1, new_position.Item2];
                }
                else
                {
                    lastCaptured = null;
                }
                if ((new_position.Item2 == 7 && color == -1) || (new_position.Item2 == 0 && color == 1))
                {
                    promoted = true;
                }
                else
                {
                    promoted = false;
                }


                lastMoves.Push(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
                board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
                board[position.Item1, position.Item2] = null;
                position = new_position;
            }
        }

        public override void Unmove(Board board)
        {
            // Restore the piece to its original position
            Move new_pos = lastMoves.Pop();
            board[new_pos.from_x, new_pos.from_y] = this;

            // If there was a captured piece, restore it to its original position
            if (lastCaptured is not null)
            {
                board[position.Item1, position.Item2] = lastCaptured;
            }
            else
            {
                board[position.Item1, position.Item2] = null;
            }

            // Check if the current piece is a Pawn and update FirstMove if applicable
            if (board[new_pos.from_x, new_pos.from_y] is Pawn pawn)
            {
                if (this.color == 1 && this.position.Item2 == 6)
                {
                    pawn.FirstMove = true;
                }
                else if (this.color == -1 && this.position.Item2 == 1)
                {
                    pawn.FirstMove = false;
                }
            }

            // Update the position and reset captured information
            position = (new_pos.from_x, new_pos.from_y);
            lastCaptured = null;
        }



        public override Piece Copy()
        {
            Pawn copy = new Pawn(this.Color, this.position)
            {
                FirstMove = this.FirstMove
            };
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos
        {
            get => position;
            set => position = value;
        }

        public override int Color => color;

        public override string Type => type;

    }




    class Knight : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Knight";
        private Piece? lastCaptured;
        private (int, int) lastPos;
        private Stack<Move> lastMoves = new Stack<Move>();

        public Knight(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");

            string BlackImage = Path.Combine(fullPath, "pieces\\black_knight.svg.png");

            string WhiteImage = Path.Combine(fullPath, "pieces\\white_knight.svg.png");
            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        private bool is_valid_move(int i, int j)
        {
            return (i < 8 && j < 8 && i > -1 && j > -1);
        }

        public override (int, int)[] Show_Possible_Moves(Board board)
        {
            List<(int, int)> moves = new List<(int, int)>();
            (int, int)[] vectors = {(2,1), (2, -1), (-2,1), (-2,-1), (1,2), (1,-2), (-1,2), (-1,-2)};
            foreach ((int, int) vector in  vectors)
            {
                if (is_valid_move(position.Item1 + vector.Item1, position.Item2+vector.Item2))
                {
                    if (board[position.Item1 + vector.Item1, position.Item2 + vector.Item2] == null)
                    {
                        moves.Add((position.Item1 + vector.Item1, position.Item2 + vector.Item2));
                    }
                    else if (board[position.Item1 + vector.Item1, position.Item2 + vector.Item2].Color != color)
                    {
                        moves.Add((position.Item1 + vector.Item1, position.Item2 + vector.Item2));
                    }
                }
            }
            return moves.ToArray();
        }

        public override void Move_to((int, int) new_position, Board board)
        {

            if (board[new_position.Item1, new_position.Item2] is not null)
            {
                lastCaptured = board[new_position.Item1, new_position.Item2];
            }
            else
            {
                lastCaptured = null;
            }
            lastMoves.Push(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
            board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override void Unmove(Board board)
        {
            // restore the piece to its original position
            Move new_pos = lastMoves.Pop();
            board[new_pos.from_x, new_pos.from_y] = this;
            // if there was a captured piece, restore it to its original position
            if (lastCaptured is not null)
            {
                board[position.Item1, position.Item2] = lastCaptured;
            }
            else
            {
                board[position.Item1, position.Item2] = null;
            }
            position = (new_pos.from_x, new_pos.from_y);
            lastCaptured = null;
        }

        public override Piece Copy()
        {
            Knight copy = new Knight(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos
        {
            get => position;
            set => position = value;
        }

        public override int Color => color;

        public override string Type => type;

    }


    class Bishop : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Bishop";
        private Piece? lastCaptured;
        private (int, int) lastPos;
        private Stack<Move> lastMoves = new Stack<Move>();

        public Bishop(int color, (int, int) position)
        {
            this.lastMoves = lastMoves;
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");

            string BlackImage = Path.Combine(fullPath, "pieces\\black_bishop.svg.png");

            string WhiteImage = Path.Combine(fullPath, "pieces\\white_bishop.svg.png");

            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        private bool is_valid_move(int i, int j)
        {
            return (i < 8 && j < 8 && i > -1 && j > -1);
        }

        public override (int, int)[] Show_Possible_Moves(Board board)
        {
            List<(int, int)> moves = new List<(int, int)>();
            (int, int)[] vectors = { (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach ((int,int) vector in vectors)
            {
                (int, int) new_position = (position.Item1 + vector.Item1, position.Item2 + vector.Item2);
                while (is_valid_move(new_position.Item1, new_position.Item2))
                {
                    if (board[new_position.Item1, new_position.Item2] != null)
                    {
                        if (board[new_position.Item1, new_position.Item2].Color == color)
                        {
                            break;
                        }
                        else
                        {
                            moves.Add(new_position);
                            break;
                        }
                    }
                    moves.Add(new_position);
                    new_position = (new_position.Item1 + vector.Item1, new_position.Item2 + vector.Item2);
                }
            }
            return moves.ToArray();
        }

        public override void Move_to((int, int) new_position, Board board)
        {
            if (board[new_position.Item1, new_position.Item2] is not null)
            {
                lastCaptured = board[new_position.Item1, new_position.Item2];
            }
            else
            {
                lastCaptured = null;
            }
            lastMoves.Push(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
            board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override void Unmove(Board board)
        {
            // restore the piece to its original position
            Move new_pos = lastMoves.Pop();
            board[new_pos.from_x, new_pos.from_y] = this;
            // if there was a captured piece, restore it to its original position
            if (lastCaptured is not null)
            {
                board[position.Item1, position.Item2] = lastCaptured;
            }
            else
            {
                board[position.Item1, position.Item2] = null;
            }
            position = (new_pos.from_x, new_pos.from_y);
            lastCaptured = null;
        }


        public override Piece Copy()
        {
            Bishop copy = new Bishop(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos
        {
            get => position;
            set => position = value;
        }

        public override int Color => color;

        public override string Type => type;
    }

    class Rook : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Rook";
        private Piece? lastCaptured;
        private (int, int) lastPos;
        private Stack<Move> lastMoves = new Stack<Move>();


        public Rook(int color, (int, int) position)
        {
            this.lastMoves = lastMoves;
            this.color = color;
            this.position = position;

            string fullPath = FindSourceDirectory("Pieces.cs");

            string BlackImage = Path.Combine(fullPath ,"pieces\\black_rook.svg.png");

            string WhiteImage = Path.Combine(fullPath, "pieces\\white_rook.svg.png");

            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        private bool is_valid_move(int i, int j)
        {
            return (i < 8 && j < 8 && i > -1 && j > -1);
        }

        public override (int, int)[] Show_Possible_Moves(Board board)
        {
            List<(int, int)> moves = new List<(int, int)>();
            (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1) };
            foreach ((int, int) vector in vectors)
            {
                (int, int) new_position = (position.Item1 + vector.Item1, position.Item2 + vector.Item2);
                while (is_valid_move(new_position.Item1, new_position.Item2))
                {
                    if (board[new_position.Item1, new_position.Item2] != null)
                    {
                        if (board[new_position.Item1, new_position.Item2].Color == color)
                        {
                            break;
                        }
                        else
                        {
                            moves.Add(new_position);
                            break;
                        }
                    }
                    moves.Add(new_position);
                    new_position = (new_position.Item1 + vector.Item1, new_position.Item2 + vector.Item2);
                }
            }
            return moves.ToArray();
        }

        public override void Move_to((int, int) new_position, Board board)
        {
            if (board[new_position.Item1, new_position.Item2] is not null)
            {
                lastCaptured = board[new_position.Item1, new_position.Item2];
            }
            else
            {
                lastCaptured = null;
            }
            lastMoves.Push(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
            board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override void Unmove(Board board)
        {
            // restore the piece to its original position
            Move new_pos = lastMoves.Pop();
            board[new_pos.from_x, new_pos.from_y] = this;
            // if there was a captured piece, restore it to its original position
            if (lastCaptured is not null)
            {
                board[position.Item1, position.Item2] = lastCaptured;
            }
            else
            {
                board[position.Item1, position.Item2] = null;
            }
            position = (new_pos.from_x, new_pos.from_y);
            lastCaptured = null;
        }

        public override Piece Copy()
        {
            Rook copy = new Rook(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos
        {
            get => position;
            set => position = value;
        }

        public override int Color => color;

        public override string Type => type;
    }

    class Queen : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "Queen";
        private Piece? lastCaptured;
        private (int, int) lastPos;
        private Stack<Move> lastMoves = new Stack<Move>();

        public Queen(int color, (int, int) position)
        {
            this.lastMoves = lastMoves;
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");

            string BlackImage = Path.Combine(fullPath ,"pieces\\black_queen.svg.png");

            string WhiteImage = Path.Combine(fullPath ,"pieces\\white_queen.svg.png");

            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        private bool is_valid_move(int i, int j)
        {
            return (i < 8 && j < 8 && i > -1 && j > -1);
        }

        public override (int, int)[] Show_Possible_Moves(Board board)
        {
            List<(int, int)> moves = new List<(int, int)>();
            (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach ((int, int) vector in vectors)
            {
                (int, int) new_position = (position.Item1 + vector.Item1, position.Item2 + vector.Item2);
                while (is_valid_move(new_position.Item1, new_position.Item2))
                {
                    if (board[new_position.Item1, new_position.Item2] != null)
                    {
                        if (board[new_position.Item1, new_position.Item2].Color == color)
                        {
                            break;
                        }
                        else
                        {
                            moves.Add(new_position);
                            break;
                        }
                    }
                    moves.Add(new_position);
                    new_position = (new_position.Item1 + vector.Item1, new_position.Item2 + vector.Item2);
                }
            }
            return moves.ToArray();
        }

        public override void Move_to((int, int) new_position, Board board)
        {

            if (board[new_position.Item1, new_position.Item2] is not null)
            {
                lastCaptured = board[new_position.Item1, new_position.Item2];
            }
            else
            {
                lastCaptured = null;
            }

            lastMoves.Push(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));

            board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
            board[position.Item1, position.Item2] = null;

            position = new_position;

        }


        public override void Unmove(Board board)
        {
            // restore the piece to its original position
            Move new_pos = lastMoves.Pop();
            board[new_pos.from_x, new_pos.from_y] = this;
            // if there was a captured piece, restore it to its original position
            if (lastCaptured is not null)
            {
                board[position.Item1, position.Item2] = lastCaptured;
            }
            else
            {
                board[position.Item1, position.Item2] = null;
            }
            position = (new_pos.from_x, new_pos.from_y);
            lastCaptured = null;
        }


        public override Piece Copy()
        {
            Queen copy = new Queen(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos
        {
            get => position;
            set => position = value;
        }

        public override int Color => color;

        public override string Type => type;
    }

    class King : Piece
    {
        private int color;
        private (int, int) position;
        private string address;
        private string type = "King";
        private Piece? lastCaptured;
        private (int, int) lastPos;
        private Stack<Move> lastMoves = new Stack<Move>();

        public King(int color, (int, int) position)
        {
            this.lastMoves = lastMoves;
            this.color = color;
            this.position = position;
            string fullPath = FindSourceDirectory("Pieces.cs");

            string BlackImage = Path.Combine(fullPath, "pieces\\black_king.svg.png");

            string WhiteImage = Path.Combine(fullPath, "pieces\\white_king.svg.png");
            this.address = color == 1 ? WhiteImage : BlackImage;
        }

        private bool is_valid_move(int i, int j)
        {
            return (i < 8 && j < 8 && i > -1 && j > -1);
        }

        public override (int, int)[] Show_Possible_Moves(Board board)
        {
            List<(int, int)> moves = new List<(int, int)>();
            (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach ((int, int) vector in vectors)
            {
                if (is_valid_move(position.Item1 + vector.Item1, position.Item2 + vector.Item2) )
                {
                    if (board[position.Item1 + vector.Item1, position.Item2 + vector.Item2] == null)
                    {
                        moves.Add((position.Item1 + vector.Item1, position.Item2 + vector.Item2));
                    }
                    else if (board[position.Item1 + vector.Item1, position.Item2 + vector.Item2].Color != color)
                    {
                        moves.Add((position.Item1 + vector.Item1, position.Item2 + vector.Item2));
                    }
                }
            }
            return moves.ToArray();
        }

        public override void Move_to((int, int) new_position, Board board)
        {
            if (board[new_position.Item1, new_position.Item2] is not null)
            {
                lastCaptured = board[new_position.Item1, new_position.Item2];
            }
            else
            {
                lastCaptured = null;
            }
            lastMoves.Push(new Move(position.Item1, position.Item2, new_position.Item1, new_position.Item2));
            board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
            board[position.Item1, position.Item2] = null;
            position = new_position;
        }

        public override void Unmove(Board board)
        {
            // restore the piece to its original position
            Move new_pos = lastMoves.Pop();
            board[new_pos.from_x, new_pos.from_y] = this;
            // if there was a captured piece, restore it to its original position
            if (lastCaptured is not null)
            {
                board[position.Item1, position.Item2] = lastCaptured;
            }
            else
            {
                board[position.Item1, position.Item2] = null;
            }
            position = (new_pos.from_x, new_pos.from_y);
            lastCaptured = null;
        }


        public bool Check(Board board, List<Piece> knights) //also check the knights no?
        {
            (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach ((int, int) vector in vectors)
            {
                (int, int) new_position = (position.Item1 + vector.Item1, position.Item2 + vector.Item2);
                while (is_valid_move(new_position.Item1, new_position.Item2))
                {
                    if (board[new_position.Item1, new_position.Item2] != null)
                    {
                        if (board[new_position.Item1, new_position.Item2].Color == color)
                        {

                            break;
                        }
                        else
                        {
                            (int, int)[] moves = board[new_position.Item1, new_position.Item2].Show_Possible_Moves(board);
                            if (moves.Contains(position))
                            {
                                return true;
                            }
                        }
                    }
                    new_position.Item1 += vector.Item1;
                    new_position.Item2 += vector.Item2;
                }
            }

            foreach (var knight in knights)
            {

                (int, int)[] moves = knight.Show_Possible_Moves(board);


                if (moves.Contains(position))
                {
                    Console.WriteLine("we are in check by a knight!!!!");
                    return true;
                }
            }

            return false;
        }


        public override Piece Copy()
        {
            King copy = new King(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos
        {
            get => position;
            set => position = value;
        }

        public override int Color => color;

        public override string Type => type;
    }

}


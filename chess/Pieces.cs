using Gdk;
using Gtk;
using System.Collections.Generic;
using Controller_namespace;
using System.Net.Sockets;
using System.Globalization;
using System.Reflection;
using System.Net;

namespace Pieces_namespace
{

    public abstract class Piece
    {
        public abstract void Move_to((int, int) new_position, Board board);
        public abstract (int, int)[] Show_Possible_Moves(Board board);

        public abstract Piece Copy();

        public abstract string Address { get; }

        public abstract (int, int) Pos { get; }

        public abstract int Color { get; }
    }

    class Pawn : Piece
    {
        private int color;
        private (int, int) position;   //x, y
        private string address;
        private bool FirstMove = true;

        public Pawn (int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            this.address = color == 1 ? ".\\pieces\\white_pawn.svg.png" : ".\\pieces\\black_pawn.svg.png";
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
            FirstMove = false;
            {
                board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
                board[position.Item1, position.Item2] = null;
                position = new_position;
            }
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

        public override (int, int) Pos => position;

        public override int Color => color;

    }




    class Knight : Piece
    {
        private int color;
        private (int, int) position;
        private string address;

        public Knight(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            this.address = color == 1 ? ".\\pieces\\white_knight.svg.png" : ".\\pieces\\black_knight.svg.png";
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
            {
                board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
                board[position.Item1, position.Item2] = null;
                position = new_position;
            }
        }

        public override Piece Copy()
        {
            Knight copy = new Knight(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos => position;

        public override int Color => color;
    }


    class Bishop : Piece
    {
        private int color;
        private (int, int) position;
        private string address;

        public Bishop(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            this.address = color == 1 ? ".\\pieces\\white_bishop.svg.png" : ".\\pieces\\black_bishop.svg.png";
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
            {
                board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
                board[position.Item1, position.Item2] = null;
                position = new_position;
            }
        }

        public override Piece Copy()
        {
            Bishop copy = new Bishop(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos => position;

        public override int Color => color;
    }

    class Rook : Piece
    {
        private int color;
        private (int, int) position;
        private string address;

        public Rook(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            this.address = color == 1 ? ".\\pieces\\white_rook.svg.png" : ".\\pieces\\black_rook.svg.png";
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
            {
                board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
                board[position.Item1, position.Item2] = null;
                position = new_position;
            }
        }

        public override Piece Copy()
        {
            Rook copy = new Rook(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos => position;

        public override int Color => color;
    }

    class Queen : Piece
    {
        private int color;
        private (int, int) position;
        private string address;

        public Queen(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            this.address = color == 1 ? ".\\pieces\\white_queen.svg.png" : ".\\pieces\\black_queen.svg.png";
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
            {
                board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
                board[position.Item1, position.Item2] = null;
                position = new_position;
            }
        }

        public override Piece Copy()
        {
            Queen copy = new Queen(this.color, this.position);
            return copy;
        }

        public override string Address => address;

        public override (int, int) Pos => position;

        public override int Color => color;
    }

    class King : Piece
    {
        private int color;
        private (int, int) position;
        private string address;

        public King(int color, (int, int) position)
        {
            this.color = color;
            this.position = position;
            this.address = color == 1 ? ".\\pieces\\white_king.svg.png" : ".\\pieces\\black_king.svg.png";
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
            {
                board[new_position.Item1, new_position.Item2] = board[position.Item1, position.Item2];
                board[position.Item1, position.Item2] = null;
                position = new_position;
            }
        }

        public bool Check(Board board, List<Piece> knights)  //also check the knights no?
        {
            (int, int)[] vectors = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
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
                            (int, int)[] moves = board[new_position.Item1, new_position.Item2].Show_Possible_Moves(board);
                            if(moves.Contains(position))
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

                foreach(var pos in moves)
                {
                    Console.WriteLine(pos);
                }

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

        public override (int, int) Pos => position;

        public override int Color => color;
    }

}


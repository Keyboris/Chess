﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ConstrainedExecution;
using Controller_namespace;
using GLib;
using Pieces_namespace;

//TODO: scrap the move/unmove system, it makes things slow and confusing. implement board copying DONE 
//implement at leeast 2 threads, let one handle the ui so that you could move the window while the move is being calculated,
//let the other one handle the move calculation
//implement castling ffs
//maybe try to thinka about multithreadiing for calculating the moves? maybe left - right? no idea



namespace Agent_namespace  //black should be -1 and should be minimizing
{
    public class Agent
    {
        private Controller ctr;

        public void setController(Controller ctr)
        {
            this.ctr = ctr;
        }

        private const int MaxDepth = 2;

        public Move? GetBestMove(Board board, int player)
        {
            int bestScore = player == -1 ? int.MaxValue : int.MinValue;
            Move? bestMove = null;



            foreach (var move in board.GetAllMoves(player))
            {

                Board boardCopy = board.Copy();

                if (ctr.Allowed_Move(boardCopy, move.from_x, move.from_y, move.to_x, move.to_y))
                {
                    Piece piece = boardCopy[move.from_x, move.from_y]!;
                    // var capturedPiece = board[move.to_x, move.to_y]; // not needed, the original board is not modified


                    piece.Move_to((move.to_x, move.to_y), boardCopy); // Make the move

                    int score = Minimax(boardCopy, -player, 1, int.MinValue, int.MaxValue);

                    //piece.Unmove(board); //scrap that
                    // if (capturedPiece != null)
                    // {
                    //     board[move.to_x, move.to_y] = capturedPiece; // again, not needed since we are not modifying the original board
                    // }

                    if ((player == -1 && score < bestScore) || (player == 1 && score > bestScore))
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }
            }

            return bestMove;
        }

        private int Minimax(Board board, int player, int depth, int alpha, int beta)
        {
            Console.WriteLine($"Depth: {depth}, Player: {player}");
            if (depth >= MaxDepth)
            {
                return Evaluate(ctr, board);
            }

            if (player == 1) // White, maximizing player
            {
                int maxEval = int.MinValue;
                
                foreach (var move in board.GetAllMoves(player))
                {
                    Board boardCopy = board.Copy();

                    if (ctr.Allowed_Move(board, move.from_x, move.from_y, move.to_x, move.to_y))
                    {
                        var piece = boardCopy[move.from_x, move.from_y];
                        piece.Move_to((move.to_x, move.to_y), boardCopy);

                        int score = Minimax(boardCopy, -player, depth + 1, alpha, beta);
                        maxEval = Math.Max(maxEval, score);
                        alpha = Math.Max(alpha, maxEval);
                        
                        if (beta <= alpha)
                        {
                            break; // Beta cutoff
                        }
                    }
                }
                
                return maxEval;
            }
            else // Black, minimizing player
            {
                int minEval = int.MaxValue;
                
                foreach (var move in board.GetAllMoves(player))
                {
                    Board boardCopy = board.Copy();

                    if (ctr.Allowed_Move(board, move.from_x, move.from_y, move.to_x, move.to_y))
                    {
                        var piece = boardCopy[move.from_x, move.from_y];
                        piece.Move_to((move.to_x, move.to_y), boardCopy);

                        int score = Minimax(boardCopy, -player, depth + 1, alpha, beta);
                        minEval = Math.Min(minEval, score);
                        beta = Math.Min(beta, minEval);
                        
                        if (beta <= alpha)
                        {
                            break; // Alpha cutoff
                        }
                    }
                }
                
                return minEval;
            }
        }


        public static int[,] BlackPawnTable = new int[8, 8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 5, 5, 5, -5, -5, 5, 5, 5 },
            { 1, 1, 1, 5, 5, 1, 1, 1 },
            { 0, 0, 0, 10, 10, 0, 0, 0 },
            { 5, 5, 10, 20, 20, 10, 5, 5 },
            { 10, 10, 20, 30, 30, 20, 10, 10 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 90, 90, 90, 90, 90, 90, 90, 90 }
        };

        public static int[,] WhitePawnTable = new int[8, 8]
        {
            { 90, 90, 90, 90, 90, 90, 90, 90 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 10, 10, 20, 30, 30, 20, 10, 10 },
            { 5, 5, 10, 20, 20, 10, 5, 5 },
            { 0, 0, 0, 10, 10, 0, 0, 0 },
            { 1, 1, 1, 5, 5, 1, 1, 1 },
            { 5, 5, 5, -5, -5, 5, 5, 5 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        public static int[,] WhiteKnightTable = new int[8, 8]
        {
            { -50, -40, -30, -30, -30, -30, -40, -50 },
            { -40, -20, 0, 0, 0, 0, -20, -40 },
            { -30, 0, 10, 15, 15, 10, 0, -30 },
            { -30, 5, 15, 20, 20, 15, 5, -30 },
            { -30, 0, 15, 20, 20, 15, 0, -30 },
            { -30, 5, 10, 15, 15, 10, 5, -30 },
            { -40, -20, 0, 5, 5, 0, -20, -40 },
            { -50, -40, -30, -30, -30, -30, -40, -50 }
        };


        public static int[,] BlackKnightTable = new int[8, 8]
        {
            { -50, -40, -30, -30, -30, -30, -40, -50 },
            { -40, -20, 0, 5, 5, 0, -20, -40 },
            { -30, 5, 10, 15, 15, 10, 5, -30 },
            { -30, 0, 15, 20, 20, 15, 0, -30 },
            { -30, 5, 15, 20, 20, 15, 5, -30 },
            { -30, 0, 10, 15, 15, 10, 0, -30 },
            { -40, -20, 0, 0, 0, 0, -20, -40 },
            { -50, -40, -30, -30, -30, -30, -40, -50 }
        };


        public static int[,] WhiteBishopTable = new int[8, 8]
        {
            { -20, -10, -10, -10, -10, -10, -10, -20 },
            { -10, 5, 0, 0, 0, 0, 5, -10 },
            { -10, 10, 10, 10, 10, 10, 10, -10 },
            { -10, 0, 10, 10, 10, 10, 0, -10 },
            { -10, 5, 5, 10, 10, 5, 5, -10 },
            { -10, 0, 5, 10, 10, 5, 0, -10 },
            { -10, 0, 0, 0, 0, 0, 0, -10 },
            { -20, -10, -10, -10, -10, -10, -10, -20 }
        };

        public static int[,] BlackBishopTable = new int[8, 8]
        {
            { -20, -10, -10, -10, -10, -10, -10, -20 },
            { -10, 0, 0, 0, 0, 0, 0, -10 },
            { -10, 0, 5, 10, 10, 5, 0, -10 },
            { -10, 5, 5, 10, 10, 5, 5, -10 },
            { -10, 0, 10, 10, 10, 10, 0, -10 },
            { -10, 10, 10, 10, 10, 10, 10, -10 },
            { -10, 5, 0, 0, 0, 0, 5, -10 },
            { -20, -10, -10, -10, -10, -10, -10, -20 }
        };

        public static int[,] BlackRookTable = new int[8, 8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 5, 10, 10, 10, 10, 10, 10, 5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { 0, 0, 0, 5, 5, 0, 0, 0 }
        };


        public static int[,] WhiteRookTable = new int[8, 8]
        {
            { 0, 0, 0, 5, 5, 0, 0, 0 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { -5, 0, 0, 0, 0, 0, 0, -5 },
            { 5, 10, 10, 10, 10, 10, 10, 5 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };


        public static int[,] WhiteQueenTable = new int[8, 8]
        {
            { -20, -10, -10, -5, -5, -10, -10, -20 },
            { -10, 0, 0, 0, 0, 0, 0, -10 },
            { -10, 0, 5, 5, 5, 5, 0, -10 },
            { -5, 0, 5, 5, 5, 5, 0, -5 },
            { 0, 0, 5, 5, 5, 5, 0, -5 },
            { -10, 5, 5, 5, 5, 5, 0, -10 },
            { -10, 0, 5, 0, 0, 0, 0, -10 },
            { -20, -10, -10, -5, -5, -10, -10, -20 }
        };

        public static int[,] BlackQueenTable = new int[8, 8]
        {
            { -20, -10, -10, -5, -5, -10, -10, -20 },
            { -10, 0, 0, 0, 0, 0, 0, -10 },
            { -10, 0, 5, 5, 5, 5, 0, -10 },
            { 0, 0, 5, 5, 5, 5, 0, -5 },
            { -5, 0, 5, 5, 5, 5, 0, -5 },
            { -10, 0, 5, 5, 5, 5, 0, -10 },
            { -10, 0, 0, 0, 0, 0, 0, -10 },
            { -20, -10, -10, -5, -5, -10, -10, -20 }
        };

        public static int[,] WhiteKingTable = new int[8, 8]
        {
            { -30, -40, -40, -50, -50, -40, -40, -30 },
            { -30, -40, -40, -50, -50, -40, -40, -30 },
            { -30, -40, -40, -50, -50, -40, -40, -30 },
            { -30, -40, -40, -50, -50, -40, -40, -30 },
            { -20, -30, -30, -40, -40, -30, -30, -20 },
            { -10, -20, -20, -20, -20, -20, -20, -10 },
            { 20, 20, 0, 0, 0, 0, 20, 20 },
            { 20, 30, 10, 0, 0, 10, 30, 20 }
        };


        public static int[,] BlackKingTable = new int[8, 8]
        {
            { 20, 30, 10, 0, 0, 10, 30, 20 },
            { 20, 20, 0, 0, 0, 0, 20, 20 },
            { -10, -20, -20, -20, -20, -20, -20, -10 },
            { -20, -30, -30, -40, -40, -30, -30, -20 },
            { -30, -40, -40, -50, -50, -40, -40, -30 },
            { -30, -40, -40, -50, -50, -40, -40, -30 },
            { -30, -40, -40, -50, -50, -40, -40, -30 },
            { -30, -40, -40, -50, -50, -40, -40, -30 }
        };

        private static int PositionValue(Piece p)
        {
            if (p.Color == 1)
            {
                return p.Type switch
                {
                    "Pawn" => WhitePawnTable[p.Pos.Item2, p.Pos.Item1],
                    "Knight" => WhiteKnightTable[p.Pos.Item2, p.Pos.Item1],
                    "Bishop" => WhiteBishopTable[p.Pos.Item2, p.Pos.Item1],
                    "Rook" => WhiteRookTable[p.Pos.Item2, p.Pos.Item1],
                    "Queen" => WhiteQueenTable[p.Pos.Item2, p.Pos.Item1],
                    "King" => WhiteKingTable[p.Pos.Item2, p.Pos.Item1],
                    _ => 0
                };
            }
            else
            {
                return p.Type switch
                {
                    "Pawn" => BlackPawnTable[p.Pos.Item2, p.Pos.Item1],
                    "Knight" => BlackKnightTable[p.Pos.Item2, p.Pos.Item1],
                    "Bishop" => BlackBishopTable[p.Pos.Item2, p.Pos.Item1],
                    "Rook" => BlackRookTable[p.Pos.Item2, p.Pos.Item1],
                    "Queen" => BlackQueenTable[p.Pos.Item2, p.Pos.Item1],
                    "King" => BlackKingTable[p.Pos.Item2, p.Pos.Item1],
                    _ => 0
                };
            }
        }

        private static int EvaluatePawnStructure(Board board)
        {

            bool isValid(int i)
            {
                return (i >= 0 && i <= 7);
            }

            int score = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    var piece = board[i, j];
                    if (piece is Pawn pawn)
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            if (k != j && board[i, k] is Pawn otherPawn && otherPawn.Color == pawn.Color) //if we have 2 pawns on the same rank
                            {
                                score -= 20 * pawn.Color; //the minus is needed since the ai is -1 and is maximising
                            }
                        }

                        bool isolated = true;
                        if (isValid(i - 1) && isValid(j - 1) && isValid(j + 1) && isValid(i + 1))
                        {
                            if (i > 0 && (board[i - 1, j - 1] is Pawn || board[i - 1, j + 1] is Pawn))
                            {
                                isolated = false;
                            }
                            if (i < 7 && (board[i + 1, j - 1] is Pawn || board[i + 1, j + 1] is Pawn))
                            {
                                isolated = false;
                            }
                        }
                        if (isolated)
                        {
                            score -= 10 * pawn.Color;
                        }

                        if (j > 0 && board[i, j - 1] is Pawn chainPawn && chainPawn.Color == pawn.Color)
                        {
                            score += 10 * pawn.Color;
                        }
                    }
                }
            }

            return score;
        }

        public static int OpenLane(Board board, int x, int y)
        {
            int score = 0;
            for (int new_y = 0; new_y < 8; new_y++)
            {
                if (board[x, new_y] is null && y != new_y)
                {
                    score += 1;
                }
            }

            return score;
        }

        private static int Evaluate(Controller ctr, Board board)  //add protection of pieces, also it breaks after promotion
        {

            Dictionary<string, int> values = new Dictionary<string, int>();
            values.Add("Pawn", 10);
            values.Add("Knight", 30);
            values.Add("Bishop", 30);
            values.Add("Rook", 50);
            values.Add("Queen", 90);
            values.Add("King", 0);  //both kings are always on board, any value for it will do

            int score = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[j, i] is not null)
                    {
                        score += PositionValue(board[j, i]);
                        score += (board[j, i].Color * values[board[j, i].Type]); // we need the minus, since the ai is maximising flipped
                        if (board[j, i] is Rook)
                        {
                            score += OpenLane(board, j, i);
                        }
                    }
                }
            }

            int check = 0;

            if (ctr.checkCheck(board, ref check))
            {
                score += -check * 150;
            }

            score += EvaluatePawnStructure(board);

            return score;
        }

    }
}

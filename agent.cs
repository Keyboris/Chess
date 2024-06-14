using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ConstrainedExecution;
using Controller_namespace;
using GLib;
using Pieces_namespace;

namespace Agent_namespace
{
    public class Agent
    {
        private Controller ctr;

        public void setController(Controller ctr)
        {
            this.ctr = ctr;
        }

        private const int MaxDepth = 2; // Adjust depth based on performance needs
        private readonly int[] PieceValues = { 100, 300, 300, 500, 900, 0 }; // Pawn, Knight, Bishop, Rook, Queen, King (King value is 0 for simplicity)

        public Move? GetBestMove(Board board, int player)
        {

            int bestScore = player == -1 ? int.MinValue : int.MaxValue;  //the ai is maximising
            Move? bestMove = null;

            foreach (var move in board.GetAllMoves(player))
            {
                if (ctr.Allowed_Move(board, move.from_x, move.from_y, move.to_x, move.to_y))
                {
                    var piece = board[move.from_x, move.from_y];

                    piece.Move_to((move.to_x, move.to_y), board);
                    int score = Minimax(board, -player, 2, int.MinValue, int.MaxValue);

                    piece.Unmove(board);

                    var pieceAfterUnmove = board[move.from_x, move.from_y];


                    if (player == -1 && score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    else if (player == 1 && score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }
                else
                {
                    Console.WriteLine("THIS MOVE IS NOT ALLOWED");
                }
            }

            Console.WriteLine($"The best move that we have found is {bestMove}");

            return bestMove;
        }



        private int Minimax(Board board, int player, int depth, int alpha, int beta)
        {

            Console.WriteLine($"CURRENT DEPTH IS {depth}!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");


            if (depth <= MaxDepth)
            {
                return Evaluate(ctr, board);
            }


            int maxEval = int.MinValue;
            int minEval = int.MaxValue;

            int bestScore = player == 1 ? int.MinValue : int.MaxValue;
            foreach (var move in board.GetAllMoves(player))
            {
                if (ctr.Allowed_Move(board, move.from_x, move.from_y, move.to_x, move.to_y))
                {
                    board[move.from_x, move.from_y].Move_to((move.to_x, move.to_y), board);
                    int score = Minimax(board, -player, depth - 1, alpha, beta);
                    board[move.from_x, move.from_y].Unmove(board);

                    if (player == -1) //maximiser
                    {
                        maxEval = int.Max(maxEval, score);
                        alpha = int.Max(alpha, maxEval);
                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                    else if (player == 1)
                    {
                        minEval = int.Min(minEval, score);
                        beta = int.Min(beta, minEval);
                        if (beta <= alpha)
                        {
                            break;
                        }
                    }

/*                    if (player == -1 && score > bestScore)
                    {
                        bestScore = score;
                    }
                    else if (player == -1 && score < bestScore)
                    {
                        bestScore = score;
                    }*/
                }

            }

            bestScore = player == 1? minEval: maxEval;
            return bestScore;
        }


        // Piece-square tables for pawns, knights, bishops, rooks, queens, and kings
        public static int[,] WhitePawnTable = new int[8, 8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 5, 5, 5, -5, -5, 5, 5, 5 },
            { 1, 1, 1, 5, 5, 1, 1, 1 },
            { 0, 0, 0, 10, 10, 0, 0, 0 },
            { 5, 5, 10, 20, 20, 10, 5, 5 },
            { 10, 10, 20, 30, 30, 20, 10, 10 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        public static int[,] BlackPawnTable = new int[8, 8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
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

        public static int[,] WhiteRookTable = new int[8, 8]
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


        public static int[,] BlackRookTable = new int[8, 8]
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

        // Similar tables can be created for other pieces

        /*        public static int Evaluate(Board board)
                {
                    int score = 0;

                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            var piece = board[i, j];
                            if (piece != null)
                            {
                                int pieceValue = GetPieceValue(piece);
                                int positionalValue = GetPieceSquareValue(piece, i, j);
                                int pieceScore = piece.Color == -1 ? pieceValue + positionalValue : -(pieceValue + positionalValue);
                                score += pieceScore;
                            }
                        }
                    }

                    score += EvaluatePawnStructure(board);
                    score += EvaluateKingSafety(board);
                    score += EvaluateMobility(board);

                    return score;
                }*/

        /*        private static int GetPieceValue(Piece piece)
                {
                    return piece switch
                    {
                        Pawn => PawnValue,
                        Knight => KnightValue,
                        Bishop => BishopValue,
                        Rook => RookValue,
                        Queen => QueenValue,
                        _ => 0
                    };
                }*/

        /*        private static int GetPieceSquareValue(Piece piece, int x, int y)
                {
                    return piece switch
                    {
                        Pawn => PawnTable[x, y],
        *//*                Knight => KnightTable[x,y],
                        Bishop => BishopValue,
                        Rook => RookValue,
                        Queen => QueenValue,*//*
                        _ => 0
                    };
                }*/


        private static int PositionValue(Piece p)
        {
            if (p.Color == 1)
            {
                return p.Type switch
                {
                    "Pawn" => -WhitePawnTable[p.Pos.Item2, p.Pos.Item1],
                    "Knght" => -WhiteKnightTable[p.Pos.Item2, p.Pos.Item1],
                    "Bishop" => -WhiteBishopTable[p.Pos.Item2, p.Pos.Item1],
                    "Rook" => -WhiteRookTable[p.Pos.Item2, p.Pos.Item1],
                    "Queen" => -WhiteQueenTable[p.Pos.Item2, p.Pos.Item1],
                    "King" => -WhiteKingTable[p.Pos.Item2, p.Pos.Item1],
                    _ => 0
                };
            }
            else
            {
                return p.Type switch
                {
                    "Pawn" => BlackPawnTable[p.Pos.Item2, p.Pos.Item1],
                    "Knght" => BlackKnightTable[p.Pos.Item2, p.Pos.Item1],
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
            int score = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    var piece = board[i, j];
                    if (piece is Pawn pawn)
                    {
                        // Evaluate doubled pawns
                        for (int k = 0; k < 8; k++)
                        {
                            if (k != j && board[i, k] is Pawn otherPawn && otherPawn.Color == pawn.Color) //if we have 2 pawns on the same rank
                            {
                                score -= -(20 * pawn.Color); //the minus is needed since the ai is -1 and is maximising
                            }
                        }

                        // Evaluate isolated pawns
                        bool isolated = true;
                        if (i > 0 && (board[i - 1, j - 1] is Pawn || board[i - 1, j + 1] is Pawn))
                        {
                            isolated = false;
                        }
                        if (i < 7 && (board[i + 1, j - 1] is Pawn || board[i + 1, j + 1] is Pawn))
                        {
                            isolated = false;
                        }
                        if (isolated)
                        {
                            score -= -(10 * pawn.Color);
                        }

                        // Evaluate pawn chains
                        if (j > 0 && board[i, j - 1] is Pawn chainPawn && chainPawn.Color == pawn.Color)
                        {
                            score += -(10 * pawn.Color);
                        }
                    }
                }
            }

            return score;
        }

        public static int OpenLane(Board board, int x,int y)
        {
            int score = 0;
            for (int new_y = 0; new_y < 8; new_y++)
            {
                if ((board[x,new_y] is null && y != new_y))
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
                for (int j = 0;  j < 8; j++)
                {
                    if (board[j,i] is not null)
                    {
                        score += PositionValue(board[j, i]);
                        score += -(board[j, i].Color * values[board[j, i].Type]); // we need the minus, since the ai is maximising
                        if (board[j,i] is Rook)
                        {
                            score += OpenLane(board, j, i);
                        }
                    }
                }
            }

            int check = 0;

            if(ctr.checkCheck(board, ref check))
            {
                score += -check * 150;
            }

            score += EvaluatePawnStructure(board);

            return score;
        }

    }
}




/*int score = 0;

// Check for pawn shield in front of the king
// ... (existing logic)
King tempKing;
// Check for castling possibilities
for (int color = -1; color <= 1; color += 2)
{



    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            if (board[j, i] is King)
            {
                if (board[j, i].Color == 1)
                {
                    tempKing = (King)board[j, i];
                }
            }
        }
    }


}

// Check king's distance from the center
int centerX = 3;
int centerY = 3; // Assuming white is player 1
                 //score -= Math.Abs(tempKing.Pos.Item1 - centerX) + Math.Abs(tempKing.Pos.Item2 - centerY);


// ... (consider surrounding piece threats)

return score;
        }

        private static int EvaluateMobility(Board board)
{
    int score = 0;

    // Count the number of legal moves for each side
    int whiteMobility = 0;
    int blackMobility = 0;

    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            var piece = board[i, j];
            if (piece != null)
            {
                var moves = piece.Show_Possible_Moves(board);
                if (piece.Color == 1)
                {
                    whiteMobility += moves.Count();
                }
                else
                {
                    blackMobility += moves.Count();
                }
            }
        }
    }

    score += whiteMobility - blackMobility;

    return score;*/







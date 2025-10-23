using System;
using System.Collections.Generic;
using Controller_namespace;
using Pieces_namespace;

namespace Agent_namespace
{
    public class Agent
    {
        private Controller ctr;
        public void setController(Controller ctr) { this.ctr = ctr; }

        private const int MaxDepth = 4; // Depth can be increased due to performance improvements

        public Move GetBestMove(Board board, int player)
        {
            // --- DEBUG PRINT ---
            Console.WriteLine($"DEBUG: GetBestMove - AI searching for best move for Player {player} at depth {MaxDepth}.");

            int bestScore = player == -1 ? int.MaxValue : int.MinValue;
            Move bestMove = null;

            // Use the new legal move generator
            List<Move> legalMoves = board.GenerateLegalMoves(player);

            if (legalMoves.Count == 0) return null; // No legal moves

            bestMove = legalMoves[0]; // Default to first move in case all moves have same score

            foreach (var move in legalMoves)
            {
                board.MakeMove(move);
                int score = Minimax(board, -player, 1, int.MinValue, int.MaxValue);
                board.UnmakeMove(move);

                if (player == -1) // Minimizing player (black)
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }
                else // Maximizing player (white)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }
            }

            // --- DEBUG PRINT ---
            Console.WriteLine($"DEBUG: GetBestMove - AI found best move with score {bestScore}.");
            return bestMove;
        }

        private int Minimax(Board board, int player, int depth, int alpha, int beta)
        {
            if (depth >= MaxDepth)
            {
                return Evaluate(board);
            }

            List<Move> legalMoves = board.GenerateLegalMoves(player);

            // Handle checkmate or stalemate terminal nodes
            if (legalMoves.Count == 0)
            {
                var kingPos = board.FindKing(player);
                if (board.IsSquareAttacked(kingPos, -player))
                {
                    // This player is in checkmate. Return a very bad score for them.
                    return player == 1 ? int.MinValue + depth : int.MaxValue - depth;
                }
                // Stalemate
                return 0;
            }

            if (player == 1) // White, maximizing player
            {
                int maxEval = int.MinValue;
                foreach (var move in legalMoves)
                {
                    board.MakeMove(move);
                    int score = Minimax(board, -player, depth + 1, alpha, beta);
                    board.UnmakeMove(move);

                    maxEval = Math.Max(maxEval, score);
                    alpha = Math.Max(alpha, maxEval);
                    if (beta <= alpha)
                    {
                        break; // Beta cutoff
                    }
                }
                return maxEval;
            }
            else // Black, minimizing player
            {
                int minEval = int.MaxValue;
                foreach (var move in legalMoves)
                {
                    board.MakeMove(move);
                    int score = Minimax(board, -player, depth + 1, alpha, beta);
                    board.UnmakeMove(move);

                    minEval = Math.Min(minEval, score);
                    beta = Math.Min(beta, minEval);
                    if (beta <= alpha)
                    {
                        break; // Alpha cutoff
                    }
                }
                return minEval;
            }
        }
        
        #region Evaluation Tables 
        public static int[,] BlackPawnTable = new int[8, 8]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 1, 1, -1, -1, 1, 1, 1 },
            { 5, 5, 5, 7, 7, 5, 5, 5 },
            { 6, 6, 8, 10, 10, 8, 6, 6 },
            { 8, 8, 10, 20, 20, 10, 8, 8 },
            { 14, 15, 20, 30, 30, 20, 15, 14 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 90, 90, 90, 90, 90, 90, 90, 90 }
        };

        public static int[,] WhitePawnTable = new int[8, 8]
        {
            { 90, 90, 90, 90, 90, 90, 90, 90 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 14, 15, 20, 30, 30, 20, 14, 15 },
            { 8, 8, 10, 20, 20, 10, 8, 8 },
            { 6, 6, 8, 10, 10, 8, 6, 6 },
            { 5, 5, 5, 7, 7, 5, 5, 5 },
            { 1, 1, 1, -1, -1, 1, 1, 1 },
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

        #endregion

        private static int PositionValue(Piece p)
        {
            if (p.Color == 1)
            {
                return p.Type switch
                {
                    "Pawn" => WhitePawnTable[p.Pos.Item2, p.Pos.Item1], "Knight" => WhiteKnightTable[p.Pos.Item2, p.Pos.Item1],
                    "Bishop" => WhiteBishopTable[p.Pos.Item2, p.Pos.Item1], "Rook" => WhiteRookTable[p.Pos.Item2, p.Pos.Item1],
                    "Queen" => WhiteQueenTable[p.Pos.Item2, p.Pos.Item1], "King" => WhiteKingTable[p.Pos.Item2, p.Pos.Item1],
                    _ => 0
                };
            }
            else
            {
                return p.Type switch
                {
                    "Pawn" => BlackPawnTable[p.Pos.Item2, p.Pos.Item1], "Knight" => BlackKnightTable[p.Pos.Item2, p.Pos.Item1],
                    "Bishop" => BlackBishopTable[p.Pos.Item2, p.Pos.Item1], "Rook" => BlackRookTable[p.Pos.Item2, p.Pos.Item1],
                    "Queen" => BlackQueenTable[p.Pos.Item2, p.Pos.Item1], "King" => BlackKingTable[p.Pos.Item2, p.Pos.Item1],
                    _ => 0
                };
            }
        }

        private static int Evaluate(Board board)
        {
            // Evaluation is always from white's perspective (positive is good for white)
            int score = 0;
            Dictionary<string, int> values = new Dictionary<string, int>()
            {
                {"Pawn", 100}, {"Knight", 320}, {"Bishop", 330}, {"Rook", 500}, {"Queen", 900}, {"King", 20000}
            };

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[j, i] is not null)
                    {
                        Piece p = board[j, i];
                        score += (values[p.Type] + PositionValue(p)) * p.Color;
                    }
                }
            }
            return score;
        }
    }
}
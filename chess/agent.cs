using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Chess;

namespace Agent_namespace
{
    public class Agent
    {
        private const int MaxDepth = 4;

        public Move GetBestMove(Board board)
        {
            int side = board.SideToMove;
            var moves = MoveGen.GenerateLegalMoves(board);
            if (moves.Count == 0) return default;

            Move best = moves[0];
            int bestScore = side == 0 ? int.MinValue : int.MaxValue;

            foreach (var move in moves)
            {
                var undo = MoveGen.MakeMove(board, move);
                int score = Minimax(board, 1, int.MinValue, int.MaxValue);
                MoveGen.UnmakeMove(board, undo);

                if (side == 0 && score > bestScore) { bestScore = score; best = move; }
                if (side == 1 && score < bestScore) { bestScore = score; best = move; }
            }
            return best;
        }

        private int Minimax(Board board, int depth, int alpha, int beta)
        {
            if (depth >= MaxDepth)
                return QSearch(board, alpha, beta);

            var moves = MoveGen.GenerateLegalMoves(board);
            if (moves.Count == 0)
            {
                if (MoveGen.IsInCheck(board, board.SideToMove))
                    return board.SideToMove == 0 ? int.MinValue + depth : int.MaxValue - depth;
                return 0; // stalemate
            }

            var sorted = moves.OrderByDescending(m => MovePriority(m, board)).ToList();

            if (board.SideToMove == 0) // White maximises
            {
                int max = int.MinValue;
                foreach (var m in sorted)
                {
                    var u = MoveGen.MakeMove(board, m);
                    max = Math.Max(max, Minimax(board, depth + 1, alpha, beta));
                    MoveGen.UnmakeMove(board, u);
                    alpha = Math.Max(alpha, max);
                    if (beta <= alpha) break;
                }
                return max;
            }
            else // Black minimises
            {
                int min = int.MaxValue;
                foreach (var m in sorted)
                {
                    var u = MoveGen.MakeMove(board, m);
                    min = Math.Min(min, Minimax(board, depth + 1, alpha, beta));
                    MoveGen.UnmakeMove(board, u);
                    beta = Math.Min(beta, min);
                    if (beta <= alpha) break;
                }
                return min;
            }
        }

        private int QSearch(Board board, int alpha, int beta)
        {
            int stand = Evaluate(board);
            int side   = board.SideToMove;

            if (side == 0)
            {
                if (stand >= beta) return beta;
                alpha = Math.Max(alpha, stand);
            }
            else
            {
                if (stand <= alpha) return alpha;
                beta = Math.Min(beta, stand);
            }

            var moves = MoveGen.GenerateLegalMoves(board)
                               .Where(m => m.IsCapture || m.IsPromotion)
                               .OrderByDescending(m => MovePriority(m, board))
                               .ToList();

            if (side == 0)
            {
                foreach (var m in moves)
                {
                    var u = MoveGen.MakeMove(board, m);
                    int score = QSearch(board, alpha, beta);
                    MoveGen.UnmakeMove(board, u);
                    if (score >= beta) return beta;
                    alpha = Math.Max(alpha, score);
                }
                return alpha;
            }
            else
            {
                foreach (var m in moves)
                {
                    var u = MoveGen.MakeMove(board, m);
                    int score = QSearch(board, alpha, beta);
                    MoveGen.UnmakeMove(board, u);
                    if (score <= alpha) return alpha;
                    beta = Math.Min(beta, score);
                }
                return beta;
            }
        }

        private int MovePriority(Move move, Board board)
        {
            int score = 0;
            if (move.IsCapture)
            {
                int victim    = board.PieceOn(move.IsEnPassant ? (move.From / 8) * 8 + move.To % 8 : move.To);
                int aggressor = board.PieceOn(move.From);
                if (victim != Piece.None)
                    score += 1_000_000 + PieceValue(Piece.TypeOf(victim)) * 1000 - PieceValue(Piece.TypeOf(aggressor));
            }
            if (move.IsPromotion) score += 200_000;
            return score;
        }

        // ---- Evaluation ----
        private static int Evaluate(Board board)
        {
            int score = 0;
            int totalMaterial = 0;
            for (int i = 0; i < 12; i++)
            {
                if (i % 6 != 5) // exclude kings from material count for phase
                    totalMaterial += BitOperations.PopCount(board.BB[i]) * PieceValue(i % 6);
            }
            double phase = Math.Min(1.0, totalMaterial / 7800.0); // 7800 approx full material

            for (int i = 0; i < 12; i++)
            {
                ulong bb = board.BB[i];
                int color = i / 6; // 0=white, 1=black
                int type  = i % 6;
                int sign  = color == 0 ? 1 : -1;
                while (bb != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(bb);
                    score += sign * (PieceValue(type) + PstValue(type, color, sq, phase));
                    bb &= bb - 1;
                }
            }
            return score;
        }

        private static int PieceValue(int type) => type switch
        {
            0 => 100,  // Pawn
            1 => 320,  // Knight
            2 => 330,  // Bishop
            3 => 500,  // Rook
            4 => 900,  // Queen
            5 => 20000,// King
            _ => 0
        };

        // Piece-square tables: indexed [rank][file], white's perspective (rank 7 = white back rank)
        // For black we mirror the rank.
        private static int PstValue(int type, int color, int sq, double phase)
        {
            int rank = sq / 8;
            int file = sq % 8;
            // Mirror for white: white's back rank is rank 7, so we flip
            int r = color == 0 ? 7 - rank : rank;
            return type switch
            {
                0 => PawnPst[r, file],
                1 => KnightPst[r, file],
                2 => BishopPst[r, file],
                3 => RookPst[r, file],
                4 => QueenPst[r, file],
                5 => (int)(phase * KingMidPst[r, file] + (1 - phase) * KingEndPst[r, file]),
                _ => 0
            };
        }

        #region PST Tables (rank 0 = own back rank, rank 7 = opponent back rank)
        static readonly int[,] PawnPst = {
            { 0,  0,  0,  0,  0,  0,  0,  0},
            {50, 50, 50, 50, 50, 50, 50, 50},
            {14, 15, 20, 30, 30, 20, 15, 14},
            { 8,  8, 10, 20, 20, 10,  8,  8},
            { 6,  6,  8, 10, 10,  8,  6,  6},
            { 5,  5,  5,  7,  7,  5,  5,  5},
            { 1,  1,  1, -1, -1,  1,  1,  1},
            { 0,  0,  0,  0,  0,  0,  0,  0},
        };
        static readonly int[,] KnightPst = {
            {-50,-40,-30,-30,-30,-30,-40,-50},
            {-40,-20,  0,  0,  0,  0,-20,-40},
            {-30,  0, 10, 15, 15, 10,  0,-30},
            {-30,  5, 15, 20, 20, 15,  5,-30},
            {-30,  0, 15, 20, 20, 15,  0,-30},
            {-30,  5, 10, 15, 15, 10,  5,-30},
            {-40,-20,  0,  5,  5,  0,-20,-40},
            {-50,-40,-30,-30,-30,-30,-40,-50},
        };
        static readonly int[,] BishopPst = {
            {-20,-10,-10,-10,-10,-10,-10,-20},
            {-10,  5,  0,  0,  0,  0,  5,-10},
            {-10, 10, 10, 10, 10, 10, 10,-10},
            {-10,  0, 10, 10, 10, 10,  0,-10},
            {-10,  5,  5, 10, 10,  5,  5,-10},
            {-10,  0,  5, 10, 10,  5,  0,-10},
            {-10,  0,  0,  0,  0,  0,  0,-10},
            {-20,-10,-10,-10,-10,-10,-10,-20},
        };
        static readonly int[,] RookPst = {
            { 0,  0,  0,  5,  5,  0,  0,  0},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            { 5, 10, 10, 10, 10, 10, 10,  5},
            { 0,  0,  0,  0,  0,  0,  0,  0},
        };
        static readonly int[,] QueenPst = {
            {-20,-10,-10, -5, -5,-10,-10,-20},
            {-10,  0,  0,  0,  0,  0,  0,-10},
            {-10,  0,  5,  5,  5,  5,  0,-10},
            { -5,  0,  5,  5,  5,  5,  0, -5},
            {  0,  0,  5,  5,  5,  5,  0, -5},
            {-10,  5,  5,  5,  5,  5,  0,-10},
            {-10,  0,  5,  0,  0,  0,  0,-10},
            {-20,-10,-10, -5, -5,-10,-10,-20},
        };
        static readonly int[,] KingMidPst = {
            { 20, 30, 10,  0,  0, 10, 30, 20},
            { 20, 20,  0,  0,  0,  0, 20, 20},
            {-10,-20,-20,-20,-20,-20,-20,-10},
            {-20,-30,-30,-40,-40,-30,-30,-20},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
        };
        static readonly int[,] KingEndPst = {
            {-50,-40,-30,-20,-20,-30,-40,-50},
            {-30,-20,-10,  0,  0,-10,-20,-30},
            {-30,-10, 20, 30, 30, 20,-10,-30},
            {-30,-10, 30, 40, 40, 30,-10,-30},
            {-30,-10, 30, 40, 40, 30,-10,-30},
            {-30,-10, 20, 30, 30, 20,-10,-30},
            {-30,-15,-10,  0,  0,-10,-15,-30},
            {-50,-30,-30,-30,-30,-30,-30,-50},
        };
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace Chess
{
    // -----------------------------------------------------------------------
    // Piece index constants (indices into Board.BB[])
    // -----------------------------------------------------------------------
    public static class Piece
    {
        public const int WhitePawn   = 0;
        public const int WhiteKnight = 1;
        public const int WhiteBishop = 2;
        public const int WhiteRook   = 3;
        public const int WhiteQueen  = 4;
        public const int WhiteKing   = 5;
        public const int BlackPawn   = 6;
        public const int BlackKnight = 7;
        public const int BlackBishop = 8;
        public const int BlackRook   = 9;
        public const int BlackQueen  = 10;
        public const int BlackKing   = 11;
        public const int None        = -1;

        // Piece type regardless of color (0-5)
        public static int TypeOf(int p)  => p % 6;
        public static int ColorOf(int p) => p / 6; // 0=white, 1=black
        public static int Make(int color, int type) => color * 6 + type;
    }

    // -----------------------------------------------------------------------
    // Move encoding
    // -----------------------------------------------------------------------
    public static class MoveFlags
    {
        public const int Quiet     = 0;
        public const int Capture   = 1;
        public const int Castle    = 2;
        public const int EnPassant = 4;
        public const int Promotion = 8;
    }

    public struct Move
    {
        // from/to are square indices 0-63
        // flags: see MoveFlags
        // promoPiece: piece index of the promoted piece (only valid when IsPromotion)
        public byte From;
        public byte To;
        public byte Flags;
        public byte PromoPiece;

        public Move(int from, int to, int flags = MoveFlags.Quiet, int promoPiece = Piece.None)
        {
            From = (byte)from;
            To   = (byte)to;
            Flags = (byte)flags;
            PromoPiece = (byte)(promoPiece == Piece.None ? 255 : promoPiece);
        }

        public bool IsCapture   => (Flags & MoveFlags.Capture)   != 0;
        public bool IsCastle    => (Flags & MoveFlags.Castle)     != 0;
        public bool IsEnPassant => (Flags & MoveFlags.EnPassant)  != 0;
        public bool IsPromotion => (Flags & MoveFlags.Promotion)  != 0;
    }

    // -----------------------------------------------------------------------
    // Undo record — everything needed to fully reverse a MakeMove
    // -----------------------------------------------------------------------
    public struct UndoInfo
    {
        public Move Move;
        public int  CapturedPiece;   // Piece.None if quiet
        public int  CastlingRights;
        public int  EnPassantFile;   // -1 = none
    }

    // -----------------------------------------------------------------------
    // Board — 12 bitboards + game state
    // -----------------------------------------------------------------------
    public class Board
    {
        // Square layout: sq = rank*8 + file
        //   rank 0 = row 0 = black's back rank (top of screen)
        //   rank 7 = row 7 = white's back rank (bottom of screen)
        //   file 0 = column 0 = a-file (left)
        public ulong[] BB = new ulong[12];

        // Castling rights: bit0=WK, bit1=WQ, bit2=BK, bit3=BQ
        public int CastlingRights = 0b1111;

        // En passant target file (0-7), or -1 if none
        public int EnPassantFile = -1;

        // Side to move: 0=White, 1=Black
        public int SideToMove = 0;

        // ---- Aggregate occupancy ----
        public ulong White => BB[0]|BB[1]|BB[2]|BB[3]|BB[4]|BB[5];
        public ulong Black => BB[6]|BB[7]|BB[8]|BB[9]|BB[10]|BB[11];
        public ulong All   => White | Black;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Sq(int sq) => 1UL << sq;

        // Returns piece index (0-11) on a square, or Piece.None
        public int PieceOn(int sq)
        {
            ulong bit = Sq(sq);
            for (int i = 0; i < 12; i++)
                if ((BB[i] & bit) != 0) return i;
            return Piece.None;
        }

        // Returns 0=white, 1=black, -1=empty
        public int ColorOn(int sq)
        {
            ulong bit = Sq(sq);
            if ((White & bit) != 0) return 0;
            if ((Black & bit) != 0) return 1;
            return -1;
        }

        public Board Copy()
        {
            var b = new Board();
            Array.Copy(BB, b.BB, 12);
            b.CastlingRights = CastlingRights;
            b.EnPassantFile  = EnPassantFile;
            b.SideToMove     = SideToMove;
            return b;
        }

        // Standard starting position
        public void Reset()
        {
            Array.Clear(BB, 0, 12);
            // Black back rank: squares 0-7 (rank 0)
            BB[Piece.BlackRook]   = Sq(0)  | Sq(7);
            BB[Piece.BlackKnight] = Sq(1)  | Sq(6);
            BB[Piece.BlackBishop] = Sq(2)  | Sq(5);
            BB[Piece.BlackQueen]  = Sq(3);
            BB[Piece.BlackKing]   = Sq(4);
            // Black pawns: rank 1, squares 8-15
            BB[Piece.BlackPawn]   = 0xFFUL << 8;
            // White pawns: rank 6, squares 48-55
            BB[Piece.WhitePawn]   = 0xFFUL << 48;
            // White back rank: rank 7, squares 56-63
            BB[Piece.WhiteRook]   = Sq(56) | Sq(63);
            BB[Piece.WhiteKnight] = Sq(57) | Sq(62);
            BB[Piece.WhiteBishop] = Sq(58) | Sq(61);
            BB[Piece.WhiteQueen]  = Sq(59);
            BB[Piece.WhiteKing]   = Sq(60);
            CastlingRights = 0b1111;
            EnPassantFile  = -1;
            SideToMove     = 0;
        }
    }

    // -----------------------------------------------------------------------
    // MoveGen — pre-computed tables + sliding piece rays + full move generation
    // -----------------------------------------------------------------------
    public static class MoveGen
    {
        private const ulong FileA = 0x0101010101010101UL;
        private const ulong FileH = 0x8080808080808080UL;
        private const ulong FileAB = FileA | (FileA << 1);
        private const ulong FileGH = FileH | (FileH >> 1);
        private const ulong Rank1  = 0xFFUL;
        private const ulong Rank8  = 0xFFUL << 56;
        private const ulong Rank2  = 0xFFUL << 8;
        private const ulong Rank7  = 0xFFUL << 48;

        public static readonly ulong[] KnightAttacks = new ulong[64];
        public static readonly ulong[] KingAttacks   = new ulong[64];

        static MoveGen()
        {
            for (int sq = 0; sq < 64; sq++)
            {
                ulong b = Board.Sq(sq);
                KnightAttacks[sq] =
                    ((b << 17) & ~FileA) | ((b << 15) & ~FileH) |
                    ((b << 10) & ~FileAB)| ((b << 6)  & ~FileGH)|
                    ((b >> 17) & ~FileH) | ((b >> 15) & ~FileA) |
                    ((b >> 10) & ~FileGH)| ((b >> 6)  & ~FileAB);

                KingAttacks[sq] =
                    ((b << 1) & ~FileA) | ((b >> 1) & ~FileH) |
                    (b << 8) | (b >> 8) |
                    ((b << 9) & ~FileA) | ((b << 7) & ~FileH) |
                    ((b >> 9) & ~FileH) | ((b >> 7) & ~FileA);
            }
        }

        // Classical o^(o-2r) sliding attack along a single ray direction.
        // shift > 0 = towards higher squares (north/east/NE/NW)
        // shift < 0 = towards lower squares (south/west/SW/SE)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RayAttack(ulong slider, ulong occ, int shift, ulong wrapMask)
        {
            if (shift > 0)
            {
                ulong attacks = 0;
                ulong ray = slider;
                for (int i = 0; i < 7; i++)
                {
                    ray = ((ray & wrapMask) << shift);
                    attacks |= ray;
                    if ((ray & occ) != 0) break;
                }
                return attacks;
            }
            else
            {
                int s = -shift;
                ulong attacks = 0;
                ulong ray = slider;
                for (int i = 0; i < 7; i++)
                {
                    ray = ((ray & wrapMask) >> s);
                    attacks |= ray;
                    if ((ray & occ) != 0) break;
                }
                return attacks;
            }
        }

        public static ulong RookAttacks(int sq, ulong occ)
        {
            ulong s = Board.Sq(sq);
            return RayAttack(s, occ,  8, ~0UL)    // north
                 | RayAttack(s, occ, -8, ~0UL)    // south
                 | RayAttack(s, occ,  1, ~FileH)  // east
                 | RayAttack(s, occ, -1, ~FileA); // west
        }

        public static ulong BishopAttacks(int sq, ulong occ)
        {
            ulong s = Board.Sq(sq);
            return RayAttack(s, occ,  9, ~FileH)  // NE
                 | RayAttack(s, occ,  7, ~FileA)  // NW
                 | RayAttack(s, occ, -7, ~FileH)  // SE
                 | RayAttack(s, occ, -9, ~FileA); // SW
        }

        public static ulong QueenAttacks(int sq, ulong occ)
            => RookAttacks(sq, occ) | BishopAttacks(sq, occ);

        // Returns a bitboard of all squares attacked by the given side
        public static ulong AttackedSquares(Board board, int side)
        {
            ulong attacked = 0;
            ulong occ = board.All;
            int offset = side * 6; // 0 for white, 6 for black

            // Pawns
            ulong pawns = board.BB[offset + Piece.WhitePawn];
            if (side == 0) // white pawns attack upward (lower rank index)
            {
                attacked |= ((pawns & ~FileA) >> 9) | ((pawns & ~FileH) >> 7);
            }
            else // black pawns attack downward
            {
                attacked |= ((pawns & ~FileH) << 9) | ((pawns & ~FileA) << 7);
            }

            // Knights
            ulong knights = board.BB[offset + Piece.WhiteKnight];
            while (knights != 0)
            {
                int sq = BitOperations.TrailingZeroCount(knights);
                attacked |= KnightAttacks[sq];
                knights &= knights - 1;
            }

            // Bishops
            ulong bishops = board.BB[offset + Piece.WhiteBishop];
            while (bishops != 0)
            {
                int sq = BitOperations.TrailingZeroCount(bishops);
                attacked |= BishopAttacks(sq, occ);
                bishops &= bishops - 1;
            }

            // Rooks
            ulong rooks = board.BB[offset + Piece.WhiteRook];
            while (rooks != 0)
            {
                int sq = BitOperations.TrailingZeroCount(rooks);
                attacked |= RookAttacks(sq, occ);
                rooks &= rooks - 1;
            }

            // Queens
            ulong queens = board.BB[offset + Piece.WhiteQueen];
            while (queens != 0)
            {
                int sq = BitOperations.TrailingZeroCount(queens);
                attacked |= QueenAttacks(sq, occ);
                queens &= queens - 1;
            }

            // King
            ulong king = board.BB[offset + Piece.WhiteKing];
            if (king != 0)
                attacked |= KingAttacks[BitOperations.TrailingZeroCount(king)];

            return attacked;
        }

        public static bool IsInCheck(Board board, int side)
        {
            ulong king = board.BB[side * 6 + Piece.WhiteKing];
            if (king == 0) return false;
            int kSq = BitOperations.TrailingZeroCount(king);
            return (AttackedSquares(board, 1 - side) & Board.Sq(kSq)) != 0;
        }

        // ---- Pseudo-legal move generation ----
        // Adds moves to the list; legality (king safety) is checked in GenerateLegalMoves
        private static void AddMoves(List<Move> moves, int from, ulong targets, int captureFlag, ulong enemyOcc)
        {
            while (targets != 0)
            {
                int to = BitOperations.TrailingZeroCount(targets);
                int flags = (Board.Sq(to) & enemyOcc) != 0 ? captureFlag | MoveFlags.Capture : captureFlag;
                moves.Add(new Move(from, to, flags));
                targets &= targets - 1;
            }
        }

        private static void AddPromotions(List<Move> moves, int from, int to, bool isCapture, int side)
        {
            int baseFlags = isCapture ? MoveFlags.Promotion | MoveFlags.Capture : MoveFlags.Promotion;
            int[] promos = side == 0
                ? new[]{ Piece.WhiteQueen, Piece.WhiteRook, Piece.WhiteBishop, Piece.WhiteKnight }
                : new[]{ Piece.BlackQueen, Piece.BlackRook, Piece.BlackBishop, Piece.BlackKnight };
            foreach (int p in promos)
                moves.Add(new Move(from, to, baseFlags, p));
        }

        public static List<Move> GeneratePseudoLegal(Board board)
        {
            var moves = new List<Move>(64);
            int side   = board.SideToMove;
            int offset = side * 6;
            ulong own    = side == 0 ? board.White : board.Black;
            ulong enemy  = side == 0 ? board.Black : board.White;
            ulong occ    = board.All;

            // ---- Pawns ----
            ulong pawns = board.BB[offset + Piece.WhitePawn];
            if (side == 0) // White: moves toward lower rank index (shift >> 8)
            {
                ulong singlePush = (pawns >> 8) & ~occ;
                ulong doublePush = ((singlePush & (0xFFUL << 40)) >> 8) & ~occ; // rank 6 pawns only
                ulong captureL   = ((pawns & ~FileA) >> 9) & enemy;
                ulong captureR   = ((pawns & ~FileH) >> 7) & enemy;

                // Single push
                ulong sp = singlePush & ~(0xFFUL); // not rank 0 (no promo)
                while (sp != 0) { int to = BitOperations.TrailingZeroCount(sp); moves.Add(new Move(to+8, to)); sp &= sp-1; }
                // Double push
                while (doublePush != 0) { int to = BitOperations.TrailingZeroCount(doublePush); moves.Add(new Move(to+16, to)); doublePush &= doublePush-1; }
                // Promotions from single push
                ulong promoSP = singlePush & 0xFFUL;
                while (promoSP != 0) { int to = BitOperations.TrailingZeroCount(promoSP); AddPromotions(moves, to+8, to, false, 0); promoSP &= promoSP-1; }
                // Capture left
                ulong cl = captureL & ~(0xFFUL);
                while (cl != 0) { int to = BitOperations.TrailingZeroCount(cl); moves.Add(new Move(to+9, to, MoveFlags.Capture)); cl &= cl-1; }
                ulong clp = captureL & 0xFFUL;
                while (clp != 0) { int to = BitOperations.TrailingZeroCount(clp); AddPromotions(moves, to+9, to, true, 0); clp &= clp-1; }
                // Capture right
                ulong cr = captureR & ~(0xFFUL);
                while (cr != 0) { int to = BitOperations.TrailingZeroCount(cr); moves.Add(new Move(to+7, to, MoveFlags.Capture)); cr &= cr-1; }
                ulong crp = captureR & 0xFFUL;
                while (crp != 0) { int to = BitOperations.TrailingZeroCount(crp); AddPromotions(moves, to+7, to, true, 0); crp &= crp-1; }
                // En passant
                if (board.EnPassantFile >= 0)
                {
                    int epRank = 2; // white captures on rank 2 (sq 16-23)
                    int epSq   = epRank * 8 + board.EnPassantFile;
                    ulong epBit = Board.Sq(epSq);
                    if (((pawns & ~FileA) >> 9 & epBit) != 0) moves.Add(new Move(epSq+9, epSq, MoveFlags.EnPassant | MoveFlags.Capture));
                    if (((pawns & ~FileH) >> 7 & epBit) != 0) moves.Add(new Move(epSq+7, epSq, MoveFlags.EnPassant | MoveFlags.Capture));
                }
            }
            else // Black: moves toward higher rank index (shift << 8)
            {
                ulong singlePush = (pawns << 8) & ~occ;
                ulong doublePush = ((singlePush & (0xFFUL << 16)) << 8) & ~occ; // rank 1 pawns only
                ulong captureL   = ((pawns & ~FileH) << 9) & enemy;
                ulong captureR   = ((pawns & ~FileA) << 7) & enemy;

                ulong sp = singlePush & ~(0xFFUL << 56);
                while (sp != 0) { int to = BitOperations.TrailingZeroCount(sp); moves.Add(new Move(to-8, to)); sp &= sp-1; }
                while (doublePush != 0) { int to = BitOperations.TrailingZeroCount(doublePush); moves.Add(new Move(to-16, to)); doublePush &= doublePush-1; }
                ulong promoSP = singlePush & (0xFFUL << 56);
                while (promoSP != 0) { int to = BitOperations.TrailingZeroCount(promoSP); AddPromotions(moves, to-8, to, false, 1); promoSP &= promoSP-1; }
                ulong cl = captureL & ~(0xFFUL << 56);
                while (cl != 0) { int to = BitOperations.TrailingZeroCount(cl); moves.Add(new Move(to-9, to, MoveFlags.Capture)); cl &= cl-1; }
                ulong clp = captureL & (0xFFUL << 56);
                while (clp != 0) { int to = BitOperations.TrailingZeroCount(clp); AddPromotions(moves, to-9, to, true, 1); clp &= clp-1; }
                ulong cr = captureR & ~(0xFFUL << 56);
                while (cr != 0) { int to = BitOperations.TrailingZeroCount(cr); moves.Add(new Move(to-7, to, MoveFlags.Capture)); cr &= cr-1; }
                ulong crp = captureR & (0xFFUL << 56);
                while (crp != 0) { int to = BitOperations.TrailingZeroCount(crp); AddPromotions(moves, to-7, to, true, 1); crp &= crp-1; }
                if (board.EnPassantFile >= 0)
                {
                    int epRank = 5;
                    int epSq   = epRank * 8 + board.EnPassantFile;
                    ulong epBit = Board.Sq(epSq);
                    if (((pawns & ~FileH) << 9 & epBit) != 0) moves.Add(new Move(epSq-9, epSq, MoveFlags.EnPassant | MoveFlags.Capture));
                    if (((pawns & ~FileA) << 7 & epBit) != 0) moves.Add(new Move(epSq-7, epSq, MoveFlags.EnPassant | MoveFlags.Capture));
                }
            }

            // ---- Knights ----
            ulong knights = board.BB[offset + Piece.WhiteKnight];
            while (knights != 0)
            {
                int sq = BitOperations.TrailingZeroCount(knights);
                AddMoves(moves, sq, KnightAttacks[sq] & ~own, MoveFlags.Quiet, enemy);
                knights &= knights - 1;
            }

            // ---- Bishops ----
            ulong bishops = board.BB[offset + Piece.WhiteBishop];
            while (bishops != 0)
            {
                int sq = BitOperations.TrailingZeroCount(bishops);
                AddMoves(moves, sq, BishopAttacks(sq, occ) & ~own, MoveFlags.Quiet, enemy);
                bishops &= bishops - 1;
            }

            // ---- Rooks ----
            ulong rooks = board.BB[offset + Piece.WhiteRook];
            while (rooks != 0)
            {
                int sq = BitOperations.TrailingZeroCount(rooks);
                AddMoves(moves, sq, RookAttacks(sq, occ) & ~own, MoveFlags.Quiet, enemy);
                rooks &= rooks - 1;
            }

            // ---- Queens ----
            ulong queens = board.BB[offset + Piece.WhiteQueen];
            while (queens != 0)
            {
                int sq = BitOperations.TrailingZeroCount(queens);
                AddMoves(moves, sq, QueenAttacks(sq, occ) & ~own, MoveFlags.Quiet, enemy);
                queens &= queens - 1;
            }

            // ---- King ----
            ulong king = board.BB[offset + Piece.WhiteKing];
            if (king != 0)
            {
                int kSq = BitOperations.TrailingZeroCount(king);
                AddMoves(moves, kSq, KingAttacks[kSq] & ~own, MoveFlags.Quiet, enemy);

                // Castling
                ulong attacked = AttackedSquares(board, 1 - side);
                if (side == 0) // White castles on rank 7
                {
                    // Kingside: e1=60, f1=61, g1=62, h1=63
                    if ((board.CastlingRights & 1) != 0
                        && (occ & (Board.Sq(61)|Board.Sq(62))) == 0
                        && (attacked & (Board.Sq(60)|Board.Sq(61)|Board.Sq(62))) == 0)
                        moves.Add(new Move(60, 62, MoveFlags.Castle));
                    // Queenside: e1=60, d1=59, c1=58, b1=57, a1=56
                    if ((board.CastlingRights & 2) != 0
                        && (occ & (Board.Sq(59)|Board.Sq(58)|Board.Sq(57))) == 0
                        && (attacked & (Board.Sq(60)|Board.Sq(59)|Board.Sq(58))) == 0)
                        moves.Add(new Move(60, 58, MoveFlags.Castle));
                }
                else // Black castles on rank 0
                {
                    // Kingside: e8=4, f8=5, g8=6, h8=7
                    if ((board.CastlingRights & 4) != 0
                        && (occ & (Board.Sq(5)|Board.Sq(6))) == 0
                        && (attacked & (Board.Sq(4)|Board.Sq(5)|Board.Sq(6))) == 0)
                        moves.Add(new Move(4, 6, MoveFlags.Castle));
                    // Queenside: e8=4, d8=3, c8=2, b8=1, a8=0
                    if ((board.CastlingRights & 8) != 0
                        && (occ & (Board.Sq(3)|Board.Sq(2)|Board.Sq(1))) == 0
                        && (attacked & (Board.Sq(4)|Board.Sq(3)|Board.Sq(2))) == 0)
                        moves.Add(new Move(4, 2, MoveFlags.Castle));
                }
            }

            return moves;
        }

        // Filter pseudo-legal moves to only those that don't leave own king in check
        public static List<Move> GenerateLegalMoves(Board board)
        {
            var pseudo = GeneratePseudoLegal(board);
            var legal  = new List<Move>(pseudo.Count);
            foreach (var move in pseudo)
            {
                var undo = MakeMove(board, move);
                if (!IsInCheck(board, 1 - board.SideToMove)) // side that just moved
                    legal.Add(move);
                UnmakeMove(board, undo);
            }
            return legal;
        }

        // ---- Make / Unmake ----
        public static UndoInfo MakeMove(Board board, Move move)
        {
            var undo = new UndoInfo
            {
                Move           = move,
                CapturedPiece  = Piece.None,
                CastlingRights = board.CastlingRights,
                EnPassantFile  = board.EnPassantFile,
            };

            int side   = board.SideToMove;
            int offset = side * 6;
            int from   = move.From;
            int to     = move.To;
            int moving = board.PieceOn(from);

            board.EnPassantFile = -1;

            // Remove moving piece from source
            board.BB[moving] &= ~Board.Sq(from);

            // Handle captures
            if (move.IsEnPassant)
            {
                // Captured pawn is on same rank as 'from', same file as 'to'
                int capSq = (from / 8) * 8 + to % 8;
                int capPiece = board.PieceOn(capSq);
                undo.CapturedPiece = capPiece;
                board.BB[capPiece] &= ~Board.Sq(capSq);
            }
            else if (move.IsCapture)
            {
                int capPiece = board.PieceOn(to);
                undo.CapturedPiece = capPiece;
                if (capPiece != Piece.None)
                    board.BB[capPiece] &= ~Board.Sq(to);
            }

            // Place piece on destination (or promoted piece)
            if (move.IsPromotion)
                board.BB[move.PromoPiece] |= Board.Sq(to);
            else
                board.BB[moving] |= Board.Sq(to);

            // Castling: move the rook
            if (move.IsCastle)
            {
                if (side == 0)
                {
                    if (to == 62) { board.BB[Piece.WhiteRook] &= ~Board.Sq(63); board.BB[Piece.WhiteRook] |= Board.Sq(61); }
                    else          { board.BB[Piece.WhiteRook] &= ~Board.Sq(56); board.BB[Piece.WhiteRook] |= Board.Sq(59); }
                }
                else
                {
                    if (to == 6)  { board.BB[Piece.BlackRook] &= ~Board.Sq(7);  board.BB[Piece.BlackRook] |= Board.Sq(5);  }
                    else          { board.BB[Piece.BlackRook] &= ~Board.Sq(0);  board.BB[Piece.BlackRook] |= Board.Sq(3);  }
                }
            }

            // Update castling rights
            if (moving == Piece.WhiteKing) board.CastlingRights &= ~0b0011;
            if (moving == Piece.BlackKing) board.CastlingRights &= ~0b1100;
            if (moving == Piece.WhiteRook)
            {
                if (from == 63) board.CastlingRights &= ~0b0001;
                if (from == 56) board.CastlingRights &= ~0b0010;
            }
            if (moving == Piece.BlackRook)
            {
                if (from == 7)  board.CastlingRights &= ~0b0100;
                if (from == 0)  board.CastlingRights &= ~0b1000;
            }
            // Rook captured on its starting square also loses rights
            if (undo.CapturedPiece == Piece.WhiteRook)
            {
                if (to == 63) board.CastlingRights &= ~0b0001;
                if (to == 56) board.CastlingRights &= ~0b0010;
            }
            if (undo.CapturedPiece == Piece.BlackRook)
            {
                if (to == 7)  board.CastlingRights &= ~0b0100;
                if (to == 0)  board.CastlingRights &= ~0b1000;
            }

            // Set en passant file for double pawn push
            if (moving == Piece.WhitePawn && from - to == 16)
                board.EnPassantFile = from % 8;
            if (moving == Piece.BlackPawn && to - from == 16)
                board.EnPassantFile = from % 8;

            board.SideToMove ^= 1;
            return undo;
        }

        public static void UnmakeMove(Board board, UndoInfo undo)
        {
            board.SideToMove   ^= 1;
            board.CastlingRights = undo.CastlingRights;
            board.EnPassantFile  = undo.EnPassantFile;

            int side = board.SideToMove;
            var move = undo.Move;
            int from = move.From;
            int to   = move.To;

            // Determine what piece is currently on 'to' (may be promoted piece)
            int onTo = board.PieceOn(to);

            // Remove from destination
            if (onTo != Piece.None)
                board.BB[onTo] &= ~Board.Sq(to);

            // Restore moving piece to source
            int moving = move.IsPromotion
                ? Piece.Make(side, Piece.TypeOf(Piece.WhitePawn)) // restore pawn
                : onTo;
            board.BB[moving] |= Board.Sq(from);

            // Restore captured piece
            if (move.IsEnPassant)
            {
                int capSq = (from / 8) * 8 + to % 8;
                board.BB[undo.CapturedPiece] |= Board.Sq(capSq);
            }
            else if (undo.CapturedPiece != Piece.None)
            {
                board.BB[undo.CapturedPiece] |= Board.Sq(to);
            }

            // Undo castling rook move
            if (move.IsCastle)
            {
                if (side == 0)
                {
                    if (to == 62) { board.BB[Piece.WhiteRook] &= ~Board.Sq(61); board.BB[Piece.WhiteRook] |= Board.Sq(63); }
                    else          { board.BB[Piece.WhiteRook] &= ~Board.Sq(59); board.BB[Piece.WhiteRook] |= Board.Sq(56); }
                }
                else
                {
                    if (to == 6)  { board.BB[Piece.BlackRook] &= ~Board.Sq(5);  board.BB[Piece.BlackRook] |= Board.Sq(7);  }
                    else          { board.BB[Piece.BlackRook] &= ~Board.Sq(3);  board.BB[Piece.BlackRook] |= Board.Sq(0);  }
                }
            }
        }
    } // end MoveGen
} // end namespace Chess

using Gtk;
using Pieces_namespace;
using Agent_namespace;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Controller_namespace
{
    public class Move
    {
        public int from_x, from_y, to_x, to_y;
        public Piece capturedPiece;
        public bool wasPawnFirstMove;
        public bool isPromotion;
        public bool isCastling;
        // Add fields for castling rights, en passant, etc., if needed for unmaking
        public (bool, bool) oldWhiteCastleRights;
        public (bool, bool) oldBlackCastleRights;

        public Move(int from_x, int from_y, int to_x, int to_y)
        {
            this.from_x = from_x; this.from_y = from_y; this.to_x = to_x; this.to_y = to_y;
            this.capturedPiece = null; this.wasPawnFirstMove = false; this.isPromotion = false; this.isCastling = false;
        }
    }

    public class Board
    {
        private Piece?[,] board = new Piece?[8, 8];
        private bool canWhiteCastleKingside = true, canWhiteCastleQueenside = true;
        private bool canBlackCastleKingside = true, canBlackCastleQueenside = true;

        public Piece? this[int i, int j]
        {
            set => board[i, j] = value;
            get => board[i, j];
        }

        public Board Copy()
        {
            Board copy = new Board();
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) if (board[i, j] != null) copy[i, j] = board[i, j].Copy();
            copy.canWhiteCastleKingside = this.canWhiteCastleKingside;
            copy.canWhiteCastleQueenside = this.canWhiteCastleQueenside;
            copy.canBlackCastleKingside = this.canBlackCastleKingside;
            copy.canBlackCastleQueenside = this.canBlackCastleQueenside;
            return copy;
        }
        
        public bool CanCastle(int color, bool isKingside)
        {
            if (color == 1) return isKingside ? canWhiteCastleKingside : canWhiteCastleQueenside;
            return isKingside ? canBlackCastleKingside : canBlackCastleQueenside;
        }

        public void MakeMove(Move move)
        {
            Piece movingPiece = this[move.from_x, move.from_y];
            move.capturedPiece = this[move.to_x, move.to_y];
            
            // Store old castling rights for unmove
            move.oldWhiteCastleRights = (canWhiteCastleKingside, canWhiteCastleQueenside);
            move.oldBlackCastleRights = (canBlackCastleKingside, canBlackCastleQueenside);

            if (movingPiece is Pawn pawn)
            {
                move.wasPawnFirstMove = pawn.FirstMove;
                // Check for promotion
                if ((pawn.Color == 1 && move.to_y == 0) || (pawn.Color == -1 && move.to_y == 7))
                {
                    move.isPromotion = true;
                    this[move.from_x, move.from_y] = new Queen(pawn.Color, pawn.Pos); // Default to Queen for AI
                }
            } else if (movingPiece is King king)
            {
                king.hasMoved = true;
                if (king.Color == 1) { canWhiteCastleKingside = canWhiteCastleQueenside = false; }
                else { canBlackCastleKingside = canBlackCastleQueenside = false; }
            } else if (movingPiece is Rook rook)
            {
                if (rook.Color == 1)
                {
                    if (rook.Pos == (0, 7)) canWhiteCastleQueenside = false;
                    else if (rook.Pos == (7, 7)) canWhiteCastleKingside = false;
                } else
                {
                    if (rook.Pos == (0, 0)) canBlackCastleQueenside = false;
                    else if (rook.Pos == (7, 0)) canBlackCastleKingside = false;
                }
            }

            // Handle castling
            if (move.isCastling)
            {
                movingPiece.Move_to((move.to_x, move.to_y), this);
                int rook_from_x = move.to_x > move.from_x ? 7 : 0;
                int rook_to_x = move.to_x > move.from_x ? 5 : 3;
                Piece rook = this[rook_from_x, move.from_y];
                rook.Move_to((rook_to_x, move.from_y), this);
            }
            else
            {
                 movingPiece.Move_to((move.to_x, move.to_y), this);
            }
        }

        public void UnmakeMove(Move move)
        {
            Piece movingPiece = this[move.to_x, move.to_y];

            // Restore castling rights
            (canWhiteCastleKingside, canWhiteCastleQueenside) = move.oldWhiteCastleRights;
            (canBlackCastleKingside, canBlackCastleQueenside) = move.oldBlackCastleRights;
            
            // Handle castling undo
            if (move.isCastling)
            {
                // Move king back
                this[move.from_x, move.from_y] = movingPiece;
                this[move.to_x, move.to_y] = null;
                movingPiece.Pos = (move.from_x, move.from_y);

                // Move rook back
                int rook_from_x = move.to_x > move.from_x ? 7 : 0;
                int rook_to_x = move.to_x > move.from_x ? 5 : 3;
                Piece rook = this[rook_to_x, move.from_y];
                this[rook_from_x, move.from_y] = rook;
                this[rook_to_x, move.from_y] = null;
                rook.Pos = (rook_from_x, move.from_y);
            }
            else
            {
                 // Handle standard unmove
                if (move.isPromotion)
                {
                    movingPiece = new Pawn(movingPiece.Color, movingPiece.Pos);
                }
                
                this[move.from_x, move.from_y] = movingPiece;
                this[move.to_x, move.to_y] = move.capturedPiece;
                movingPiece.Pos = (move.from_x, move.from_y);
            }

            if (movingPiece is Pawn pawn) pawn.FirstMove = move.wasPawnFirstMove;
            if (movingPiece is King king) king.hasMoved = !((king.Color == 1 && king.Pos == (4,7)) || (king.Color == -1 && king.Pos == (4,0)));
        }

        public (int, int) FindKing(int color)
        {
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) if (board[i, j] is King k && k.Color == color) return (i, j);
            return (-1, -1); // Should not happen
        }

        public bool IsSquareAttacked((int, int) square, int attackerColor)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Piece p = board[i, j];
                    if (p != null && p.Color == attackerColor)
                    {
                        // For pawns, we check their capture moves specifically
                        if (p is Pawn pawn)
                        {
                            int y_dir = -pawn.Color;
                            if (square.Item2 == p.Pos.Item2 + y_dir && (square.Item1 == p.Pos.Item1 + 1 || square.Item1 == p.Pos.Item1 - 1))
                            {
                                return true;
                            }
                        }
                        else if (p is King king)
                        {
                            // Call the new, non-recursive method for kings to break the loop
                            foreach (var move in king.GetStandardMoves())
                            {
                                if (move.to_x == square.Item1 && move.to_y == square.Item2) return true;
                            }
                        }
                        else
                        {
                            // This is fine for all other pieces
                            foreach (var move in p.Show_Possible_Moves(this))
                            {
                                if (move.to_x == square.Item1 && move.to_y == square.Item2) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public List<Move> GenerateLegalMoves(int color)
        {
            List<Move> legalMoves = new List<Move>();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] != null && board[i, j].Color == color)
                    {
                        List<Move> pseudoLegalMoves = board[i, j].Show_Possible_Moves(this);
                        foreach (var move in pseudoLegalMoves)
                        {
                            MakeMove(move);
                            var kingPos = FindKing(color);
                            if (!IsSquareAttacked(kingPos, -color))
                            {
                                legalMoves.Add(move);
                            }
                            UnmakeMove(move);
                        }
                    }
                }
            }
            return legalMoves;
        }
    }

    public class Controller
    {
        public Board board = new Board();
        public View_namespace.Cell[,] cells = new View_namespace.Cell[8, 8];
        public List<(int, int)> ModifiedBgs = new List<(int, int)>();
        public Piece Selected;
        public int player = 1;
        private int winner = 0;
        private bool paused = false;
        public bool ended = false;
        private Move last_move_for_promotion;
        private Agent agent = new Agent();

        public Controller(Agent agent)
        {
            this.agent = agent;
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            // Pawns
            for (int i = 0; i < 8; i++) { board[i, 1] = new Pawn(-1, (i, 1)); board[i, 6] = new Pawn(1, (i, 6)); }
            // Rooks
            board[0, 0] = new Rook(-1, (0, 0)); board[7, 0] = new Rook(-1, (7, 0)); board[0, 7] = new Rook(1, (0, 7)); board[7, 7] = new Rook(1, (7, 7));
            // Knights
            board[1, 0] = new Knight(-1, (1, 0)); board[6, 0] = new Knight(-1, (6, 0)); board[1, 7] = new Knight(1, (1, 7)); board[6, 7] = new Knight(1, (6, 7));
            // Bishops
            board[2, 0] = new Bishop(-1, (2, 0)); board[5, 0] = new Bishop(-1, (5, 0)); board[2, 7] = new Bishop(1, (2, 7)); board[5, 7] = new Bishop(1, (5, 7));
            // Queens
            board[3, 0] = new Queen(-1, (3, 0)); board[3, 7] = new Queen(1, (3, 7));
            // Kings
            board[4, 0] = new King(-1, (4, 0)); board[4, 7] = new King(1, (4, 7));
        }

        public void RestoreBackgrounds()
        {
            foreach ((int, int) coord in ModifiedBgs) cells[coord.Item1, coord.Item2].ModifyBg(StateType.Normal, cells[coord.Item1, coord.Item2].BgColor);
            ModifiedBgs.Clear();
        }

        public void DisplayMoves(List<Move> moves)
        {
            Gdk.Color moveColor = new Gdk.Color(33, 36, 148);
            foreach (var move in moves)
            {
                ModifiedBgs.Add((move.to_x, move.to_y));
                cells[move.to_x, move.to_y].ModifyBg(StateType.Normal, moveColor);
            }
        }
        
        public void ExecuteMove(Move move)
        {
            if (winner != 0 || ended) return;
            
            // --- Promotion Logic ---
            Piece movingPiece = board[move.from_x, move.from_y];
            if (movingPiece is Pawn && (move.to_y == 0 || move.to_y == 7))
            {
                if (player == 1) // Human player promotes
                {
                    paused = true;
                    last_move_for_promotion = move;
                    View_namespace.Program.Show_Choices(movingPiece.Color);
                    return; // Wait for player choice
                }
                // AI promotes to Queen by default in MakeMove
            }

            board.MakeMove(move);
            UpdateUI(move);
            CheckEndGame();
        }

        private void UpdateUI(Move move)
        {
            // Clear 'from' square
            cells[move.from_x, move.from_y].Image.Pixbuf = null;
            
            // Clear 'to' square of any existing piece image
            foreach (var child in cells[move.to_x, move.to_y].Children) cells[move.to_x, move.to_y].Remove(child);
            
            // Draw piece on 'to' square
            Piece pieceOnToSquare = board[move.to_x, move.to_y];
            if (pieceOnToSquare != null)
            {
                Gdk.Pixbuf originalPixbuf = new Gdk.Pixbuf(pieceOnToSquare.Address);
                var scaledPixbuf = originalPixbuf.ScaleSimple(90, 90, Gdk.InterpType.Bilinear);
                cells[move.to_x, move.to_y].Image = new Image(scaledPixbuf);
                cells[move.to_x, move.to_y].Add(cells[move.to_x, move.to_y].Image);
                cells[move.to_x, move.to_y].Image.Show();
            }

            // Handle castling UI for rook
            if (move.isCastling)
            {
                 int rook_from_x = move.to_x > move.from_x ? 7 : 0;
                 int rook_to_x = move.to_x > move.from_x ? 5 : 3;
                 cells[rook_from_x, move.from_y].Image.Pixbuf = null;
                 Piece rook = board[rook_to_x, move.from_y];
                 Gdk.Pixbuf rookPixbuf = new Gdk.Pixbuf(rook.Address).ScaleSimple(90, 90, Gdk.InterpType.Bilinear);
                 cells[rook_to_x, move.from_y].Image = new Image(rookPixbuf);
                 cells[rook_to_x, move.from_y].Add(cells[rook_to_x, move.from_y].Image);
                 cells[rook_to_x, move.from_y].Image.Show();
            }
        }
        
        private void CheckEndGame()
        {
            player = -player; // Switch player
            var legalMoves = board.GenerateLegalMoves(player);
            
            if (legalMoves.Count == 0)
            {
                ended = true;
                var kingPos = board.FindKing(player);
                if (board.IsSquareAttacked(kingPos, -player))
                {
                    winner = -player; // Checkmate
                    Console.WriteLine($"Checkmate! Winner: {winner}");
                    WinAnimation(winner);
                }
                else
                {
                    winner = 0; // Stalemate
                    Console.WriteLine("Stalemate!");
                    DrawAnimation();
                }
            }
        }
        
        public void HandlePressChessBoard(int i, int j)
        {
            if (paused || ended) return;

            if (board[i, j] != null && board[i, j].Color == player)
            {
                RestoreBackgrounds();
                Selected = board[i, j];
                var legalMoves = board.GenerateLegalMoves(player).Where(m => m.from_x == i && m.from_y == j).ToList();
                DisplayMoves(legalMoves);
            }
            else if (ModifiedBgs.Contains((i, j)))
            {
                RestoreBackgrounds();
                Move selectedMove = new Move(Selected.Pos.Item1, Selected.Pos.Item2, i, j);
                
                // Find the exact move object to get castling flag
                var legalMovesForPiece = board.GenerateLegalMoves(player).Where(m => m.from_x == Selected.Pos.Item1 && m.from_y == Selected.Pos.Item2);
                var actualMove = legalMovesForPiece.FirstOrDefault(m => m.to_x == i && m.to_y == j);
                
                if (actualMove != null) {
                    ExecuteMove(actualMove);
                    if (!ended && player == -1) // If its now AIs turn
                    {
                        GLib.Idle.Add(() => { aiMove(); return false; });
                    }
                }
            }
            else
            {
                RestoreBackgrounds();
            }
        }
        
        public void HandlePressChoiseBox(int i)
        {
            paused = false;
            Piece promotedPiece;
            (int, int) pos = (last_move_for_promotion.from_x, last_move_for_promotion.from_y);

            switch(i)
            {
                case 0: promotedPiece = new Knight(player, pos); break;
                case 1: promotedPiece = new Bishop(player, pos); break;
                case 2: promotedPiece = new Rook(player, pos); break;
                default: promotedPiece = new Queen(player, pos); break;
            }

            board[pos.Item1, pos.Item2] = promotedPiece;
            
            // Now execute the full move
            ExecuteMove(last_move_for_promotion);
             if (!ended && player == -1) // If it's now AIs turn
            {
                GLib.Idle.Add(() => { aiMove(); return false; });
            }
        }
        
        private async Task aiMove()
        {
            Move bestMove = await Task.Run(() => agent.GetBestMove(board, -1));
            if (bestMove is null)
            {
                // This case should be handled by the CheckEndGame logic, but as a fallback:
                winner = player; // The player who has no moves loses.
                WinAnimation(winner);
                return;
            }
            ExecuteMove(bestMove);
            Console.WriteLine("Computer has made a move");
        }

        public void WinAnimation(int winningPlayer)
        {
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) if (board[i, j] != null && board[i, j].Color == winningPlayer) cells[i, j].ModifyBg(StateType.Normal, new Gdk.Color(33, 36, 148));
        }

        public void DrawAnimation()
        {
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) if (board[i, j] != null) cells[i, j].ModifyBg(StateType.Normal, new Gdk.Color(33, 36, 148));
        }
    }

}

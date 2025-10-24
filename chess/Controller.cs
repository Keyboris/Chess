using Gtk;
using Pieces_namespace;
using Agent_namespace;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace Controller_namespace
{

    public class Move
    {
        public int from_x, from_y, to_x, to_y;
        public Piece capturedPiece;
        public bool wasPawnFirstMove;
        public bool isPromotion;
        public bool isCastling;
        public bool movingPieceOldHasMoved;
        public (bool, bool) oldWhiteCastleRights;
        public (bool, bool) oldBlackCastleRights;

        public bool isEnPassant;
        public (int, int)? oldEnPassantTarget;
        private bool isMating = false;
        private bool isChecking = false;

        public Move(int from_x, int from_y, int to_x, int to_y)
        {
            this.from_x = from_x;
            this.from_y = from_y;
            this.to_x = to_x;
            this.to_y = to_y;
            this.capturedPiece = null;
            this.wasPawnFirstMove = false;
            this.isPromotion = false;
            this.isCastling = false;
            this.isEnPassant = false;
        }
        
        public bool IsChecking
        {
            get => isChecking;
            set => isChecking = value;
        }
        
        public bool IsMating
        {
            get => isMating;
            set => isMating = value;
        }
    }

    // (Board class from line 41 to 326)
    public class Board
    {
        private Piece?[,] board = new Piece?[8, 8];
        private bool canWhiteCastleKingside = true, canWhiteCastleQueenside = true;
        private bool canBlackCastleKingside = true, canBlackCastleQueenside = true;

        private int whiteScore = 0, blackScore = 0;

        public int getWhiteScore => whiteScore;
        public int getBlackScore => blackScore;
        private (int, int)? enPassantTarget = null; 

        public (int, int)? EnPassantTarget 
        { 
            get => enPassantTarget; 
            set => enPassantTarget = value; 
        }

        public Piece? this[int i, int j]
        {
            set => board[i, j] = value;
            get => board[i, j];
        }



        public Board Copy()
        {
            Board copy = new Board();
            for (int i = 0; i < 8; i++) 
                for (int j = 0; j < 8; j++) 
                    if (board[i, j] != null) 
                        copy[i, j] = board[i, j].Copy();
            
            copy.canWhiteCastleKingside = this.canWhiteCastleKingside;
            copy.canWhiteCastleQueenside = this.canWhiteCastleQueenside;
            copy.canBlackCastleKingside = this.canBlackCastleKingside;
            copy.canBlackCastleQueenside = this.canBlackCastleQueenside;
            copy.enPassantTarget = this.enPassantTarget;
            return copy;
        }

        public bool CanCastle(int color, bool isKingside)
        {
            if (color == 1) return isKingside ? canWhiteCastleKingside : canWhiteCastleQueenside;
            return isKingside ? canBlackCastleKingside : canBlackCastleQueenside;
        }
        
        public void CalculateInitialScores()
        {
            whiteScore = 0;
            blackScore = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] != null)
                    {
                        if (board[i, j].Color == 1)
                            whiteScore += board[i, j].Score;
                        else
                            blackScore += board[i, j].Score;
                    }
                }
            }
            Console.WriteLine($"DEBUG: Initial scores calculated. White: {whiteScore}, Black: {blackScore}");
        }

        public void MakeMove(Move move)
        {
            // If this[move.from_x, move.from_y] is null, it's because of a bad
            // board setup in InitializeBoard. This prevents an immediate crash.
            Piece movingPiece = this[move.from_x, move.from_y];
            if (movingPiece == null)
            {
                // This should not happen if the board is set up correctly.
                Console.WriteLine($"ERROR: MakeMove - No piece found at ({move.from_x},{move.from_y}). Check InitializeBoard setup.");
                return;
            }

            move.capturedPiece = this[move.to_x, move.to_y];

            if (move.capturedPiece is not null)
            {
                if (move.capturedPiece.Color == 1)
                {
                    whiteScore -= move.capturedPiece.Score;
                }
                else
                {
                    blackScore -= move.capturedPiece.Score;
                }
            }
            
            // Store old castling rights for unmove
            move.oldWhiteCastleRights = (canWhiteCastleKingside, canWhiteCastleQueenside);
            move.oldBlackCastleRights = (canBlackCastleKingside, canBlackCastleQueenside);


            if (move.capturedPiece is Rook capturedRook)
            {
                if (capturedRook.Color == 1) // White rook captured
                {
                    if (capturedRook.Pos == (0, 7)) canWhiteCastleQueenside = false;
                    else if (capturedRook.Pos == (7, 7)) canWhiteCastleKingside = false;
                }
                else // Black rook captured
                {
                    if (capturedRook.Pos == (0, 0)) canBlackCastleQueenside = false;
                    else if (capturedRook.Pos == (7, 0)) canBlackCastleKingside = false;
                }
            }
        
            move.oldEnPassantTarget = enPassantTarget;
            enPassantTarget = null;

            if (movingPiece is Pawn pawn)
            {
                move.wasPawnFirstMove = pawn.FirstMove;
                
                // Check if this is a two-square pawn move
                if (Math.Abs(move.to_y - move.from_y) == 2)
                {
                    // Set en passant target to the square the pawn passed over
                    enPassantTarget = (move.from_x, (move.from_y + move.to_y) / 2);
                }
                
                // Handle en passant capture
                if (move.isEnPassant)
                {
                    // Remove the captured pawn (which is NOT on the destination square)
                    int capturedPawnY = move.from_y; // Same row as the capturing pawn
                    move.capturedPiece = this[move.to_x, capturedPawnY];
                    this[move.to_x, capturedPawnY] = null;
                }
                
                // Check for promotion
                if ((pawn.Color == 1 && move.to_y == 0) || (pawn.Color == -1 && move.to_y == 7))
                {
                    move.isPromotion = true;
                    
                    Piece promoted = new Queen(pawn.Color, (move.from_x, move.from_y)); 

                    this[move.from_x, move.from_y] = promoted; 
                    movingPiece = promoted; 
                }
            } 
            else if (movingPiece is King king)
            {
                move.movingPieceOldHasMoved = king.hasMoved;
                king.hasMoved = true;
                if (king.Color == 1) { canWhiteCastleKingside = canWhiteCastleQueenside = false; }
                else { canBlackCastleKingside = canBlackCastleQueenside = false; }
            } 
            else if (movingPiece is Rook rook)
            {
                move.movingPieceOldHasMoved = rook.hasMoved;
                if (rook.Color == 1)
                {
                    if (rook.Pos == (0, 7)) canWhiteCastleQueenside = false;
                    else if (rook.Pos == (7, 7)) canWhiteCastleKingside = false;
                } 
                else
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

            // Restore en passant target
            enPassantTarget = move.oldEnPassantTarget;
            
            // Restore castling rights
            (canWhiteCastleKingside, canWhiteCastleQueenside) = move.oldWhiteCastleRights;
            (canBlackCastleKingside, canBlackCastleQueenside) = move.oldBlackCastleRights;
            
            // Handle en passant undo
            if (move.isEnPassant)
            {
                // Move the capturing pawn back
                this[move.from_x, move.from_y] = movingPiece;
                this[move.to_x, move.to_y] = null;
                movingPiece.Pos = (move.from_x, move.from_y);
                
                // Restore the captured pawn
                int capturedPawnY = move.from_y;
                this[move.to_x, capturedPawnY] = move.capturedPiece;
                
                if (movingPiece is Pawn movedPawn) movedPawn.FirstMove = move.wasPawnFirstMove;
                return;
            }
            
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

                if (move.capturedPiece is not null)
                {
                    if (move.capturedPiece.Color == 1)
                    {
                        whiteScore += move.capturedPiece.Score;
                    }
                    else
                    {
                        blackScore += move.capturedPiece.Score;
                    }
                }

            }

            // Restore hasMoved from the Move object
            if (movingPiece is Pawn pawn) pawn.FirstMove = move.wasPawnFirstMove;
            else if (movingPiece is King king) king.hasMoved = move.movingPieceOldHasMoved;
            else if (movingPiece is Rook rook) rook.hasMoved = move.movingPieceOldHasMoved;
        }

        public (int, int) FindKing(int color)
        {
            for (int i = 0; i < 8; i++) 
                for (int j = 0; j < 8; j++) 
                    if (board[j, i] is King k && k.Color == color) 
                        return (j, i);
            
            // This is now a critical error. The AI cannot function without kings.
            string colorName = (color == 1) ? "White" : "Black";
            throw new InvalidOperationException($"CRITICAL ERROR: No {colorName} King found on the board. The AI cannot function without a King. Please check your InitializeBoard setup.");
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
                                var opponentKingPos = FindKing(-color);
                                if (IsSquareAttacked(opponentKingPos, color)) // 'color' is the attacking player
                                {
                                    move.IsChecking = true;
                                }
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

        private const int AI_PLAYER = 1;
        private const int HUMAN_PLAYER = -1;

        public int player = AI_PLAYER; // Player 1 is White (AI), Player -1 is Black (Human)
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

        // Call this from your Program.cs after the UI is initialized
        public void StartGame()
        {
            Console.WriteLine($"DEBUG: StartGame - Current player is {player}.");
            if (!ended && player == AI_PLAYER)
            {
                Console.WriteLine("DEBUG: StartGame - AI's turn (White). Triggering aiMove().");
                GLib.Idle.Add(() => { aiMove(); return false; });
            }
        }

        private void InitializeBoard()
        {

            // 1. Clear the board
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    board[i, j] = null;
        

            // Pawns
            for (int i = 0; i < 8; i++) 
            {
                board[i, 1] = new Pawn(-1, (i, 1)); 
                board[i, 6] = new Pawn(1, (i, 6));
            }
            
            // // Rooks
            board[0, 0] = new Rook(-1, (0, 0)); 
            board[7, 0] = new Rook(-1, (7, 0)); 
            board[0, 7] = new Rook(1, (0, 7)); 
            board[7, 7] = new Rook(1, (7, 7));
            
            // Knights
            board[1, 0] = new Knight(-1, (1, 0)); 
            board[6, 0] = new Knight(-1, (6, 0)); 
            board[1, 7] = new Knight(1, (1, 7)); 
            board[6, 7] = new Knight(1, (6, 7));
            
            // Bishops
            board[2, 0] = new Bishop(-1, (2, 0)); 
            board[5, 0] = new Bishop(-1, (5, 0)); 
            board[2, 7] = new Bishop(1, (2, 7)); 
            board[5, 7] = new Bishop(1, (5, 7));
            
            // Queens
            board[3, 0] = new Queen(-1, (3, 0)); 
            board[3, 7] = new Queen(1, (3, 7));
            
            // Kings
            board[4, 0] = new King(-1, (4, 0)); 
            board[4, 7] = new King(1, (4, 7));


            board.CalculateInitialScores();

            var blackKingPos = board.FindKing(-1);
            var whiteKingPos = board.FindKing(1);
            Console.WriteLine($"DEBUG: InitializeBoard - Black King at {blackKingPos}, White King at {whiteKingPos}");
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

            Piece p = board[move.from_x, move.from_y];
            if (p != null)
            {
                Console.WriteLine($"DEBUG: ExecuteMove - Player {player} is moving {p.Type} from ({move.from_x},{move.from_y}) to ({move.to_x},{move.to_y})");
            }

            // --- Promotion Logic ---
            Piece movingPiece = board[move.from_x, move.from_y];
            if (movingPiece is Pawn && (move.to_y == 0 || move.to_y == 7))
            {
                if (player == HUMAN_PLAYER) // Human player (Black) promotes
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

            Console.WriteLine($"White score: {board.getWhiteScore}, Black score: {board.getBlackScore}");

            CheckEndGame();
        }
        
        public void DrawInitialBoardState()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // Clear any old images (from the standard setup)
                    if (cells[i, j].Image != null)
                    {
                        cells[i, j].Remove(cells[i, j].Image);
                        cells[i, j].Image = null;
                    }

                    // If a piece exists in our logic, draw it
                    if (board[i, j] != null)
                    {
                        Piece p = board[i, j];
                        Gdk.Pixbuf originalPixbuf = new Gdk.Pixbuf(p.Address);
                        var scaledPixbuf = originalPixbuf.ScaleSimple(90, 90, Gdk.InterpType.Bilinear);
                        
                        Image pieceImage = new Image(scaledPixbuf);
                        cells[i, j].Image = pieceImage; // Store reference
                        cells[i, j].Add(pieceImage);   // Add to UI
                        cells[i, j].Image.Show();
                    }
                }
            }
        }

        private void UpdateUI(Move move)
        {
            if (cells[move.from_x, move.from_y].Image != null)
            {
                cells[move.from_x, move.from_y].Image.Pixbuf = null;
            }

            foreach (var child in cells[move.to_x, move.to_y].Children) 
                cells[move.to_x, move.to_y].Remove(child);

            // Handle en passant capture UI
            if (move.isEnPassant)
            {
                // Remove the captured pawn from its actual position
                int capturedPawnY = move.from_y; // Same row as the capturing pawn
                if (cells[move.to_x, capturedPawnY].Image != null)
                {
                    cells[move.to_x, capturedPawnY].Remove(cells[move.to_x, capturedPawnY].Image);
                    cells[move.to_x, capturedPawnY].Image.Pixbuf = null;
                }
            }
            
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
                if (cells[rook_from_x, move.from_y].Image != null)
                {
                    cells[rook_from_x, move.from_y].Remove(cells[rook_from_x, move.from_y].Image);
                    cells[rook_from_x, move.from_y].Image = null;
                }
                if (cells[rook_to_x, move.from_y].Image != null)
                {
                    cells[rook_to_x, move.from_y].Remove(cells[rook_to_x, move.from_y].Image);
                    cells[rook_to_x, move.from_y].Image = null;
                }
                Piece rook = board[rook_to_x, move.from_y];
                Gdk.Pixbuf rookPixbuf = new Gdk.Pixbuf(rook.Address).ScaleSimple(90, 90, Gdk.InterpType.Bilinear);
                Image rookImage = new Image(rookPixbuf);
                cells[rook_to_x, move.from_y].Add(rookImage);
                cells[rook_to_x, move.from_y].Image = rookImage;
                cells[rook_to_x, move.from_y].Image.Show();
            }
        }
        
        private void CheckEndGame()
        {
            player = -player; // Switch player
            Console.WriteLine($"DEBUG: CheckEndGame - Player switched. It is now Player {player}'s turn.");
            
            var legalMoves = board.GenerateLegalMoves(player);
            
            Console.WriteLine($"DEBUG: CheckEndGame - Found {legalMoves.Count} legal moves for Player {player}.");
            
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
            // This line locks all input unless it's the human's turn.
            if (paused || ended || player != HUMAN_PLAYER) return; 
            
            Console.WriteLine($"DEBUG: HandlePressChessBoard - Clicked ({i},{j}). Current player: {player}");

            // This check is still good. It ensures the human (player -1) can only select their own pieces.
            if (board[i, j] != null && board[i, j].Color == player)
            {
                RestoreBackgrounds();
                Selected = board[i, j];
                var legalMoves = board.GenerateLegalMoves(player).Where(m => m.from_x == i && m.from_y == j).ToList();
                DisplayMoves(legalMoves);

                Console.WriteLine($"DEBUG: HandlePressChessBoard - Selected own piece: {board[i, j].Type}. Found {legalMoves.Count} moves.");
            }
            else if (ModifiedBgs.Contains((i, j)))
            {
                RestoreBackgrounds();
                Move selectedMove = new Move(Selected.Pos.Item1, Selected.Pos.Item2, i, j);
                
                // Find the exact move object to get castling flag
                var legalMovesForPiece = board.GenerateLegalMoves(player).Where(m => m.from_x == Selected.Pos.Item1 && m.from_y == Selected.Pos.Item2);
                var actualMove = legalMovesForPiece.FirstOrDefault(m => m.to_x == i && m.to_y == j);
                
                if (actualMove != null) {
                    Console.WriteLine($"DEBUG: HandlePressChessBoard - Moving selected piece to ({i},{j}).");
                    ExecuteMove(actualMove);
                    
                    if (!ended && player == AI_PLAYER)
                    {
                        Console.WriteLine("DEBUG: HandlePressChessBoard - Human move complete. Triggering aiMove().");
                        GLib.Idle.Add(() => { aiMove(); return false; });
                    }
                }
            }
            else
            {
                Console.WriteLine("DEBUG: HandlePressChessBoard - Clicked empty/invalid square. Restoring backgrounds.");
                RestoreBackgrounds();
            }
        }
        
        public void HandlePressChoiseBox(int i)
        {
            paused = false;
            Piece promotedPiece;
            (int, int) pos = (last_move_for_promotion.from_x, last_move_for_promotion.from_y);

            // player is -1 (Black/Human) here, which is correct
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
             
            if (!ended && player == AI_PLAYER) 
            {
                GLib.Idle.Add(() => { aiMove(); return false; });
            }
        }
        
        private async Task aiMove()
        {
            Console.WriteLine($"DEBUG: aiMove - AI (Player {player}) is thinking...");
            
            // Get move for the current player (which is 1, the AI)
            Move bestMove = await Task.Run(() => agent.GetBestMove(board, player)); 
            if (bestMove is null)
            {
                // This case should be handled by the CheckEndGame logic, but as a fallback:
                winner = player; // The player who has no moves loses.
                WinAnimation(winner);
                return;
            }
            
            Console.WriteLine($"DEBUG: aiMove - AI has chosen move. Calling ExecuteMove.");
            ExecuteMove(bestMove);
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
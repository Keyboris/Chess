using Gtk;
using Pango;
using Pieces_namespace;
using Agent_namespace;
using System.Text;
using Controller_namespace;
//using ObserverNamespace;
using System;
using System.Transactions;
using System.Diagnostics;

//THE MAIN RULE:  I,J MEANS X,Y AND Y STARTS AT 0 AND GOES TO 7!
//ANOTHER IMPORTANT RULE: 1 MEANS WHITE, -1 MEANS BLACK, BC WHITE GO UP (0+1,..) BLACK GO DOWN (7-1,...) 


//TODO:
//minimax


/*namespace ObserverNamespace
{
    public interface IChessObserver
    {
        (int,int, int, int) Update(Board board);
    }
}*/



namespace Controller_namespace
{

    /*    public interface IChessController
        {
            bool checkCheck(Board board, ref int check);
            bool Allowed_Move(Board board, int from_x, int from_y, int to_x, int to_y);

            bool mateCheck(Board board, int player, ref int winner);

        }

        public interface IChessObserver<T>
        {
            (int, int, int, int) getMove(T value, Board board);
        }*/


    public class Move
    {
        public int from_x;
        public int from_y;
        public int to_x;
        public int to_y;

        public Move(int from_x, int from_y, int to_x, int to_y)
        {
            this.from_x = from_x;
            this.from_y = from_y;
            this.to_x = to_x;
            this.to_y = to_y;
        }
    }

    public class Board
    {
        private Piece?[,] board = new Piece?[8, 8]; //this holds the internal state of the game

        public Board()
        {

        }

        public Piece? this[int i, int j]  //this thing was j,i before, i counldnt find the mistake for 5 full days)))))))))))
        {
            set
            {
                board[i, j] = value;
            }

            get
            {
                return board[i, j];
            }
        }

        public Board Copy()
        {
            Board copy = new Board();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] != null)
                    {
                        copy[i, j] = board[i, j].Copy();
                    }
                }
            }
            return copy;
        }


        public bool CheckStalemate(int color)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] is not null)
                    {
                        if (board[i,j].Color != color)
                        {
                            if (board[i,j].Show_Possible_Moves(this).Length > 0)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public Move[] GetAllMoves(int color)
        {
            Console.WriteLine("We have started calculating the moves");
            List<Move> moves = new List<Move>();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[j, i] != null)
                    {
                        if (board[j, i].Color == color)
                        {
                            foreach (var move in board[j, i].Show_Possible_Moves(this))
                            {
                                moves.Add(new Move(j, i, move.Item1, move.Item2));
                            }
                        }
                    }
                }
            }

            Console.WriteLine("we have finished calculating the moves");
            return moves.ToArray();
        }
    }



    public class Controller //: IChessController
    {
        public Board board = new Board();
        public View_namespace.Cell[,] cells = new View_namespace.Cell[8, 8];   //Cells is an array of custon widgets which are added to the table widget, the definition of a Cell can be found in View.cs
        public List<(int, int)> ModifiedBgs = new List<(int, int)>();
        //IChessObserver obs;
        public Piece Selected;
        public int player = 1;
        private int winner = 0;
        private bool paused = false;
        public bool ended = false;
        private ((int, int), (int, int)) last_move = ((-1, -1), (-1, -1));   //ugly
        private int Check = 0; //1 for white in check, -1 for black in check
        private Agent agent = new Agent();
        //private IObserver<IChessController> observer;
        public (int, int) White_King; //important pieces, i dont want to look for them every turn
        public (int, int) Black_King;
        public List<Piece> White_Knights = new List<Piece>(); //another immportant list for flow control
        public List<Piece> Black_Knights = new List<Piece>();

        public Controller(Agent agent)
        {

            this.agent = agent;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++) 
                {
                    if (j == 1)
                    {
                        board[i, j] = new Pawn(-1, (i, j));
                    }
                    else if (j == 6)
                    {
                        board[i, j] = new Pawn(1, (i, j));
                    }
                    else if (j == 7 && i == 6)
                    {
                        board[i, j] = new Knight(1, (i, j));
                        White_Knights.Add(board[i, j]);
                    }
                    else if (j == 7 && i == 1)
                    {
                        board[i, j] = new Knight(1, (i, j)); 
                        White_Knights.Add(board[i, j]);
                    }
                    else if (j == 0 && i == 1)
                    {
                        board[i, j] = new Knight(-1, (i, j));
                        Black_Knights.Add(board[i, j]);
                    }
                    else if (j == 0 && i == 6)
                    {
                        board[i, j] = new Knight(-1, (i, j));
                        Black_Knights.Add(board[i, j]);
                    }
                    else if (j == 0 && (i == 5 || i == 2))
                    {
                        board[i, j] = new Bishop(-1, (i, j));
                    }
                    else if (j == 7 && (i == 5 || i == 2))
                    {
                        board[i, j] = new Bishop(1, (i, j));
                    }
                    else if (j == 0 && (i == 0 || i == 7))
                    {
                       board[i, j] = new Rook(-1, (i, j));
                    }
                    else if (j == 7 && (i == 0 || i == 7))
                    {
                        board[i, j] = new Rook(1, (i, j));
                    }
                    else if (j == 0 && i == 3)
                    {
                        board[i, j] = new Queen(-1, (i, j));
                    }
                    else if (j == 7 && i == 3)
                    {
                        board[i, j] = new Queen(1, (i, j));
                    }
                    else if (j == 0 && i == 4)
                    {
                        board[i, j] = new King(-1, (i, j));
                        Black_King = (i, j);
                    }
                    else if (j == 7 && i == 4)
                    {
                        board[i, j] = new King(1, (i, j));
                        White_King = (i, j);
                    }
                    else
                    {
                        board[i, j] = null;
                    }
                }
            }
        }





        public void RestoreBackgrounds()
        {
            foreach ((int, int) coord in ModifiedBgs)
            {
                cells[coord.Item1, coord.Item2].ModifyBg(StateType.Normal, cells[coord.Item1, coord.Item2].BgColor);
            }
            ModifiedBgs.Clear();
        }

        public void DisplayMoves(int i, int j)
        {
            Gdk.Color newBackgroundColor = new Gdk.Color(33, 36, 148);

            cells[i, j].ModifyBg(StateType.Normal, newBackgroundColor);   //set a default color of each cell, keep a set of changed backgrounds, when the new click is detected
        }

        public void Move(int from_x, int from_y, int to_i, int to_j, bool priority, ref bool capture, ref Piece capturedPiece, ref bool promotion)
        {
            if (winner != 0)
            {
                return;
            }



            if ((Allowed_Move(board, from_x, from_y, to_i, to_j) || priority) && !paused)
            {

                last_move = ((from_x, from_y), (to_i, to_j));

                if (board[from_x, from_y] is Pawn)
                {
                    if ((board[from_x, from_y].Color == 1 && to_j == 0) || (board[from_x, from_y].Color == -1 && to_j == 7))
                    {
                        View_namespace.Program.Show_Choices(board[from_x, from_y].Color);
                        promotion = true;
                        paused = true;
                        return;
                    }
                    else
                    {
                        promotion = false;
                    }
                }



                foreach (var child in cells[to_i, to_j].Children)
                {
                    cells[to_i, to_j].Remove(child);
                }



                Piece p = board[from_x, from_y];


                cells[from_x, from_y].Image.Pixbuf = null;
                cells[from_x, from_y].Image = null;

                
                if (board[to_i, to_j] is not null)
                {
                    capturedPiece = board[to_i, to_j].Copy();
                    capture = true;
                }
                else
                {
                    capturedPiece = null;
                    capture = false;
                }


                board[from_x, from_y].Move_to((to_i, to_j), board);



                if ((from_x, from_y) == Black_King)
                {
                    Black_King = (to_i, to_j);
                }
                else if ((from_x, from_y) == White_King)
                {
                    White_King = (to_i, to_j);
                }

                Image image = new Image();
                image.SetSizeRequest(30, 30);

                if (board[to_i, to_j] is null)
                {
                    throw new Exception("the piece where to move is null");
                }
                else if (board[to_i, to_j].Address is null)
                {
                    throw new Exception("the address is null somehow?");
                }
                Gdk.Pixbuf originalPixbuf = new Gdk.Pixbuf(board[to_i, to_j].Address);
                var scaledPixbuf = originalPixbuf.ScaleSimple(90, 90, Gdk.InterpType.Bilinear);

                image.Pixbuf = scaledPixbuf;

                cells[to_i, to_j].Image = image;
                cells[to_i, to_j].Image.Pixbuf = image.Pixbuf;

                cells[to_i, to_j].Add(cells[to_i, to_j].Image);
                cells[to_i, to_j].Image.Show();


                // Check for check immediately after the move

                player = -player;

                //agent.GetMove(board);

                //player = -player;
            }


            if (checkCheck(board, ref Check))
            {
                Console.WriteLine($"Check detected! Current check state: {Check}");

                // Check for checkmate or stalemate
                if (mateCheck(board, player, ref winner))
                {
                    Console.WriteLine($"Checkmate detected! Winner: {winner}");
                    WinAnimation(winner);
                }
                else if (board.CheckStalemate(player))
                {
                    Console.WriteLine("Stalemate detected!");
                    winner = 0; // maybe set a specific value for a stalemate 
                    DrawAnimation();
                }


            }
        }

        /*        public void Unmove((int, int) fromPos, (int,int) toPos, bool capture, Piece? capturedPiece, bool promotion)  //we may reset the to piece
                {
                    board[fromPos.Item1, fromPos.Item2] = board[toPos.Item1, toPos.Item2];
                    board[toPos.Item1, toPos.Item2] = capture ? capturedPiece : null;


                    if (promotion)
                    {
                        int temp_color = board[fromPos.Item1, fromPos.Item2].Color;
                        (int, int) temp_pos = board[fromPos.Item1, fromPos.Item2].Pos;
                        board[fromPos.Item1, fromPos.Item2] = new Pawn(temp_color, temp_pos);
                    }
                    //promotion?


                }*/

        public bool checkCheck(Board board, ref int check)
        {
            bool isCheck = false;


            if (player == 1) // Black's turn, check for white king in check
            {
                King tempKing = new King(board[White_King.Item1, White_King.Item2].Color, board[White_King.Item1, White_King.Item2].Pos);

                if (tempKing.Check(board, Black_Knights))
                {
                    check = 1;
                    Check = 1; // Update global Check variable
                    isCheck = true;
                }
                else
                {
                    Check = 0; // Reset if not in check
                }
            }
            else if (player == -1) // White's turn, check for black king in check
            {
                King? tempKing = new King(0, (-1,-1));

                for(int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (board[j,i] is King)
                        {
                            if (board[j,i].Color  == -1)
                            {
                                tempKing = (King)board[j, i];
                            }
                        }
                    }
                }

                if (tempKing!.Check(board, White_Knights))
                {
                    check = -1;
                    Check = -1;
                    isCheck = true;
                }
                else
                {
                    Check = 0; // Reset if not in check
                }
            }

            return isCheck;
        }


        public bool mateCheck(Board board, int player, ref int winner)
        {
            Board copy = board.Copy();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (copy[i, j] != null)
                    {
                        if (copy[i, j].Color == player)
                        {
                            (int, int)[] moves = copy[i, j].Show_Possible_Moves(copy);

                            foreach (var move in moves)
                            {
                                if (Allowed_Move(copy, i, j, move.Item1, move.Item2))
                                {
                                    Console.WriteLine($"the move {move} is allowed!");
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("MATEMATEMATE");
            winner = -player;
            ended = true;
            return true;

        }

        public bool Allowed_Move(Board board, int from_x, int from_y, int to_x, int to_y)
        {
            // Create a deep copy of the board
            Board copy = board.Copy();
            bool white = false;
            bool black = false;


            if (board[from_x, from_y] is King)
            {
                if (board[from_x, from_y].Color == 1)
                {
                    White_King = (to_x, to_y);
                    white = true;
                }
                else
                {
                    Black_King = (to_x, to_y);
                    black = true;
                }
            }


            copy[from_x, from_y]!.Move_to((to_x, to_y), copy);

            // Check if the move results in the current player still being in check
            int tempCheck = 0;
            bool isCheckAfterMove = checkCheck(copy, ref tempCheck);

            // If the move results in the current player still being in check, it's not allowed
            if (isCheckAfterMove && tempCheck == player)
            {
                if (white)
                {
                    White_King = (from_x, from_y);
                }
                else if (black)
                {
                    Black_King = (from_x, from_y);
                }
                return false;
            }

            // If the move is valid and doesn't leave the player in check, it's allowed
            if (white)
            {
                White_King = (from_x, from_y);
            }
            else if (black)  //ugly, but we need to make sure that everything is still the same way as it was before
            {
                Black_King = (from_x, from_y);
            }
            return true;
        }




        public void HandlePressChessBoard(int i, int j)
        {

            Console.WriteLine($"({i},{j}), {board[i,j]}");

            if (!paused) // && player == 1
            {
                if (board[i, j] is not null && board[i, j].Color == player)
                {
                    RestoreBackgrounds();
                    Selected = board[i, j]!;
                    (int, int)[] moves = board[i, j].Show_Possible_Moves(board); 
                    foreach ((int, int) cell in moves)
                    {
                        ModifiedBgs.Add(cell);
                        DisplayMoves(cell.Item1, cell.Item2);

                    }
                }
                else if (ModifiedBgs.Contains((i, j)))
                {
                    RestoreBackgrounds();

                    Piece temp = null;
                    bool temp_capture = false;
                    bool temp_promotion = false;
                    Move(Selected.Pos.Item1, Selected.Pos.Item2, i, j, false, ref temp_capture, ref temp, ref temp_promotion);
                    Move bestMove = agent.GetBestMove(board, -1);
                    if (bestMove is null)
                    {
                        winner = 1;
                        WinAnimation(winner);
                        return;
                    }
                    bool t = false;
                    Console.WriteLine($"{(bestMove.from_x, bestMove.from_y)} -> {(bestMove.to_x, bestMove.to_y)}");
                    Piece? s = null;
                    bool p = false;
                    Move(bestMove.from_x, bestMove.from_y, bestMove.to_x, bestMove.to_y, false, ref t, ref s, ref p);
                    Console.WriteLine("Computer has made a move");
                }
                else
                {
                    RestoreBackgrounds();
                }
            }
            else
            {
                Console.WriteLine("PAUSED");
            }
        }



        public void HandlePressChoiseBox(int i)
        {
            Piece[] pieces = { new Knight(player, (last_move.Item1.Item1, last_move.Item1.Item2)), new Bishop(player, (last_move.Item1.Item1, last_move.Item1.Item2)), new Rook(player, (last_move.Item1.Item1, last_move.Item1.Item2)), new Queen(player, (last_move.Item1.Item1, last_move.Item1.Item2)) };

            board[Selected.Pos.Item1, Selected.Pos.Item2] = pieces[i];

            if (pieces[i] is Knight)
            {
                if (player == 1)
                {
                    White_Knights.Add(pieces[i]);
                }
                else
                {
                    Black_Knights.Add(pieces[i]);
                }
            }

            paused = false;

            Piece temp = null;
            bool temp_capture = false;
            bool temp_promotion = false;
            Move(last_move.Item1.Item1, last_move.Item1.Item2, last_move.Item2.Item1, last_move.Item2.Item2, true, ref temp_capture, ref temp, ref temp_promotion);


        }

        public void WinAnimation(int player)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++) 
                {
                    if (board[i,j] is not null && board[i,j].Color == player)
                    {
                        cells[i, j].ModifyBg(StateType.Normal, new Gdk.Color(33, 36, 148));
                    }
                }
            }
        }

        public void DrawAnimation()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] is not null)
                    {
                        cells[i, j].ModifyBg(StateType.Normal, new Gdk.Color(33, 36, 148));
                    }
                }
            }
        }

    }
}
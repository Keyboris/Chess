using GLib;
using Gtk;
using Pieces_namespace;
using System;

namespace View_namespace
{

    public class Cell : EventBox
    {
        private bool isWhite;
        private Gdk.Color backgroundColor;
        public Gtk.Image? Image;
        private int x;
        private int y;
        Controller_namespace.Controller ctr;

        public Cell(bool isWhite, int x, int y, Controller_namespace.Controller ctr)
        {
            this.isWhite = isWhite;
            this.x = x;
            this.y = y;
            this.ctr = ctr;

            if (ctr.board[x, y] is not null)
            {
                Image image = new Image();
                image.SetSizeRequest(30, 30);

                Gdk.Pixbuf originalPixbuf = new Gdk.Pixbuf(ctr.board[x, y].Address);
                var scaledPixbuf = originalPixbuf.ScaleSimple(90, 90, Gdk.InterpType.Bilinear);

                image.Pixbuf = scaledPixbuf;
                this.Image = image;

                Add(Image);
            }

            backgroundColor = isWhite ? new Gdk.Color(235, 225, 197) : new Gdk.Color(41, 94, 29);

            ModifyBg(StateType.Normal, backgroundColor);

        }

        public bool IsWhite => isWhite;
        public int X => x;
        public int Y => y;

        public Gdk.Color BgColor => backgroundColor;

        public void PressedEvent(object sender, EventArgs e)
        {
            ctr.HandlePressChessBoard(X, Y);
        }
    }

    public class MainWindow : Window
    {

        public Overlay overlay = new Overlay();

        public Controller_namespace.Controller ctr;
        public Agent_namespace.Agent agent;
        public MainWindow() : base("Chess Board")
        {
            Agent_namespace.Agent agent = new Agent_namespace.Agent();
            Controller_namespace.Controller ctr = new Controller_namespace.Controller(agent);
            this.ctr = ctr;
            agent.setController(ctr);   //an easier and faster alternative to observer methods

            

            SetDefaultSize(700, 700);
            Resizable = false;
            SetPosition(WindowPosition.Center);

            DeleteEvent += OnWindowDeleteEvent;

            Table table = new Table(8, 8, true);
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    bool isWhite = (row + col) % 2 == 0;
                    Cell cell = new Cell(isWhite, row, col, ctr);
                    cell.ButtonPressEvent += cell.PressedEvent;
                    table.Attach(cell, (uint)row, (uint)row + 1, (uint)col, (uint)col + 1);
                    ctr.cells[row, col] = cell;   //i need to do it bc table does not have any sort of indexer
                }
            }

            overlay.Add(table);
            Add(overlay);

        }
        
            private void OnWindowDeleteEvent(object sender, DeleteEventArgs a)
            {
                Gtk.Application.Quit();
                a.RetVal = true;
            }



    }



    class Program
    {
        public static MainWindow Win { get; private set; }
        private static Controller_namespace.Controller ctr;

        static void Main(string[] args)
        {
            Gtk.Application.Init();

            Win = new MainWindow();
            ctr = Win.ctr;
            Win.ShowAll();

            // --- FIX: ADD THIS LINE ---
            // This forces the UI to draw the custom board from your Controller,
            // fixing both the visual bug and the NullReferenceException.
            ctr.DrawInitialBoardState(); 
            // --------------------------

            // --- THIS IS THE FIX ---
            Console.WriteLine("DEBUG: Main - Calling ctr.StartGame() to begin.");
            ctr.StartGame();
            // ---------------------

            Gtk.Application.Run();
        }

        public static void Show_Choices(int color)
        {
            Table table = new Table(1, 3, true);
            Piece[] pieces = { new Knight(color, (-1, -1)), new Bishop(color, (-1, -1)), new Rook(color, (-1, -1)), new Queen(color, (-1, -1)) };

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                EventBox box = new EventBox();
                box.ModifyBg(StateType.Normal, new Gdk.Color(200, 200, 200));
                box.ButtonPressEvent += (sender, args) => Piece_Chosen(sender, args, index);
                Image image = new Image(pieces[i].Address);
                box.Add(image);
                table.Attach(box, (uint)i, (uint)i + 1, 0, 1);
            }


            EventBox eventBox = new EventBox();
            eventBox.Add(table);


            eventBox.Halign = Align.Center;
            eventBox.Valign = Align.Center;


            Win.overlay.AddOverlay(eventBox);
            eventBox.ShowAll();
        }

        static void Piece_Chosen(object sender, ButtonPressEventArgs args, int i)
        {

            ctr.HandlePressChoiseBox(i);
            foreach (var child in Win.overlay.Children)
            {
                if (child is Gtk.EventBox)
                {
                    Win.overlay.Remove(child);
                }
            }
        }

    }


}
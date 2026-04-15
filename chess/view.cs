using Gtk;
using Gdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Chess;

namespace View_namespace
{
    // Controller talks only through this interface
    public interface IChessView
    {
        void UpdateBoard(Board board);
        void ShowPromotionChoice(int side);
        void ShowResult(int result); // 0=white wins, 1=black wins, 2=draw
    }

    // ---- Cell widget ----
    public class Cell : EventBox
    {
        public readonly int Sq; // board square index
        public readonly Gdk.Color BaseColor;
        public Gtk.Image? PieceImage;

        private readonly Action<int> _onClick;

        public Cell(int sq, bool isLight, Action<int> onClick)
        {
            Sq        = sq;
            BaseColor = isLight ? new Gdk.Color(235, 225, 197) : new Gdk.Color(41, 94, 29);
            _onClick  = onClick;
            ModifyBg(StateType.Normal, BaseColor);
            ButtonPressEvent += (_, __) => _onClick(Sq);
        }

        public void SetPiece(string? imagePath)
        {
            if (PieceImage != null) { Remove(PieceImage); PieceImage = null; }
            if (imagePath == null) return;
            var pb = new Gdk.Pixbuf(imagePath).ScaleSimple(90, 90, Gdk.InterpType.Bilinear);
            PieceImage = new Gtk.Image(pb);
            Add(PieceImage);
            PieceImage.Show();
        }

        public void Highlight(Gdk.Color color) => ModifyBg(StateType.Normal, color);
        public void ResetColor()               => ModifyBg(StateType.Normal, BaseColor);
    }

    // ---- Main window ----
    public class MainWindow : Gtk.Window, IChessView
    {
        private readonly Cell[] _cells = new Cell[64];
        private readonly Controller_namespace.Controller _ctr;
        private readonly List<int> _highlighted = new();
        private int _selectedSq = -1;

        private static readonly Gdk.Color MoveHint  = new Gdk.Color(33, 36, 148);
        private static readonly Gdk.Color WinColor  = new Gdk.Color(33, 36, 148);

        public MainWindow() : base("Chess")
        {
            var agent = new Agent_namespace.Agent();
            _ctr = new Controller_namespace.Controller(agent);
            _ctr.View = this;

            SetDefaultSize(720, 720);
            Resizable = false;
            SetPosition(WindowPosition.Center);
            DeleteEvent += (_, a) => { Gtk.Application.Quit(); a.RetVal = true; };

            var table = new Table(8, 8, true);
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int sq      = rank * 8 + file;
                    bool isLight = (rank + file) % 2 == 0;
                    var cell    = new Cell(sq, isLight, OnCellClicked);
                    _cells[sq]  = cell;
                    // GTK table: col=file, row=rank
                    table.Attach(cell, (uint)file, (uint)file + 1, (uint)rank, (uint)rank + 1);
                }
            }

            Add(table);
        }

        public void Start()
        {
            ShowAll();
            UpdateBoard(_ctr.Board);
            _ctr.StartGame();
        }

        // ---- IChessView ----
        public void UpdateBoard(Board board)
        {
            ClearHighlights();
            for (int sq = 0; sq < 64; sq++)
            {
                int piece = board.PieceOn(sq);
                _cells[sq].SetPiece(piece == Piece.None ? null : ImagePath(piece));
            }
        }

        public void ShowPromotionChoice(int side)
        {
            // Build a simple overlay with 4 piece choices
            int[] types = { Piece.TypeOf(Piece.WhiteQueen), Piece.TypeOf(Piece.WhiteRook),
                            Piece.TypeOf(Piece.WhiteBishop), Piece.TypeOf(Piece.WhiteKnight) };
            var box = new HBox(true, 4);
            foreach (int t in types)
            {
                int captured = t;
                int pieceIdx = Piece.Make(side, captured);
                var eb = new EventBox();
                eb.ModifyBg(StateType.Normal, new Gdk.Color(200, 200, 200));
                eb.Add(new Gtk.Image(ImagePath(pieceIdx)));
                eb.ButtonPressEvent += (_, __) =>
                {
                    // Remove overlay
                    var parent = eb.Parent?.Parent as Gtk.Window;
                    _ctr.HandlePromotionChoice(captured);
                };
                box.PackStart(eb, true, true, 0);
            }

            var dialog = new Gtk.Window("Promote pawn");
            dialog.Add(box);
            dialog.SetDefaultSize(360, 90);
            dialog.SetPosition(WindowPosition.Center);
            dialog.ShowAll();
        }

        public void ShowResult(int result)
        {
            string msg = result == 2 ? "Draw (stalemate)!"
                       : result == 0 ? "White wins!"
                       : "Black wins!";
            var dlg = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, msg);
            dlg.Run();
            dlg.Destroy();
        }

        // ---- Input handling ----
        private void OnCellClicked(int sq)
        {
            if (_ctr.Ended || _ctr.Board.SideToMove != 1) return; // only human (black) clicks

            if (_selectedSq >= 0 && _highlighted.Contains(sq))
            {
                // Execute the move
                var moves = _ctr.GetLegalMovesFrom(_selectedSq);
                var move  = moves.FirstOrDefault(m => m.To == sq);
                ClearHighlights();
                _selectedSq = -1;
                _ctr.ExecuteMove(move);
                return;
            }

            ClearHighlights();
            _selectedSq = -1;

            // Select own piece
            if (_ctr.Board.ColorOn(sq) == 1) // black piece
            {
                _selectedSq = sq;
                var legalTargets = _ctr.GetLegalMovesFrom(sq);
                foreach (var m in legalTargets)
                {
                    _cells[m.To].Highlight(MoveHint);
                    _highlighted.Add(m.To);
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (int sq in _highlighted) _cells[sq].ResetColor();
            _highlighted.Clear();
        }

        // ---- Image resolution ----
        private static string? _piecesDir;

        private static string ImagePath(int piece)
        {
            if (_piecesDir == null)
            {
                string exe = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                string dir = exe;
                while (!System.IO.Directory.GetFiles(dir, "*.csproj").Any())
                {
                    string? parent = System.IO.Directory.GetParent(dir)?.FullName;
                    if (parent == null || parent == dir) break;
                    dir = parent;
                }
                _piecesDir = System.IO.Path.Combine(dir, "pieces");
            }

            string color = piece < 6 ? "white" : "black";
            string name  = (piece % 6) switch
            {
                0 => "pawn", 1 => "knight", 2 => "bishop",
                3 => "rook", 4 => "queen",  5 => "king", _ => "pawn"
            };
            return System.IO.Path.Combine(_piecesDir, $"{color}_{name}.svg.png");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Gtk.Application.Init();
            var win = new MainWindow();
            win.Start();
            Gtk.Application.Run();
        }
    }
}

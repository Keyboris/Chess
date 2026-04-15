using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chess;

namespace Controller_namespace
{
    public class Controller
    {
        public Board Board = new Board();

        private const int White = 0;
        private const int Human = 1; // Black = human

        public bool Ended   { get; private set; } = false;
        public int  Winner  { get; private set; } = -1; // -1=none, 0=white, 1=black, 2=draw

        private bool _paused = false;
        private Move _pendingPromoMove;

        private Agent_namespace.Agent _agent;
        public View_namespace.IChessView? View { get; set; }

        public Controller(Agent_namespace.Agent agent)
        {
            _agent = agent;
            Board.Reset();
        }

        public void StartGame()
        {
            if (!Ended && Board.SideToMove == White)
                GLib.Idle.Add(() => { _ = TriggerAiMove(); return false; });
        }

        // Called by the view when the human clicks a square
        public List<Move> GetLegalMovesFrom(int sq)
        {
            if (_paused || Ended || Board.SideToMove != Human) return new List<Move>();
            return MoveGen.GenerateLegalMoves(Board)
                          .Where(m => m.From == sq).ToList();
        }

        public void ExecuteMove(Move move)
        {
            if (Ended || _paused) return;

            // Human promotion: pause and ask view for piece choice
            if (!move.IsPromotion && Board.SideToMove == Human)
            {
                int movingPiece = Board.PieceOn(move.From);
                bool isPawn = Piece.TypeOf(movingPiece) == Piece.TypeOf(Piece.WhitePawn);
                int toRank  = move.To / 8;
                if (isPawn && toRank == 7)
                {
                    _paused = true;
                    _pendingPromoMove = move;
                    View?.ShowPromotionChoice(Human);
                    return;
                }
            }

            MoveGen.MakeMove(Board, move);
            View?.UpdateBoard(Board);
            CheckEndGame();
        }

        // Called by view after human picks a promotion piece
        public void HandlePromotionChoice(int pieceType)
        {
            _paused = false;
            int promoPiece = Piece.Make(Human, pieceType);
            var promoMove  = new Move(
                _pendingPromoMove.From,
                _pendingPromoMove.To,
                MoveFlags.Promotion | (_pendingPromoMove.IsCapture ? MoveFlags.Capture : MoveFlags.Quiet),
                promoPiece);

            MoveGen.MakeMove(Board, promoMove);
            View?.UpdateBoard(Board);
            CheckEndGame();

            if (!Ended && Board.SideToMove == White)
                GLib.Idle.Add(() => { _ = TriggerAiMove(); return false; });
        }

        private void CheckEndGame()
        {
            var legal = MoveGen.GenerateLegalMoves(Board);
            if (legal.Count == 0)
            {
                Ended = true;
                if (MoveGen.IsInCheck(Board, Board.SideToMove))
                {
                    Winner = 1 - Board.SideToMove; // opponent wins
                    View?.ShowResult(Winner);
                }
                else
                {
                    Winner = 2; // stalemate / draw
                    View?.ShowResult(Winner);
                }
                return;
            }

            if (!Ended && Board.SideToMove == White)
                GLib.Idle.Add(() => { _ = TriggerAiMove(); return false; });
        }

        private async Task TriggerAiMove()
        {
            Move best = await Task.Run(() => _agent.GetBestMove(Board));
            ExecuteMove(best);
        }
    }
}

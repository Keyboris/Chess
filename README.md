# Chess Game with AI

A fully-featured chess game implemented in C# with GTK# for the graphical interface. Play against an intelligent AI opponent that uses minimax search with alpha-beta pruning and quiescence search.

## Features

### Complete Chess Rules Implementation
- **All standard chess moves**: Regular piece movement, captures, castling (kingside and queenside), en passant, and pawn promotion
- **Legal move validation**: Only legal moves are allowed; the game prevents moves that would leave the king in check
- **Check and checkmate detection**: Automatic game-ending detection for checkmate and stalemate
- **Move history tracking**: Full move/unmove system for AI search

### AI Opponent
- **Minimax algorithm** with alpha-beta pruning for efficient search
- **Quiescence search** to handle tactical positions more accurately
- **Move ordering** using MVV-LVA (Most Valuable Victim - Least Valuable Aggressor) heuristic
- **Position evaluation** with piece-square tables for both midgame and endgame
- **Configurable search depth** (default: 4 ply)
- **Material and positional evaluation** to guide the AI's decision-making

### User Interface
- **Visual chess board** with alternating light and dark squares
- **Piece graphics** with proper scaling
- **Move highlighting** to show available moves for the selected piece
- **Win/draw animations** to celebrate the game outcome
- **Promotion dialog** for choosing a piece when a pawn reaches the back rank

## Requirements

- .NET 7.0 or higher
- GTK# 3.24.24.95
- Linux, macOS, or Windows with GTK+ installed

## Installation

1. Clone this repository:
```bash
git clone https://github.com/yourusername/chess-game.git
cd chess-game
```

2. Install GTK# dependencies (if not already installed):
   - **Linux**: `sudo apt-get install gtk-sharp3`
   - **macOS**: `brew install gtk+3`
   - **Windows**: Download GTK# from [gtk.org](https://www.gtk.org/docs/installations/windows/)

3. Build the project:
```bash
dotnet build
```

4. Run the game:
```bash
dotnet run
```

## How to Play

1. **White (AI) moves first** - The AI will automatically make its first move
2. **Click on a piece** to select it and see available moves (highlighted in blue)
3. **Click on a highlighted square** to move your piece there
4. **Pawn promotion**: When your pawn reaches the opposite end, choose which piece to promote to
5. **The game ends** when checkmate or stalemate occurs

## Project Structure

```
chess/
├── Controller.cs    # Game logic, board state, and move execution
├── Pieces.cs        # Piece classes (Pawn, Knight, Bishop, Rook, Queen, King)
├── agent.cs         # AI implementation with minimax and evaluation
├── view.cs          # GTK# UI components and event handling
├── chess.csproj     # Project configuration
└── pieces/          # PNG images for chess pieces
    ├── white_*.png
    └── black_*.png
```

## Technical Details

### AI Search
- **Minimax depth**: 4 plies (configurable via `MaxDepth` constant)
- **Alpha-beta pruning**: Reduces search space significantly
- **Quiescence search**: Extends search for captures and promotions
- **Evaluation function**: Combines material value with position-specific bonuses

### Chess Rules Implementation
- **Castling**: Checks for king/rook movement, empty squares, and squares under attack
- **En passant**: Tracks the last pawn double-move for capture opportunities
- **Move generation**: Pseudo-legal moves are generated, then filtered for legality
- **Check detection**: Uses square attack detection to validate king safety

## Configuration

You can adjust the AI difficulty by changing the `MaxDepth` constant in `agent.cs`:

```csharp
private const int MaxDepth = 4; // Increase for stronger (but slower) AI
```

## Future Enhancements

- [ ] Switch to bitboards model
- [ ] Add time controls and chess clock
- [ ] Implement transposition tables for faster search
- [ ] Add game save/load functionality
- [ ] Support for player vs. player mode
- [ ] Move history display with notation
- [ ] Opening book integration
- [ ] Difficulty levels

## License

This project is open source and available under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- Piece images from standard chess sets
- Chess programming concepts from the Chess Programming Wiki
- GTK# for the cross-platform GUI framework

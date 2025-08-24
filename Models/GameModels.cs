namespace WordScapeBlazorWasm.Models
{
    public class GameSettings
    {
        public int MinWordLength { get; set; } = 3;
        public int MaxWordLength { get; set; } = 7;
    }

    public class PuzzleState
    {
        public string TargetWord { get; set; } = "";
        public List<string> PossibleWords { get; set; } = new();
        public HashSet<string> FoundWords { get; set; } = new();
        public List<char> CircleLetters { get; set; } = new();
        public string CurrentGuess { get; set; } = "";
        public bool IsComplete => FoundWords.Count == PossibleWords.Count;
        public int Score => FoundWords.Sum(word => word.Length * 10);
        
        // Grid properties
        public CrosswordGrid Grid { get; set; } = new();
    }

    public class CircleLetter
    {
        public char Letter { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsSelected { get; set; }
        public int Index { get; set; }
    }

    public class CrosswordGrid
    {
        public const char Blank = '_';
        public int MaxX { get; set; }
        public int MaxY { get; set; }
        public char[,] Letters { get; set; } = new char[0,0];
        public Dictionary<string, WordPlacement> PlacedWords { get; set; } = new();
        public List<GridCell> Cells { get; set; } = new();
    }

    public class WordPlacement
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public bool IsHorizontal { get; set; }
        public string Word { get; set; } = "";
    }

    public class GridCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Letter { get; set; }
        public bool IsBlank => Letter == CrosswordGrid.Blank;
        public bool IsRevealed { get; set; }
    }

    public enum WordStatus
    {
        IsAlreadyInGrid,
        IsShownInGridForFirstTime,
        IsNotInGrid
    }
}

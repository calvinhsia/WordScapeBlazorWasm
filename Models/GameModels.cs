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
        
        // Grid properties - using the original GenGrid system
        public GenGrid? Grid { get; set; }
        
        // Cached legacy grid to maintain state
        private CrosswordGrid? _cachedLegacyGrid;
        
        // Compatibility properties for existing code
        public CrosswordGrid LegacyGrid 
        {
            get
            {
                if (_cachedLegacyGrid == null)
                {
                    _cachedLegacyGrid = ConvertToLegacyGrid();
                }
                return _cachedLegacyGrid;
            }
        }
        
        private CrosswordGrid ConvertToLegacyGrid()
        {
            if (Grid == null)
                return new CrosswordGrid { MaxX = 15, MaxY = 15, Cells = new List<GridCell>() };
            if (Grid == null) 
            {
                return new CrosswordGrid();
            }
            
            var legacyGrid = new CrosswordGrid
            {
                MaxX = Grid._MaxX,
                MaxY = Grid._MaxY,
                Letters = Grid._chars,
                PlacedWords = new Dictionary<string, WordPlacement>()
            };
            
            // Convert placed words
            foreach (var kvp in Grid._dictPlacedWords)
            {
                var word = kvp.Key;
                var ltrPlaced = kvp.Value;
                legacyGrid.PlacedWords[word] = new WordPlacement
                {
                    StartX = ltrPlaced.nX,
                    StartY = ltrPlaced.nY,
                    IsHorizontal = ltrPlaced.IsHoriz,
                    Word = word
                };
            }
            
            // Convert cells
            legacyGrid.Cells = new List<GridCell>();
            for (int y = 0; y < legacyGrid.MaxY; y++)
            {
                for (int x = 0; x < legacyGrid.MaxX; x++)
                {
                    var cell = new GridCell
                    {
                        X = x,
                        Y = y,
                        Letter = legacyGrid.Letters[x, y],
                        IsRevealed = false
                    };
                    legacyGrid.Cells.Add(cell);
                }
            }
            
            return legacyGrid;
        }
    }

    public class CircleLetter
    {
        public char Letter { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsSelected { get; set; }
        public int Index { get; set; }
    }

    // Legacy grid classes for backward compatibility
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
        public bool IsBlank => Letter == CrosswordGrid.Blank || Letter == '_';
        public bool IsRevealed { get; set; }
    }

    public enum WordStatus
    {
        IsAlreadyInGrid,
        IsShownInGridForFirstTime,
        IsNotInGrid
    }
}

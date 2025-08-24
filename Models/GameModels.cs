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
    }

    public class CircleLetter
    {
        public char Letter { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsSelected { get; set; }
        public int Index { get; set; }
    }
}

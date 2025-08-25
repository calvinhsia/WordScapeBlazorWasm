using DictionaryLib;
using System.Diagnostics;
using System.Linq;

namespace WordScapeBlazorWasm.Models
{
    public class WordGenerationParms
    {
        private Random? _random;
        public Random _Random { get { if (_random is null) { _random = new Random(1); } return _random; } set { _random = value; } }
        public int LenTargetWord { get; set; } = 7;
        public int MinSubWordLength { get; set; } = 5;
        public int MaxX { get; set; } = 15;
        public int MaxY { get; set; } = 15;
        public int MaxSubWords { get; set; } = 1500;
        public override string ToString() => $"LenTargetWord = {LenTargetWord}  MinSubWordLength={MinSubWordLength}  {MaxX},{MaxY}";
    }

    public class WordScapePuzzle
    {
        public int LenTargetWord = 7; // at the time of generation. User could have changed it, invalidating this one
        public int MinSubWordLength = 5; // at the time of generation. User could have changed it, invalidating this one
        public WordGenerator? wordGenerator;
        public WordContainer? wordContainer;
        public GenGrid? genGrid;

        public static Task<WordScapePuzzle> CreateNextPuzzleTask(WordGenerationParms wordGenerationParms)
        {
            return Task.Run(() =>
            {
                WordScapePuzzle puzzleNext = new WordScapePuzzle()
                {
                    LenTargetWord = wordGenerationParms.LenTargetWord,
                    MinSubWordLength = wordGenerationParms.MinSubWordLength
                };
                try
                {
                    puzzleNext.wordGenerator = new WordGenerator(wordGenerationParms);
                    puzzleNext.wordContainer = puzzleNext.wordGenerator.GenerateWord();
                    puzzleNext.genGrid = new GenGrid(wordGenerationParms.MaxX, wordGenerationParms.MaxY, puzzleNext.wordContainer, wordGenerationParms._Random);
                    puzzleNext.genGrid.Generate();
                }
                catch (Exception)
                {
                    // old version of dict threw nullref sometimes at end of alphabet
                }
                Debug.WriteLine($"");
                return puzzleNext;
            });
        }
    }

    public class WordContainer
    {
        public string InitialWord { get; set; } = string.Empty;
        public List<string> subwords = new List<string>();
        public int cntLookups;
        public override string ToString()
        {
            return $"{InitialWord} #Subw={subwords.Count}";
        }
    }

    public class WordGenerator
    {
        public readonly DictionaryLib.DictionaryLib _dictionaryLibSmall;
        public readonly DictionaryLib.DictionaryLib _dictionaryLibLarge;
        public int _MinSubWordLen => _wordGenerationParms.MinSubWordLength;
        public int _TargetLen => _wordGenerationParms.LenTargetWord;
        private readonly WordGenerationParms _wordGenerationParms;
        private int _numMaxSubWords => _wordGenerationParms.MaxSubWords;

        public WordGenerator(WordGenerationParms wordGenerationParms)
        {
            _wordGenerationParms = wordGenerationParms;
            _dictionaryLibSmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small, _wordGenerationParms._Random);
            _dictionaryLibLarge = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Large, _wordGenerationParms._Random);
        }

        // avoid having
        public bool IsWordInLargeDictionary(string word) // forwarder so xamarin doesn't need ref to dict
        {
            return _dictionaryLibLarge.IsWord(word);
        }

        public WordContainer GenerateWord()
        {
            var word = string.Empty;
            while (word.Length != _TargetLen)
            {
                word = _dictionaryLibSmall.RandomWord();
            }
            var wc = new WordContainer()
            {
                InitialWord = word.ToUpper()
            };
            var subwrds = _dictionaryLibSmall.GenerateSubWords(word, out wc.cntLookups, MinLength: _MinSubWordLen, MaxSubWords: _numMaxSubWords);
            wc.subwords = subwrds.OrderByDescending(w => w.Length).Select(p => p.ToUpper()).ToList();
            return wc;
        }

        public static bool IgnorePluralGerundPastTenseWords<T>(string subword, Dictionary<string, T> _dictWords)
        {
            if (!subword.EndsWith("S"))
            {
                if (_dictWords.ContainsKey(subword + "S"))
                {
                    return true;
                }
            }
            if (!subword.EndsWith("D")) // past tense: if "removed", don't add "remove"
            {
                if (_dictWords.ContainsKey(subword + "D"))
                {
                    return true;
                }
            }
            else
            {
                if (_dictWords.ContainsKey(subword.Substring(0, subword.Length - 2) + "R"))
                {
                    return true;
                }
            }
            if (subword.EndsWith("R")) // "remover", "removed": allow only one
            {
                if (_dictWords.ContainsKey(subword.Substring(0, subword.Length - 2) + "D"))
                {
                    return true;
                }
            }
            if (_dictWords.ContainsKey(subword + "LY")) // discretely: dont put discrete
            {
                return true;
            }
            if (_dictWords.ContainsKey(subword + "ED")) // disobeyed : don't put disobey
            {
                return true;
            }
            if (_dictWords.ContainsKey(subword + "ER")) // disobeyer : don't put disobey
            {
                return true;
            }
            if (_dictWords.ContainsKey(subword + "ING")) // disobeying : don't put disobey
            {
                return true;
            }
            return false;
        }
    }

    public class LtrPlaced
    {
        public int nX;
        public int nY;
        public char ltr;
        public bool IsHoriz; // orientation of the 1st word placed in this square
        public override string ToString()
        {
            return $"({nX,2},{nY,2}) {ltr} IsHorz={IsHoriz}";
        }
    }

    /// <summary>
    /// Given a set or words, places them in an array of char
    /// </summary>
    public class GenGrid
    {
        public const char Blank = '_';
        readonly WordContainer _wordContainer;
        readonly public Random _random;
        /// <summary>
        /// initially, the max size of desired grid. Once grid filled in and recentered, 
        /// these are recalculated to be potentially smaller
        /// </summary>
        public int _MaxY;
        public int _MaxX;
        public char[,] _chars;
        public int NumWordsPlaced => _dictPlacedWords.Count;
        public Dictionary<string, LtrPlaced> _dictPlacedWords = new Dictionary<string, LtrPlaced>(); // subword to 1st letter
        public int nLtrsPlaced;
        public readonly List<LtrPlaced> _ltrsPlaced = new List<LtrPlaced>();

        // Optimization: Cache character positions for faster lookup
        private readonly Dictionary<char, List<LtrPlaced>> _charToPositions = new Dictionary<char, List<LtrPlaced>>();
        
        // Optimization: Pre-sorted words by length (longer first for better placement)
        private List<string> _sortedWords = new List<string>();
        
        // Optimization: Cache for intersection validation
        private readonly HashSet<string> _processedWordPairs = new HashSet<string>();

        internal int _tmpminX;
        internal int _tmpmaxX;
        internal int _tmpminY;
        internal int _tmpmaxY;

        public GenGrid(int maxX, int maxY, WordContainer wordCont, Random rand)
        {
            this._random = rand;
            this._wordContainer = wordCont;
            this._MaxY = maxY;
            this._MaxX = maxX;
            _tmpminX = maxX;
            _tmpminY = maxY;
            _tmpmaxX = 0;
            _tmpmaxY = 0;
            _chars = new char[_MaxX, _MaxY];
            
            // Optimization: Initialize grid in single loop
            for (int y = 0; y < _MaxY; y++)
            {
                for (int x = 0; x < _MaxX; x++)
                {
                    _chars[x, y] = Blank;
                }
            }
            
            // Optimization: Pre-sort words by length (longer first) and shuffle within same length
            _sortedWords = _wordContainer.subwords
                .GroupBy(w => w.Length)
                .OrderByDescending(g => g.Key)
                .SelectMany(g => g.OrderBy(w => rand.Next()))
                .ToList();
        }

        public void Generate()
        {
            PlaceWords();
            ResizeGridArraySmaller();
        }

        internal void ResizeGridArraySmaller()
        {
            if (NumWordsPlaced > 1)
            {
                // Optimization: Calculate new dimensions first
                var newMaxX = _tmpmaxX - _tmpminX + 1;
                var newMaxY = _tmpmaxY - _tmpminY + 1;
                
                // Optimization: Update all letter positions in batch
                foreach (var ltr in _ltrsPlaced)
                {
                    ltr.nX -= _tmpminX;
                    ltr.nY -= _tmpminY;
                }
                
                // Optimization: Use Array.Copy for better performance if possible
                char[,] newCharArr = new char[newMaxX, newMaxY];
                for (int y = 0; y < newMaxY; y++)
                {
                    for (int x = 0; x < newMaxX; x++)
                    {
                        newCharArr[x, y] = _chars[x + _tmpminX, y + _tmpminY];
                    }
                }
                
                _chars = newCharArr;
                _MaxX = newMaxX;
                _MaxY = newMaxY;
            }
        }

        internal void PlaceWords()
        {
            // Optimization: Use pre-sorted words instead of original order
            foreach (var subword in _sortedWords)
            {
                if (NumWordsPlaced == 0)
                {
                    int x, y, incY = 0, incX = 0;
                    if (_random.NextDouble() < .5) // horiz. Try to make 1st word centrally located
                    {
                        y = _MaxY / 4 + _random.Next(_MaxY / 2);
                        x = _random.Next(_MaxX - subword.Length);
                        incX = 1;
                    }
                    else
                    { // up/down
                        x = _MaxX / 4 + _random.Next(_MaxX / 2);
                        y = _random.Next(_MaxY - subword.Length);
                        incY = 1;
                    }
                    PlaceOneWord(subword, x, y, incX, incY);
                }
                else
                {// not 1st word: find random common letter and see if it can be placed. don't do singular if plural already placed
                    if (WordGenerator.IgnorePluralGerundPastTenseWords(subword, _dictPlacedWords))
                    {
                        continue;
                    }
                    
                    // Optimization: Try placement using character-position cache instead of shuffling all letters
                    if (TryPlaceWordOptimized(subword))
                    {
                        // Successfully placed
                    }
                }
                
                // Optimization: Remove arbitrary limit or make it configurable
                if (NumWordsPlaced >= 12) // Increased from 6 for better puzzles
                {
                    break;
                }
            }
        }

        private void PlaceOneWord(string subword, int x, int y, int incX, int incY)
        {
            var isFirstLetter = true;
            if (x < _tmpminX)
            {
                _tmpminX = x;
            }
            if (y < _tmpminY)
            {
                _tmpminY = y;
            }
            foreach (var ltr in subword)
            {
                var ltrPlaced = new LtrPlaced() { nX = x, nY = y, ltr = ltr, IsHoriz = incX > 0 };
                if (isFirstLetter)
                {
                    _dictPlacedWords[subword] = ltrPlaced;
                    isFirstLetter = false;
                }
                _ltrsPlaced.Add(ltrPlaced);
                
                // Optimization: Update character-position cache
                if (!_charToPositions.ContainsKey(ltr))
                {
                    _charToPositions[ltr] = new List<LtrPlaced>();
                }
                _charToPositions[ltr].Add(ltrPlaced);
                
                _chars[x, y] = ltr;
                x += incX;
                y += incY;
                nLtrsPlaced++;
            }
            if (x > _tmpmaxX)
            {
                _tmpmaxX = x - incX;
            }
            if (y > _tmpmaxY)
            {
                _tmpmaxY = y - incY;
            }
        }

        // Optimization: Improved placement using smart character selection
        private bool TryPlaceWordOptimized(string subword)
        {
            // Get unique characters in the word, prioritizing less common ones
            var uniqueChars = subword.Distinct()
                .OrderBy(c => _charToPositions.ContainsKey(c) ? _charToPositions[c].Count : 0)
                .ToList();
            
            foreach (var targetChar in uniqueChars)
            {
                if (_charToPositions.ContainsKey(targetChar))
                {
                    // Use a more intelligent ordering - try positions that are more isolated first
                    var positions = _charToPositions[targetChar]
                        .OrderBy(pos => CountAdjacentLetters(pos))
                        .ThenBy(x => _random.Next())
                        .ToList();
                    
                    foreach (var ltrPlaced in positions)
                    {
                        // Quick check to avoid duplicate processing
                        var pairKey = $"{subword}-{ltrPlaced.nX}-{ltrPlaced.nY}";
                        if (_processedWordPairs.Contains(pairKey))
                            continue;
                        
                        _processedWordPairs.Add(pairKey);
                        
                        if (TryPlaceWord(subword, ltrPlaced))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        // Optimization: Helper method to count adjacent letters for better placement
        private int CountAdjacentLetters(LtrPlaced pos)
        {
            int count = 0;
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };
            
            for (int i = 0; i < 4; i++)
            {
                int nx = pos.nX + dx[i];
                int ny = pos.nY + dy[i];
                if (nx >= 0 && nx < _MaxX && ny >= 0 && ny < _MaxY && _chars[nx, ny] != Blank)
                {
                    count++;
                }
            }
            return count;
        }

        private bool TryPlaceWord(string subword, LtrPlaced ltrPlaced)
        {
            var didPlaceWord = false;
            var theChar = ltrPlaced.ltr;
            // if the cur ltr is part of a horiz word, then we'll try to go vert and vv
            var DoHoriz = !ltrPlaced.IsHoriz;
            var ndxAt = 0;
            while (true)
            {
                var at = subword.IndexOf(theChar, ndxAt);
                if (at < 0)
                {
                    break;
                }
                int x0 = -1, y0 = -1, incx = 0, incy = 0;
                if (DoHoriz)
                { // if it fits on grid
                    if (ltrPlaced.nX - at >= 0)
                    {
                        if (ltrPlaced.nX - at + subword.Length <= _MaxX)
                        {
                            // if the prior and post squares are empty if they exist
                            if (ltrPlaced.nX - at == 0 || _chars[ltrPlaced.nX - at - 1, ltrPlaced.nY] == Blank)
                            {
                                if (ltrPlaced.nX - at + subword.Length == _MaxX || _chars[ltrPlaced.nX - at + subword.Length, ltrPlaced.nY] == Blank)
                                {
                                    x0 = ltrPlaced.nX - at;
                                    y0 = ltrPlaced.nY;
                                    incx = 1;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (ltrPlaced.nY - at >= 0)
                    {
                        if (ltrPlaced.nY - at + subword.Length <= _MaxY)
                        {
                            // if the prior and post squares are empty if they exist
                            if (ltrPlaced.nY - at == 0 || _chars[ltrPlaced.nX, ltrPlaced.nY - at - 1] == Blank)
                            {
                                if (ltrPlaced.nY - at + subword.Length == _MaxY || _chars[ltrPlaced.nX, ltrPlaced.nY - at + subword.Length] == Blank)
                                {
                                    x0 = ltrPlaced.nX;
                                    y0 = ltrPlaced.nY - at;
                                    incy = 1;
                                }
                            }
                        }
                    }
                }
                if (x0 >= 0)
                {
                    var doesfit = true;
                    int ndxc = 0;
                    foreach (var chr in subword)
                    {
                        var val = _chars[x0 + incx * ndxc, y0 + incy * ndxc];
                        if (val != Blank && val != chr) // reject?
                        {
                            doesfit = false;
                            break;
                        }
                        // if blank and the adjacent ones are not empty, we need to reject (brit xword)
                        if (val == Blank)
                        {
                            if (DoHoriz) // incx>0
                            {
                                if (y0 - 1 >= 0 && _chars[x0 + incx * ndxc, y0 - 1] != Blank)
                                {
                                    doesfit = false;
                                    break;
                                }
                                if (y0 + 1 < _MaxY && _chars[x0 + incx * ndxc, y0 + 1] != Blank)
                                {
                                    doesfit = false;
                                    break;
                                }
                            }
                            else
                            { // incy>0
                                if (x0 - 1 >= 0 && _chars[x0 - 1, y0 + incy * ndxc] != Blank)
                                {
                                    doesfit = false;
                                    break;
                                }
                                if (x0 + 1 < _MaxX && _chars[x0 + 1, y0 + incy * ndxc] != Blank)
                                {
                                    doesfit = false;
                                    break;
                                }

                            }
                        }
                        ndxc++;
                    }
                    if (doesfit)
                    {
                        PlaceOneWord(subword, x0, y0, incx, incy);
                        didPlaceWord = true;

                    }
                }
                ndxAt = at + 1;
            }

            return didPlaceWord;
        }

        public string ShowGrid()
        {
            var grid = Environment.NewLine;
            for (int y = 0; y < _MaxY; y++)
            {
                for (int x = 0; x < _MaxX; x++)
                {
                    grid += _chars[x, y].ToString();
                }
                grid += Environment.NewLine;
            }
            return grid;
        }
    }
}

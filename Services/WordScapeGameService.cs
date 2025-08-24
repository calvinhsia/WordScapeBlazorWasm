using DictionaryLib;
using WordScapeBlazorWasm.Models;

namespace WordScapeBlazorWasm.Services
{
    public class WordScapeGameService
    {
        private readonly DictionaryLib.DictionaryLib _dictionary;
        private readonly Random _random;

        public WordScapeGameService()
        {
            _dictionary = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            _random = new Random();
        }

        public async Task<PuzzleState> GeneratePuzzleAsync(GameSettings settings)
        {
            Console.WriteLine($"🎮 GeneratePuzzleAsync called - MinLength: {settings.MinWordLength}, MaxLength: {settings.MaxWordLength}");
            await Task.Delay(1); // Make it async for UI responsiveness
            
            // Get a random word of max length
            var targetWord = GetRandomWordOfLength(settings.MaxWordLength);
            if (string.IsNullOrEmpty(targetWord))
            {
                // Fallback to any word if specific length not found
                targetWord = "PUZZLE";
                Console.WriteLine($"⚠️ Using fallback word: {targetWord}");
            }
            else
            {
                Console.WriteLine($"🎯 Target word selected: {targetWord}");
            }

            // Find all subwords
            var possibleWords = FindAllSubwords(targetWord, settings.MinWordLength);
            Console.WriteLine($"📝 Found {possibleWords.Count} possible words");
            
            // Create letter circle from target word letters
            var circleLetters = CreateCircleLetters(targetWord);

            // Generate crossword grid
            var grid = GenerateCrosswordGrid(possibleWords, targetWord);

            var puzzle = new PuzzleState
            {
                TargetWord = targetWord,
                PossibleWords = possibleWords,
                CircleLetters = circleLetters,
                FoundWords = new HashSet<string>(),
                CurrentGuess = "",
                Grid = grid
            };
            
            Console.WriteLine($"✅ Puzzle generated successfully!");
            return puzzle;
        }

        private string GetRandomWordOfLength(int length)
        {
            // Use some predefined words that are likely to be in the dictionary
            var fallbackWords = new Dictionary<int, string[]>
            {
                [3] = new[] { "THE", "AND", "FOR", "ARE", "BUT", "NOT", "YOU", "ALL", "CAN", "HER", "WAS", "ONE", "OUR", "HAD", "HAS" },
                [4] = new[] { "THAT", "WITH", "HAVE", "THIS", "WILL", "YOUR", "FROM", "THEY", "KNOW", "WANT", "BEEN", "GOOD", "MUCH", "SOME", "TIME" },
                [5] = new[] { "WOULD", "THERE", "COULD", "OTHER", "AFTER", "FIRST", "NEVER", "THESE", "THINK", "WHERE", "BEING", "EVERY", "GREAT", "MIGHT", "SHALL" },
                [6] = new[] { "SHOULD", "THROUGH", "PEOPLE", "SCHOOL", "FATHER", "MOTHER", "SISTER", "BROTHER", "FRIEND", "FAMILY", "LITTLE", "CHANGE", "PUBLIC", "REALLY", "SYSTEM" },
                [7] = new[] { "BECAUSE", "WITHOUT", "THOUGHT", "ANOTHER", "BETWEEN", "NOTHING", "MORNING", "EVENING", "PERHAPS", "PRESENT", "CERTAIN", "COUNTRY", "GENERAL", "STUDENT", "SEVERAL" },
                [8] = new[] { "ALTHOUGH", "TOGETHER", "NATIONAL", "LEARNING", "CHILDREN", "PROBLEMS", "BUSINESS", "STUDENTS", "PATTERNS", "RESEARCH", "QUESTION", "INTEREST", "THINKING", "CONSIDER", "COMPLETE" },
                [9] = new[] { "EDUCATION", "IMPORTANT", "COMMUNITY", "DIFFERENT", "KNOWLEDGE", "BEAUTIFUL", "CHRISTMAS", "SOMETIMES", "WONDERFUL", "SOMETHING", "COUNTRIES", "FOLLOWING", "POLITICAL", "COMMITTEE", "MATERIALS" },
                [10] = new[] { "UNDERSTAND", "GOVERNMENT", "EVERYTHING", "MANAGEMENT", "BACKGROUND", "INDIVIDUAL", "CONFERENCE", "RESTAURANT", "BASKETBALL", "TECHNOLOGY", "GENERATION", "PARTICULAR", "POPULATION", "TELEVISION", "CALIFORNIA" }
            };

            // Try fallback words first
            if (fallbackWords.ContainsKey(length))
            {
                var words = fallbackWords[length];
                var shuffled = words.OrderBy(x => _random.Next()).ToArray();
                
                foreach (var word in shuffled)
                {
                    if (_dictionary.IsWord(word))
                    {
                        return word;
                    }
                }
            }

            // If no fallback words work, return a default based on length
            return length switch
            {
                3 => "THE",
                4 => "WORD",
                5 => "GREAT",
                6 => "SIMPLE",
                7 => "PROBLEM",
                8 => "COMPLETE",
                9 => "DIFFERENT",
                10 => "UNDERSTAND",
                _ => "PUZZLE"
            };
        }

        private List<string> FindAllSubwords(string targetWord, int minLength)
        {
            var validWords = new HashSet<string>();
            var letters = targetWord.ToCharArray();
            
            // Generate all possible permutations of different lengths
            for (int len = minLength; len <= targetWord.Length; len++)
            {
                GeneratePermutations("", letters.ToList(), len, validWords);
            }
            
            // Filter valid dictionary words and ensure they can be formed from target letters
            var result = validWords.Where(word => 
                word.Length >= minLength && 
                _dictionary.IsWord(word) &&
                CanFormWordFromLetters(word, targetWord))
                .OrderBy(w => w.Length)
                .ThenBy(w => w)
                .ToList();

            // Ensure we have at least some words by adding common subwords if needed
            if (result.Count < 5)
            {
                var commonWords = new[] { "THE", "AND", "FOR", "ARE", "BUT", "NOT", "YOU", "ALL", "CAN", "HER", "WAS", "ONE", "HAD", "HAS", "GET", "USE", "MAN", "NEW", "NOW", "OLD", "SEE", "HIM", "TWO", "HOW", "ITS", "WHO", "OIL", "SIT", "SET", "RUN", "EAT", "FAR", "SEA", "EYE", "RED", "TOP", "ARM", "TOO", "END", "WHY", "LET", "TRY" };
                foreach (var word in commonWords)
                {
                    if (word.Length >= minLength && CanFormWordFromLetters(word, targetWord) && _dictionary.IsWord(word))
                    {
                        result.Add(word);
                        if (result.Count >= 10) break;
                    }
                }
            }

            return result.Distinct().OrderBy(w => w.Length).ThenBy(w => w).ToList();
        }

        private void GeneratePermutations(string current, List<char> remaining, int targetLength, HashSet<string> results)
        {
            if (current.Length == targetLength)
            {
                results.Add(current);
                return;
            }

            if (current.Length >= targetLength) return;

            for (int i = 0; i < remaining.Count; i++)
            {
                var nextChar = remaining[i];
                var nextRemaining = new List<char>(remaining);
                nextRemaining.RemoveAt(i);
                GeneratePermutations(current + nextChar, nextRemaining, targetLength, results);
            }
        }

        private bool CanFormWordFromLetters(string word, string availableLetters)
        {
            var available = availableLetters.ToCharArray().ToList();
            
            foreach (char c in word)
            {
                if (!available.Remove(c))
                {
                    return false;
                }
            }
            return true;
        }

        private List<char> CreateCircleLetters(string word)
        {
            return word.ToCharArray().ToList();
        }

        public bool IsValidGuess(string guess, PuzzleState puzzle)
        {
            Console.WriteLine($"🔍 Validating guess: '{guess}'");
            
            if (string.IsNullOrEmpty(guess) || guess.Length < 3)
            {
                Console.WriteLine($"❌ Invalid - too short or empty");
                return false;
            }

            var isInDictionary = _dictionary.IsWord(guess);
            var canFormWord = CanFormWordFromLetters(guess, puzzle.TargetWord);
            var isPossible = puzzle.PossibleWords.Contains(guess);
            
            Console.WriteLine($"   📚 In dictionary: {isInDictionary}");
            Console.WriteLine($"   🔤 Can form from letters: {canFormWord}");
            Console.WriteLine($"   ✅ In possible words: {isPossible}");
            
            var result = isInDictionary && canFormWord && isPossible;
            Console.WriteLine($"   🎯 Final result: {result}");
            
            return result;
        }

        public bool TryAddWord(string word, PuzzleState puzzle)
        {
            if (IsValidGuess(word, puzzle) && !puzzle.FoundWords.Contains(word))
            {
                puzzle.FoundWords.Add(word);
                ShowWordInGrid(word, puzzle);
                return true;
            }
            return false;
        }

        private CrosswordGrid GenerateCrosswordGrid(List<string> possibleWords, string targetWord)
        {
            var grid = new CrosswordGrid();
            const int maxSize = 15;
            
            // Initialize grid with blanks
            grid.MaxX = maxSize;
            grid.MaxY = maxSize;
            grid.Letters = new char[maxSize, maxSize];
            
            for (int y = 0; y < maxSize; y++)
            {
                for (int x = 0; x < maxSize; x++)
                {
                    grid.Letters[x, y] = CrosswordGrid.Blank;
                }
            }

            var wordsToPlace = possibleWords.OrderByDescending(w => w.Length).Take(Math.Min(8, possibleWords.Count)).ToList();
            var placedLetters = new List<PlacedLetter>();

            // Place first word in the center
            if (wordsToPlace.Any())
            {
                var firstWord = wordsToPlace[0];
                var isHorizontal = _random.NextDouble() < 0.5;
                int startX, startY;

                if (isHorizontal)
                {
                    startY = maxSize / 2;
                    startX = (maxSize - firstWord.Length) / 2;
                }
                else
                {
                    startX = maxSize / 2;
                    startY = (maxSize - firstWord.Length) / 2;
                }

                PlaceWordInGrid(grid, firstWord, startX, startY, isHorizontal, placedLetters);
                wordsToPlace.RemoveAt(0);
            }

            // Try to place remaining words by intersecting with existing letters
            foreach (var word in wordsToPlace.Take(6)) // Limit to 6 additional words
            {
                TryPlaceWordByIntersection(grid, word, placedLetters);
            }

            // Create cells list for UI
            CreateGridCells(grid);

            return grid;
        }

        private void PlaceWordInGrid(CrosswordGrid grid, string word, int startX, int startY, bool isHorizontal, List<PlacedLetter> placedLetters)
        {
            grid.PlacedWords[word] = new WordPlacement
            {
                StartX = startX,
                StartY = startY,
                IsHorizontal = isHorizontal,
                Word = word
            };

            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? startX + i : startX;
                int y = isHorizontal ? startY : startY + i;
                
                grid.Letters[x, y] = word[i];
                placedLetters.Add(new PlacedLetter
                {
                    X = x,
                    Y = y,
                    Letter = word[i],
                    IsHorizontal = isHorizontal
                });
            }
        }

        private bool TryPlaceWordByIntersection(CrosswordGrid grid, string word, List<PlacedLetter> placedLetters)
        {
            // Try to find a letter in the word that matches a placed letter
            foreach (var placedLetter in placedLetters.OrderBy(x => _random.Next()))
            {
                for (int i = 0; i < word.Length; i++)
                {
                    if (word[i] == placedLetter.Letter)
                    {
                        // Try to place word perpendicular to the existing word
                        bool newWordIsHorizontal = !placedLetter.IsHorizontal;
                        int startX, startY;

                        if (newWordIsHorizontal)
                        {
                            startX = placedLetter.X - i;
                            startY = placedLetter.Y;
                        }
                        else
                        {
                            startX = placedLetter.X;
                            startY = placedLetter.Y - i;
                        }

                        // Check if the word fits and doesn't conflict
                        if (CanPlaceWord(grid, word, startX, startY, newWordIsHorizontal))
                        {
                            PlaceWordInGrid(grid, word, startX, startY, newWordIsHorizontal, placedLetters);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool CanPlaceWord(CrosswordGrid grid, string word, int startX, int startY, bool isHorizontal)
        {
            // Check bounds
            if (startX < 0 || startY < 0) return false;
            if (isHorizontal && startX + word.Length > grid.MaxX) return false;
            if (!isHorizontal && startY + word.Length > grid.MaxY) return false;

            // Check if positions are available or match
            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? startX + i : startX;
                int y = isHorizontal ? startY : startY + i;

                char existingLetter = grid.Letters[x, y];
                if (existingLetter != CrosswordGrid.Blank && existingLetter != word[i])
                {
                    return false;
                }

                // Check adjacent cells don't have conflicting letters (simplified British crossword rule)
                if (existingLetter == CrosswordGrid.Blank)
                {
                    if (isHorizontal)
                    {
                        // Check above and below
                        if (y > 0 && grid.Letters[x, y - 1] != CrosswordGrid.Blank) return false;
                        if (y < grid.MaxY - 1 && grid.Letters[x, y + 1] != CrosswordGrid.Blank) return false;
                    }
                    else
                    {
                        // Check left and right
                        if (x > 0 && grid.Letters[x - 1, y] != CrosswordGrid.Blank) return false;
                        if (x < grid.MaxX - 1 && grid.Letters[x + 1, y] != CrosswordGrid.Blank) return false;
                    }
                }
            }

            return true;
        }

        private void CreateGridCells(CrosswordGrid grid)
        {
            // Find the actual bounds of placed letters
            int minX = grid.MaxX, maxX = -1, minY = grid.MaxY, maxY = -1;
            
            for (int y = 0; y < grid.MaxY; y++)
            {
                for (int x = 0; x < grid.MaxX; x++)
                {
                    if (grid.Letters[x, y] != CrosswordGrid.Blank)
                    {
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            // If we found content, resize the grid to fit
            if (maxX >= 0)
            {
                // Add some padding
                minX = Math.Max(0, minX - 1);
                minY = Math.Max(0, minY - 1);
                maxX = Math.Min(grid.MaxX - 1, maxX + 1);
                maxY = Math.Min(grid.MaxY - 1, maxY + 1);

                var newWidth = maxX - minX + 1;
                var newHeight = maxY - minY + 1;

                // Create new smaller grid
                var newLetters = new char[newWidth, newHeight];
                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        newLetters[x, y] = grid.Letters[minX + x, minY + y];
                    }
                }

                // Update grid properties
                grid.Letters = newLetters;
                grid.MaxX = newWidth;
                grid.MaxY = newHeight;

                // Update placement coordinates
                foreach (var placement in grid.PlacedWords.Values)
                {
                    placement.StartX -= minX;
                    placement.StartY -= minY;
                }
            }

            // Create cells for the optimized grid
            grid.Cells.Clear();
            for (int y = 0; y < grid.MaxY; y++)
            {
                for (int x = 0; x < grid.MaxX; x++)
                {
                    grid.Cells.Add(new GridCell
                    {
                        X = x,
                        Y = y,
                        Letter = grid.Letters[x, y],
                        IsRevealed = false
                    });
                }
            }
        }

        public WordStatus ShowWordInGrid(string word, PuzzleState puzzle)
        {
            if (puzzle.Grid.PlacedWords.TryGetValue(word, out var placement))
            {
                bool wasAlreadyRevealed = true;
                
                // Check if any letters need to be revealed
                for (int i = 0; i < word.Length; i++)
                {
                    int x = placement.IsHorizontal ? placement.StartX + i : placement.StartX;
                    int y = placement.IsHorizontal ? placement.StartY : placement.StartY + i;
                    
                    var cell = puzzle.Grid.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
                    if (cell != null && !cell.IsRevealed)
                    {
                        wasAlreadyRevealed = false;
                        cell.IsRevealed = true;
                    }
                }

                return wasAlreadyRevealed ? WordStatus.IsAlreadyInGrid : WordStatus.IsShownInGridForFirstTime;
            }

            return WordStatus.IsNotInGrid;
        }

        public string? GetWordAtPosition(int x, int y, PuzzleState puzzle)
        {
            // Find the word that contains this position and hasn't been found yet
            foreach (var kvp in puzzle.Grid.PlacedWords)
            {
                var word = kvp.Key;
                var placement = kvp.Value;
                
                // Skip already found words
                if (puzzle.FoundWords.Contains(word)) continue;
                
                // Check if position is within this word
                bool isWithinWord = false;
                if (placement.IsHorizontal)
                {
                    if (y == placement.StartY && x >= placement.StartX && x < placement.StartX + word.Length)
                        isWithinWord = true;
                }
                else
                {
                    if (x == placement.StartX && y >= placement.StartY && y < placement.StartY + word.Length)
                        isWithinWord = true;
                }
                
                if (isWithinWord)
                {
                    return word;
                }
            }
            
            return null;
        }

        public void TemporarilyRevealWord(string word, PuzzleState puzzle)
        {
            if (puzzle.Grid.PlacedWords.TryGetValue(word, out var placement))
            {
                // Reveal all letters of this word temporarily
                for (int i = 0; i < word.Length; i++)
                {
                    int x = placement.IsHorizontal ? placement.StartX + i : placement.StartX;
                    int y = placement.IsHorizontal ? placement.StartY : placement.StartY + i;
                    
                    var cell = puzzle.Grid.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
                    if (cell != null)
                    {
                        cell.IsRevealed = true;
                    }
                }
            }
        }

        public void HideWord(string word, PuzzleState puzzle)
        {
            if (puzzle.Grid.PlacedWords.TryGetValue(word, out var placement))
            {
                // Hide letters that are not part of already found words
                for (int i = 0; i < word.Length; i++)
                {
                    int x = placement.IsHorizontal ? placement.StartX + i : placement.StartX;
                    int y = placement.IsHorizontal ? placement.StartY : placement.StartY + i;
                    
                    var cell = puzzle.Grid.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
                    if (cell != null)
                    {
                        // Check if this cell is part of any found word
                        bool isPartOfFoundWord = false;
                        foreach (var foundWord in puzzle.FoundWords)
                        {
                            if (puzzle.Grid.PlacedWords.TryGetValue(foundWord, out var foundPlacement))
                            {
                                for (int j = 0; j < foundWord.Length; j++)
                                {
                                    int foundX = foundPlacement.IsHorizontal ? foundPlacement.StartX + j : foundPlacement.StartX;
                                    int foundY = foundPlacement.IsHorizontal ? foundPlacement.StartY : foundPlacement.StartY + j;
                                    
                                    if (foundX == x && foundY == y)
                                    {
                                        isPartOfFoundWord = true;
                                        break;
                                    }
                                }
                            }
                            if (isPartOfFoundWord) break;
                        }
                        
                        // Only hide if not part of a found word
                        if (!isPartOfFoundWord)
                        {
                            cell.IsRevealed = false;
                        }
                    }
                }
            }
        }
    }

    public class PlacedLetter
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Letter { get; set; }
        public bool IsHorizontal { get; set; }
    }
}

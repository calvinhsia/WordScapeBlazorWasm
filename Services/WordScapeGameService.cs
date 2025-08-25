using DictionaryLib;
using WordScapeBlazorWasm.Models;

namespace WordScapeBlazorWasm.Services
{
    public class WordScapeGameService
    {
        private readonly DictionaryLib.DictionaryLib _dictionarySmall;
        private readonly DictionaryLib.DictionaryLib _dictionaryLarge;
        private readonly Random _random;

        public WordScapeGameService()
        {
            _dictionarySmall = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            _dictionaryLarge = new DictionaryLib.DictionaryLib(DictionaryType.Large);
            // Use fixed seed for consistent debugging/testing results
            _random = new Random(12345);
        }

        public async Task<PuzzleState> GeneratePuzzleAsync(GameSettings settings)
        {
            Console.WriteLine($"🎮 GeneratePuzzleAsync called - MinLength: {settings.MinWordLength}, MaxLength: {settings.MaxWordLength}");
            
            // Add yield points for WebAssembly single-threaded environment
            await Task.Yield(); // Yield to allow UI updates

            try
            {
                // For now, use fallback to ensure the app loads quickly
                // TODO: Re-enable original source after debugging
                return await CreateFallbackPuzzleAsync(settings);
                
                // Original source implementation (temporarily disabled)
                /*
                var wordGenerationParms = new WordGenerationParms()
                {
                    LenTargetWord = settings.MaxWordLength,
                    MinSubWordLength = settings.MinWordLength,
                    MaxX = 15,
                    MaxY = 15,
                    _Random = _random
                };

                var wordScapePuzzle = await WordScapePuzzle.CreateNextPuzzleTask(wordGenerationParms);
                var genGrid = wordScapePuzzle.genGrid;
                var targetWord = wordScapePuzzle.wordContainer.InitialWord;

                Console.WriteLine($"✅ Generated puzzle with target word: '{targetWord}', Grid size: {genGrid._MaxX}x{genGrid._MaxY}");

                var allWords = genGrid._dictPlacedWords.Keys.ToList();
                var possibleWords = new HashSet<string>(allWords);

                var puzzle = new PuzzleState
                {
                    TargetWord = targetWord,
                    PossibleWords = possibleWords.ToList(),
                    Grid = genGrid,
                    CircleLetters = CreateCircleLetters(targetWord)
                };

                Console.WriteLine($"📝 Puzzle created with {possibleWords.Count} possible words");
                return puzzle;
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating puzzle: {ex.Message}");
                return await CreateFallbackPuzzleAsync(settings);
            }
        }

        private async Task<PuzzleState> CreateFallbackPuzzleAsync(GameSettings settings)
        {
            Console.WriteLine($"🚨 CreateFallbackPuzzle: MaxLength={settings.MaxWordLength}, MinLength={settings.MinWordLength}");
            
            // Yield periodically for WebAssembly UI responsiveness
            await Task.Yield();
            
            var targetWord = GetRandomWordOfLength(settings.MaxWordLength);
            Console.WriteLine($"🎯 Selected target word: '{targetWord}'");
            
            // Yield before intensive subword finding
            await Task.Yield();
            
            var possibleWords = FindAllSubwords(targetWord, settings.MinWordLength);
            Console.WriteLine($"📝 Found {possibleWords.Count} subwords: {string.Join(", ", possibleWords.Take(10))}");
            
            // Yield before grid generation
            await Task.Yield();
            
            var puzzle = new PuzzleState
            {
                TargetWord = targetWord,
                PossibleWords = possibleWords,
                Grid = await GenerateCrosswordGridAsync(possibleWords, targetWord),
                CircleLetters = CreateCircleLetters(targetWord)
            };

            Console.WriteLine($"✅ Fallback puzzle created successfully");
            return puzzle;
        }

        private async Task<GenGrid> GenerateCrosswordGridAsync(List<string> possibleWords, string targetWord)
        {
            // Create a simple GenGrid for fallback
            var wordContainer = new WordContainer { InitialWord = targetWord, subwords = possibleWords };
            var genGrid = new GenGrid(15, 15, wordContainer, _random);

            Console.WriteLine($"🎯 GenerateCrosswordGrid: Target='{targetWord}', PossibleWords={possibleWords.Count}");
            if (possibleWords.Count > 0)
            {
                Console.WriteLine($"📝 Available words: {string.Join(", ", possibleWords.Take(10))}");
            }

            if (!possibleWords.Any()) return genGrid;

            // Yield before sorting (potentially CPU intensive for large word lists)
            await Task.Yield();

            // Sort words by length (longest first) for better placement
            var sortedWords = possibleWords.OrderByDescending(w => w.Length).ToList();
            var placedWords = new List<string>();

            // Place the first word horizontally in the center
            var firstWord = sortedWords.First();
            int startX = (genGrid._MaxX - firstWord.Length) / 2;
            int startY = genGrid._MaxY / 2;

            Console.WriteLine($"🏠 Placing first word '{firstWord}' at ({startX},{startY})");
            PlaceWordHorizontally(genGrid, firstWord, startX, startY);
            placedWords.Add(firstWord);

            // Yield before intensive word placement loop
            await Task.Yield();

            // Try to place additional words by finding intersections
            int attempts = 0;
            foreach (var word in sortedWords.Skip(1))
            {
                attempts++;
                if (placedWords.Count >= 8) break; // Limit to avoid overcrowding
                
                // Yield every few attempts to prevent UI blocking
                if (attempts % 3 == 0)
                {
                    await Task.Yield();
                }
                
                // Only show detailed debugging for first few attempts to avoid spam
                bool showDetailedDebug = attempts <= 3;
                
                if (showDetailedDebug) Console.WriteLine($"🔍 Attempt {attempts}: Trying to place '{word}'...");
                if (TryPlaceIntersectingWord(genGrid, word, placedWords, showDetailedDebug))
                {
                    placedWords.Add(word);
                    Console.WriteLine($"✅ Successfully placed '{word}' (total: {placedWords.Count})");
                }
                else
                {
                    if (showDetailedDebug) Console.WriteLine($"❌ Failed to place '{word}'");
                }
            }

            Console.WriteLine($"🎮 Final grid has {placedWords.Count} words: {string.Join(", ", placedWords)}");
            return genGrid;
        }

        private void PlaceWordHorizontally(GenGrid genGrid, string word, int startX, int startY)
        {
            for (int i = 0; i < word.Length; i++)
            {
                genGrid._chars[startX + i, startY] = word[i];
            }

            genGrid._dictPlacedWords[word] = new LtrPlaced
            {
                nX = startX,
                nY = startY,
                IsHoriz = true
            };
        }

        private void PlaceWordVertically(GenGrid genGrid, string word, int startX, int startY)
        {
            for (int i = 0; i < word.Length; i++)
            {
                genGrid._chars[startX, startY + i] = word[i];
            }

            genGrid._dictPlacedWords[word] = new LtrPlaced
            {
                nX = startX,
                nY = startY,
                IsHoriz = false
            };
        }

        private bool TryPlaceIntersectingWord(GenGrid genGrid, string newWord, List<string> placedWords, bool showDebug = false)
        {
            // Try to intersect with each placed word
            foreach (var placedWord in placedWords)
            {
                var placement = genGrid._dictPlacedWords[placedWord];
                if (showDebug) Console.WriteLine($"   📍 Checking intersection with '{placedWord}' at ({placement.nX},{placement.nY}) IsHoriz={placement.IsHoriz}");
                
                // Find common letters
                for (int newIdx = 0; newIdx < newWord.Length; newIdx++)
                {
                    for (int placedIdx = 0; placedIdx < placedWord.Length; placedIdx++)
                    {
                        if (newWord[newIdx] == placedWord[placedIdx])
                        {
                            if (showDebug) Console.WriteLine($"   ✨ Found common letter '{newWord[newIdx]}' at newWord[{newIdx}] and placedWord[{placedIdx}]");
                            
                            // Try to place the new word intersecting at this letter
                            if (placement.IsHoriz)
                            {
                                // Place new word vertically
                                int newStartX = placement.nX + placedIdx;
                                int newStartY = placement.nY - newIdx;
                                
                                if (showDebug) Console.WriteLine($"   📐 Trying vertical placement at ({newStartX},{newStartY})");
                                if (CanPlaceWordVertically(genGrid, newWord, newStartX, newStartY, showDebug))
                                {
                                    if (showDebug) Console.WriteLine($"   ✅ Can place '{newWord}' vertically at ({newStartX},{newStartY})");
                                    PlaceWordVertically(genGrid, newWord, newStartX, newStartY);
                                    return true;
                                }
                                else
                                {
                                    if (showDebug) Console.WriteLine($"   ❌ Cannot place '{newWord}' vertically at ({newStartX},{newStartY}) - bounds or conflict");
                                }
                            }
                            else
                            {
                                // Place new word horizontally
                                int newStartX = placement.nX - newIdx;
                                int newStartY = placement.nY + placedIdx;
                                
                                if (showDebug) Console.WriteLine($"   📐 Trying horizontal placement at ({newStartX},{newStartY})");
                                if (CanPlaceWordHorizontally(genGrid, newWord, newStartX, newStartY, showDebug))
                                {
                                    if (showDebug) Console.WriteLine($"   ✅ Can place '{newWord}' horizontally at ({newStartX},{newStartY})");
                                    PlaceWordHorizontally(genGrid, newWord, newStartX, newStartY);
                                    return true;
                                }
                                else
                                {
                                    if (showDebug) Console.WriteLine($"   ❌ Cannot place '{newWord}' horizontally at ({newStartX},{newStartY}) - bounds or conflict");
                                }
                            }
                        }
                    }
                }
            }
            
            return false;
        }

        private bool CanPlaceWordHorizontally(GenGrid genGrid, string word, int startX, int startY, bool showDebug = false)
        {
            // Use the comprehensive TryPlaceWord validation
            return TryPlaceWord(genGrid, word, startX, startY, true, showDebug);
        }

        private bool CanPlaceWordVertically(GenGrid genGrid, string word, int startX, int startY, bool showDebug = false)
        {
            // Use the comprehensive TryPlaceWord validation
            return TryPlaceWord(genGrid, word, startX, startY, false, showDebug);
        }

        /// <summary>
        /// Comprehensive word placement validation from original Xamarin source.
        /// Ensures that placing a word doesn't create invalid letter sequences.
        /// </summary>
        private bool TryPlaceWord(GenGrid genGrid, string word, int startX, int startY, bool isHorizontal, bool showDebug = false)
        {
            // Check bounds
            int endX = isHorizontal ? startX + word.Length - 1 : startX;
            int endY = isHorizontal ? startY : startY + word.Length - 1;
            
            if (startX < 0 || startY < 0 || endX >= genGrid._MaxX || endY >= genGrid._MaxY)
            {
                if (showDebug) Console.WriteLine($"     ❌ Bounds check failed: word='{word}' at ({startX},{startY}) would extend to ({endX},{endY}), grid is {genGrid._MaxX}x{genGrid._MaxY}");
                return false;
            }

            // Check for direct conflicts (letters that don't match at intersection points)
            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? startX + i : startX;
                int y = isHorizontal ? startY : startY + i;
                char existingChar = genGrid._chars[x, y];
                
                if (existingChar != GenGrid.Blank && existingChar != word[i])
                {
                    if (showDebug) Console.WriteLine($"     ❌ Conflict at ({x},{y}): existing='{existingChar}', new='{word[i]}'");
                    return false;
                }
            }

            // Create a temporary copy of the grid to test the placement
            var tempGrid = CopyGrid(genGrid);
            
            // Temporarily place the word in the copy
            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? startX + i : startX;
                int y = isHorizontal ? startY : startY + i;
                tempGrid[x, y] = word[i];
            }

            // Validate all consecutive letter sequences in the affected area
            // Check horizontal sequences around the placed word
            for (int y = Math.Max(0, startY - 1); y <= Math.Min(genGrid._MaxY - 1, endY + 1); y++)
            {
                if (!ValidateHorizontalSequences(tempGrid, genGrid._MaxX, y, showDebug))
                {
                    if (showDebug) Console.WriteLine($"     ❌ Invalid horizontal sequence created at row {y}");
                    return false;
                }
            }

            // Check vertical sequences around the placed word
            for (int x = Math.Max(0, startX - 1); x <= Math.Min(genGrid._MaxX - 1, endX + 1); x++)
            {
                if (!ValidateVerticalSequences(tempGrid, genGrid._MaxY, x, showDebug))
                {
                    if (showDebug) Console.WriteLine($"     ❌ Invalid vertical sequence created at column {x}");
                    return false;
                }
            }

            if (showDebug) Console.WriteLine($"     ✅ Word '{word}' can be placed at ({startX},{startY}) {(isHorizontal ? "horizontally" : "vertically")}");
            return true;
        }

        private char[,] CopyGrid(GenGrid genGrid)
        {
            var copy = new char[genGrid._MaxX, genGrid._MaxY];
            for (int x = 0; x < genGrid._MaxX; x++)
            {
                for (int y = 0; y < genGrid._MaxY; y++)
                {
                    copy[x, y] = genGrid._chars[x, y];
                }
            }
            return copy;
        }

        private bool ValidateHorizontalSequences(char[,] grid, int maxX, int row, bool showDebug)
        {
            int sequenceStart = -1;
            
            for (int x = 0; x <= maxX; x++) // Go one past to handle sequence at end
            {
                bool hasLetter = x < maxX && grid[x, row] != GenGrid.Blank;
                
                if (hasLetter && sequenceStart == -1)
                {
                    // Start of a new sequence
                    sequenceStart = x;
                }
                else if (!hasLetter && sequenceStart != -1)
                {
                    // End of sequence
                    int length = x - sequenceStart;
                    if (length > 1) // Only validate sequences longer than 1 letter
                    {
                        string sequence = ExtractHorizontalSequence(grid, sequenceStart, row, length);
                        if (!_dictionarySmall.IsWord(sequence))
                        {
                            if (showDebug) Console.WriteLine($"     ❌ Invalid horizontal sequence: '{sequence}' at ({sequenceStart},{row})");
                            return false;
                        }
                    }
                    sequenceStart = -1;
                }
            }
            
            return true;
        }

        private bool ValidateVerticalSequences(char[,] grid, int maxY, int column, bool showDebug)
        {
            int sequenceStart = -1;
            
            for (int y = 0; y <= maxY; y++) // Go one past to handle sequence at end
            {
                bool hasLetter = y < maxY && grid[column, y] != GenGrid.Blank;
                
                if (hasLetter && sequenceStart == -1)
                {
                    // Start of a new sequence
                    sequenceStart = y;
                }
                else if (!hasLetter && sequenceStart != -1)
                {
                    // End of sequence
                    int length = y - sequenceStart;
                    if (length > 1) // Only validate sequences longer than 1 letter
                    {
                        string sequence = ExtractVerticalSequence(grid, column, sequenceStart, length);
                        if (!_dictionarySmall.IsWord(sequence))
                        {
                            if (showDebug) Console.WriteLine($"     ❌ Invalid vertical sequence: '{sequence}' at ({column},{sequenceStart})");
                            return false;
                        }
                    }
                    sequenceStart = -1;
                }
            }
            
            return true;
        }

        private string ExtractHorizontalSequence(char[,] grid, int startX, int y, int length)
        {
            var sequence = new char[length];
            for (int i = 0; i < length; i++)
            {
                sequence[i] = grid[startX + i, y];
            }
            return new string(sequence);
        }

        private string ExtractVerticalSequence(char[,] grid, int x, int startY, int length)
        {
            var sequence = new char[length];
            for (int i = 0; i < length; i++)
            {
                sequence[i] = grid[x, startY + i];
            }
            return new string(sequence);
        }

        public string? GetWordAtPosition(int x, int y, PuzzleState puzzle)
        {
            // Find the word that contains this position and hasn't been found yet
            foreach (var kvp in puzzle.Grid?._dictPlacedWords ?? new Dictionary<string, LtrPlaced>())
            {
                var word = kvp.Key;
                var placement = kvp.Value;
                
                // Skip already found words
                if (puzzle.FoundWords.Any(fw => fw.Word == word)) 
                {
                    continue;
                }
                
                // Check if position is within this word
                bool isWithinWord = false;
                if (placement.IsHoriz)
                {
                    if (y == placement.nY && x >= placement.nX && x < placement.nX + word.Length)
                        isWithinWord = true;
                }
                else
                {
                    if (x == placement.nX && y >= placement.nY && y < placement.nY + word.Length)
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
            Console.WriteLine($"🔍 TemporarilyRevealWord: '{word}'");
            
            if (puzzle.Grid?._dictPlacedWords.TryGetValue(word, out var placement) == true)
            {
                Console.WriteLine($"   Found placement: ({placement.nX},{placement.nY}) IsHoriz={placement.IsHoriz}");
                
                // Reveal all letters of this word temporarily
                for (int i = 0; i < word.Length; i++)
                {
                    int x = placement.IsHoriz ? placement.nX + i : placement.nX;
                    int y = placement.IsHoriz ? placement.nY : placement.nY + i;
                    
                    var cell = puzzle.LegacyGrid.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
                    if (cell != null)
                    {
                        Console.WriteLine($"   Revealing cell at ({x},{y}) with letter '{word[i]}'");
                        cell.IsRevealed = true;
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Could not find cell at ({x},{y})");
                    }
                }
            }
            else
            {
                Console.WriteLine($"   ❌ Word '{word}' not found in placed words");
            }
        }

        public void HideWord(string word, PuzzleState puzzle)
        {
            Console.WriteLine($"🫥 HideWord: '{word}'");
            
            if (puzzle.Grid?._dictPlacedWords.TryGetValue(word, out var placement) == true)
            {
                // Hide letters that are not part of already found words
                for (int i = 0; i < word.Length; i++)
                {
                    int x = placement.IsHoriz ? placement.nX + i : placement.nX;
                    int y = placement.IsHoriz ? placement.nY : placement.nY + i;
                    
                    var cell = puzzle.LegacyGrid.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
                    if (cell != null)
                    {
                        // Check if this cell is part of any found word
                        bool isPartOfFoundWord = false;
                        foreach (var foundWord in puzzle.FoundWords)
                        {
                            if (puzzle.Grid._dictPlacedWords.TryGetValue(foundWord.Word, out var foundPlacement))
                            {
                                for (int j = 0; j < foundWord.Word.Length; j++)
                                {
                                    int foundX = foundPlacement.IsHoriz ? foundPlacement.nX + j : foundPlacement.nX;
                                    int foundY = foundPlacement.IsHoriz ? foundPlacement.nY : foundPlacement.nY + j;
                                    
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

        public WordStatus ShowWordInGrid(string word, PuzzleState puzzle)
        {
            if (puzzle.Grid?._dictPlacedWords?.TryGetValue(word, out var placement) == true)
            {
                bool wasAlreadyRevealed = true;
                
                // Check if any letters need to be revealed
                for (int i = 0; i < word.Length; i++)
                {
                    int x = placement.IsHoriz ? placement.nX + i : placement.nX;
                    int y = placement.IsHoriz ? placement.nY : placement.nY + i;
                    
                    var cell = puzzle.LegacyGrid.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
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

        private string GetRandomWordOfLength(int length)
        {
            // Use words that are likely to have many subwords
            var goodTargetWords = new Dictionary<int, string[]>
            {
                [3] = new[] { "THE", "AND", "FOR", "ARE", "BUT", "NOT", "YOU", "ALL", "CAN", "HER", "WAS", "ONE", "OUR", "HAD", "HAS" },
                [4] = new[] { "THAT", "WITH", "HAVE", "THIS", "WILL", "YOUR", "FROM", "THEY", "KNOW", "WANT", "BEEN", "GOOD", "MUCH", "SOME", "TIME" },
                [5] = new[] { "GREAT", "THINK", "THERE", "OTHER", "AFTER", "FIRST", "NEVER", "THESE", "WHERE", "BEING", "EVERY", "MIGHT", "SHALL", "HEART", "EARTH" },
                [6] = new[] { "PLANET", "MASTER", "GARDEN", "THREAD", "STREAM", "MOTHER", "FATHER", "FRIEND", "CHANGE", "ORANGE", "STRONG", "SIMPLE", "HEARTS" },
                [7] = new[] { "THREADS", "STREAMS", "MASTERS", "GARDENS", "PLANETS", "READING", "HEATING", "EARING", "TEACHER", "CREATES", "LARGEST", "STRANGE" },
                [8] = new[] { "CREATION", "STRENGTH", "LEARNING", "STREAMED", "THREADED", "MASTERED", "GARDENED", "PLANETED", "TEACHERS", "STRONGER", "TOGETHER", "BUSINESS" },
                [9] = new[] { "SOMETHING", "STREAMING", "THREADING", "MASTERING", "GARDENING", "THREATING", "SEARCHING", "BREATHING", "CREATIONS", "GREATNESS", "STRONGMAN" },
                [10] = new[] { "EVERYTHING", "STRENGTHEN", "STREAMLINE", "THREADLIKE", "MASTERMIND", "SEARCHABLE", "BREATHLESS", "CREATIONIST", "GREATENING", "STRONGHOLD" }
            };

            // Always try good target words first
            if (goodTargetWords.ContainsKey(length))
            {
                var words = goodTargetWords[length];
                var shuffled = words.OrderBy(x => _random.Next()).ToArray();
                
                foreach (var word in shuffled)
                {
                    if (_dictionarySmall.IsWord(word))
                    {
                        Console.WriteLine($"🎯 Selected good target word: '{word}' (length {length})");
                        return word;
                    }
                }
            }

            // If no good words work, return a default that should have subwords
            Console.WriteLine($"⚠️ Using fallback for length {length}");
            return length switch
            {
                3 => "THE",
                4 => "THAT",
                5 => "GREAT",
                6 => "PLANET",
                7 => "THREADS",
                8 => "CREATION",
                9 => "SOMETHING",
                10 => "EVERYTHING",
                _ => "SOMETHING"
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
                _dictionarySmall.IsWord(word) &&
                CanFormWordFromLetters(word, targetWord))
                .OrderBy(w => w.Length)
                .ThenBy(w => w)
                .ToList();

            // Apply filtering to remove plural/gerund/past tense duplicates (from original source)
            result = IgnorePluralGerundPastTenseWords(result);

            // Ensure we have at least some words by adding common subwords if needed
            if (result.Count < 5)
            {
                var commonWords = new[] { "THE", "AND", "FOR", "ARE", "BUT", "NOT", "YOU", "ALL", "CAN", "HER", "WAS", "ONE", "HAD", "HAS", "GET", "USE", "MAN", "NEW", "NOW", "OLD", "SEE", "HIM", "TWO", "HOW", "ITS", "WHO", "OIL", "SIT", "SET", "RUN", "EAT", "FAR", "SEA", "EYE", "RED", "TOP", "ARM", "TOO", "END", "WHY", "LET", "TRY" };
                foreach (var word in commonWords)
                {
                    if (word.Length >= minLength && CanFormWordFromLetters(word, targetWord) && _dictionarySmall.IsWord(word))
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
            var letters = word.ToCharArray().ToList();
            
            // Randomize the order of letters in the circle
            for (int i = letters.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (letters[i], letters[j]) = (letters[j], letters[i]);
            }
            
            Console.WriteLine($"🔀 Randomized circle letters: {string.Join("", letters)} (from word: {word})");
            return letters;
        }

        public FoundWordType ValidateWord(string guess, PuzzleState puzzle)
        {
            Console.WriteLine($"🔍 Validating word: '{guess}'");
            
            if (string.IsNullOrEmpty(guess) || guess.Length < 3)
            {
                Console.WriteLine($"❌ Invalid - too short or empty");
                return FoundWordType.SubWordNotAWord;
            }

            var canFormWord = CanFormWordFromLetters(guess, puzzle.TargetWord);
            if (!canFormWord)
            {
                Console.WriteLine($"❌ Cannot form from target letters");
                return FoundWordType.SubWordNotAWord;
            }

            // Check if word is in the puzzle grid (highest priority)
            var isPossible = puzzle.PossibleWords.Contains(guess);
            if (isPossible)
            {
                Console.WriteLine($"✅ Found in puzzle grid");
                return FoundWordType.SubWordInGrid;
            }

            // Check if word is in small dictionary
            var isInSmallDict = _dictionarySmall.IsWord(guess);
            if (isInSmallDict)
            {
                Console.WriteLine($"📚 Found in small dictionary");
                return FoundWordType.SubWordNotInGrid;
            }

            // Check if word is in large dictionary
            var isInLargeDict = _dictionaryLarge.IsWord(guess);
            if (isInLargeDict)
            {
                Console.WriteLine($"📖 Found in large dictionary");
                return FoundWordType.SubWordInLargeDictionary;
            }

            Console.WriteLine($"❌ Not found in any dictionary");
            return FoundWordType.SubWordNotAWord;
        }

        public bool IsValidGuess(string guess, PuzzleState puzzle)
        {
            var wordType = ValidateWord(guess, puzzle);
            // Accept words that are in grid or in any dictionary and can be formed
            return wordType != FoundWordType.SubWordNotAWord;
        }

        public bool TryAddWord(string word, PuzzleState puzzle)
        {
            var wordType = ValidateWord(word, puzzle);
            if (wordType != FoundWordType.SubWordNotAWord)
            {
                var foundWord = new FoundWord { Word = word, Type = wordType };
                if (!puzzle.FoundWords.Any(fw => fw.Word == word))
                {
                    puzzle.FoundWords.Add(foundWord);
                    if (wordType == FoundWordType.SubWordInGrid)
                    {
                        ShowWordInGrid(word, puzzle);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Filter out plural, gerund, and past tense forms when the root word is also present.
        /// From original Xamarin WordScape source to prevent duplicate word forms.
        /// </summary>
        private List<string> IgnorePluralGerundPastTenseWords(List<string> words)
        {
            var filteredWords = new List<string>();
            var wordsSet = new HashSet<string>(words);

            foreach (var word in words)
            {
                bool shouldInclude = true;

                // Skip plurals if singular exists
                if (word.EndsWith("S") && word.Length > 3)
                {
                    var singular = word.Substring(0, word.Length - 1);
                    if (wordsSet.Contains(singular))
                    {
                        Console.WriteLine($"   🚫 Skipping plural '{word}' because singular '{singular}' exists");
                        shouldInclude = false;
                    }
                }

                // Skip past tense -ED forms if root exists
                if (word.EndsWith("ED") && word.Length > 4)
                {
                    var root = word.Substring(0, word.Length - 2);
                    if (wordsSet.Contains(root))
                    {
                        Console.WriteLine($"   🚫 Skipping past tense '{word}' because root '{root}' exists");
                        shouldInclude = false;
                    }
                }

                // Skip gerund -ING forms if root exists
                if (word.EndsWith("ING") && word.Length > 5)
                {
                    var root = word.Substring(0, word.Length - 3);
                    if (wordsSet.Contains(root))
                    {
                        Console.WriteLine($"   🚫 Skipping gerund '{word}' because root '{root}' exists");
                        shouldInclude = false;
                    }
                }

                // Skip comparative -ER forms if root exists
                if (word.EndsWith("ER") && word.Length > 4)
                {
                    var root = word.Substring(0, word.Length - 2);
                    if (wordsSet.Contains(root))
                    {
                        Console.WriteLine($"   🚫 Skipping comparative '{word}' because root '{root}' exists");
                        shouldInclude = false;
                    }
                }

                // Skip superlative -EST forms if root exists
                if (word.EndsWith("EST") && word.Length > 5)
                {
                    var root = word.Substring(0, word.Length - 3);
                    if (wordsSet.Contains(root))
                    {
                        Console.WriteLine($"   🚫 Skipping superlative '{word}' because root '{root}' exists");
                        shouldInclude = false;
                    }
                }

                if (shouldInclude)
                {
                    filteredWords.Add(word);
                }
            }

            Console.WriteLine($"   📝 Filtered words from {words.Count} to {filteredWords.Count}");
            return filteredWords;
        }
    }
}

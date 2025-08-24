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
            await Task.Delay(1); // Make it async for UI responsiveness
            
            // Get a random word of max length
            var targetWord = GetRandomWordOfLength(settings.MaxWordLength);
            if (string.IsNullOrEmpty(targetWord))
            {
                // Fallback to any word if specific length not found
                targetWord = "PUZZLE";
            }

            // Find all subwords
            var possibleWords = FindAllSubwords(targetWord, settings.MinWordLength);
            
            // Create letter circle from target word letters
            var circleLetters = CreateCircleLetters(targetWord);

            return new PuzzleState
            {
                TargetWord = targetWord,
                PossibleWords = possibleWords,
                CircleLetters = circleLetters,
                FoundWords = new HashSet<string>(),
                CurrentGuess = ""
            };
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
            if (string.IsNullOrEmpty(guess) || guess.Length < 3)
                return false;

            return _dictionary.IsWord(guess) && 
                   CanFormWordFromLetters(guess, puzzle.TargetWord) &&
                   puzzle.PossibleWords.Contains(guess);
        }

        public bool TryAddWord(string word, PuzzleState puzzle)
        {
            if (IsValidGuess(word, puzzle) && !puzzle.FoundWords.Contains(word))
            {
                puzzle.FoundWords.Add(word);
                return true;
            }
            return false;
        }
    }
}

# WordScape Blazor WASM

A word puzzle game built with Blazor WebAssembly that implements the WordScape game concept. Players form words by selecting letters from a circular arrangement.

## Features

- **Interactive Letter Circle**: Drag around the circle to select letters and form words
- **Word Validation**: Uses DictionaryLib_Calvin_Hsia package for dictionary validation
- **Customizable Settings**: Adjust minimum and maximum word lengths (3-10 characters)
- **Persistent Settings**: Settings are saved in browser localStorage
- **Score Tracking**: Points awarded based on word length
- **Progress Tracking**: Shows found words vs total possible words
- **Responsive Design**: Works on desktop and mobile devices

## How to Play

1. **Start the Game**: Click "Play WordScape" from the home page or "New Game" in the game
2. **Form Words**: Click and drag around the letter circle to select letters in sequence
3. **Submit Words**: Click "Submit" to check if your word is valid
4. **Find All Words**: Try to find all possible words to complete the puzzle
5. **Customize**: Use the Settings panel to adjust word length preferences

## Game Rules

- Words must be at least 3 characters long (configurable)
- Letters can only be used from the available circle
- Words must be valid dictionary words
- Each letter can be used once per word
- Points are awarded based on word length (length × 10)

## Technical Details

### Architecture
- **Frontend**: Blazor WebAssembly (standalone, no server)
- **Dictionary**: DictionaryLib_Calvin_Hsia NuGet package (small dictionary)
- **Styling**: CSS Grid and Flexbox with responsive design
- **State Management**: Local component state with browser localStorage for settings

### Key Components
- `WordScapeGameService`: Core game logic and word generation
- `GameSettingsService`: Settings persistence using localStorage
- `PuzzleState`: Game state management
- `CircleLetter`: Letter positioning and selection logic

### Word Generation Algorithm
1. Select a random word of maximum length from the dictionary
2. Find all valid subwords that can be formed from the target word's letters
3. Filter words based on minimum length settings
4. Create circular letter arrangement for player interaction

## Development

### Prerequisites
- .NET 9.0 SDK
- Modern web browser

### Running the Project
```bash
dotnet restore
dotnet build
dotnet run
```

The application will be available at `http://localhost:5096`

### Project Structure
```
WordScapeBlazorWasm/
├── Models/                 # Game data models
├── Services/              # Game logic and settings services
├── Pages/                 # Razor pages/components
├── Layout/               # App layout components
├── wwwroot/              # Static web assets
└── Properties/           # App configuration
```

## Dependencies

- Microsoft.AspNetCore.Components.WebAssembly (9.0.2)
- DictionaryLib_Calvin_Hsia (1.0.7)

## Browser Compatibility

- Chrome 80+
- Firefox 78+
- Safari 13+
- Edge 80+

## License

This project is for educational purposes. The DictionaryLib_Calvin_Hsia package is used under its respective license terms.

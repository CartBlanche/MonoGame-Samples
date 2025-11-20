# Currency and Auto-Language Detection Feature

## Summary

Implemented automatic OS language detection on first launch and configurable currency symbols for the Blackjack game.

## Features Implemented

### 1. Auto-Detect OS Language on First Launch

**How it works:**
- When the game launches for the first time (no settings file exists), it automatically detects the operating system's language
- Sets both the game language and default currency based on OS language
- Falls back to English + $ if OS language is not supported

**Language → Currency Mapping:**
- **English** (en) → $ (US Dollar)
- **Français** (fr) → € (Euro)
- **Español** (es) → € (Euro)
- **Italiano** (it) → € (Euro)
- **日本語** (ja) → ¥ (Yen)
- **中文** (zh) → ¥ (Yuan/Yen)
- **Russian** (ru) → ₽ (Ruble) - *Language fallback to English until Russian translation is added*
- **Other languages** → English + $

**Code Location:** [GameSettings.cs:166-215](Core/Game/Misc/GameSettings.cs#L166-L215)

### 2. Configurable Currency Symbol

**Available Currencies:**
- $ (Dollar)
- € (Euro)
- £ (Pound)
- ¥ (Yen/Yuan)
- ₽ (Ruble)

**How to change:**
- Go to Settings → Display → Currency
- Use arrow keys or click to cycle through currencies
- Changes are saved immediately

**Code Locations:**
- Settings property: [GameSettings.cs:38](Core/Game/Misc/GameSettings.cs#L38)
- UI implementation: [SettingsScreen.cs:129-132, 234-260](Core/Game/Screens/SettingsScreen.cs#L129-L132)
- Display usage: [BetGameComponent.cs:404, 408](Core/Game/Blackjack/Misc/BetGameComponent.cs#L404)

### 3. Localized Currency Labels

The "Currency" label in settings is translated to all supported languages:
- **English:** Currency
- **Français:** Devise
- **Español:** Moneda
- **Italiano:** Valuta
- **日本語:** 通貨
- **中文:** 货币

## Technical Implementation

### Settings Persistence

The currency symbol is saved in the game settings JSON file along with other preferences:

```json
{
  "Language": "English",
  "Theme": "Red",
  "Currency": "$",
  ...
}
```

### First Launch Detection

First launch is detected by checking if the settings file exists:
```csharp
if (File.Exists(settingsFilePath))
{
    // Load existing settings
}
else
{
    // First launch - auto-detect OS language
    DetectAndSetOSLanguage();
}
```

### Balance Display

All balance and bet amount displays now use the configured currency symbol:

**Before:**
```csharp
spriteBatch.DrawString(font, "$" + player.Balance.ToString(), ...);
```

**After:**
```csharp
spriteBatch.DrawString(font, GameSettings.Instance.Currency + player.Balance.ToString(), ...);
```

## User Experience

### First-Time User Flow

1. User launches game for the first time
2. Game detects OS language (e.g., French)
3. Game automatically sets:
   - Language: Français
   - Currency: €
4. Settings are saved
5. All in-game text appears in French
6. All money amounts show € symbol

### Changing Currency

1. User goes to Settings
2. Navigates to "Currency" option
3. Cycles through: $ → € → £ → ¥ → ₽ → $ (loops)
4. New currency is immediately saved
5. Returns to game - all balances now show the new symbol

## Console Logging

For debugging, the feature logs important events:

```
[Settings] Created default settings (first launch)
[Settings] Detected OS language: en (en-US)
[Settings] Detected language: English, Currency: $
[Settings] Saved to /path/to/settings.json
```

Or when loading existing settings:

```
[Settings] Loaded from /path/to/settings.json
[Settings] PersistWinnings=True, SavedPlayerBalance=500
```

## Files Modified

### Core Settings
- `Core/Game/Misc/GameSettings.cs` - Added Currency property, auto-detection logic
- `Core/Game/Screens/SettingsScreen.cs` - Added Currency UI control and cycle methods

### Localization
- `Core/Game/Resources.resx` - English "Currency"
- `Core/Game/Resources.es.resx` - Spanish "Moneda"
- `Core/Game/Resources.fr.resx` - French "Devise"
- `Core/Game/Resources.it.resx` - Italian "Valuta"
- `Core/Game/Resources.ja.resx` - Japanese "通貨"
- `Core/Game/Resources.zh.resx` - Chinese "货币"
- `Core/Game/Resources.Designer.cs` - Generated property accessor

### Display Logic
- `Core/Game/Blackjack/Misc/BetGameComponent.cs` - Updated balance/bet displays to use `GameSettings.Instance.Currency`

## Testing

To test the auto-detection:
1. Delete your settings file (location shown in console logs)
2. Launch the game
3. Check console for detected language
4. Verify currency symbol matches expected default
5. Check that language UI is in the detected language

To test currency changes:
1. Go to Settings → Display → Currency
2. Cycle through all 5 currency options
3. Return to gameplay
4. Verify all balance amounts show the selected currency

## Future Enhancements

Potential improvements:
- Add Russian translation files to support ru → ₽ language pairing
- Add more currencies (₹ Rupee, ₣ Franc, etc.)
- Allow custom currency text input
- Apply culture-specific number formatting (1,000.00 vs 1.000,00)

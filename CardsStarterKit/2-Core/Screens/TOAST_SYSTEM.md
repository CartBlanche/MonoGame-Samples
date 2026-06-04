# Toast System (Shared)

This document describes the shared toast pipeline in `Cards.Framework.Core`.

## Core Types

- `ToastNotificationManager`: queue/update/draw/input handling.
- `ToastNotification`: payload for one toast item.
- `ToastVisualStyle`: per-category style override.

Primary implementation lives in:

- `2-Core/Screens/ToastNotificationManager.cs`

## Quick Start

1. Construct the manager once from your game class:

```csharp
_toasts = new ToastNotificationManager(GraphicsDevice, _screenManager);
```

2. Register category styles (optional):

```csharp
_toasts.SetStyle("achievement", new ToastVisualStyle
{
    FrameColor = new Color(244, 202, 112),
    TopFillColor = new Color(33, 24, 12, 230),
    BottomFillColor = new Color(17, 12, 8, 220),
    ShowAccentBar = true,
    AccentColor = new Color(255, 198, 104),
    AccentHighlightColor = new Color(255, 224, 146),
});
```

3. Enqueue notifications from producers/adapters:

```csharp
_toasts.Enqueue(new ToastNotification
{
    Identifier = achievementKey,
    Category = "achievement",
    Header = Resources.AchievementUnlockedHeader,
    Title = displayName,
    Subtitle = subtitle,
    IconTexture = fallbackIcon,
});
```

4. Update per frame:

```csharp
_toasts.Update(gameTime);
```

5. Forward input to activation handlers:

```csharp
if (_toasts.HandleMouseRelease(point)) { /* consumed */ }
if (_toasts.HandleTap(tapPosition)) { /* consumed */ }
```

6. Draw after screen/game draw pass:

```csharp
_toasts.Draw();
```

## Adapter Pattern (Recommended)

Keep domain-specific logic out of `ToastNotificationManager`.

- Domain layer emits events (achievements, network join/leave, etc.).
- A game-specific adapter maps domain events to `ToastNotification` payloads.
- Adapter handles icon lookup, localization, click actions, and queue policies.

This keeps the shared system stable and reusable across games.

## Async Icon Bytes

If icon bytes are loaded asynchronously (for example from platform APIs):

1. Set `PendingIconBytes` on the target `ToastNotification`.
2. Do not create textures on worker threads.
3. `ToastNotificationManager.Update` promotes bytes into `Texture2D` on the game thread.

## Categories and Styling

Suggested categories:

- `achievement`
- `network`
- `system`

Each can have independent frame/fill/accent colors while sharing animation/layout behavior.

## Notes

- `ToastTopOffset` can be customized per game.
- Current manager displays up to two active toasts at once.
- Keep producers small and testable by using adapters.

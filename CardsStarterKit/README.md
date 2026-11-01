# Card Game Starter Kit

This sample is a fully-functional Blackjack card game for Windows, macOS, Linux, iOS and Android that is built on top of an easily-extensible card game and animation framework you can use to build your own card games.

## How the Sample Works

The full game of Blackjack that can be built and played from this sample is built from a number of different underlying components, each of which can be separated out and extended to create new functionality:

- Classes from the GameStateManagement sample to handle the loading, updating, and transitioning of screens.
- The **AnimatedGameComponent** class, inherited from **DrawableGameComponent**, to handle animating the position, scale, and text drawing of in-game objects.
- The Cards Framework, represented primarily by the **CardGame**, **CardPacket**, **Player**, **GameTable**, and **GameRule** classes, that provide generic card game functionality and are extended by the Blackjack classes to provide the specific game logic.

Upon starting the game, the **GameplayScreen** creates the **BlackjackCardGame**, passing in a **BlackJackTable** and initializing **BlackjackPlayers**. Per-frame, the **BlackjackCardGame** handles the input per-frame via the **GameScreen.HandleInput** method, and passed via the **InputState** object to all of the other classes it manages.

The various phases of gameplay are handled by a state machine inside **BlackjackCardGame**, deciding when to start dealing, when to add and resolve rules, and when to start a new round, based primarily on user interactions with the various input buttons, which are **AnimatedGameComponent** objects.

Drawing is handled by the objects that inherit from **AnimatedGameComponent**. Each object is responsible for drawing itself and updating its animation states per-frame. Many of the objects, including the **GameTable**, derive from **AnimatedGameComponent**.

## Extending the Sample

If you are interested in creating a different type of card game, extending the Cards Framework is a good place to start:

- Create a new class that inherits from **CardsGame**.
- Create a new class that inherits from **GameTable**.
- Create a new player class that inherits from **Player**.
- Create any new **GameRule** classes you need to judge card values specific to your game.

You can then modify the **GameplayScreen.Initialize** method to call your custom **CardsGame.Initialize**, add the necessary **Player** objects, and start a round of play.

---

## Pointer and gestures

`InputState` also gives every game one pointer that works the same on desktop and touch, and the gestures a card game needs. It is additive: everything that was in `InputState` still works.

- **One pointer.** `PointerPosition`, `PointerDown`, `PointerPressed` and `PointerReleased` (with `PointerPressPosition`, `PointerReleasePosition` and `PointerSource`) combine the left mouse button and the primary touch, in the game's base-resolution space. The primary touch is the first finger down, followed by its id so a second finger cannot take over, and the position follows a finger as it moves (the older `CurrentCursorLocation` only updates when a finger lands).
- **Gestures.** `PointerGestureEvents` lists what happened this frame, in order: `Click`, `DoubleClick`, `DragStart`, `DragMove`, `DragEnd`, `DragCancel`, `LongPress`. A press that moves past the drag threshold is a drag and never a click; a press held still is a long-press and never a click; a double-click is two clicks close in time and place. Thresholds are in `PointerGestures.Options` (a finger gets a larger drag threshold and double-click radius than a mouse).
- **Cancelling.** Call `CancelPointerGesture(gameTime)` when something should abandon a drag in progress, such as a dialog opening or the window losing focus.
- **Tests.** The recognizer (`PointerGestureRecognizer`) and the device merge (`PointerTracker`) are plain classes with no device access, tested in `Tests/Cards.Framework.Tests` with scripted sequences on a fake clock. Run `dotnet test Tests/Cards.Framework.Tests`.

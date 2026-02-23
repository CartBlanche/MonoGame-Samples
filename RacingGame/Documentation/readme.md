# Racing Game Starter Kit

This topic contains the following sections.

- [Introduction to Racing Game](#introduction-to-racing-game)
- [Goals](#goals)
- [Minimum Shader Profile](#minimum-shader-profile)
- [Getting Started](#getting-started)
- [Playing Racing Game](#playing-racing-game)
- [Racing Game Known Issues](#racing-game-known-issues)

---

## Introduction to Racing Game

The Racing Game Starter Kit is a complete XNA Game Studio game. The project comes ready to compile and run. It's easy to customize with a little bit of C# programming. You are free to use the source code as the basis for your own XNA Game Studio game projects, and to share your work with others.

Racing Game is a 3D auto racing game that features advanced graphics, audio, and input processing. Race around the track and try to beat the ghost car to achieve the best time.

> **Note:** This documentation assumes that you have a basic knowledge of programming concepts and the Visual C# environment. You can learn more about these topics in the product documentation by clicking one of the **Help** menu items, or by positioning the mouse cursor on language keywords or user interface elements such as windows or dialog boxes, and then pressing F1.

---

## Goals

This starter kit provides a complete XNA Game Studio game, including source code and game content such as models, textures, and sounds. This starter kit documentation describes the general layout and controls for Racing Game. The Racing Game Starter Kit demonstrates the following:

- Advanced graphics using shaders and post-processing effects
- Game state management through UI and gameplay screens
- Simple automobile physics, including collision detection

---

## Minimum Shader Profile

- Vertex Shader Model 2.0
- Pixel Shader Model 2.0

---

## Getting Started

Follow these procedures to get started.

### To build and run the Racing Game project

- Press **F5** or, on the **Debug** menu, click **Start Debugging**.

The project will build and then run within the debugger.

### To build the project without running it

- Press **F6** or, on the **Build** menu, click **Build Solution**.

The project will build without running.

When Racing Game starts, it will begin in Demo mode. To start the game, press the **START** button on your Xbox 360 Controller. This will bring you to the **Main Menu**.

---

## Playing Racing Game

### Racing Game Controls

You can use a keyboard, Xbox 360 Controller, or a mouse (Windows-only) to play Racing Game. The controls are mapped as follows:

| Action | Controller | Keyboard | Mouse |
|--------|------------|----------|-------|
| Start the game. | **START** | SPACEBAR | Left mouse button |
| Exit the game. | **BACK** | ESC | |
| Turn the car. | Left stick | LEFT ARROW, RIGHT ARROW, or **A**, **D** | Move mouse left or right |
| Accelerate the car. | Right trigger, or **A** | UP ARROW, or **W** | Left mouse button |
| Brake the car. | Left trigger, or **B** | DOWN ARROW, or **S** | Right mouse button |
| Zoom the camera in. | **X** | PAGE UP | Mouse wheel |
| Zoom the camera out. | **Y** | PAGE DOWN | Mouse wheel |
| Show or hide the FPS counter. | Left bumper + **Y** | F1 | |
| Toggle full-screen mode (Windows-only). | | ALT+ENTER | |

---

## Racing Game Known Issues

This section identifies some of the known issues you may encounter when playing Racing Game.

- Some Intel graphics adapters may report to Racing Game incorrectly that they support Shader Model 2.0. As a result, the game may render poorly.
- Shadows will incorrectly change orientation depending on the orientation of the camera.
- The player's vehicle interacts incorrectly with the loop obstacles on the track. The vehicle may sink below the level of the track, and will not fall off a loop when upside down.
- The player's vehicle may interact incorrectly when colliding with guard rails. The vehicle may bounce or change position abruptly.
- Players cannot change their name in the **Options** screen by using an Xbox 360 Controller. Players need to use a keyboard.
- The player cannot pause the game. Pressing **BACK** ends the current game and returns to the **Main Menu**.
- The screen may flicker on some Windows-based computers when running a debug build of the game. To address this, find the following line of code in the **BaseGame** constructor (`Graphics\BaseGame.cs`, approximately line 1026):

  ```csharp
  graphicsManager.SynchronizeWithVerticalRetrace = false;
  ```

  Change the value of the property to `true`, like this:

  ```csharp
  graphicsManager.SynchronizeWithVerticalRetrace = true;
  ```

---

*© 2008 Microsoft Corporation. All rights reserved.*  
*Send feedback to [xnags@microsoft.com](mailto:xnags@microsoft.com?subject=Documentation%20Feedback:%20Racing%20Game%20Starter%20Kit).*

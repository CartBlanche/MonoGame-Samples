# Flocking Sample

This sample demonstrates how AIs can use simple rules to move together and create complex behaviors.

## Sample Overview

When programming the AI for your game, you often want your actors to move and react together without having to behave identically. For example, you might want to simulate a school of fish that all swim together without a centralized control or a battalion of soldiers that can march together in formation around obstacles.

This sample demonstrates some of these behaviors. The sample has a flock of birds that fly to, and in the same direction as, other birds they see nearby. The sample also has a cat that you can turn on and who then chases the birds as they run away.

This sample is based on the Chase and Evade sample, and assumes that the reader is familiar with the code and concepts explained in that sample.

## Building and Running

This project supports the following platforms:

- **Windows** (`Platforms/Windows`)
- **DesktopGL** (`Platforms/Desktop`)
- **Android** (`Platforms/Android`)
- **iOS** (`Platforms/iOS`)

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [MonoGame 3.8.*](https://www.monogame.net/)
- For Android/iOS: Visual Studio 2022+ with Xamarin/MAUI workloads, or JetBrains Rider

### Build and Run (VS Code)

1. Open the root folder in VS Code.
2. Use the built-in tasks (Ctrl+Shift+B or `Terminal > Run Task...`) to build or run:
   - `build-windows`, `run-windows`
   - `build-desktopgl`, `run-desktopgl`
   - `build-android`, `run-android`
   - `build-ios`
3. Use the launch configurations in `.vscode/launch.json` to debug Windows or DesktopGL.

### Build and Run (Visual Studio)

1. Open `Flocking.sln` in Visual Studio.
2. Set the desired platform project as the startup project.
3. Build and run as usual.

#### Notes
- Android and iOS require appropriate emulators or devices and platform SDKs.
- Content is pre-built as `.xnb` files and does not require a content pipeline build step.

### Controls

| Action                                 | Windows Phone                        | Windows - Keyboard Control | Windows/Xbox - Gamepad Control         |
|----------------------------------------|--------------------------------------|---------------------------|----------------------------------------|
| Select the tuning parameter.           | **DRAG** tuning bar                  | UP ARROW, DOWN ARROW      | D-Pad Up and Down                      |
| Increase/decrease the tuning parameter.| **DRAG** tuning bar                  | LEFT ARROW, RIGHT ARROW   | D-Pad Left and Right, Left/Right Triggers |
| Reset the bird flock.                  | **TAP** "Reset Flock" button         | X                        | X                                      |
| Reset the tuning parameters.           | **TAP** "Reset Distance" button      | B                        | B                                      |
| Add/remove the cat                     | **TAP** "Add/Remove Cat" button      | Y                        | Y                                      |
| Move the cat.                          | **TAP** or **DRAG** on screen        | W, S, A, D                | Left Thumbstick                        |
| Exit the game.                         | **BACK**                             | ESC or ALT+F4             | **BACK**                               |

## How the Sample Works

### Flocking Behavior

Flocking behavior is controlled by three simple behaviors: cohesion, alignment, and separation. Other behaviors can be present, but they are not required. In this sample, the birds also have a flee behavior.

- **Cohesion**: Each bird flies towards others it can see. For each other bird inside its `detectionDist` value, the bird changes its `direction` towards the other bird in proportion to its `moveInFlockDirInfluence` setting and according to how close it is to the midpoint between its `detectionDist` and `separationDist` values.

- **Alignment**: Each bird flies in the general direction of others it can see. For each other bird inside its `detectionDist` value, the bird adds the `direction` the other bird is facing to its own `direction` in proportion to its `moveInFlockDirInfluence` setting.

- **Separation**: Each bird flies away from others that are too close. For each other bird inside both its `detectionDist` *and* its `separationDist` values, the bird applies the separation rule instead of the cohesion rule. To move one bird a comfortable distance away from another, the bird adds the opposite of the direction towards the other bird's direction to its `direction` in proportion to its `moveInFlockDirInfluence` setting and according to the ratio of how close the other bird is relative to its `separationDist` value.

- **Flee**: Each bird flies away from the cat if the bird can see the cat. If the cat is inside the bird's `detectionDist` and the bird isn't already moving away from the cat, the bird adds the opposite of the direction towards the cat to its `direction` value.
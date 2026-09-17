# Pathfinding Sample - MonoGame 3.8.* Sample

This sample demonstrates how AIs can use algorithms to navigate a map using waypoints.

## Project Overview

The Pathfinding sample shows how enemy movement can be driven by graph search over a set of waypoints. It includes three classic algorithms for comparison:

- Breadth-First search
- Best-First search
- A* pathfinding

The sample visualizes the search process with colored nodes and moves a tank along the resulting route once a path has been found.

## Project Structure

The project has been migrated to a modern SDK-style MonoGame layout:

- `Core/PathfindingSample.Core.csproj` - Shared game logic project
- `Core/PathfindingSample.cs` - Main game class and update/draw loop
- `Core/Map.cs` - Map management, runtime XML loading, and waypoint layout
- `Core/PathFinder.cs` - Search algorithms and path construction
- `Core/Tank.cs` - Tank movement along the generated route
- `Core/WaypointList.cs` - Waypoint definitions and helpers
- `Core/Content/Content.mgcb` - MonoGame content pipeline for textures and the HUD font
- `Core/Content/Map*.xml` - Runtime map definitions used by the sample
- `Platforms/Desktop/` - DesktopGL entry point and project
- `Platforms/Windows/` - WindowsDX entry point and project
- `Platforms/Android/` - Android entry point and project
- `Platforms/iOS/` - iOS entry point and project

## Supported Platforms

This sample now targets the current MonoGame project flow:

- Windows
- DesktopGL
- Android
- iOS

## Controls

| Action | Windows / Desktop Keyboard | Windows / Desktop Gamepad | Android / iOS Touch |
| --- | --- | --- | --- |
| Start the pathfinding algorithm | A | A | Tap "Start/Stop" button |
| Reset the map | B | B | Tap "Reset" button |
| Switch the map | Y | Y | Tap "Next Map" button |
| Switch the pathfinding algorithm | X | X | Tap "Pathfinding Mode:" button |
| Change the time step | Left Arrow, Right Arrow | D-Pad Left, D-Pad Right | Drag the "Time Step" slider |
| Exit the game | ESC or ALT+F4 | Back | Platform back action |

## How the Sample Works

### Pathfinding Behavior

All of the pathfinding algorithms below rely on searching a graph of nodes, named waypoints for the purposes of this sample, to get from a start point to a goal. The algorithms described below can be useful in different situations, so it is important to understand how your algorithm will be used. Adding cost to your nodes is a prime example. For instance, if traveling over mountains resulted in slower movement than traveling over a plain, the cost of a particular mountain node might be 5 while a plain node would be 1. This lets certain pathfinding algorithms determine whether it is more efficient to go over the mountain or around it.

#### Breadth-First

The Breadth-First algorithm explores all nodes neighboring a given root node, then searches the neighbors of those neighbors, continuing until the goal is found or all available nodes have been searched. This is useful if you want to exhaustively search the map for every possible path, but it is also the slowest implementation.

#### Best-First

The Best-First algorithm searches through the node graph in an attempt to find the shortest distance to the goal. It will always find the shortest distance to the goal, but it does not take node cost into account. Best-First is best used in scenarios where nodes have a uniform cost.

#### A*

A* pathfinding combines Best-First search with a heuristic, or educated guess, to search for the lowest-cost route. A* will always return the route with the lowest total cost to the goal. This is most useful when nodes have varying costs associated with them.

## Extending the Sample

- Add cost to different nodes to support multiple terrain types.
- Add the ability to move the goal at runtime and have the path change dynamically.
- Add additional algorithms.

## Content Notes

The sample includes its textures, HUD font, and map XML files under `Core/Content/`. Map data is loaded at runtime from the XML files rather than through the old XNA content pipeline.

## Troubleshooting

- If you are expecting the legacy XNA project files, those are still present for reference, but they are no longer part of the active build path.
- If content fails to load, verify the XML map files and source textures are still present in `Core/Content/`.
- The desktop build has been validated on this workspace; other platforms may still need platform-specific verification.

## License

This sample is based on Microsoft XNA Community Game Platform samples and is provided for educational purposes.

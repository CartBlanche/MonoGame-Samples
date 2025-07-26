# Shatter Effect Sample

This sample shows how you can apply an effect on any model in your game to shatter it apart. The effect is simulated with a vertex shader.

## Sample Overview

The shatter effect operates independently on every triangle in the model. For every triangle in the model, the vertex shader rotates the vertices of the triangle around the x, y, and z axes by random velocities. At the same time, the triangle is translated along its normal. This creates the appearance of the entire model shattering outwards into small pieces. To get this effect to work properly, the model must be processed beforehand with a custom processor that derives from [ModelProcessor](http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.content.pipeline.processors.modelprocessor.aspx).

### Sample Controls

This sample uses the following keyboard and gamepad controls:

| Action                     | Keyboard control | Gamepad control |
|----------------------------|------------------|-----------------|
| Shatter the model          | UP ARROW         | **A**           |
| Reverse the shatter effect | DOWN ARROW       | **B**           |
| Exit the sample            | ESC or ALT+F4    | **BACK**        |

## How the Sample Works

The shatter effect is achieved with the help of both a processor and an effect.

### Build Time Processing

The processor, `ShatterProcessor`, is derived from **ModelProcessor** and overrides the **Process** method. The processor manipulates the model’s data in three ways before passing it to **ModelProcessor** to produce the processor’s final output.

#### 1.  Break down the model’s triangles

This step reverses the effect of indexing a model’s vertices to break up the triangles. While it will increase the number of vertices for the model on the GPU, this is required so that we can independently manipulate every triangle without affecting nearby ones. An indexed model does not give us such freedom.

#### 2.  Calculate the center point of every triangle in the model

Once the model’s triangles have been broken down, the processor calculates the center point of every triangle using the formula:

```
TriangleCenter = (Vertex_1 + Vertex_2 + Vertex_3) / 3
```

The triangle centers are then saved in a `Vector3[]` array. For every triangle in the model, the triangle center is used in the vertex shader as the point to rotate the triangle around.

#### 3.  Create two vertex channels for the model

Now that the triangle centers have been calculated, the processor creates a per-vertex channel to store them. For every three vertices, we store the triangle center that corresponds to the triangle they form. The channel is called `triangleCenterChannel`. This channel adds a `float3 TEXCOORD1` field to the vertex declaration for the vertex shader to use. The channel is created with the help of a helper function called `ReplicateTriangleDataToEachVertex`. This function uses the `Vector3[]` array created earlier and returns the corresponding center for every vertex in the model.

The second channel the processor creates is called `rotationalVelocityChannel`. This channel creates a `float3 TEXCOORD2` field in the vertex declaration for the model. For every vertex, the processor generates a random set of x, y, and z velocities in the range of –1 to 1. The vertex shader uses these values to determine how to rotate each vertex as the model is shattered.

The processor then passes the newly created model to the parent processor, **ModelProcessor**, to complete processing it.

## Extending the Sample

The sample can be extended in a variety of ways:

- The effect could simulate wind drag for every triangle based on its size, so that bigger triangles fall a bit slower than smaller ones. Create another vertex channel containing information about triangle size and precalculate the channel at processing time.
- Group together chunks of triangles that are close to each other and rotate/translate them as a single entity. This gives the illusion of chunks of the model breaking versus smaller pieces.

---

## Project Structure
- `Core/` — Shared game logic and content pipeline files
- `Core/Content/` — Prebuilt .xnb content and related assets
- `Platforms/Windows/` — Windows-specific project (net8.0-windows)
- `Platforms/DesktopGL/` — DesktopGL cross-platform project (net8.0)
- `Platforms/Android/` — Android project (net8.0-android)
- `Platforms/iOS/` — iOS project (net8.0-ios)

## Building and Running

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MonoGame 3.8.*](https://www.monogame.net/)
- Visual Studio 2022+ or VSCode with C# Dev Kit

### Windows & DesktopGL
- Open the solution in Visual Studio and set the desired platform project as startup, or
- In VSCode, use the provided `launch.json` and `tasks.json` to build and run:
  - Press `Ctrl+Shift+B` to build (choose `build-windows` or `build-desktopgl`)
  - Press `F5` to launch

### Android & iOS
- Build using Visual Studio or `dotnet build` from the command line:
  - `dotnet build Platforms/Android/ShatterEffectSample.Android.csproj`
  - `dotnet build Platforms/iOS/ShatterEffectSample.iOS.csproj`
- Deploy to device/emulator as per your platform requirements.

## Notes
- No Content.mgcb file is used; the game loads .xnb files directly from the `Content/` folder.
- Platform-specific code is separated to avoid `#if` blocks.
- Only Windows, DesktopGL, Android, and iOS are supported. Linux, MacOS, and PSMobile are not supported in MonoGame 3.8.*.

---
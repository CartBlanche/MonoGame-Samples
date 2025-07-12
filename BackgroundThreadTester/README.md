# Background Thread Tester - MonoGame 3.8.4

This project demonstrates background thread management in MonoGame applications.

## Project Structure

This project has been modernized to use SDK-style projects with MonoGame 3.8.4 NuGet packages:

- **BackgroundThreadTester.Windows.csproj** - Windows DirectX version (net8.0-windows)
- **BackgroundThreadTester.DesktopGL.csproj** - Cross-platform OpenGL version (net8.0)
- **BackgroundThreadTester.Android.csproj** - Android version (net8.0-android)
- **BackgroundThreadTester.iOS.csproj** - iOS version (net8.0-ios)

## Building and Running

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

### Build Commands
```bash
# Windows version
dotnet build BackgroundThreadTester.Windows.csproj

# DesktopGL version
dotnet build BackgroundThreadTester.DesktopGL.csproj

# Android version (requires Android SDK)
dotnet build BackgroundThreadTester.Android.csproj
```

### Run Commands
```bash
# Windows version
dotnet run --project BackgroundThreadTester.Windows.csproj

# DesktopGL version
dotnet run --project BackgroundThreadTester.DesktopGL.csproj
```

### VS Code Support
The project includes `.vscode/tasks.json` and `.vscode/launch.json` for building and debugging in VS Code.

**Available VS Code configurations:**
- **Launch Windows** - Debug Windows version with internal console
- **Launch DesktopGL** - Debug DesktopGL version with internal console  
- **Launch Windows (External Console)** - Run Windows version with external terminal
- **Launch DesktopGL (External Console)** - Run DesktopGL version with external terminal

**Quick Start:**
1. Press `F5` to start debugging
2. Select a launch configuration
3. Set breakpoints by clicking in the left margin

📖 **Troubleshooting**: See `VSCODE_LAUNCH_GUIDE.md` for detailed setup and troubleshooting instructions.

### Visual Studio Support
Open `BackgroundThreadTester.sln` in Visual Studio 2022.

## Features

- **Modern Async Background Tasks** - Uses Task-based async/await patterns instead of raw Thread
- **Cross-Platform Thread Safety** - Proper thread marshaling for UI operations across all platforms
- **Cancellation Support** - Background tasks can be cancelled cooperatively using CancellationToken
- **Error Handling** - Comprehensive exception handling with try/catch blocks
- **Non-Blocking Operations** - Uses Task.Delay() instead of Thread.Sleep() for better responsiveness
- **Thread-Safe Component Management** - Uses locks for safe component addition from background threads
- **Resource Cleanup** - Proper disposal of background task resources
- Cross-platform compatibility
- Uses existing .xnb content files (no .mgcb pipeline needed)

## Modern Threading Patterns Used

This project demonstrates modern .NET threading best practices:

### ✅ Task-Based Async Pattern
```csharp
// Modern approach using async/await
private async Task CreateBackgroundTaskAsync(CancellationToken cancellationToken)
{
    await Task.Delay(2000, cancellationToken); // Non-blocking delay
}
```

### ✅ Cooperative Cancellation
```csharp
// Cancellation token support
cancellationToken.ThrowIfCancellationRequested();
```

### ✅ Thread-Safe Operations
```csharp
// Thread-safe component addition
lock (_componentLock)
{
    Components.Add(testTexture);
}
```

### ✅ Exception Handling
```csharp
try {
    // Background work
} catch (OperationCanceledException) {
    // Handle cancellation
} catch (Exception ex) {
    // Handle other errors
}
```

### ❌ Legacy Patterns (Avoided)
- Direct Thread usage with Thread.Start()
- Thread.Sleep() blocking calls
- Unsafe cross-thread operations
- No cancellation support
- No error handling

## Original Description

Sample originally created by CircleOf14 and modified by Kenneth Pouncey to create new textures to be added
to the game components dynamically in the background.

Of special interest look at the following methods:

- `CreateBackgroundThread()` - Creates a modern async background task with cancellation support
- `CreateBackgroundTaskAsync()` - **NEW** Modern async worker using Task-based patterns with proper error handling
- `BackgroundWorkerThread()` - Legacy method (now redirects to modern implementation for compatibility)

### Key Modern Improvements:
1. **Async/Await Pattern**: Uses `Task.Delay()` instead of `Thread.Sleep()` for non-blocking operations
2. **Cancellation Support**: All background operations can be cancelled using `CancellationToken`
3. **Thread Safety**: Components are added using thread-safe locking mechanisms
4. **Error Handling**: Comprehensive exception handling for robust operation
5. **Resource Management**: Proper cleanup and disposal of background task resources
6. **Cross-Platform**: Works consistently across Windows, DesktopGL, Android, and iOS

Make sure to read the comments in these two methods.

## MonoGame Version
This project uses MonoGame 3.8.* NuGet packages for better dependency management and cross-platform support.


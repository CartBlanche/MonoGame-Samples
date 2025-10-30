using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Numerics;

namespace Shooter.Core.Services;

/// <summary>
/// Input service for handling keyboard, mouse, and gamepad input.
/// 
/// UNITY COMPARISON:
/// This replaces Unity's Input class (old input system) and provides similar functionality.
/// Unity 2019+ has a new Input System package, but the old Input class works like this:
/// - Input.GetKey() = IsKeyDown()
/// - Input.GetKeyDown() = IsKeyPressed()
/// - Input.GetAxis("Mouse X") = MouseDelta.X
/// - Input.mousePosition = MousePosition
/// 
/// EDUCATIONAL NOTE:
/// Unlike Unity which polls input automatically, MonoGame requires you to explicitly
/// call Update() each frame to capture the current input state. This gives you more
/// control but also more responsibility.
/// </summary>
public class InputService : IInputService
{
    private KeyboardState _currentKeyboardState;
    private KeyboardState _previousKeyboardState;
    
    private MouseState _currentMouseState;
    private MouseState _previousMouseState;
    
    private GamePadState _currentGamePadState;
    private GamePadState _previousGamePadState;
    
    private bool _isMouseLocked;
    private Point _screenCenter;
    
    /// <summary>
    /// Current mouse position in screen coordinates.
    /// Similar to Input.mousePosition in Unity.
    /// </summary>
    public System.Numerics.Vector2 MousePosition => new(
        _currentMouseState.X,
        _currentMouseState.Y
    );
    
    /// <summary>
    /// Mouse movement delta since last frame.
    /// Normalized to screen dimensions for resolution-independent sensitivity.
    /// 
    /// EDUCATIONAL NOTE:
    /// In Unity, you'd use Input.GetAxis("Mouse X") and Input.GetAxis("Mouse Y").
    /// MonoGame gives you raw delta values without Unity's smoothing/acceleration.
    /// We normalize by screen dimensions to make sensitivity values consistent
    /// across different resolutions (e.g., 800x600 vs 1920x1080).
    /// 
    /// This means a sensitivity of 1.0 will rotate ~1 degree per pixel of movement
    /// relative to screen width/height.
    /// </summary>
    public System.Numerics.Vector2 MouseDelta
    {
        get
        {
            if (!_isMouseLocked)
                return System.Numerics.Vector2.Zero;
            
            // Get raw pixel delta from screen center
            float rawDeltaX = _currentMouseState.X - _screenCenter.X;
            float rawDeltaY = _currentMouseState.Y - _screenCenter.Y;
            
            // Normalize by screen dimensions for resolution independence
            // This gives values roughly in the range of -1 to 1 for full screen movement
            float normalizedX = rawDeltaX / _screenCenter.X;
            float normalizedY = rawDeltaY / _screenCenter.Y;
                
            return new System.Numerics.Vector2(normalizedX, normalizedY);
        }
    }
    
    /// <summary>
    /// Mouse scroll wheel delta.
    /// Positive = scroll up, Negative = scroll down.
    /// Similar to Input.GetAxis("Mouse ScrollWheel") in Unity.
    /// </summary>
    public float MouseScrollDelta => 
        _currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
    
    /// <summary>
    /// Whether the mouse cursor is locked to the center of the screen.
    /// Used for FPS camera control.
    /// 
    /// UNITY COMPARISON:
    /// Unity uses Cursor.lockState = CursorLockMode.Locked
    /// MonoGame requires manual re-centering each frame.
    /// </summary>
    public bool IsMouseLocked
    {
        get => _isMouseLocked;
        set
        {
            _isMouseLocked = value;
            if (value)
            {
                Mouse.SetPosition(_screenCenter.X, _screenCenter.Y);
            }
        }
    }
    
    public void Initialize(Point screenCenter)
    {
        _screenCenter = screenCenter;
        _currentKeyboardState = Keyboard.GetState();
        _currentMouseState = Mouse.GetState();
        _currentGamePadState = GamePad.GetState(PlayerIndex.One);
    }
    
    /// <summary>
    /// Update input state. Must be called every frame before checking input.
    /// 
    /// EDUCATIONAL NOTE:
    /// Unity does this automatically in its game loop.
    /// In MonoGame, you control when state is captured, giving you flexibility
    /// for things like input recording/playback or frame-perfect input.
    /// </summary>
    public void Update()
    {
        // Store previous state for "pressed this frame" detection
        _previousKeyboardState = _currentKeyboardState;
        _previousMouseState = _currentMouseState;
        _previousGamePadState = _currentGamePadState;
        
        // Capture current state
        _currentKeyboardState = Keyboard.GetState();
        _currentMouseState = Mouse.GetState();
        _currentGamePadState = GamePad.GetState(PlayerIndex.One);
        
        // Re-center mouse if locked (for FPS controls)
        if (_isMouseLocked)
        {
            Mouse.SetPosition(_screenCenter.X, _screenCenter.Y);
        }
    }
    
    #region Keyboard Input
    
    /// <summary>
    /// Check if a key is currently held down.
    /// Similar to Input.GetKey(KeyCode) in Unity.
    /// </summary>
    public bool IsKeyDown(Keys key) => _currentKeyboardState.IsKeyDown(key);
    
    /// <summary>
    /// Check if a key was pressed this frame (wasn't down last frame, is down this frame).
    /// Similar to Input.GetKeyDown(KeyCode) in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// This is crucial for single-shot actions like jumping or shooting.
    /// Without comparing to previous state, holding the key would trigger every frame.
    /// </summary>
    public bool IsKeyPressed(Keys key) =>
        _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    
    /// <summary>
    /// Check if a key was released this frame.
    /// Similar to Input.GetKeyUp(KeyCode) in Unity.
    /// </summary>
    public bool IsKeyReleased(Keys key) =>
        _currentKeyboardState.IsKeyUp(key) && _previousKeyboardState.IsKeyDown(key);
    
    #endregion
    
    #region Mouse Input
    
    /// <summary>
    /// Check if a mouse button is currently held down.
    /// Similar to Input.GetMouseButton(0/1/2) in Unity.
    /// </summary>
    public bool IsMouseButtonDown(MouseButton button) => button switch
    {
        MouseButton.Left => _currentMouseState.LeftButton == ButtonState.Pressed,
        MouseButton.Right => _currentMouseState.RightButton == ButtonState.Pressed,
        MouseButton.Middle => _currentMouseState.MiddleButton == ButtonState.Pressed,
        _ => false
    };
    
    /// <summary>
    /// Check if a mouse button was pressed this frame.
    /// Similar to Input.GetMouseButtonDown(0/1/2) in Unity.
    /// </summary>
    public bool IsMouseButtonPressed(MouseButton button) => button switch
    {
        MouseButton.Left => 
            _currentMouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released,
        MouseButton.Right => 
            _currentMouseState.RightButton == ButtonState.Pressed && 
            _previousMouseState.RightButton == ButtonState.Released,
        MouseButton.Middle => 
            _currentMouseState.MiddleButton == ButtonState.Pressed && 
            _previousMouseState.MiddleButton == ButtonState.Released,
        _ => false
    };
    
    /// <summary>
    /// Check if a mouse button was released this frame.
    /// Similar to Input.GetMouseButtonUp(0/1/2) in Unity.
    /// </summary>
    public bool IsMouseButtonReleased(MouseButton button) => button switch
    {
        MouseButton.Left => 
            _currentMouseState.LeftButton == ButtonState.Released && 
            _previousMouseState.LeftButton == ButtonState.Pressed,
        MouseButton.Right => 
            _currentMouseState.RightButton == ButtonState.Released && 
            _previousMouseState.RightButton == ButtonState.Pressed,
        MouseButton.Middle => 
            _currentMouseState.MiddleButton == ButtonState.Released && 
            _previousMouseState.MiddleButton == ButtonState.Pressed,
        _ => false
    };
    
    #endregion
    
    #region GamePad Input
    
    /// <summary>
    /// Check if a gamepad button is currently held down.
    /// </summary>
    public bool IsButtonDown(Buttons button) => _currentGamePadState.IsButtonDown(button);
    
    /// <summary>
    /// Check if a gamepad button was pressed this frame.
    /// </summary>
    public bool IsButtonPressed(Buttons button) =>
        _currentGamePadState.IsButtonDown(button) && _previousGamePadState.IsButtonUp(button);
    
    /// <summary>
    /// Get left thumbstick position (-1 to 1 on each axis).
    /// Similar to Input.GetAxis("Horizontal") / Input.GetAxis("Vertical") in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// Unity's Input.GetAxis applies a dead zone and smoothing automatically.
    /// MonoGame gives you raw values - you should apply your own dead zone:
    /// if (Math.Abs(value) < 0.1f) value = 0;
    /// </summary>
    public System.Numerics.Vector2 GetLeftThumbstick()
    {
        var thumbstick = _currentGamePadState.ThumbSticks.Left;
        return new System.Numerics.Vector2(thumbstick.X, thumbstick.Y);
    }
    
    /// <summary>
    /// Get right thumbstick position (used for camera control).
    /// </summary>
    public System.Numerics.Vector2 GetRightThumbstick()
    {
        var thumbstick = _currentGamePadState.ThumbSticks.Right;
        return new System.Numerics.Vector2(thumbstick.X, thumbstick.Y);
    }
    
    /// <summary>
    /// Get left trigger value (0 to 1).
    /// </summary>
    public float GetLeftTrigger() => _currentGamePadState.Triggers.Left;
    
    /// <summary>
    /// Get right trigger value (0 to 1).
    /// </summary>
    public float GetRightTrigger() => _currentGamePadState.Triggers.Right;
    
    /// <summary>
    /// Check if a gamepad is connected.
    /// Similar to checking Input.GetJoystickNames() in Unity.
    /// </summary>
    public bool IsGamePadConnected() => _currentGamePadState.IsConnected;
    
    #endregion
    
    #region Helper Methods for Common Input Patterns
    
    /// <summary>
    /// Get WASD/Arrow keys movement input as a vector.
    /// Common pattern for FPS/top-down movement.
    /// 
    /// EDUCATIONAL NOTE:
    /// In Unity, you'd typically use:
    /// float h = Input.GetAxis("Horizontal");
    /// float v = Input.GetAxis("Vertical");
    /// 
    /// This is a convenience method that combines keyboard and gamepad input.
    /// </summary>
    public System.Numerics.Vector2 GetMovementInput()
    {
        var movement = System.Numerics.Vector2.Zero;
        
        // Keyboard WASD
        if (IsKeyDown(Keys.W) || IsKeyDown(Keys.Up))
            movement.Y += 1f;
        if (IsKeyDown(Keys.S) || IsKeyDown(Keys.Down))
            movement.Y -= 1f;
        if (IsKeyDown(Keys.A) || IsKeyDown(Keys.Left))
            movement.X -= 1f;
        if (IsKeyDown(Keys.D) || IsKeyDown(Keys.Right))
            movement.X += 1f;
        
        // If no keyboard input, check gamepad
        if (movement == System.Numerics.Vector2.Zero && IsGamePadConnected())
        {
            movement = GetLeftThumbstick();
            // Apply dead zone
            if (Math.Abs(movement.X) < 0.1f) movement.X = 0;
            if (Math.Abs(movement.Y) < 0.1f) movement.Y = 0;
        }
        
        // Normalize diagonal movement to prevent faster diagonal speed
        if (movement.LengthSquared() > 1f)
            movement = System.Numerics.Vector2.Normalize(movement);
        
        return movement;
    }
    
    /// <summary>
    /// Check if the jump button was pressed (Space or gamepad A).
    /// </summary>
    public bool IsJumpPressed() =>
        IsKeyPressed(Keys.Space) || IsButtonPressed(Buttons.A);
    
    /// <summary>
    /// Check if the fire button is held (Left Mouse or gamepad Right Trigger).
    /// </summary>
    public bool IsFireDown() =>
        IsMouseButtonDown(MouseButton.Left) || GetRightTrigger() > 0.5f;
    
    /// <summary>
    /// Check if the aim button is held (Right Mouse or gamepad Left Trigger).
    /// </summary>
    public bool IsAimDown() =>
        IsMouseButtonDown(MouseButton.Right) || GetLeftTrigger() > 0.5f;
    
    #endregion
}

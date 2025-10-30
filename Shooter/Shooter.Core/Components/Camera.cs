using System.Numerics;
using Shooter.Core.Plugins.Graphics;

namespace Shooter.Core.Components;

/// <summary>
/// Camera component for 3D rendering.
/// 
/// UNITY COMPARISON:
/// Similar to Unity's Camera component but more manual.
/// Unity cameras automatically render the scene based on their settings.
/// MonoGame cameras just provide view/projection matrices - YOU control rendering.
/// 
/// EDUCATIONAL NOTE - HOW CAMERAS WORK:
/// 
/// A 3D camera transforms the world into a 2D image through matrices:
/// 
/// 1. World Space → View Space (View Matrix):
///    - Positions things relative to the camera
///    - Camera at origin (0,0,0) looking down -Z axis
///    - Built from camera position, target, and up vector
/// 
/// 2. View Space → Clip Space (Projection Matrix):
///    - Applies perspective (things farther away look smaller)
///    - Or orthographic (no perspective, like 2D)
///    - Defines viewing frustum (what's visible)
/// 
/// 3. Clip Space → Screen Space (Viewport Transform):
///    - Maps to actual pixels on screen
///    - Done automatically by GPU
/// 
/// The full transform: Vertex * World * View * Projection = Screen Position
/// </summary>
public class Camera : EntityComponent, ICamera
{
    private Vector3 _position;
    private Vector3 _target;
    private Vector3 _up = Vector3.UnitY;
    
    private float _fieldOfView = 75.0f; // In degrees
    private float _nearPlane = 0.1f;
    private float _farPlane = 1000.0f;
    private float _aspectRatio = 16.0f / 9.0f;
    
    // Cached matrices (recalculated when camera moves)
    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;
    private bool _viewMatrixDirty = true;
    private bool _projectionMatrixDirty = true;
    
    /// <summary>
    /// Position of the camera in world space.
    /// Similar to transform.position in Unity.
    /// </summary>
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                _viewMatrixDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Point the camera is looking at.
    /// Unity uses transform.rotation instead, but LookAt is more intuitive for beginners.
    /// </summary>
    public Vector3 Target
    {
        get => _target;
        set
        {
            if (_target != value)
            {
                _target = value;
                _viewMatrixDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Up direction for the camera (usually Vector3.UnitY).
    /// Defines which way is "up" for the camera's orientation.
    /// </summary>
    public Vector3 Up
    {
        get => _up;
        set
        {
            if (_up != value)
            {
                _up = value;
                _viewMatrixDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Direction the camera is looking (normalized).
    /// Similar to transform.forward in Unity.
    /// </summary>
    public Vector3 Forward => Vector3.Normalize(_target - _position);
    
    /// <summary>
    /// Field of view in degrees (vertical FOV).
    /// Similar to Camera.fieldOfView in Unity.
    /// 
    /// EDUCATIONAL NOTE - FIELD OF VIEW:
    /// - Small FOV (30-50°): Telephoto lens, narrow view, less distortion
    /// - Medium FOV (60-90°): Normal lens, natural perspective
    /// - Large FOV (90-120°): Wide angle, fish-eye effect
    /// 
    /// Most FPS games use 75-90° for a good balance.
    /// </summary>
    public float FieldOfView
    {
        get => _fieldOfView;
        set
        {
            if (_fieldOfView != value)
            {
                _fieldOfView = Math.Clamp(value, 1.0f, 179.0f);
                _projectionMatrixDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Near clipping plane distance.
    /// Objects closer than this won't be rendered.
    /// Similar to Camera.nearClipPlane in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// Don't set this too small (like 0.001) or you'll get Z-fighting.
    /// Z-fighting is when surfaces flicker because the depth buffer
    /// doesn't have enough precision to tell them apart.
    /// 
    /// Good values: 0.1 to 1.0 for most scenes.
    /// </summary>
    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            if (_nearPlane != value)
            {
                _nearPlane = Math.Max(0.01f, value);
                _projectionMatrixDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Far clipping plane distance.
    /// Objects farther than this won't be rendered.
    /// Similar to Camera.farClipPlane in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// The ratio between near and far affects depth precision.
    /// Near=0.1, Far=10000 gives less precision than Near=0.1, Far=1000.
    /// 
    /// Only set far plane as far as you need to see.
    /// </summary>
    public float FarPlane
    {
        get => _farPlane;
        set
        {
            if (_farPlane != value)
            {
                _farPlane = Math.Max(_nearPlane + 0.1f, value);
                _projectionMatrixDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Aspect ratio (width / height).
    /// Usually set based on window size: screenWidth / screenHeight
    /// </summary>
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (_aspectRatio != value)
            {
                _aspectRatio = value;
                _projectionMatrixDirty = true;
            }
        }
    }
    
    /// <summary>
    /// View matrix: transforms from world space to view space.
    /// This is calculated automatically when camera position/target changes.
    /// 
    /// EDUCATIONAL NOTE - VIEW MATRIX:
    /// The view matrix is the INVERSE of the camera's world transform.
    /// 
    /// Why? Because we want to transform the world AS IF the camera is at the origin.
    /// If camera is at (10,0,0), we need to move everything by (-10,0,0).
    /// 
    /// Built using CreateLookAt():
    /// - Eye: Camera position
    /// - Target: Point camera looks at
    /// - Up: Which way is up
    /// </summary>
    public Matrix4x4 ViewMatrix
    {
        get
        {
            if (_viewMatrixDirty)
            {
                _viewMatrix = Matrix4x4.CreateLookAt(_position, _target, _up);
                _viewMatrixDirty = false;
            }
            return _viewMatrix;
        }
    }
    
    /// <summary>
    /// Projection matrix: transforms from view space to clip space.
    /// Applies perspective projection (or orthographic for 2D/UI).
    /// 
    /// EDUCATIONAL NOTE - PROJECTION MATRIX:
    /// 
    /// Perspective projection simulates how human eyes see:
    /// - Objects farther away appear smaller
    /// - Parallel lines converge at vanishing points
    /// - Field of view determines how "zoomed in" the view is
    /// 
    /// The projection matrix:
    /// 1. Scales based on FOV (wider FOV = smaller objects)
    /// 2. Applies perspective divide (divides by Z depth)
    /// 3. Maps to normalized device coordinates (-1 to 1)
    /// 
    /// Orthographic (not implemented here) keeps sizes constant.
    /// Used for 2D games, UI, and technical drawings.
    /// </summary>
    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            if (_projectionMatrixDirty)
            {
                // Convert FOV from degrees to radians
                float fovRadians = _fieldOfView * (float)Math.PI / 180.0f;
                
                // Create perspective projection matrix
                _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                    fovRadians,
                    _aspectRatio,
                    _nearPlane,
                    _farPlane
                );
                
                _projectionMatrixDirty = false;
            }
            return _projectionMatrix;
        }
    }
    
    /// <summary>
    /// Initialize the camera with a default setup.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // Default position: back and up from origin
        _position = new Vector3(0, 5, 10);
        _target = Vector3.Zero;
        _up = Vector3.UnitY;
    }
    
    /// <summary>
    /// Update camera (for animated cameras, following targets, etc.)
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        // Camera-specific update logic can go here
        // For example: following a player, camera shake, etc.
    }
    
    /// <summary>
    /// Set camera position and target.
    /// Convenience method for positioning the camera.
    /// </summary>
    public void LookAt(Vector3 position, Vector3 target)
    {
        Position = position;
        Target = target;
    }
    
    /// <summary>
    /// Set camera position and target with custom up vector.
    /// </summary>
    public void LookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        Position = position;
        Target = target;
        Up = up;
    }
    
    /// <summary>
    /// Move the camera forward/backward in the direction it's facing.
    /// Useful for FPS camera controls.
    /// </summary>
    public void MoveForward(float distance)
    {
        Vector3 forward = Forward;
        Position += forward * distance;
        Target += forward * distance;
    }
    
    /// <summary>
    /// Strafe the camera left/right (perpendicular to forward direction).
    /// </summary>
    public void Strafe(float distance)
    {
        Vector3 forward = Forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, _up));
        Position += right * distance;
        Target += right * distance;
    }
    
    /// <summary>
    /// Rotate camera around its position (yaw and pitch).
    /// Used for FPS mouse look.
    /// 
    /// EDUCATIONAL NOTE - EULER ANGLES:
    /// Yaw = rotation around Y axis (looking left/right)
    /// Pitch = rotation around X axis (looking up/down)
    /// Roll = rotation around Z axis (tilting head)
    /// 
    /// FPS cameras typically only use yaw and pitch, no roll.
    /// </summary>
    /// <param name="yaw">Horizontal rotation in radians</param>
    /// <param name="pitch">Vertical rotation in radians</param>
    /// <summary>
    /// Rotate the camera to look in a specific direction based on yaw and pitch angles.
    /// </summary>
    /// <param name="yawDegrees">Horizontal rotation in degrees (0 = forward along -Z)</param>
    /// <param name="pitchDegrees">Vertical rotation in degrees (positive = look up, negative = look down)</param>
    public void Rotate(float yawDegrees, float pitchDegrees)
    {
        // Convert degrees to radians
        float yawRadians = yawDegrees * (MathF.PI / 180f);
        float pitchRadians = pitchDegrees * (MathF.PI / 180f);
        
        // Calculate forward direction from yaw and pitch
        // Yaw rotates around Y axis, pitch rotates around local X axis
        float horizontalDistance = MathF.Cos(pitchRadians);
        
        Vector3 forward = new Vector3(
            MathF.Sin(yawRadians) * horizontalDistance,
            -MathF.Sin(pitchRadians), // Negative for correct up/down
            MathF.Cos(yawRadians) * horizontalDistance
        );
        
        // Normalize to ensure it's a unit vector
        forward = Vector3.Normalize(forward);
        
        // Update target based on current position
        Target = Position + forward;
    }
}

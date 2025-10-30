using System.Numerics;

namespace Shooter.Core.Components;

/// <summary>
/// 3D transformation component for position, rotation, and scale.
/// 
/// UNITY COMPARISON:
/// This replaces Unity's Transform component. Key differences:
/// - Unity's Transform is built-in and always present on GameObjects
/// - MonoGame requires you to manage transforms manually
/// - We use System.Numerics types (Vector3, Quaternion, Matrix4x4)
/// - Parent-child hierarchy must be implemented explicitly
/// 
/// NOTE: In Unity, transforms are updated automatically by the engine.
/// In MonoGame, you need to update world matrices when parent/child relationships change.
/// </summary>
public class Transform3D : EntityComponent
{
    private Vector3 _localPosition = Vector3.Zero;
    private Quaternion _localRotation = Quaternion.Identity;
    private Vector3 _localScale = Vector3.One;
    
    private Transform3D? _parent;
    private readonly List<Transform3D> _children = new();
    
    private Matrix4x4 _localMatrix = Matrix4x4.Identity;
    private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    private bool _isDirty = true;

    #region Local Space Properties

    /// <summary>
    /// Local position relative to parent.
    /// Equivalent to Unity's transform.localPosition
    /// </summary>
    public Vector3 LocalPosition
    {
        get => _localPosition;
        set
        {
            _localPosition = value;
            MarkDirty();
        }
    }

    /// <summary>
    /// Local rotation relative to parent.
    /// Equivalent to Unity's transform.localRotation
    /// </summary>
    public Quaternion LocalRotation
    {
        get => _localRotation;
        set
        {
            _localRotation = Quaternion.Normalize(value);
            MarkDirty();
        }
    }

    /// <summary>
    /// Local scale relative to parent.
    /// Equivalent to Unity's transform.localScale
    /// </summary>
    public Vector3 LocalScale
    {
        get => _localScale;
        set
        {
            _localScale = value;
            MarkDirty();
        }
    }

    #endregion

    #region World Space Properties

    /// <summary>
    /// World position.
    /// Equivalent to Unity's transform.position
    /// </summary>
    public Vector3 Position
    {
        get
        {
            UpdateMatrices();
            return new Vector3(_worldMatrix.M41, _worldMatrix.M42, _worldMatrix.M43);
        }
        set
        {
            if (_parent != null)
            {
                // Convert world position to local position
                _parent.UpdateMatrices();
                Matrix4x4.Invert(_parent.WorldMatrix, out var invParent);
                LocalPosition = Vector3.Transform(value, invParent);
            }
            else
            {
                LocalPosition = value;
            }
        }
    }

    /// <summary>
    /// World rotation.
    /// Equivalent to Unity's transform.rotation
    /// </summary>
    public Quaternion Rotation
    {
        get
        {
            UpdateMatrices();
            return Quaternion.CreateFromRotationMatrix(_worldMatrix);
        }
        set
        {
            if (_parent != null)
            {
                _parent.UpdateMatrices();
                var parentRot = Quaternion.CreateFromRotationMatrix(_parent.WorldMatrix);
                LocalRotation = Quaternion.Conjugate(parentRot) * value;
            }
            else
            {
                LocalRotation = value;
            }
        }
    }

    /// <summary>
    /// Forward direction in world space (similar to Unity's transform.forward).
    /// In MonoGame/OpenGL convention, forward is -Z axis.
    /// </summary>
    public Vector3 Forward
    {
        get
        {
            UpdateMatrices();
            return Vector3.Normalize(new Vector3(_worldMatrix.M31, _worldMatrix.M32, _worldMatrix.M33));
        }
    }

    /// <summary>
    /// Right direction in world space (similar to Unity's transform.right).
    /// </summary>
    public Vector3 Right
    {
        get
        {
            UpdateMatrices();
            return Vector3.Normalize(new Vector3(_worldMatrix.M11, _worldMatrix.M12, _worldMatrix.M13));
        }
    }

    /// <summary>
    /// Up direction in world space (similar to Unity's transform.up).
    /// </summary>
    public Vector3 Up
    {
        get
        {
            UpdateMatrices();
            return Vector3.Normalize(new Vector3(_worldMatrix.M21, _worldMatrix.M22, _worldMatrix.M23));
        }
    }

    /// <summary>
    /// The world transformation matrix.
    /// Useful for rendering and physics calculations.
    /// </summary>
    public Matrix4x4 WorldMatrix
    {
        get
        {
            UpdateMatrices();
            return _worldMatrix;
        }
    }

    #endregion

    #region Hierarchy

    /// <summary>
    /// Parent transform. Setting this creates a parent-child relationship.
    /// Equivalent to Unity's transform.parent
    /// </summary>
    public Transform3D? Parent
    {
        get => _parent;
        set
        {
            if (_parent == value) return;

            // Remove from old parent
            _parent?._children.Remove(this);

            _parent = value;

            // Add to new parent
            _parent?._children.Add(this);

            MarkDirty();
        }
    }

    /// <summary>
    /// Read-only list of child transforms.
    /// Equivalent to accessing children via transform.GetChild() in Unity
    /// </summary>
    public IReadOnlyList<Transform3D> Children => _children.AsReadOnly();

    /// <summary>
    /// Number of child transforms.
    /// Equivalent to Unity's transform.childCount
    /// </summary>
    public int ChildCount => _children.Count;

    #endregion

    #region Methods

    /// <summary>
    /// Rotates the transform to look at a target position.
    /// Similar to Unity's transform.LookAt()
    /// </summary>
    public void LookAt(Vector3 target, Vector3? up = null)
    {
        up ??= Vector3.UnitY;
        
        var direction = Vector3.Normalize(target - Position);
        if (direction == Vector3.Zero) return;

        var rotMatrix = Matrix4x4.CreateLookAt(Position, target, up.Value);
        Matrix4x4.Invert(rotMatrix, out var inverted);
        Rotation = Quaternion.CreateFromRotationMatrix(inverted);
    }

    /// <summary>
    /// Rotates around an axis by the specified angle.
    /// Similar to Unity's transform.Rotate()
    /// </summary>
    public void Rotate(Vector3 axis, float angleRadians)
    {
        var rotation = Quaternion.CreateFromAxisAngle(axis, angleRadians);
        LocalRotation *= rotation;
    }

    /// <summary>
    /// Moves the transform in the direction and distance of translation.
    /// Similar to Unity's transform.Translate()
    /// </summary>
    public void Translate(Vector3 translation, bool relativeToSelf = true)
    {
        if (relativeToSelf)
        {
            LocalPosition += Vector3.Transform(translation, LocalRotation);
        }
        else
        {
            LocalPosition += translation;
        }
    }

    /// <summary>
    /// Transforms a direction from local space to world space.
    /// Similar to Unity's transform.TransformDirection()
    /// </summary>
    public Vector3 TransformDirection(Vector3 direction)
    {
        UpdateMatrices();
        return Vector3.TransformNormal(direction, _worldMatrix);
    }

    /// <summary>
    /// Transforms a point from local space to world space.
    /// Similar to Unity's transform.TransformPoint()
    /// </summary>
    public Vector3 TransformPoint(Vector3 point)
    {
        UpdateMatrices();
        return Vector3.Transform(point, _worldMatrix);
    }

    #endregion

    #region Internal Update Logic

    private void UpdateMatrices()
    {
        if (!_isDirty) return;

        // Create local transformation matrix
        _localMatrix = Matrix4x4.CreateScale(_localScale) *
                      Matrix4x4.CreateFromQuaternion(_localRotation) *
                      Matrix4x4.CreateTranslation(_localPosition);

        // Combine with parent's world matrix if there is one
        if (_parent != null)
        {
            _parent.UpdateMatrices();
            _worldMatrix = _localMatrix * _parent._worldMatrix;
        }
        else
        {
            _worldMatrix = _localMatrix;
        }

        _isDirty = false;
    }

    private void MarkDirty()
    {
        if (_isDirty) return;

        _isDirty = true;

        // Mark all children as dirty too
        foreach (var child in _children)
        {
            child.MarkDirty();
        }
    }

    #endregion

    public override void OnDestroy()
    {
        // Remove from parent
        Parent = null;

        // Clear children (they will handle their own cleanup)
        _children.Clear();

        base.OnDestroy();
    }
}

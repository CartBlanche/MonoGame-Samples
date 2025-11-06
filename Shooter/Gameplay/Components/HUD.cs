using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Core.Components;
using Shooter.Core.Entities;
using Color = Microsoft.Xna.Framework.Color;

namespace Shooter.Gameplay.Components;

/// <summary>
/// Heads-Up Display (HUD) component for showing player status.
/// Displays health bar, ammo counter, and weapon information.
/// 
/// UNITY COMPARISON:
/// Unity: UI Canvas with UI elements (Text, Image, Slider)
/// MonoGame: Manual SpriteBatch rendering with textures and fonts
/// 
/// Unity's UI system is automatic and visual editor-based.
/// MonoGame requires manual positioning and drawing code.
/// </summary>
public class HUD : EntityComponent
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Texture2D? _pixel;
    private GraphicsDevice? _graphicsDevice;

    // HUD element positions and sizes
    // Move HUD to top left for debug visibility
    private Vector2 _healthBarSize = new Vector2(200, 30);
    private Vector2 _ammoBarSize = new Vector2(200, 18);
    private Vector2 _weaponBarSize = new Vector2(200, 18);
    private int _margin = 20;
    private Vector2 _healthBarPosition = new Vector2(20, 20);
    private Vector2 _ammoBarPosition = new Vector2(20, 60);
    private Vector2 _weaponBarPosition = new Vector2(20, 85);

    // References to player components
    private Health? _playerHealth;
    private WeaponController? _weaponController;

    // Colors
    private Color _healthBarBackground = new Color(40, 40, 40, 200);
    private Color _healthBarFill = new Color(220, 50, 50, 255);
    private Color _healthBarBorder = new Color(255, 255, 255, 255);
    private Color _textColor = Color.White;
    private Color _textShadowColor = new Color(0, 0, 0, 150);

    /// <summary>
    /// Initialize the HUD component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        // Find player health and weapon controller
        if (Owner != null)
        {
            _playerHealth = Owner.GetComponent<Health>();
            _weaponController = Owner.GetComponent<WeaponController>();

            if (_playerHealth == null)
                Console.WriteLine("[HUD] Warning: No Health component found on player!");
            if (_weaponController == null)
                Console.WriteLine("[HUD] Warning: No WeaponController component found on player!");
        }

        Console.WriteLine("[HUD] Initialized");
    }

    /// <summary>
    /// Set up graphics resources for the HUD.
    /// Should be called from Game.cs after GraphicsDevice is ready.
    /// </summary>
    public void LoadContent(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont? font = null)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _font = font; // Will use default system font if null

        // Create 1x1 white pixel for drawing rectangles
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Position bars at lower right after graphics device is available
        int screenW = _graphicsDevice.Viewport.Width;
        int screenH = _graphicsDevice.Viewport.Height;
        _healthBarPosition = new Vector2(screenW - _healthBarSize.X - _margin, screenH - _healthBarSize.Y - _ammoBarSize.Y - _weaponBarSize.Y - _margin);
        _ammoBarPosition = new Vector2(screenW - _ammoBarSize.X - _margin, screenH - _ammoBarSize.Y - _weaponBarSize.Y - _margin);
        _weaponBarPosition = new Vector2(screenW - _weaponBarSize.X - _margin, screenH - _weaponBarSize.Y - _margin);

        Console.WriteLine("[HUD] Content loaded");
    }

    /// <summary>
    /// Draw the HUD overlay
    /// </summary>
    public override void Draw(Core.Components.GameTime gameTime)
    {
        base.Draw(gameTime);

        if (_spriteBatch == null || _pixel == null)
            return;

        _spriteBatch.Begin();

        DrawHealthBar();
        
        // Debug: Check weapon status
        if (_weaponController == null)
        {
            // Draw debug text
            if (_font != null)
            {
                _spriteBatch.DrawString(_font, "NO WEAPON CONTROLLER", new Vector2(20, 100), Color.Red);
            }
        }
        else if (_weaponController.CurrentWeapon == null)
        {
            // Draw debug text
            if (_font != null)
            {
                _spriteBatch.DrawString(_font, $"WEAPON CONTROLLER OK, NO CURRENT WEAPON (Index: {_weaponController.CurrentWeaponIndex})", new Vector2(20, 100), Color.Yellow);
            }
        }
        else
        {
            DrawAmmoBar();
            DrawWeaponBar();
        }

        _spriteBatch.End();
    }

    /// <summary>
    /// Draw the ammo counter
    /// </summary>
    // Rectangle-based ammo bar
    private void DrawAmmoBar()
    {
        if (_spriteBatch == null || _pixel == null || _weaponController == null)
            return;

        var weapon = _weaponController.CurrentWeapon;
        if (weapon == null)
            return;

        float ammoPercent = weapon.CurrentAmmoInMag / (float)Math.Max(weapon.MagazineSize, 1);
        ammoPercent = Math.Clamp(ammoPercent, 0f, 1f);

        Rectangle backgroundRect = new Rectangle((int)_ammoBarPosition.X, (int)_ammoBarPosition.Y, (int)_ammoBarSize.X, (int)_ammoBarSize.Y);
        Rectangle fillRect = new Rectangle((int)_ammoBarPosition.X + 2, (int)_ammoBarPosition.Y + 2, (int)((_ammoBarSize.X - 4) * ammoPercent), (int)_ammoBarSize.Y - 4);
        Rectangle borderRect = new Rectangle((int)_ammoBarPosition.X, (int)_ammoBarPosition.Y, (int)_ammoBarSize.X, (int)_ammoBarSize.Y);

        Color ammoBarBackground = new Color(30, 30, 60, 200);
        Color ammoBarFill = weapon.IsReloading ? new Color(255, 220, 40, 255) : (weapon.CurrentAmmoInMag == 0 ? new Color(220, 50, 50, 255) : new Color(40, 180, 255, 255));
        Color ammoBarBorder = new Color(255, 255, 255, 255);

        // Draw background
        _spriteBatch.Draw(_pixel, backgroundRect, ammoBarBackground);
        // Draw fill
        _spriteBatch.Draw(_pixel, fillRect, ammoBarFill);
        // Draw border
        DrawRectangleBorder(borderRect, 2, ammoBarBorder);
    }

    /// <summary>
    /// Draw the weapon name
    /// </summary>
    // Rectangle-based weapon bar
    private void DrawWeaponBar()
    {
        if (_spriteBatch == null || _pixel == null || _weaponController == null)
            return;

        var weapon = _weaponController.CurrentWeapon;
        if (weapon == null)
            return;

        Rectangle backgroundRect = new Rectangle((int)_weaponBarPosition.X, (int)_weaponBarPosition.Y, (int)_weaponBarSize.X, (int)_weaponBarSize.Y);
        Rectangle borderRect = new Rectangle((int)_weaponBarPosition.X, (int)_weaponBarPosition.Y, (int)_weaponBarSize.X, (int)_weaponBarSize.Y);

        Color weaponBarBackground = new Color(40, 40, 40, 200);
        Color weaponBarBorder = new Color(255, 255, 255, 255);

        // Draw background
        _spriteBatch.Draw(_pixel, backgroundRect, weaponBarBackground);
        // Draw border
        DrawRectangleBorder(borderRect, 2, weaponBarBorder);

        // Draw weapon name text (if font available)
        if (_font != null)
        {
            string weaponText = weapon.Name.ToUpper();
            Vector2 textSize = _font.MeasureString(weaponText);
            Vector2 textPos = new Vector2(_weaponBarPosition.X + (_weaponBarSize.X - textSize.X) / 2, _weaponBarPosition.Y + (_weaponBarSize.Y - textSize.Y) / 2);
            _spriteBatch.DrawString(_font, weaponText, textPos + new Vector2(1, 1), _textShadowColor);
            _spriteBatch.DrawString(_font, weaponText, textPos, _textColor);
        }
    }

    /// <summary>
    /// Helper method to draw a rectangle border
    /// </summary>
    private void DrawRectangleBorder(Rectangle rect, int thickness, Color color)
    {
        if (_spriteBatch == null || _pixel == null)
            return;

        // Top
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public override void OnDestroy()
    {
        _pixel?.Dispose();
        _pixel = null;

        base.OnDestroy();
    }

    /// <summary>
    /// Draw the health bar
    /// </summary>
    private void DrawHealthBar()
    {
        if (_spriteBatch == null || _pixel == null || _playerHealth == null)
            return;

        float healthPercent = _playerHealth.CurrentHealth / _playerHealth.MaxHealth;
        healthPercent = Math.Clamp(healthPercent, 0f, 1f);

        Rectangle backgroundRect = new Rectangle(
            (int)_healthBarPosition.X,
            (int)_healthBarPosition.Y,
            (int)_healthBarSize.X,
            (int)_healthBarSize.Y
        );

        Rectangle fillRect = new Rectangle(
            (int)_healthBarPosition.X + 2,
            (int)_healthBarPosition.Y + 2,
            (int)((_healthBarSize.X - 4) * healthPercent),
            (int)_healthBarSize.Y - 4
        );

        Rectangle borderRect = new Rectangle(
            (int)_healthBarPosition.X,
            (int)_healthBarPosition.Y,
            (int)_healthBarSize.X,
            (int)_healthBarSize.Y
        );

        // Draw background
        _spriteBatch.Draw(_pixel, backgroundRect, _healthBarBackground);
        // Draw fill
        _spriteBatch.Draw(_pixel, fillRect, _healthBarFill);
        // Draw border (4 lines)
        DrawRectangleBorder(borderRect, 2, _healthBarBorder);

        // Draw health text
        if (_font != null)
        {
            string healthText = $"{(int)_playerHealth.CurrentHealth} / {(int)_playerHealth.MaxHealth}";
            Vector2 textSize = _font.MeasureString(healthText);
            Vector2 textPos = new Vector2(
                _healthBarPosition.X + (_healthBarSize.X - textSize.X) / 2,
                _healthBarPosition.Y + (_healthBarSize.Y - textSize.Y) / 2
            );
            // Draw shadow
            _spriteBatch.DrawString(_font, healthText, textPos + new Vector2(1, 1), _textShadowColor);
            // Draw text
            _spriteBatch.DrawString(_font, healthText, textPos, _textColor);
        }
    }
}
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Shooter.Gameplay;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Enemies;
using Shooter.Gameplay.Weapons;
using Shooter.Rendering.PostProcess;

namespace Shooter.UI
{
    /// <summary>
    /// In-game HUD (MainScene / GameHUD prefab). Layout is the prefab's RectTransforms on a 960 x 540 reference canvas
    /// (CanvasScaler: scale with screen size, match width), kept in Unity's y-up canvas space and flipped when drawn.
    /// Ported: CrosshairManager, AmmoCounter (+FillBarColorChange), PlayerHealthBar, StanceHUD, Compass directions,
    /// the pause hint and the objective message (DisplayMessageManager / NotificationToast).
    /// The layer is drawn into an off-screen target in linear light with premultiplied-alpha accumulation and blended over the
    /// finished frame by the final post pass (FinalPost.fx), because Unity blends UI in linear space (measured on the reference
    /// shots: black at alpha 0.6 over 32 gives 18, not the 13 a gamma-space blend gives).
    /// Also ported with the enemies (4c-3a): objective counter, notification toasts, compass enemy markers.
    /// The jetpack counter (JetpackCounter + FillBarColorChange, shown once the jetpack is unlocked) and its "Jetpack unlocked" toast come with the jetpack.
    /// Not ported: the frame-rate counter (inactive in the scene).
    /// </summary>
    public sealed class Hud
    {
        private const float RefWidth = 960f;

        /// <summary>Scale between a spritefont's nominal size and the pixel size TextMeshPro gives the same font size (spritefont size is in points: 96 / 72 px per pt, so 0.75 undoes it; checked against the Unity message width).</summary>
        private const float FontPixelFactor = 0.75f;

        // GameHUD.prefab values
        private static readonly Color HealthColor = new Color(0.97647065f, 0.12941177f, 0.21568629f, 1f);
        private static readonly Color BarBackground = new Color(0f, 0f, 0f, 0.4f);
        private static readonly Color AmmoBackground = new Color(0f, 0f, 0f, 0.4392157f);
        private static readonly Color AmmoBackgroundEmpty = new Color(1f, 0f, 0f, 0.43137255f);
        private static readonly Color KeyGrey = new Color(0.1792453f, 0.1792453f, 0.1792453f, 1f);
        private const float CrosshairSharpness = 5f;     // CrosshairManager.CrosshairUpdateshrpness
        private const float AmmoFillSharpness = 20f;     // AmmoCounter.AmmoFillMovementSharpness
        // JetpackCounter on GameHUD.prefab: FillBarColorChange values, and the Image colours the instance starts with (before the first lerp)
        private static readonly Vector4 JetpackFgDefault = new Vector4(0f, 1f, 0.8367977f, 1f), JetpackFgFlashFull = Vector4.One;
        private static readonly Vector4 JetpackBgDefault = new Vector4(0f, 0f, 0f, 0.44705883f), JetpackBgFlashEmpty = new Vector4(1f, 0f, 0f, 0.4392157f);
        private static readonly Vector4 JetpackFgStart = new Vector4(0f, 0.96470594f, 1f, 1f), JetpackBgStart = new Vector4(0f, 0f, 0f, 0.4f);
        private const float BarColorSharpness = 5f;      // FillBarColorChange.ColorChangeSharpness
        private const float CompassVisibilityAngle = 180f;
        private const float CompassMarkerScale = 0.5f;   // Compass.Update: directions have distanceRatio 1 -> Lerp(1, MinScale 0.5, 1)

        // ObjectiveKillEnemies in the scene (Objective.Title / Description / DelayVisible) and ObjectiveToastPrimary.prefab; the strings and
        // the delay are replaced by the objective's own values in BindCombat
        private const string ObjectiveTitleDefault = "Eliminate all the enemies";
        private const string ObjectiveDescriptionDefault = "Defeat all the enemies in the map";
        private const float ObjectiveDelayVisibleDefault = 4f, ObjectiveFadeIn = 0.5f;
        private const float ObjectiveCompletionDelay = 0.35f, ObjectiveFadeOut = 2f;   // ObjectiveToastPrimary: CompletionDelay, FadeOutDuration

        // NotificationToast.prefab (VisibleDuration 3, fade in 0.5, fade out 2) in NotificationsRect (GameHUD: bottom-left, pos (0, 106.7), padding 25, lower-left, spacing 5)
        private const float NotifyFadeIn = 0.5f, NotifyVisible = 3f, NotifyFadeOut = 2f, NotifyHeight = 14f, NotifySpacing = 5f, NotifyPadX = 5f;
        private const float NotifyLeft = 25f, NotifyBottom = 106.7f + 25f;

        // Compass.cs / CompassMarkerEnemy.prefab
        private const float CompassHeightMultiplier = 2f, CompassMarginRatio = 0.8f, CompassDistanceMinScale = 50f, CompassMarkerSize = 12f;
        private static readonly Color CompassEnemyDefault = new Color(1f, 0.42219353f, 0f, 1f), CompassEnemyAlt = new Color(1f, 0f, 0f, 1f);

        // NotificationToast on GameMessageGeneric.prefab
        private const float MessageFadeIn = 0.5f, MessageVisible = 3f, MessageFadeOut = 0.5f;

        // straight-alpha source over a premultiplied accumulation target: rgb = src.rgb * a + dst.rgb * (1 - a), a = a + dst.a * (1 - a)
        private static readonly BlendState LayerBlend = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };

        private readonly GraphicsDevice _gd;
        private readonly SpriteBatch _sb;
        private RenderTarget2D _layer;
        private readonly Texture2D _white;
        private readonly Texture2D _gradient, _lifeIcon, _stanceStand, _stanceCrouch, _keyBg, _jetpackIcon;
        private readonly Texture2D _enemyIcon;
        private readonly SpriteFont _font35, _font22, _font12, _font10, _font9, _font7;
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> _textures = new System.Collections.Generic.Dictionary<string, Texture2D>();
        private readonly System.Collections.Generic.Dictionary<long, Texture2D> _rounded = new System.Collections.Generic.Dictionary<long, Texture2D>();
        private readonly ContentManager _content;

        private PlayerController _player;
        private PlayerWeaponsManager _weapons;
        private WeaponController _hudWeapon;

        // CrosshairManager state
        private CrosshairData _crossDefault, _crossTarget;
        private CrosshairData _crossCurrent;
        private Vector4 _crossColor;
        private float _crossSize;
        private bool _wasPointingAtEnemy;
        private string _crossSprite;

        // JetpackCounter / FillBarColorChange state
        private Jetpack _jetpack;
        private Vector4 _jetFg = JetpackFgStart, _jetBg = JetpackBgStart;
        private float _prevJetpackRatio = 1f;

        // AmmoCounter / FillBarColorChange state
        private float _ammoFill = 1f;
        private float _prevAmmoRatio = 1f;
        private Vector4 _ammoFg = Vector4.One, _ammoBg;

        private float _time;
        private string _message = ObjectiveTitleDefault;   // ObjectiveKillEnemies.Title, broadcast from Objective.Start
        private float _messageStart;                       // _time at which the message starts fading in (DisplayMessageEvent.DelayBeforeDisplay)

        // objective toast / notifications (ObjectiveHUDManager, NotificationHUDManager)
        private EnemyDirector _director;
        private string _objTitle = ObjectiveTitleDefault, _objDescription = ObjectiveDescriptionDefault, _objCounter = "";
        private float _objDelay = ObjectiveDelayVisibleDefault;
        private float _objCompleteTime = float.PositiveInfinity;   // time at which the fade-out starts (completion + CompletionDelay)
        private readonly List<KeyValuePair<string, float>> _notifications = new List<KeyValuePair<string, float>>();   // text, time created (oldest first)
        private readonly Texture2D _radarBg, _radarIcon;

        private int _width, _height;
        private float _scale, _canvasHeight;

        /// <summary>Player health 0..1 (PlayerHealthBar); follows the bound Health every update, set by hand for shots.</summary>
        public float HealthRatio = 1f;
        /// <summary>GameFlowManager.GameIsEnding: the critical-health vignette stops pulsing.</summary>
        public bool GameIsEnding;

        // FeedbackFlashHUD (GameHUD.prefab values; Heal* is only used for the colour, the script fades both with the Damage* values)
        private const float DamageFlashDuration = 0.2f, DamageFlashMaxAlpha = 0.7f;
        private const float CriticalVignetteMaxAlpha = 1f, PulsatingVignetteFrequency = 8f;
        private static readonly Vector4 DamageFlashColor = new Vector4(1f, 0f, 0f, 1f);
        private static readonly Vector4 HealFlashColor = new Vector4(0.6084906f, 1f, 0.95854604f, 0.5176471f);

        private Health _health;
        private bool _flashActive;
        private float _flashStart = float.NegativeInfinity;
        private Vector4 _flashColor = DamageFlashColor;
        private readonly Texture2D _vignette;

        public Hud(GraphicsDevice gd, ContentManager content)
        {
            _gd = gd;
            _content = content;
            _sb = new SpriteBatch(gd);
            _white = new Texture2D(gd, 1, 1);
            _white.SetData(new[] { Color.White });
            _gradient = Tex("Textures/UI/Gradiants");
            _lifeIcon = Tex("Textures/UI/HUD_LifeBar_Icon");
            _stanceStand = Tex("Textures/UI/Stance_Stand_Icon");
            _stanceCrouch = Tex("Textures/UI/Stance_Crouch_Icon");
            _keyBg = Tex("Textures/UI/HUD_Key_Bg");
            _jetpackIcon = Tex("Textures/UI/HUD_Jetpack_Icon");
            _font35 = content.Load<SpriteFont>("Fonts/Roboto-Black_35");
            _font22 = content.Load<SpriteFont>("Fonts/Roboto-Black_22");
            _font12 = content.Load<SpriteFont>("Fonts/Roboto-Regular_12");
            _font10 = content.Load<SpriteFont>("Fonts/Roboto-Black_10");
            _font9 = content.Load<SpriteFont>("Fonts/Roboto-Black_9");
            _font7 = content.Load<SpriteFont>("Fonts/Roboto-Black_7");
            _enemyIcon = Tex("Textures/UI/Notification_Enemy_Icon");
            _radarBg = Tex("Textures/UI/Radar_Enemy_IconBg");
            _radarIcon = Tex("Textures/UI/Radar_Enemy_Icon");
            _vignette = Tex("Textures/UI/Transparent_Center");
        }

        /// <summary>Attach to the player's Health (PlayerHealthBar, FeedbackFlashHUD); a fresh Health after a reload restarts the flash.</summary>
        public void BindHealth(Health health)
        {
            if (_health != null)
            {
                _health.OnDamaged -= OnPlayerDamaged;
                _health.OnHealed -= OnPlayerHealed;
            }
            _health = health;
            _flashActive = false;
            _flashStart = float.NegativeInfinity;
            if (_health != null)
            {
                _health.OnDamaged += OnPlayerDamaged;
                _health.OnHealed += OnPlayerHealed;
                HealthRatio = _health.GetRatio();
            }
        }

        private void OnPlayerDamaged(float damage, object source) { StartFlash(DamageFlashColor); }
        private void OnPlayerHealed(float amount) { StartFlash(HealFlashColor); }

        private void StartFlash(Vector4 color)
        {
            _flashStart = _time;
            _flashActive = true;
            _flashColor = color;
        }

        /// <summary>FeedbackFlashHUD.Update: alpha of the damage / heal flash (0 when off).</summary>
        private float FlashAlpha()
        {
            if (!_flashActive)
            {
                return 0f;
            }
            float t = (_time - _flashStart) / DamageFlashDuration;
            return t < 1f ? DamageFlashMaxAlpha * (1f - t) : 0f;
        }

        /// <summary>FeedbackFlashHUD.Update: critical-health vignette alpha (0 when the health is not critical).</summary>
        private float VignetteAlpha()
        {
            if (_health == null || !_health.IsCritical())
            {
                return 0f;
            }
            float alpha = (1f - _health.CurrentHealth / _health.MaxHealth / _health.CriticalHealthRatio) * CriticalVignetteMaxAlpha;
            return GameIsEnding ? alpha : ((float)Math.Sin(_time * PulsatingVignetteFrequency) / 2f + 0.5f) * alpha;
        }

        /// <summary>
        /// Attach to the scene's enemies and objective: toast text and notifications follow ObjectiveUpdateEvent, the compass shows the live
        /// enemies. Like the Bind, loading the scene again restarts the toast.
        /// </summary>
        public void BindCombat(EnemyDirector director)
        {
            if (_director != null)
            {
                _director.Events.ObjectiveUpdated -= OnObjectiveUpdated;
                _director.Events.ObjectiveCompleted -= OnObjectiveCompleted;
            }
            _director = director;
            _notifications.Clear();
            _objCounter = "";
            _objCompleteTime = float.PositiveInfinity;
            ObjectiveKillEnemies o = director != null ? director.Objective : null;
            _objTitle = o != null ? o.Title : ObjectiveTitleDefault;
            _objDescription = o != null ? o.Description : ObjectiveDescriptionDefault;
            _objDelay = o != null ? o.DelayVisible : ObjectiveDelayVisibleDefault;
            _message = _objTitle;
            _messageStart = 0f;
            if (director != null)
            {
                director.Events.ObjectiveUpdated += OnObjectiveUpdated;
                director.Events.ObjectiveCompleted += OnObjectiveCompleted;
            }
        }

        /// <summary>DisplayMessageEvent: the message toast shows <paramref name="text"/> after <paramref name="delay"/> seconds (GameFlowManager's win message).</summary>
        public void ShowMessage(string text, float delay)
        {
            _message = text;
            _messageStart = _time + delay;
        }

        /// <summary>NotificationHUDManager.CreateNotification: a toast in the lower-left stack.</summary>
        public void Notify(string text)
        {
            _notifications.Add(new KeyValuePair<string, float>(text, _time));
        }

        /// <summary>JetpackCounter.Awake / NotificationHUDManager: follow the player's Jetpack (the counter shows once it is unlocked; the unlock adds a toast).</summary>
        public void BindJetpack(Jetpack jetpack)
        {
            if (_jetpack != null)
            {
                _jetpack.Unlocked -= OnJetpackUnlocked;
            }
            _jetpack = jetpack;
            _jetFg = JetpackFgStart;
            _jetBg = JetpackBgStart;
            _prevJetpackRatio = 1f;   // FillBarColorChange.Initialize(1, 0)
            if (_jetpack != null)
            {
                _jetpack.Unlocked += OnJetpackUnlocked;
            }
        }

        private void OnJetpackUnlocked()
        {
            Notify("Jetpack unlocked");
        }

        private void OnObjectiveUpdated(ObjectiveUpdateEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.DescriptionText)) _objDescription = evt.DescriptionText;
            if (!string.IsNullOrEmpty(evt.CounterText)) _objCounter = evt.CounterText;
            if (!string.IsNullOrEmpty(evt.NotificationText)) _notifications.Add(new KeyValuePair<string, float>(evt.NotificationText, _time));
        }

        private void OnObjectiveCompleted(Objective o)
        {
            _objCompleteTime = _time + ObjectiveCompletionDelay;
        }

        private Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            if (!_textures.TryGetValue(name, out Texture2D t))
            {
                t = _content.Load<Texture2D>(name);
                _textures[name] = t;
            }
            return t;
        }

        /// <summary>Attach to a (new) player / weapon manager, like the scene loading again: message and bars restart.</summary>
        public void Bind(PlayerController player, PlayerWeaponsManager weapons)
        {
            if (_weapons != null)
            {
                _weapons.SwitchedToWeapon -= OnWeaponChanged;
            }
            _player = player;
            _weapons = weapons;
            _time = 0f;
            _wasPointingAtEnemy = false;
            _hudWeapon = null;
            _crossColor = new Vector4(1f, 1f, 1f, 0.5882353f);   // V's Image colour in the scene, before the first lerp to the weapon's colour
            _weapons.SwitchedToWeapon += OnWeaponChanged;
            OnWeaponChanged(_weapons.ActiveWeapon);
            // AmmoCounter.Initialize: one counter per weapon in the slots
            WeaponController w = FirstWeapon();
            _hudWeapon = w;
            _ammoFill = w != null ? w.CurrentAmmoRatio : 1f;
            _prevAmmoRatio = 1f;
            _ammoFg = Vector4.One;
            _ammoBg = ToVector(AmmoBackground);
        }

        private WeaponController FirstWeapon()
        {
            for (int i = 0; i < 9; i++)
            {
                WeaponController w = _weapons.GetWeaponAtSlotIndex(i);
                if (w != null)
                {
                    return w;
                }
            }
            return null;
        }

        private void OnWeaponChanged(WeaponController w)
        {
            if (w != null)
            {
                _crossDefault = w.Settings.CrosshairDefault;
                _crossTarget = w.Settings.CrosshairTargetInSight;
            }
            ApplyCrosshair(true);
        }

        // CrosshairManager.UpdateCrosshairPointingAtEnemy
        private void ApplyCrosshair(bool force)
        {
            if (_crossDefault.Sprite == null)
            {
                return;
            }
            bool pointing = _weapons != null && _weapons.IsPointingAtEnemy;
            if ((force || !_wasPointingAtEnemy) && pointing)
            {
                _crossCurrent = _crossTarget;
                _crossSprite = _crossCurrent.Sprite;
                _crossSize = _crossCurrent.Size;
            }
            else if ((force || _wasPointingAtEnemy) && !pointing)
            {
                _crossCurrent = _crossDefault;
                _crossSprite = _crossCurrent.Sprite;
                _crossSize = _crossCurrent.Size;
            }
        }

        /// <summary>Jump straight to the steady state (crosshair, bars settled, message fully visible) for --shot captures.</summary>
        public void SnapToSteadyState(float time)
        {
            _time = time;
            ApplyCrosshair(true);
            _crossColor = _crossCurrent.Color;
            _crossSize = _crossCurrent.Size;
            if (_hudWeapon != null)
            {
                _ammoFill = _hudWeapon.CurrentAmmoRatio;
                _prevAmmoRatio = _hudWeapon.CurrentAmmoRatio;
            }
            _ammoFg = Vector4.One;
            _ammoBg = ToVector(AmmoBackground);
            _jetFg = JetpackFgDefault;
            _jetBg = JetpackBgDefault;
            if (_jetpack != null)
            {
                _prevJetpackRatio = _jetpack.CurrentFillRatio;
            }
        }

        public void Update(float dt)
        {
            if (_weapons == null)
            {
                return;
            }
            _time += dt;

            if (_health != null)
            {
                HealthRatio = _health.GetRatio();   // PlayerHealthBar.Update
            }
            if (_flashActive && (_time - _flashStart) / DamageFlashDuration >= 1f)
            {
                _flashActive = false;
            }

            // CrosshairManager.Update
            ApplyCrosshair(false);
            _wasPointingAtEnemy = _weapons.IsPointingAtEnemy;
            float k = Math.Min(1f, dt * CrosshairSharpness);
            _crossColor = Vector4.Lerp(_crossColor, _crossCurrent.Color, k);
            _crossSize = MathHelper.Lerp(_crossSize, _crossCurrent.Size, k);

            // JetpackCounter.Update + FillBarColorChange.UpdateVisual (only while the jetpack is unlocked)
            if (_jetpack != null && _jetpack.IsJetpackUnlocked)
            {
                float ratio = _jetpack.CurrentFillRatio;
                float kb = Math.Min(1f, dt * BarColorSharpness);
                if (ratio == 1f && ratio != _prevJetpackRatio)
                {
                    _jetFg = JetpackFgFlashFull;
                }
                else if (ratio < 0f)
                {
                    _jetBg = JetpackBgFlashEmpty;
                }
                else
                {
                    _jetFg = Vector4.Lerp(_jetFg, JetpackFgDefault, kb);
                    _jetBg = Vector4.Lerp(_jetBg, JetpackBgDefault, kb);
                }
                _prevJetpackRatio = ratio;
            }

            // AmmoCounter.Update + FillBarColorChange.UpdateVisual
            if (_hudWeapon != null)
            {
                float ratio = _hudWeapon.CurrentAmmoRatio;
                _ammoFill = MathHelper.Lerp(_ammoFill, ratio, Math.Min(1f, dt * AmmoFillSharpness));
                float full = 1f, empty = _hudWeapon.AmmoNeededToShoot;
                float kb = Math.Min(1f, dt * BarColorSharpness);
                if (ratio == full && ratio != _prevAmmoRatio)
                {
                    _ammoFg = new Vector4(0f, 0f, 0f, 1f);   // FlashForegroundColorFull
                }
                else if (ratio < empty)
                {
                    _ammoBg = ToVector(AmmoBackgroundEmpty);   // FlashBackgroundColorEmpty
                }
                else
                {
                    _ammoFg = Vector4.Lerp(_ammoFg, Vector4.One, kb);
                    _ammoBg = Vector4.Lerp(_ammoBg, ToVector(AmmoBackground), kb);
                }
                _prevAmmoRatio = ratio;
            }
        }

        // ------------------------------------------------------------------ layout helpers (Unity y-up canvas space)

        private struct URect
        {
            public float X, Y, W, H;   // min corner (bottom-left), size
            public URect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
            public float CenterX { get { return X + W * 0.5f; } }
            public float CenterY { get { return Y + H * 0.5f; } }
        }

        /// <summary>RectTransform: anchors, anchoredPosition, sizeDelta and pivot resolved against the parent rect.</summary>
        private static URect Layout(URect parent, float aminX, float aminY, float amaxX, float amaxY, float posX, float posY, float sizeX, float sizeY, float pivX, float pivY)
        {
            float ax0 = parent.X + parent.W * aminX, ax1 = parent.X + parent.W * amaxX;
            float ay0 = parent.Y + parent.H * aminY, ay1 = parent.Y + parent.H * amaxY;
            float w = (ax1 - ax0) + sizeX, h = (ay1 - ay0) + sizeY;
            float px = ax0 + (ax1 - ax0) * pivX + posX;
            float py = ay0 + (ay1 - ay0) * pivY + posY;
            return new URect(px - w * pivX, py - h * pivY, w, h);
        }

        private static URect Stretch(URect parent) { return parent; }

        /// <summary>Image.preserveAspect: the sprite fits inside the rect and sits according to the pivot.</summary>
        private static URect FitAspect(URect r, float spriteW, float spriteH, float pivX, float pivY)
        {
            float s = Math.Min(r.W / spriteW, r.H / spriteH);
            float w = spriteW * s, h = spriteH * s;
            return new URect(r.X + (r.W - w) * pivX, r.Y + (r.H - h) * pivY, w, h);
        }

        private Rectangle ToPixels(URect r)
        {
            float left = r.X * _scale;
            float top = (_canvasHeight - (r.Y + r.H)) * _scale;
            return new Rectangle((int)Math.Round(left), (int)Math.Round(top), (int)Math.Round(r.W * _scale), (int)Math.Round(r.H * _scale));
        }

        private void Fill(URect r, Vector4 color)
        {
            _sb.Draw(_white, ToPixels(r), Tint(color));
        }

        private void Sprite(Texture2D tex, URect r, Vector4 color)
        {
            _sb.Draw(tex, ToPixels(r), Tint(color));
        }

        /// <summary>Unity colour (gamma-authored rgba) to the layer's linear straight-alpha tint.</summary>
        private static Color Tint(Vector4 c)
        {
            return new Color(PostProcessor.GammaToLinear(c.X), PostProcessor.GammaToLinear(c.Y), PostProcessor.GammaToLinear(c.Z), c.W);
        }

        private static Vector4 ToVector(Color c) { return c.ToVector4(); }
        private static Color FromVector(Vector4 v) { return new Color(v); }
        private static Vector4 Rgba(Color c, float alpha) { Vector4 v = c.ToVector4(); v.W = alpha; return v; }

        /// <summary>Text centred (or left-aligned) on a rect's middle line; size is the Unity font size in canvas pixels.</summary>
        private void Text(SpriteFont font, int fontNominal, string text, URect r, float unitySize, bool left, Color color, float alpha = 1f)
        {
            float s = _scale * unitySize / fontNominal * FontPixelFactor;
            Vector2 size = font.MeasureString(text) * s;
            Rectangle px = ToPixels(r);
            float x = left ? px.X : px.X + (px.Width - size.X) * 0.5f;
            float y = px.Y + (px.Height - size.Y) * 0.5f;
            _sb.DrawString(font, text, new Vector2((float)Math.Round(x), (float)Math.Round(y)), Tint(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, alpha)), 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
        }

        private void TextRight(SpriteFont font, int fontNominal, string text, URect r, float unitySize, Color color, float alpha = 1f)
        {
            float s = _scale * unitySize / fontNominal * FontPixelFactor;
            Vector2 size = font.MeasureString(text) * s;
            Rectangle px = ToPixels(r);
            float x = px.X + px.Width - size.X;
            float y = px.Y + (px.Height - size.Y) * 0.5f;
            _sb.DrawString(font, text, new Vector2((float)Math.Round(x), (float)Math.Round(y)), Tint(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, alpha)), 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
        }

        // ------------------------------------------------------------------ draw

        /// <summary>Draws the HUD into the overlay layer for a frame of the given size; null when there is nothing to draw.</summary>
        public Texture2D BuildLayer(int width, int height)
        {
            if (_weapons == null)
            {
                return null;
            }
            if (_layer == null || _layer.Width != width || _layer.Height != height)
            {
                _layer?.Dispose();
                _layer = new RenderTarget2D(_gd, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }
            // the previous frame's final pass left the layer bound as a sampler texture; it must not be sampled while it is the render target
            for (int i = 0; i < 4; i++)
            {
                _gd.Textures[i] = null;
            }
            RenderTargetBinding[] previous = _gd.GetRenderTargets();
            Viewport viewport = _gd.Viewport;
            _gd.SetRenderTarget(_layer);
            _gd.Viewport = new Viewport(0, 0, width, height);
            _gd.Clear(Color.Transparent);
            DrawLayer(width, height);
            if (previous != null && previous.Length > 0)
            {
                _gd.SetRenderTargets(previous);
            }
            else
            {
                _gd.SetRenderTarget(null);
            }
            _gd.Viewport = viewport;
            return _layer;
        }

        private void DrawLayer(int width, int height)
        {
            _width = width;
            _height = height;
            _scale = width / RefWidth;
            _canvasHeight = height / _scale;
            var canvas = new URect(0f, 0f, RefWidth, _canvasHeight);

            _sb.Begin(SpriteSortMode.Deferred, LayerBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            DrawFeedback(canvas);   // FeedbackFlashCanvas has sorting order 0, the HUD canvas 1
            DrawMessage(canvas);
            DrawObjective(canvas);
            DrawNotifications(canvas);
            DrawCrosshair(canvas);
            DrawPauseHint(canvas);
            DrawBottomLeft(canvas);
            DrawJetpack(canvas);
            DrawAmmo(canvas);
            _sb.End();

            DrawCompass(canvas);
        }

        private void DrawFeedback(URect canvas)
        {
            float flash = FlashAlpha();
            if (flash > 0f)
            {
                var c = _flashColor;
                c.W *= flash;   // Image colour alpha x CanvasGroup alpha
                Fill(Stretch(canvas), c);
            }
            float vignette = VignetteAlpha();
            if (vignette > 0f && _vignette != null)
            {
                Sprite(_vignette, Stretch(canvas), new Vector4(0f, 0f, 0f, vignette));
            }
        }

        private void DrawCrosshair(URect canvas)
        {
            if (_crossSprite == null)
            {
                return;
            }
            // Crosshair (anchored centre, pos (0, -0.2)) > V: V's Image is CrosshairManager.CrosshairImage, so the weapon's sprite
            // replaces Crosshair_Blaster_Center and V's sizeDelta (lerped to CrosshairSize) is its size; preserveAspect is on.
            Texture2D sprite = Tex(_crossSprite);
            var crosshair = Layout(canvas, 0.5f, 0.5f, 0.5f, 0.5f, 0f, -0.2f, 5f, 5f, 0.5f, 0.5f);
            URect v = Layout(crosshair, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, _crossSize, _crossSize, 0.5f, 0.5f);
            Sprite(sprite, FitAspect(v, sprite.Width, sprite.Height, 0.5f, 0.5f), _crossColor);
        }

        private void DrawBottomLeft(URect canvas)
        {
            // BottomLeftcorner: VerticalLayoutGroup (padding 25, lower-left); HorizontalContent: PlayerHealth 194 x 30, 5 apart, Stance 30 x 30
            var health = new URect(25f, 25f, 194f, 30f);
            Fill(health, ToVector(BarBackground));
            var fill = new URect(health.X, health.Y, health.W * MathHelper.Clamp(HealthRatio, 0f, 1f), health.H);
            Fill(fill, ToVector(HealthColor));
            URect icon = Layout(health, 0f, 0f, 1f, 1f, 5f, 0f, -10f, -10f, 0f, 0.5f);
            Sprite(_lifeIcon, FitAspect(icon, _lifeIcon.Width, _lifeIcon.Height, 0f, 0.5f), Vector4.One);

            var stance = new URect(health.X + health.W + 5f, 25f, 30f, 30f);
            Fill(stance, new Vector4(0f, 0f, 0f, 0.2f));
            Texture2D st = _player != null && _player.IsCrouching ? _stanceCrouch : _stanceStand;
            URect stIcon = Layout(stance, 0.5f, 0.5f, 0.5f, 0.5f, -11.789978f, -12.915344f, 25f, 25f, 0f, 0f);
            Sprite(st, stIcon, new Vector4(0f, 0f, 0f, 1f));
        }

        private void DrawJetpack(URect canvas)
        {
            if (_jetpack == null || !_jetpack.IsJetpackUnlocked)
            {
                return;
            }
            // JetpackCounter 106 x 22, first child of BottomLeftcorner's VerticalLayoutGroup (lower-left, padding 25, spacing 5): above the 30 high health row
            var bar = new URect(25f, 25f + 30f + 5f, 106f, 22f);
            Fill(bar, _jetBg);
            var fill = new URect(bar.X, bar.Y, bar.W * MathHelper.Clamp(_jetpack.CurrentFillRatio, 0f, 1f), bar.H);   // Image.Filled, horizontal from the left
            Fill(fill, _jetFg);
            URect icon = Layout(bar, 0f, 0f, 1f, 1f, 4f, 0f, -8f, -8f, 0f, 0.5f);
            Sprite(_jetpackIcon, FitAspect(icon, _jetpackIcon.Width, _jetpackIcon.Height, 0f, 0.5f), new Vector4(0f, 0f, 0f, 1f));
        }

        private void DrawAmmo(URect canvas)
        {
            if (_hudWeapon == null)
            {
                return;
            }
            // AmmoCounter.prefab 153 x 22, bottom-right of the WeaponHUDManager layout group (padding 25)
            var bar = new URect(canvas.W - 25f - 153f, 25f, 153f, 22f);
            bool active = _hudWeapon == _weapons.ActiveWeapon;
            float alpha = active ? 1f : 0.5f;
            Vector4 bg = _ammoBg; bg.W *= alpha;
            Fill(bar, bg);
            var fill = new URect(bar.X, bar.Y, bar.W * MathHelper.Clamp(_ammoFill, 0f, 1f), bar.H);
            Vector4 fg = _ammoFg; fg.W *= alpha;
            Fill(fill, fg);
            // WeaponIcon: 200 x 22 container anchored bottom-right, icon stretched inside it, pivot (1, 0.5), black
            Texture2D icon = Tex(_hudWeapon.Settings.WeaponIcon);
            if (icon != null)
            {
                URect container = Layout(bar, 1f, 0f, 1f, 0f, 0f, 0f, 200f, 22f, 1f, 0f);
                Sprite(icon, FitAspect(container, icon.Width, icon.Height, 1f, 0.5f), new Vector4(0f, 0f, 0f, alpha));
            }
        }

        private void DrawPauseHint(URect canvas)
        {
            // PauseMenuInfo: top-left, pivot (0, 1), pos (22, 0), 219 x 40
            URect info = Layout(canvas, 0f, 1f, 0f, 1f, 22f, 0f, 219f, 40f, 0f, 1f);
            URect tab = Layout(info, 0f, 0.5f, 0f, 0.5f, 0f, 0f, 50f, 25f, 0f, 0.5f);
            Rounded(tab, KeyGrey, 5f);
            Text(_font12, 12, "Tab", tab, 12f, false, Color.White);
            URect label = Layout(info, 0.5f, 0.5f, 0.5f, 0.5f, 45f, 0f, 200f, 30f, 0.5f, 0.5f);
            Text(_font10, 10, "Pause/Options", label, 10f, true, Color.White);
        }

        private void DrawMessage(URect canvas)
        {
            // DisplayMessageRect: stretched horizontally at mid-height, scale 0.7; the message hangs from its centre line (UITable, Down)
            float t = _time - _messageStart;
            float alpha;
            if (t < 0f) return;
            if (t < MessageFadeIn) alpha = t / MessageFadeIn;
            else if (t < MessageFadeIn + MessageVisible) alpha = 1f;
            else if (t < MessageFadeIn + MessageVisible + MessageFadeOut) alpha = 1f - (t - MessageFadeIn - MessageVisible) / MessageFadeOut;
            else return;

            const float scale = 0.7f;
            float w = canvas.W * scale, h = 60f * scale;
            var band = new URect(canvas.CenterX - w * 0.5f, canvas.CenterY - h, w, h);
            Sprite(_gradient, band, new Vector4(1f, 1f, 1f, 0.29803923f * alpha));
            Text(_font35, 35, _message, band, 34.92f * scale, false, Color.White, alpha);
        }

        private void DrawObjective(URect canvas)
        {
            // ObjectivesRect: top-left, pivot (0, 1), pos (0, -55), layout padding left 43; the toast is 187 wide: title strip 18 high, then the description strip 14 high
            float alpha = MathHelper.Clamp((_time - _objDelay) / ObjectiveFadeIn, 0f, 1f);
            if (_time > _objCompleteTime)
            {
                alpha = Math.Min(alpha, 1f - (_time - _objCompleteTime) / ObjectiveFadeOut);   // ObjectiveToast.Complete(): fade out, then the toast is destroyed
            }
            if (alpha <= 0f)
            {
                return;
            }
            URect panel = Layout(canvas, 0f, 1f, 0f, 1f, 0f, -55f, 215.42f, 186.36f, 0f, 1f);
            var title = new URect(panel.X + 43f, panel.Y + panel.H - 18f, 187f, 18f);
            var sub = new URect(title.X, title.Y - 14f, 187f, 14f);
            Fill(title, new Vector4(0f, 0f, 0f, 0.6f * alpha));
            Fill(sub, new Vector4(0f, 0f, 0f, 0.2f * alpha));
            // icon: 18 x 18 white square hanging off the title's left edge (pivot (1, 0.5)), red enemy icon on top
            var icon = new URect(title.X - 18f, title.Y, 18f, 18f);
            Fill(icon, new Vector4(1f, 1f, 1f, alpha));
            Sprite(_enemyIcon, icon, new Vector4(1f, 0f, 0f, alpha));
            // texts: 4 px margins, left aligned, vertically centred; the title is upper-case (fontStyle 16)
            var titleText = new URect(title.X + 4f, title.Y, title.W - 8f, title.H);
            Text(_font9, 9, _objTitle.ToUpperInvariant(), titleText, 9f, true, new Color(1f, 0.7019608f, 0f), alpha);
            var subText = new URect(sub.X + 4f, sub.Y, sub.W - 8f, sub.H);
            Text(_font7, 7, _objDescription, subText, 7f, true, Color.White, alpha);
            if (_objCounter.Length > 0)
            {
                // CounterText: size 7, right aligned (TMP alignment 4); its rect is assumed to span the strip like the description's (not checked)
                TextRight(_font7, 7, _objCounter, subText, 7f, Color.White, alpha);
            }
        }

        private void DrawNotifications(URect canvas)
        {
            // newest on top, oldest at the bottom of the lower-left aligned stack
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                float age = _time - _notifications[i].Value;
                if (age >= NotifyFadeIn + NotifyVisible + NotifyFadeOut)
                {
                    _notifications.RemoveAt(i);
                }
            }
            for (int i = 0; i < _notifications.Count; i++)
            {
                float age = _time - _notifications[i].Value;
                float alpha = age < NotifyFadeIn ? age / NotifyFadeIn : age < NotifyFadeIn + NotifyVisible ? 1f : 1f - (age - NotifyFadeIn - NotifyVisible) / NotifyFadeOut;
                string text = _notifications[i].Key;
                float textW = _font9.MeasureString(text).X * (9f / 9f) * FontPixelFactor;
                float w = textW + 2f * NotifyPadX;
                var rect = new URect(NotifyLeft, NotifyBottom + i * (NotifyHeight + NotifySpacing), w, NotifyHeight);
                Fill(rect, new Vector4(1f, 1f, 1f, 0.8f * alpha));   // BackgroundImage: white, alpha 0.8, no sprite
                var textRect = new URect(rect.X + NotifyPadX, rect.Y, textW, NotifyHeight);
                Text(_font9, 9, text, textRect, 9f, true, Color.Black, alpha);   // TextMeshPro vertex colour black
            }
        }

        private void DrawCompass(URect canvas)
        {
            // Compass: top-centre, pivot (0.5, 1), pos (0, -25), 388 x 30, RectMask2D
            URect compass = Layout(canvas, 0.5f, 1f, 0.5f, 1f, 0f, -25f, 388f, 30f, 0.5f, 1f);
            Rectangle clip = ToPixels(compass);
            var raster = new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };
            Rectangle oldScissor = _gd.ScissorRectangle;
            _gd.ScissorRectangle = Rectangle.Intersect(clip, _gd.Viewport.Bounds);
            _sb.Begin(SpriteSortMode.Deferred, LayerBlend, SamplerState.LinearClamp, DepthStencilState.None, raster);
            Fill(compass, new Vector4(1f, 1f, 1f, 0.101960786f));                                   // CompassBackground
            Fill(new URect(compass.X, compass.CenterY - 0.5f, compass.W, 1f), Vector4.One);          // LineImage (height 1)

            float widthMultiplier = compass.W / CompassVisibilityAngle;
            float yaw = _player != null ? _player.YawDegrees : 0f;
            // Directions: Unity local positions (1,0,0) N, (1,0,-1) NE, (0,0,-1) E, ... ; angle = SignedAngle(forward, dir, up)
            string[] names = { "N", "", "E", "", "S", "", "W", "" };
            float[] headings = { 90f, 135f, 180f, -135f, -90f, -45f, 0f, 45f };   // atan2(x, z) of each direction in degrees
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.WrapAngle(MathHelper.ToRadians(headings[i] - yaw));
                angle = MathHelper.ToDegrees(angle);
                if (angle > -CompassVisibilityAngle / 2f && angle < CompassVisibilityAngle / 2f)
                {
                    // marker transform: localPosition (width * angle, -height / 2) from the compass pivot (top-centre), scale 0.5
                    float mx = compass.CenterX + widthMultiplier * angle;
                    float my = compass.CenterY;
                    float s = CompassMarkerScale;
                    var tick = new URect(mx - 3f * s * 0.5f, my - 30f * s * 0.5f, 3f * s, 30f * s);
                    Fill(tick, Vector4.One);
                    if (names[i].Length > 0)
                    {
                        var label = new URect(mx - 53.86f * s * 0.5f, my + 24.4f * s - 20f * s * 0.5f, 53.86f * s, 20f * s);
                        Text(_font22, 22, names[i], label, 22f * s, false, Color.White);
                    }
                }
            }
            DrawCompassEnemies(compass, widthMultiplier, yaw);
            _sb.End();
            _gd.ScissorRectangle = oldScissor;
        }

        private void DrawCompassEnemies(URect compass, float widthMultiplier, float yaw)
        {
            if (_director == null || _player == null)
            {
                return;
            }
            Vector3 p = _player.Position;
            foreach (Enemy e in _director.Enemies.Enemies)
            {
                // Compass.Update for a non-direction element: signed angle between the player's heading and the direction to the element, in Unity space (z negated)
                Vector3 d = e.Position - p;
                float heading = MathHelper.ToDegrees((float)Math.Atan2(d.X, -d.Z));
                float angle = MathHelper.ToDegrees(MathHelper.WrapAngle(MathHelper.ToRadians(heading - yaw)));
                if (!(angle > -CompassVisibilityAngle / 2f && angle < CompassVisibilityAngle / 2f))
                {
                    continue;
                }
                float limit = compass.H / 2f * CompassMarginRatio;
                float heightDifference = MathHelper.Clamp(d.Y * CompassHeightMultiplier, -limit, limit);
                float distanceRatio = MathHelper.Clamp(d.Length() / CompassDistanceMinScale, 0f, 1f);
                float size = CompassMarkerSize * MathHelper.Lerp(1f, CompassMarkerScale, distanceRatio);
                float mx = compass.CenterX + widthMultiplier * angle;
                float my = compass.CenterY + heightDifference;
                var rect = new URect(mx - size * 0.5f, my - size * 0.5f, size, size);
                Sprite(_radarBg, rect, ToVector(e.IsDetectingTarget ? CompassEnemyAlt : CompassEnemyDefault));   // MainImage colour: DefaultColor / AltColor
                float fg = size * 13f / 12f;   // ForegroundImage stretches over the marker with sizeDelta (1, 1)
                Sprite(_radarIcon, new URect(mx - fg * 0.5f, my - fg * 0.5f, fg, fg), new Vector4(1f, 0f, 0f, 1f));
            }
        }

        /// <summary>Rounded rectangle standing in for Unity's built-in UISprite (sliced); a thin darker rim like the original.</summary>
        private void Rounded(URect r, Color color, float radiusCanvas)
        {
            Rectangle px = ToPixels(r);
            int radius = Math.Max(1, (int)Math.Round(radiusCanvas * _scale));
            long key = ((long)px.Width << 40) | ((long)px.Height << 20) | (long)radius;
            if (!_rounded.TryGetValue(key, out Texture2D tex))
            {
                tex = BuildRounded(px.Width, px.Height, radius);
                _rounded[key] = tex;
            }
            _sb.Draw(tex, px, Tint(color.ToVector4()));
        }

        private Texture2D BuildRounded(int w, int h, int radius)
        {
            var data = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Math.Max(Math.Max(radius - x - 0.5f, x + 0.5f - (w - radius)), 0f);
                    float dy = Math.Max(Math.Max(radius - y - 0.5f, y + 0.5f - (h - radius)), 0f);
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    float a = MathHelper.Clamp(radius - dist + 0.5f, 0f, 1f);
                    float rim = MathHelper.Clamp(radius - dist - 0.5f, 0f, 1f);   // darker over the outer pixel
                    float shade = 0.35f + 0.65f * rim;
                    data[y * w + x] = new Color(shade, shade, shade, a);
                }
            }
            var t = new Texture2D(_gd, w, h);
            t.SetData(data);
            return t;
        }
    }
}

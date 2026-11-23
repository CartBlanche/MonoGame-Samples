//-----------------------------------------------------------------------------
// PreScreenSkyCubeMapping.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using RacingGame.Graphics;
using RacingGame.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RacingGame.GameScreens;
using XnaModel = Microsoft.Xna.Framework.Graphics.Model;

namespace RacingGame.Shaders
{
    /// <summary>
    /// Pre screen sky cube mapping
    /// </summary>
    public class PreScreenSkyCubeMapping : ShaderEffect
    {
        /// <summary>
        /// Shader effect filename.
        /// </summary>
        const string Filename = "PreScreenSkyCubeMapping.fx";

        /// <summary>
        /// Sky cube map texture filename.
        /// </summary>
        const string SkyCubeMapFilename = "SkyCubeMap";

        /// <summary>
        /// Stars-only night sky cube map (added on top of the day sky, see the shader).
        /// </summary>
        const string NightSkyCubeMapFilename = "NightSkyCubeMap";
        private TextureCube nightSkyCubeMapTexture = null;
        private EffectParameter nightSkyParameter = null;
        private EffectParameter moonDirParameter = null, moonParamsParameter = null;
        // angular radius of the (enlarged) moon in degrees
        const float MoonRadiusDegrees = 3.2f;

        /// <summary>
        /// Default sky color
        /// </summary>
        static readonly Color DefaultSkyColor = new Color(232, 232, 232);

        /// <summary>
        /// The Cube Map texture for the sky!
        /// </summary>
        private TextureCube skyCubeMapTexture = null;

        /// <summary>
        /// Sky cube map texture
        /// </summary>
        /// <returns>Texture cube</returns>
        public TextureCube SkyCubeMapTexture
        {
            get
            {
                return skyCubeMapTexture;
            }
        }

        private XnaModel cube;
        /// <summary>
        /// Create pre screen sky cube mapping
        /// </summary>
        public PreScreenSkyCubeMapping()
            : base(Filename)
        {
            cube = BaseGame.Content.Load<XnaModel>(Path.Combine(Directories.ContentDirectory, "Models", "Cube"));
        }
        /// <summary>
        /// Reload
        /// </summary>
        protected override void GetParameters()
        {
            base.GetParameters();
            
            // Load and set cube map texture.
            // On Android the DDS asset uses DXT1 compression which is not supported
            // on GLES. Catch the failure so the rest of the game still loads;
            // sky rendering and reflections will be skipped until the content
            // pipeline is configured to produce an Android-compatible format.
            try
            {
                skyCubeMapTexture = BaseGame.Content.Load<TextureCube>(
                    Path.Combine(Directories.ContentDirectory,
                     "Textures",
                    SkyCubeMapFilename));
                diffuseTexture.SetValue(skyCubeMapTexture);
            }
            catch (Exception ex)
            {
                Log.Write("SkyCubeMap load failed (likely unsupported DXT format on this platform): " + ex.Message);
                skyCubeMapTexture = null;
            }

            // Night stars layer; optional, the sky simply has no stars if it fails to load.
            nightSkyParameter = effect.Parameters["NightSky"];
            moonDirParameter = effect.Parameters["MoonDir"];
            moonParamsParameter = effect.Parameters["MoonParams"];
            try
            {
                nightSkyCubeMapTexture = BaseGame.Content.Load<TextureCube>(
                    Path.Combine(Directories.ContentDirectory,
                    "Textures", NightSkyCubeMapFilename));
                effect.Parameters["nightTexture"]?.SetValue(nightSkyCubeMapTexture);
            }
            catch (Exception ex)
            {
                Log.Write("NightSkyCubeMap load failed: " + ex.Message);
                nightSkyCubeMapTexture = null;
            }

            // Set sky color to nearly white
            AmbientColor = DefaultSkyColor;
        }
        /// <summary>
        /// The sky has its own tint (TrackLighting.Sky), not the material tints.
        /// </summary>
        protected override bool TintColors
        {
            get { return false; }
        }

        /// <summary>
        /// Render sky with help of shader.
        /// </summary>
        public void RenderSky(Color setSkyColor)
        {
            // Can't render with shader if shader is not valid!
            if (this.Valid == false)
                return;

            // Texture failed to load (e.g. unsupported format on this platform).
            if (skyCubeMapTexture == null)
                return;

            // Don't use or write to the z buffer
            BaseGame.Device.DepthStencilState = DepthStencilState.None;
            BaseGame.Device.RasterizerState = RasterizerState.CullNone;

            // Also don't use any kind of blending.
            BaseGame.Device.BlendState = BlendState.Opaque;

            // Set effect parameters
            AmbientColor = BaseGame.Lighting.Sky(setSkyColor);
            if (nightSkyParameter != null)
                nightSkyParameter.SetValue(new Vector4(
                    nightSkyCubeMapTexture != null ? BaseGame.Lighting.StarBlend : 0.0f,
                    1.0f, 0.08f, 0.25f));
            if (moonDirParameter != null && moonParamsParameter != null)
            {
                // game up is +Z, the cube map's up is +Y (same swap as the sky shader's texCoord)
                Vector3 l = Vector3.Normalize(BaseGame.Lighting.LightPos);
                moonDirParameter.SetValue(new Vector4(
                    l.X, l.Z, l.Y,
                    1.0f - (float)Math.Cos(MathHelper.ToRadians(MoonRadiusDegrees))));
                moonParamsParameter.SetValue(new Vector4(
                    BaseGame.Lighting.StarBlend, 0.45f, 7.0f, 0.0f));
            }
            effect.Parameters["view"].SetValue(BaseGame.ViewMatrix);
            ProjectionMatrix = BaseGame.ProjectionMatrix;

            // Override model's effect and render
            cube.Meshes[0].MeshParts[0].Effect = effect;
            cube.Meshes[0].Draw();

            // Reset previous render states
            BaseGame.Device.DepthStencilState = DepthStencilState.Default;
            BaseGame.Device.RasterizerState = RasterizerState.CullCounterClockwise;

            BaseGame.Device.BlendState = BlendState.AlphaBlend;
        }

        /// <summary>
        /// Render sky
        /// </summary>
        public void RenderSky()
        {
            RenderSky(DefaultSkyColor);
        }
    }
}

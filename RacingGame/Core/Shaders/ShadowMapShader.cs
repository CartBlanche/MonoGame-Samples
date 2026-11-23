//-----------------------------------------------------------------------------
// ShadowMapShader.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.GameScreens;
using Model = RacingGame.Graphics.Model;
using Texture = RacingGame.Graphics.Texture;

namespace RacingGame.Shaders
{
    /// <summary>
    /// Shadow map shader
    /// </summary>
    public class ShadowMapShader : ShaderEffect
    {
        /// <summary>
        /// Shadow mapping shader filename
        /// </summary>
        const string ShaderFilename = "ShadowMap.fx";

        /// <summary>
        /// Shadow map texture we render to.
        /// </summary>
        internal RenderToTexture
            shadowMapTexture = null;

        /// <summary>
        /// Restrict near and far plane for much better depth resolution!
        /// </summary>
        internal float
            shadowNearPlane = 1.0f,
            shadowFarPlane = 1.0f * 28;

        /// <summary>
        /// Virtual point light parameters for directional shadow map lighting.
        /// Used to create a point light position for the directional light.
        /// </summary>
        internal float
            virtualLightDistance = 24,
            virtualVisibleRange = 23.5f;

        /// <summary>
        /// Shadow distance
        /// </summary>
        /// <returns>Float</returns>
        public float ShadowDistance
        {
            get
            {
                return virtualLightDistance;
            }
        }

        private Vector3 shadowLightPos = Vector3.Zero;

        /// <summary>
        /// Shadow light position
        /// </summary>
        /// <returns>Vector 3</returns>
        public Vector3 ShadowLightPos
        {
            get
            {
                return shadowLightPos;
            }
        }

        /// <summary>
        /// Texel width and height and offset for texScaleBiasMatrix,
        /// this way we can directly access the middle of each texel.
        /// </summary>
        float
            texelWidth = 1.0f / 1024.0f,
            texelHeight = 1.0f / 1024.0f,
            texOffsetX = 0.5f,
            texOffsetY = 0.5f;

        /// <summary>
        /// Compare depth bias
        /// </summary>
        internal float compareDepthBias = 0.00025f;

        /// <summary>
        /// Tex extra scale
        /// </summary>
        /// <returns>1.0f</returns>
        internal float texExtraScale = 1.0f;

        /// <summary>
        /// Shadow map depth bias value
        /// </summary>
        /// <returns>+</returns>
        internal float shadowMapDepthBiasValue = 0.00025f;

        /// <summary>
        /// The matrix to convert proj screen coordinates in the -1..1 range
        /// to the shadow depth map texture coordinates.
        /// </summary>
        Matrix texScaleBiasMatrix;

        /// <summary>
        /// Used matrices for the light casting the shadows.
        /// </summary>
        internal Matrix lightProjectionMatrix, lightViewMatrix;

        /// <summary>
        /// Additional effect handles
        /// </summary>
        private EffectParameter
            shadowTexTransform,
            worldViewProjLight,
            nearPlane,
            farPlane,
            depthBias,
            shadowMapDepthBias,
            shadowMap,
            shadowMapTexelSize,
            shadowDistanceFadeoutTexture;

        /// <summary>
        /// Shadow map blur post screen shader, used in RenderShadows
        /// to blur the shadow results.
        /// </summary>
        internal ShadowMapBlur shadowMapBlur = null;
        /// <summary>
        /// Calculate the texScaleBiasMatrix for converting proj screen
        /// coordinates in the -1..1 range to the shadow depth map
        /// texture coordinates.
        /// </summary>
        internal void CalcShadowMapBiasMatrix()
        {
            texelWidth = 1.0f / (float)shadowMapTexture.Width;
            texelHeight = 1.0f / (float)shadowMapTexture.Height;
            texOffsetX = 0.5f + (0.5f / (float)shadowMapTexture.Width);
            texOffsetY = 0.5f + (0.5f / (float)shadowMapTexture.Height);

            texScaleBiasMatrix = new Matrix(
                0.5f * texExtraScale, 0.0f, 0.0f, 0.0f,
                0.0f, -0.5f * texExtraScale, 0.0f, 0.0f,
                0.0f, 0.0f, texExtraScale, 0.0f,
                texOffsetX, texOffsetY, 0.0f, 1.0f);
        }
        /// <summary>
        /// Shadow map shader
        /// </summary>
        public ShadowMapShader()
            : base(ShaderFilename)
        {
            // We use R32F, etc. and have a lot of precision
            compareDepthBias = 0.0001f;

            // Ok, time to create the shadow map render target
            shadowMapTexture = new RenderToTexture(
                RenderToTexture.SizeType.ShadowMap);

            CalcShadowMapBiasMatrix();

            shadowMapBlur = new ShadowMapBlur();
        }

        protected override void SetParameterDefaultValues()
        {
            // FilterTaps is hardcoded inside PS_UseShadowMap20 to avoid GLSL std140
            // float2 array padding (16 bytes/element on GPU vs 8 bytes on CPU),
            // which caused a BlockCopy overrun on DesktopGL.
        }
        /// <summary>
        /// Get parameters
        /// </summary>
        protected override void GetParameters()
        {
            // Can't get parameters if loading failed!
            if (effect == null)
                return;

            base.GetParameters();

            // Get additional parameters
            shadowTexTransform = effect.Parameters["shadowTexTransform"];
            worldViewProjLight = effect.Parameters["worldViewProjLight"];
            nearPlane = effect.Parameters["nearPlane"];
            farPlane = effect.Parameters["farPlane"];
            depthBias = effect.Parameters["depthBias"];
            shadowMapDepthBias = effect.Parameters["shadowMapDepthBias"];
            shadowMap = effect.Parameters["ShadowMap"];
            shadowMapTexelSize = effect.Parameters["shadowMapTexelSize"];
            shadowDistanceFadeoutTexture =
                effect.Parameters["shadowDistanceFadeoutTexture"];
            // Load shadowDistanceFadeoutTexture
            if (shadowDistanceFadeoutTexture != null)
                shadowDistanceFadeoutTexture.SetValue(
                    new Texture("ShadowDistanceFadeoutMap").XnaTexture);
        }
        /// <summary>
        /// Update parameters
        /// </summary>
        public override void SetParameters(Material setMat)
        {
            // Can't set parameters if loading failed!
            if (effect == null)
                return;

            shadowNearPlane = 1.0f;
            shadowFarPlane = 6.25f * 28 * 1.25f;
            virtualLightDistance = 5.5f * 24 * 1.3f;
            virtualVisibleRange = 5.5f * 23.5f;

            compareDepthBias = 0.00065f;
            shadowMapDepthBiasValue = 0.00065f;

            base.SetParameters(setMat);

            // Set all extra parameters for this shader
            depthBias?.SetValue(compareDepthBias);
            shadowMapDepthBias?.SetValue(shadowMapDepthBiasValue);
            shadowMapTexelSize?.SetValue(
                new Vector2(texelWidth, texelHeight));
            if (nearPlane != null)
                nearPlane.SetValue(shadowNearPlane);
            farPlane?.SetValue(shadowFarPlane);
        }
        /// <summary>
        /// Calc simple directional shadow mapping matrix
        /// </summary>
        /// <summary>
        /// How much wider the shadow map reaches than the classic one (1 = classic). Integrated
        /// shadows only. Default 1: widening to 2x or 3x was tried (2026-11-23) and showed no
        /// visible gain (no extra palm or rock shadows, only coarser edges), so it stays a test
        /// option: env RACINGGAME_SHADOWCOVER=1..6.
        /// </summary>
        public static float ShadowCoverage
        {
            get
            {
                if (RacingGame.Graphics.TrackLighting.IntegratedShadows == false)
                    return 1.0f;
                return shadowCoverageValue;
            }
        }
        static readonly float shadowCoverageValue = ParseCoverage();
        static float ParseCoverage()
        {
            float v;
            return float.TryParse(Environment.GetEnvironmentVariable("RACINGGAME_SHADOWCOVER"),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out v) && v >= 1.0f && v <= 6.0f ? v : 1.0f;
        }

        // Feedback round 1 switches (integrated only; env RACINGGAME_SHADOWAHEAD / SHADOWSQ / SHADOWSNAP)
        static readonly float LookAheadUnits = EnvF("RACINGGAME_SHADOWAHEAD", 35.0f);
        // Integrated map size per unit of light distance (width and height), 0 = classic proportions.
        static readonly float MapSize = EnvF("RACINGGAME_SHADOWMAPSIZE", 0.85f);
        // Distance (units) the car keeps from the usable border of the map.
        static readonly float ShadowCarMargin = EnvF("RACINGGAME_SHADOWCARMARGIN", 14.0f);
        static readonly bool SnapTexels = EnvF("RACINGGAME_SHADOWSNAP", 1.0f) > 0.5f;
        static float EnvF(string n, float def)
        {
            float v;
            return float.TryParse(Environment.GetEnvironmentVariable(n),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out v) ? v : def;
        }

        private void CalcSimpleDirectionalShadowMappingMatrix()
        {
            // Put light for directional mode away from origin (create virutal point
            // light). But adjust field of view to see enough of the visible area.
            float virtualFieldOfView = (float)Math.Atan2(
                virtualVisibleRange, virtualLightDistance);

            // Set projection matrix for light
            // Integrated shadows can cover a wider area (ShadowCoverage > 1, the same map stretched
            // over more world); classic keeps the original size exactly.
            bool integrated = RacingGame.Graphics.TrackLighting.IntegratedShadows;
            float cover = ShadowCoverage;
            // Integrated: the map is wider than the classic one (classic width was only 0.645 of
            // its height, so a car driving across the sun saw shadows only about 58 units ahead)
            // and a little less tall (the sun axis had more reach than needed).
            float widthFactor = 1.0f, heightFactor = 1.0f;
            if (integrated && MapSize > 0)
            {
                widthFactor = MapSize / virtualFieldOfView;
                heightFactor = MapSize;
            }
            lightProjectionMatrix = Matrix.CreatePerspective(
                // Don't use graphics fov and aspect ratio in directional lighting mode
                virtualFieldOfView * cover * widthFactor,
                1.0f * cover * heightFactor,
                shadowNearPlane,
                shadowFarPlane);

            // Light view plane axes (the light looks along -LightDirection, up is world z)
            Vector3 lf = -BaseGame.LightDirection;
            Vector3 lr = Vector3.Normalize(Vector3.Cross(lf, new Vector3(0, 0, 1)));
            Vector3 lu = Vector3.Cross(lr, lf);

            // Calc light look pos, put it a little bit in front of our car
            // (integrated: further ahead, a racing game sees the shadows coming from afar, but never
            // so far that the car itself gets near the border of the map)
            Vector3 carDir = RacingGameManager.Player.CarDirection;
            float lookAhead = virtualVisibleRange / 6;
            if (integrated)
            {
                lookAhead = LookAheadUnits;
                float hx = 0.44f * virtualFieldOfView * cover * widthFactor * virtualVisibleRange - ShadowCarMargin;
                float hy = 0.44f * cover * heightFactor * virtualVisibleRange - ShadowCarMargin;
                float ar = Math.Abs(Vector3.Dot(carDir, lr)), au = Math.Abs(Vector3.Dot(carDir, lu));
                if (ar > 0.001f) lookAhead = Math.Min(lookAhead, hx / ar);
                if (au > 0.001f) lookAhead = Math.Min(lookAhead, hy / au);
                lookAhead = Math.Max(lookAhead, 0.0f);
            }
            Vector3 lightLookPos =
                RacingGameManager.InMenu ? RacingGameManager.Player.CarPosition :
                RacingGameManager.Player.CarPosition + carDir * lookAhead;

            if (integrated && SnapTexels && shadowMapTexture != null)
            {
                // Snap the look-at point to whole texels of the light view plane (at the depth of
                // the look-at point), so the texel grid stays fixed in the world while the car
                // moves (no swimming, no crawling edges).
                float tx = virtualVisibleRange * virtualFieldOfView * cover * widthFactor / shadowMapTexture.Width;
                float ty = virtualVisibleRange * 1.0f * cover * heightFactor / shadowMapTexture.Height;
                float a = Vector3.Dot(lightLookPos, lr);
                float b = Vector3.Dot(lightLookPos, lu);
                lightLookPos += lr * ((float)Math.Round(a / tx) * tx - a) + lu * ((float)Math.Round(b / ty) * ty - b);
            }

            // Well, this is how directional lights are done:
            lightViewMatrix = Matrix.CreateLookAt(
                // Use our current car position for our light look at origin!
                lightLookPos +
                BaseGame.LightDirection * virtualVisibleRange,//virtualLightDistance,
                lightLookPos,
                new Vector3(0, 0, 1));

            // Update light pos
            Matrix invView = Matrix.Invert(lightViewMatrix);
            shadowLightPos = new Vector3(invView.M41, invView.M42, invView.M43);
        }
        /// <summary>
        /// Integrated shadows: is a sphere inside (or touching) the shadow light frustum?
        /// A caster outside this frustum cannot put anything into the map, because the
        /// projection runs along the light rays; this is the exact selection radius.
        /// Allocation free: one view transform and a few compares.
        /// </summary>
        internal bool IsInShadowFrustum(Vector3 center, float radius)
        {
            Matrix v = lightViewMatrix;
            float x = center.X * v.M11 + center.Y * v.M21 + center.Z * v.M31 + v.M41;
            float y = center.X * v.M12 + center.Y * v.M22 + center.Z * v.M32 + v.M42;
            float z = center.X * v.M13 + center.Y * v.M23 + center.Z * v.M33 + v.M43;
            float d = -z;
            if (d < -radius || d > shadowFarPlane + radius)
                return false;
            Matrix p = lightProjectionMatrix;
            // Half extents of the frustum at depth d: d / M11 and d / M22
            float hx = d / p.M11 + radius;
            float hy = d / p.M22 + radius;
            return x >= -hx && x <= hx && y >= -hy && y <= hy;
        }

        bool alphaGenerate = false;
        Texture2D lastCasterTexture = null;
        EffectParameter casterTextureParam = null;

        /// <summary>
        /// Integrated shadows: switch the generate pass between the plain depth technique
        /// (tex null) and the alpha tested one (cut-out leaves) with the given texture.
        /// </summary>
        internal void SetGenerateCaster(Texture2D tex)
        {
            if (effect == null)
                return;
            if (tex == null)
            {
                if (alphaGenerate == false)
                    return;
                alphaGenerate = false;
                effect.CurrentTechnique = effect.Techniques["GenerateShadowMap20"];
            }
            else
            {
                if (casterTextureParam == null)
                    casterTextureParam = effect.Parameters["casterTexture"];
                if (alphaGenerate == false)
                {
                    alphaGenerate = true;
                    effect.CurrentTechnique = effect.Techniques["GenerateShadowMapAlpha20"];
                }
                if (lastCasterTexture != tex)
                {
                    lastCasterTexture = tex;
                    casterTextureParam?.SetValue(tex);
                }
            }
            effect.CurrentTechnique.Passes[0].Apply();
        }

        /// <summary>
        /// Update shadow world matrix.
        /// Calling this function is important to keep the shaders
        /// WorldMatrix and WorldViewProjMatrix up to date.
        /// </summary>
        /// <param name="setWorldMatrix">World matrix</param>
        internal void UpdateGenerateShadowWorldMatrix(Matrix setWorldMatrix)
        {
            Matrix world = setWorldMatrix;
            WorldMatrix = world;
            WorldViewProjMatrix =
                world * lightViewMatrix * lightProjectionMatrix;
            effect.CurrentTechnique.Passes[0].Apply();
        }

        /// <summary>
        /// Generate shadow
        /// </summary>
        internal void GenerateShadows(BaseGame.RenderHandler renderObjects)
        {
            // Can't generate shadow if loading failed!
            if (effect == null)
                return;

            // This method sets all required shader variables.
            this.SetParameters(null);
            Matrix remViewMatrix = BaseGame.ViewMatrix;
            Matrix remProjMatrix = BaseGame.ProjectionMatrix;
            CalcSimpleDirectionalShadowMappingMatrix();

            // Time to generate the shadow texture
            // Start rendering onto the shadow map
            shadowMapTexture.SetRenderTarget();

            // Make sure depth buffer is on
            BaseGame.Device.DepthStencilState = DepthStencilState.Default;
            // Disable alpha
            BaseGame.Device.BlendState = BlendState.Opaque;

            // Clear render target
            shadowMapTexture.Clear(Color.White);

            effect.CurrentTechnique = effect.Techniques["GenerateShadowMap20"];
            alphaGenerate = false;
            lastCasterTexture = null;

            // Render shadows with help of the GenerateShadowMap shader
            RenderSinglePassShader(renderObjects);

            // Resolve the render target to get the texture (required for Xbox)
            shadowMapTexture.Resolve();
            // Set render target back to default
            BaseGame.ResetRenderTarget(false);

            BaseGame.ViewMatrix = remViewMatrix;
            BaseGame.ProjectionMatrix = remProjMatrix;
        }
        /// <summary>
        /// Update calc shadow world matrix, has to be done for each object
        /// we want to render in CalcShadows.
        /// </summary>
        /// <param name="setWorldMatrix">Set world matrix</param>
        internal void UpdateCalcShadowWorldMatrix(Matrix setWorldMatrix)
        {
            this.WorldMatrix = setWorldMatrix;
            this.WorldViewProjMatrix =
                setWorldMatrix * BaseGame.ViewMatrix * BaseGame.ProjectionMatrix;

            // Compute the matrix to transform from view space to light proj:
            // inverse of view matrix * light view matrix * light proj matrix
            Matrix lightTransformMatrix =
                setWorldMatrix *
                lightViewMatrix *
                lightProjectionMatrix *
                texScaleBiasMatrix;
            shadowTexTransform.SetValue(lightTransformMatrix);

            Matrix worldViewProjLightMatrix =
                setWorldMatrix *
                lightViewMatrix *
                lightProjectionMatrix;
            worldViewProjLight.SetValue(worldViewProjLightMatrix);

            effect.CurrentTechnique.Passes[0].Apply();
        }

        BaseGame.RenderHandler pendingReceiveObjects, receiveShadowsHandler;

        /// <summary>The shadow receive pass drawn into the shadow blur's scene map.</summary>
        void RenderReceivePass()
        {
            effect.CurrentTechnique = effect.Techniques["UseShadowMap20"];

            // This method sets all required shader variables.
            this.SetParameters(null);

            // Use the shadow map texture here which was generated in
            // GenerateShadows().
            shadowMap?.SetValue(shadowMapTexture.XnaTexture);

            // Render shadows with help of the UseShadowMap shader
            RenderSinglePassShader(pendingReceiveObjects);
        }

        /// <summary>
        /// Calc shadows with help of generated light depth map,
        /// all objects have to be rendered again for comparing.
        /// We could save a pass when directly rendering here, but this
        /// has 2 disadvantages: 1. we can't post screen blur the shadow
        /// and 2. we can't use any other shader, especially bump and specular
        /// mapping shaders don't have any instructions left with ps_1_1.
        /// This way everything is kept simple, we can do as complex shaders
        /// as we want, the shadow shaders work seperately.
        /// </summary>
        /// <param name="renderObjects">Render objects</param>
        public void RenderShadows(BaseGame.RenderHandler renderObjects)
        {
            // Can't calc shadows if loading failed!
            if (effect == null)
                return;

            // Make sure z buffer and writing z buffer is on
            BaseGame.Device.DepthStencilState = DepthStencilState.Default;

            // Render shadows into our shadowMapBlur render target. The pass is a cached delegate
            // (a closure here allocated every frame); the objects to draw wait in a field.
            pendingReceiveObjects = renderObjects;
            shadowMapBlur.RenderShadows(
                receiveShadowsHandler ?? (receiveShadowsHandler = RenderReceivePass));
            pendingReceiveObjects = null;

            // Start rendering the shadow map blur (pass 1, which messes up our
            // background), pass 2 can be done below without any render targets.
            shadowMapBlur.RenderShadows();

            // Kill background z buffer (else glass will not be rendered correctly)
            RacingGameManager.Device.Clear(ClearOptions.DepthBuffer, Color.Black, 1, 0);
        }
        /// <summary>
        /// Generates and renders shadows for all game objects
        /// </summary>
        public static void PrepareGameShadows()
        {
            if (BaseGame.AllowShadowMapping)
            {
                // Generate shadows
                ShaderEffect.shadowMapping.GenerateShadows(
                    delegate
                    {
                        RacingGameManager.Landscape.GenerateShadow();
                        RacingGameManager.CarModel.GenerateShadow(
                            RacingGameManager.Player.CarRenderMatrix);
                    });

                // Integrated shadows (prototype): the lit shaders look the map up themselves,
                // so the receive pass, the blur and the composite are skipped.
                if (TrackLighting.IntegratedShadows)
                {
                    ShaderEffect.shadowMapping.PublishIntegratedShadows();
                    return;
                }

                // Render shadows
                ShaderEffect.shadowMapping.RenderShadows(
                    delegate
                    {
                        RacingGameManager.Landscape.UseShadow();
                        RacingGameManager.CarModel.UseShadow(
                            RacingGameManager.Player.CarRenderMatrix);
                    });
            }
            else if (TrackLighting.IntegratedShadows)
                BaseGame.Lighting.PublishShadowsOff();
        }
        /// <summary>Publish the shadow map to the terrain and road shaders (integrated mode).</summary>
        internal void PublishIntegratedShadows()
        {
            BaseGame.Lighting.PublishShadows(
                lightViewMatrix * lightProjectionMatrix * texScaleBiasMatrix,
                shadowMapTexture.XnaTexture, texelWidth);
        }

        /// <summary>
        /// Show Shadows
        /// </summary>
        public void ShowShadows()
        {
            shadowMapBlur.ShowShadows();
        }
    }
}

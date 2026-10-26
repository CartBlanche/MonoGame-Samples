using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Rendering.Particles;

namespace Shooter.Rendering
{
    public sealed partial class SceneRenderer
    {
        private const int MaxBones = 48; // must match MAX_BONES in Effects/Common.fxh

        private readonly Matrix[] _boneScratch = new Matrix[MaxBones];

        private static readonly BlendState AdditiveBlend = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One
        };

        private static readonly BlendState MultiplyBlend = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One
        };

        private static readonly BlendState AlphaBlendStraight = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };

        private static readonly BlendState PremultipliedBlend = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };

        private static readonly BlendState ProjectorBlend = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue
        };

        private static readonly RasterizerState ProjectorRaster = new RasterizerState
        {
            CullMode = CullMode.CullCounterClockwiseFace,
            DepthBias = -1f / 65536f,
            SlopeScaleDepthBias = -1f
        };

        private static readonly DepthStencilState DepthTestOnly = DepthStencilState.DepthRead;

        // cached effect parameters of Lit.fx
        private EffectParameter _lWorld, _lViewProj, _lView, _lShadowVP, _lBones, _lCamPos, _lBaseColor, _lTiling, _lEmission,
            _lMetallic, _lSmooth, _lBump, _lOcc, _lCutoff, _lFlags, _lFlags2, _lSrgb, _lSunDir, _lSunColor, _lAmbient,
            _lShadowParams, _lShadowFade, _lShadowBias, _lBaseMap, _lNormalMap, _lMetalMap, _lOccMap, _lEmitMap, _lShadowMap, _lSsaoMap;

        private void InitLitConstants()
        {
            var p = _litFx.Parameters;
            _lWorld = p["World"];
            _lViewProj = p["ViewProjection"];
            _lView = p["View"];
            _lShadowVP = p["ShadowViewProjection"];
            _lBones = p["Bones"];
            _lCamPos = p["CameraPosition"];
            _lBaseColor = p["BaseColor"];
            _lTiling = p["TilingOffset"];
            _lEmission = p["EmissionColor"];
            _lMetallic = p["Metallic"];
            _lSmooth = p["Smoothness"];
            _lBump = p["BumpScale"];
            _lOcc = p["OcclusionStrength"];
            _lCutoff = p["Cutoff"];
            _lFlags = p["MaterialFlags"];
            _lFlags2 = p["MaterialFlags2"];
            _lSrgb = p["SrgbFlags"];
            _lSunDir = p["SunDirection"];
            _lSunColor = p["SunColor"];
            _lAmbient = p["AmbientColor"];
            _lShadowParams = p["ShadowParams"];
            _lShadowFade = p["ShadowFade"];
            _lShadowBias = p["ShadowBias"];
            _lBaseMap = p["BaseMap"];
            _lNormalMap = p["NormalMap"];
            _lMetalMap = p["MetallicGlossMap"];
            _lOccMap = p["OcclusionMap"];
            _lEmitMap = p["EmissionMap"];
            _lShadowMap = p["ShadowMap"];
            _lSsaoMap = p["SsaoMap"];
            if (Environment.GetEnvironmentVariable("DUMP") != null)
            {
                foreach (var prm in _litFx.Parameters)
                    Console.WriteLine($"Lit param: {prm.Name} {prm.ParameterClass} {prm.ParameterType} rows={prm.RowCount} cols={prm.ColumnCount} elems={prm.Elements.Count}");
            }
        }

        private static void Set(EffectParameter p, Vector4 v) { if (p != null) p.SetValue(v); }
        private static void Set(EffectParameter p, Vector3 v) { if (p != null) p.SetValue(v); }
        private static void Set(EffectParameter p, Vector2 v) { if (p != null) p.SetValue(v); }
        private static void Set(EffectParameter p, float v) { if (p != null) p.SetValue(v); }
        private static void Set(EffectParameter p, Matrix v) { if (p != null) p.SetValue(v); }
        private static void Set(EffectParameter p, Texture2D v) { if (p != null) p.SetValue(v); }

        // ================================================================== shadow map

        private void RenderShadowMap(SceneLighting lighting, Camera camera, ref FrameLight light, int itemCount)
        {
            _casters.Clear();

            // preliminary fit (sphere only) to pre-select casters
            var prelim = ShadowMapper.Fit(camera, light.SunDirection, lighting.ShadowDistance, ShadowMapSize, float.MaxValue);
            Vector3 dir = light.SunDirection;
            float minAlong = float.MaxValue;
            for (int i = 0; i < itemCount; i++)
            {
                ref RenderItem item = ref _items[i];
                if (item.Mesh == null || !item.CastShadows || item.Layer != RenderLayer.Default || !_hasBounds[i])
                    continue;
                BoundingBox b = _worldBounds[i];
                Vector3 c = (b.Min + b.Max) * 0.5f;
                float r = (b.Max - b.Min).Length() * 0.5f;
                Vector3 rel = c - prelim.SphereCenter;
                float along = Vector3.Dot(rel, dir);
                float perp = (rel - dir * along).Length();
                if (perp > prelim.SphereRadius + r)
                    continue;
                _casters.Add(i);
                minAlong = Math.Min(minAlong, Vector3.Dot(c, dir) - r);
            }

            var fit = ShadowMapper.Fit(camera, light.SunDirection, lighting.ShadowDistance, ShadowMapSize, minAlong);
            light.ShadowViewProjection = fit.ViewProjection;

            // URP GetShadowBias: bias in world texels, soft shadow kernel radius 2.5 (medium quality)
            float depthBias = -lighting.ShadowDepthBias * fit.WorldTexelSize * 2.5f;
            float normalBias = -lighting.ShadowNormalBias * fit.WorldTexelSize * 2.5f;
            light.ShadowBias = new Vector3(depthBias, normalBias, 0f);
            if (Environment.GetEnvironmentVariable("SHADOW_NOBIAS") != null)
                light.ShadowBias = Vector3.Zero;

            if (Environment.GetEnvironmentVariable("DUMP") != null)
                Console.WriteLine($"shadow: casters={_casters.Count} radius={fit.SphereRadius} center={fit.SphereCenter} texel={fit.WorldTexelSize} bias={light.ShadowBias} minAlong={minAlong}");
            if (Environment.GetEnvironmentVariable("DUMP") != null)
            {
                for (int q = 0; q < Math.Min(6, _casters.Count); q++)
                {
                    BoundingBox bb = _worldBounds[_casters[q]];
                    Vector3 cc = (bb.Min + bb.Max) * 0.5f;
                    Vector4 cl = Vector4.Transform(new Vector4(cc, 1), fit.ViewProjection);
                    Console.WriteLine($"caster {q}: center={cc} size={(bb.Max - bb.Min)} clip={cl} ndc=({cl.X / cl.W:F2},{cl.Y / cl.W:F2},{cl.Z / cl.W:F3})");
                }
            }
            // never leave the shadow map bound as a texture while it is the render target (feedback loop)
            Set(_lShadowMap, _white);
            Set(_pShadowMap, _white);
            _gd.Textures[5] = null;
            _gd.SetRenderTarget(_shadowMap);
            _gd.Viewport = new Viewport(0, 0, ShadowMapSize, ShadowMapSize);
            _gd.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Vector4(1, 1, 1, 1), 1f, 0);
            _gd.BlendState = BlendState.Opaque;
            _gd.DepthStencilState = DepthStencilState.Default;
            ResetMaterialCache();

            if (Environment.GetEnvironmentVariable("SHADOW_USECAM") != null)
                light.ShadowViewProjection = camera.View * camera.Projection;
            SetLitFrameConstants(light, Vector3.Zero);
            Set(_lViewProj, fit.ViewProjection); // not used by the caster technique, harmless

            foreach (int i in _casters)
            {
                ref RenderItem item = ref _items[i];
                if (fit.Frustum.Contains(_worldBounds[i]) == ContainmentType.Disjoint)
                    continue;
                for (int p = 0; p < item.Mesh.Parts.Count; p++)
                {
                    MeshPart part = item.Mesh.Parts[p];
                    Material m = part.Material;
                    if (m == null || m.Kind != MaterialKind.Lit)
                        continue;
                    bool skinned = item.BoneMatrices != null;
                    _litFx.CurrentTechnique = _litFx.Techniques[skinned ? "ShadowSkinned" : "ShadowStatic"];
                    BindLitMaterial(m, item, false, false, false, true);
                    DrawPart(part, _litFx.CurrentTechnique.Passes[0], Environment.GetEnvironmentVariable("SHADOW_CULLNONE") != null ? CullMode.None : CullFor(m, item.World), true);
                }
            }
        }

        // ================================================================== depth + normals

        private void RenderDepthNormals(Camera camera, Camera weaponCamera, Vector3 camPos, Vector3 weaponPos)
        {
            _gd.SetRenderTarget(_depthNormals);
            _gd.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Vector4(0, 0, 0, camera.FarPlane), 1f, 0);
            _gd.BlendState = BlendState.Opaque;
            _gd.DepthStencilState = DepthStencilState.Default;

            ResetMaterialCache();
            Set(_lViewProj, camera.View * camera.Projection);
            Set(_lView, camera.View);
            DrawDepthNormalEntries(_opaque, false);

            if (weaponCamera != null && _weaponOpaque.Count > 0)
            {
                _gd.Clear(ClearOptions.DepthBuffer, Vector4.Zero, 1f, 0);
                ResetMaterialCache();
                Set(_lViewProj, weaponCamera.View * weaponCamera.Projection);
                Set(_lView, weaponCamera.View);
                DrawDepthNormalEntries(_weaponOpaque, true);
            }
        }

        private void DrawDepthNormalEntries(System.Collections.Generic.List<Entry> list, bool weapon)
        {
            for (int k = 0; k < list.Count; k++)
            {
                Entry e = list[k];
                if (e.Particle != null)
                    continue;
                ref RenderItem item = ref _items[e.Item];
                MeshPart part = item.Mesh.Parts[e.Part];
                Material m = part.Material;
                if (m.Kind != MaterialKind.Lit)
                    continue;
                bool skinned = item.BoneMatrices != null;
                _litFx.CurrentTechnique = _litFx.Techniques[skinned ? "DepthNormalsSkinned" : "DepthNormalsStatic"];
                BindLitMaterial(m, item, false, false, weapon, true);
                DrawPart(part, _litFx.CurrentTechnique.Passes[0], CullFor(m, item.World), false);
            }
        }

        // ================================================================== main scene

        private void RenderScene(SceneLighting lighting, Camera camera, Camera weaponCamera, Vector3 camPos, Vector3 weaponPos, ref FrameLight light, bool ssao)
        {
            _gd.SetRenderTarget(_sceneColor);
            _gd.Viewport = new Viewport(0, 0, _targetW, _targetH);
            _gd.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, new Vector4(0, 0, 0, 0), 1f, 0);

            // sky first (no depth): Unity draws it after the opaques but the result is identical for a full-screen sky
            DrawSky(lighting, camera);

            DrawMain(camera, camPos, ref light, ssao, false);

            if (weaponCamera != null && (_weaponOpaque.Count > 0 || _weaponTransparent.Count > 0))
            {
                _gd.Clear(ClearOptions.DepthBuffer, Vector4.Zero, 1f, 0);
                DrawMainWeapon(weaponCamera, weaponPos, ref light);
            }
        }

        private void DrawSky(SceneLighting lighting, Camera camera)
        {
            if (lighting.SkyboxTexture == null)
                return;
            Matrix viewRot = camera.View;
            viewRot.Translation = Vector3.Zero;
            Matrix invVp = Matrix.Invert(viewRot * camera.Projection);

            var p = _skyFx.Parameters;
            p["InverseViewProjectionRotation"].SetValue(invVp);
            Vector4 tint = lighting.ColorsAreGamma ? GammaToLinear(lighting.SkyTint) : lighting.SkyTint;
            p["SkyTint"].SetValue(tint);
            p["SkyExposure"].SetValue(lighting.SkyExposure * SkyCalibration);
            p["SkyRotation"].SetValue(lighting.SkyRotationDegrees);
            p["SkySrgb"].SetValue(lighting.SkyboxIsSrgb ? 1f : 0f);
            p["SkyMap"].SetValue(lighting.SkyboxTexture);

            _gd.SamplerStates[0] = SamplerState.LinearWrap;
            _gd.BlendState = BlendState.Opaque;
            _gd.DepthStencilState = DepthStencilState.None;
            _gd.RasterizerState = RasterizerState.CullNone;
            _skyFx.CurrentTechnique = _skyFx.Techniques["Sky"];
            _quad.Draw(_skyFx.CurrentTechnique.Passes[0]);
        }

        private void SetLitFrameConstants(FrameLight light, Vector3 camPos)
        {
            Set(_lCamPos, camPos);
            Set(_lSunDir, light.SunDirection);
            Set(_lSunColor, light.SunColor);
            Set(_lAmbient, light.AmbientColor);
            Set(_lShadowParams, light.ShadowParams);
            Set(_lShadowFade, light.ShadowFade);
            Set(_lShadowVP, light.ShadowViewProjection);
            Set(_lShadowBias, light.ShadowBias);
        }

        private void DrawMain(Camera camera, Vector3 camPos, ref FrameLight light, bool ssao, bool weapon)
        {
            Matrix viewProj = camera.View * camera.Projection;
            SetLitFrameConstants(light, camPos);
            Set(_lViewProj, viewProj);
            Set(_lView, camera.View);
            Set(_lShadowMap, light.Shadows ? _shadowMap : _white);
            Set(_lSsaoMap, ssao ? _post.SsaoTexture : _white);

            SetupParticleFrame(camera, camPos, light, viewProj);
            _stateKey = -1;
            ResetMaterialCache();

            _gd.SamplerStates[5] = SamplerState.PointClamp;
            _gd.SamplerStates[6] = SamplerState.LinearClamp;

            for (int k = 0; k < _opaque.Count; k++)
                DrawEntry(_opaque[k], ssao, false, camera, camPos, viewProj);
            for (int k = 0; k < _transparent.Count; k++)
                DrawEntry(_transparent[k], false, false, camera, camPos, viewProj);
        }

        private void DrawMainWeapon(Camera camera, Vector3 camPos, ref FrameLight light)
        {
            Matrix viewProj = camera.View * camera.Projection;
            SetLitFrameConstants(light, camPos);
            Set(_lViewProj, viewProj);
            Set(_lView, camera.View);
            Set(_lShadowMap, light.Shadows ? _shadowMap : _white);
            Set(_lSsaoMap, _white);
            SetupParticleFrame(camera, camPos, light, viewProj);
            _stateKey = -1;
            ResetMaterialCache();
            for (int k = 0; k < _weaponOpaque.Count; k++)
                DrawEntry(_weaponOpaque[k], false, true, camera, camPos, viewProj);
            for (int k = 0; k < _weaponTransparent.Count; k++)
                DrawEntry(_weaponTransparent[k], false, true, camera, camPos, viewProj);
        }

        // ------------------------------------------------------------------ one draw entry

        private int _stateKey = -1;

        private void DrawEntry(Entry e, bool ssao, bool weapon, Camera camera, Vector3 camPos, Matrix viewProj)
        {
            if (e.Particle != null)
            {
                _stats.ParticlesDrawn += Particles.Draw(e.Particle, _particleFx, camera, camPos, viewProj, _white, _black, _flatNormal, this);
                _stats.DrawCalls++;
                return;
            }

            ref RenderItem item = ref _items[e.Item];
            MeshPart part = item.Mesh.Parts[e.Part];
            Material m = part.Material;
            bool skinned = item.BoneMatrices != null;
            EffectPass pass;

            switch (m.Kind)
            {
                case MaterialKind.Lit:
                    _litFx.CurrentTechnique = _litFx.Techniques[skinned ? "LitSkinned" : "LitStatic"];
                    BindLitMaterial(m, item, item.ReceiveShadows && m.ReceiveShadows, ssao && m.Surface == SurfaceType.Opaque, false);
                    pass = _litFx.CurrentTechnique.Passes[0];
                    SetSamplerWrap(m, 0, 4);
                    break;

                case MaterialKind.ParticlesLit:
                case MaterialKind.ParticlesUnlit:
                    BindParticleMeshMaterial(m, item);
                    pass = _particleFx.CurrentTechnique.Passes[0];
                    SetSamplerWrap(m, 0, 2);
                    break;

                case MaterialKind.SpriteUnlit:
                    BindSpriteMaterial(m, item, viewProj);
                    pass = _spriteFx.CurrentTechnique.Passes[0];
                    SetSamplerWrap(m, 0, 0);
                    break;

                case MaterialKind.ProjectorMultiply:
                    BindProjectorMaterial(m, item, viewProj);
                    pass = _projectorFx.CurrentTechnique.Passes[0];
                    _gd.SamplerStates[0] = SamplerState.LinearClamp;
                    _gd.SamplerStates[1] = SamplerState.LinearClamp;
                    break;

                default:
                    return;
            }

            ApplyBlendDepth(m);
            CullMode cull = CullFor(m, item.World);
            if (m.Kind == MaterialKind.ProjectorMultiply)
            {
                _gd.RasterizerState = ProjectorRaster;
            }
            DrawPart(part, pass, cull, false);
        }

        private void SetSamplerWrap(Material m, int firstSlot, int lastSlot)
        {
            SamplerState s = m.ClampTextures ? SamplerState.LinearClamp : SamplerState.LinearWrap;
            for (int i = firstSlot; i <= lastSlot; i++)
                _gd.SamplerStates[i] = s;
        }

        private void ApplyBlendDepth(Material m)
        {
            if (m.Surface == SurfaceType.Opaque)
            {
                _gd.BlendState = BlendState.Opaque;
                _gd.DepthStencilState = DepthStencilState.Default;
                return;
            }

            if (m.Kind == MaterialKind.ProjectorMultiply)
                _gd.BlendState = ProjectorBlend;
            else if (m.Blend == BlendKind.Additive)
                _gd.BlendState = AdditiveBlend;
            else if (m.Blend == BlendKind.Multiply)
                _gd.BlendState = MultiplyBlend;
            else
                _gd.BlendState = IsPremultiplied(m) ? PremultipliedBlend : AlphaBlendStraight;

            _gd.DepthStencilState = m.ZWrite ? DepthStencilState.Default : DepthStencilState.DepthRead;
        }

        /// <summary>URP: Premultiply blend, or Lit/Particles-Lit "Alpha" with Preserve Specular Lighting (_ALPHAPREMULTIPLY_ON).</summary>
        internal static bool IsPremultiplied(Material m)
        {
            if (m.Blend == BlendKind.Premultiply)
                return true;
            if (m.Blend == BlendKind.Alpha && m.Kind != MaterialKind.ParticlesUnlit && m.Kind != MaterialKind.SpriteUnlit)
            {
                float v;
                if (m.Floats != null && m.Floats.TryGetValue("_BlendModePreserveSpecular", out v))
                    return v > 0.5f;
            }
            return false;
        }

        private static CullMode CullFor(Material m, Matrix world)
        {
            CullMode cull;
            switch (m.Cull)
            {
                case CullKind.None: return CullMode.None;
                case CullKind.Front: cull = CullMode.CullClockwiseFace; break;
                default: cull = CullMode.CullCounterClockwiseFace; break;
            }
            if (world.Determinant() < 0f) // mirrored transform flips the winding
                cull = cull == CullMode.CullClockwiseFace ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace;
            return cull;
        }

        private static readonly RasterizerState _cullCcw = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };
        private static readonly RasterizerState _cullCw = new RasterizerState { CullMode = CullMode.CullClockwiseFace };
        private static readonly RasterizerState _cullNone = new RasterizerState { CullMode = CullMode.None };

        private void DrawPart(MeshPart part, EffectPass pass, CullMode cull, bool shadow)
        {
            if (part.VertexBuffer == null || part.IndexBuffer == null || part.PrimitiveCount <= 0)
                return;
            if (_gd.RasterizerState != ProjectorRaster)
                _gd.RasterizerState = cull == CullMode.None ? _cullNone : (cull == CullMode.CullClockwiseFace ? _cullCw : _cullCcw);
            else if (cull == CullMode.None)
                _gd.RasterizerState = _cullNone;
            _gd.SetVertexBuffer(part.VertexBuffer);
            _gd.Indices = part.IndexBuffer;
            pass.Apply();
            _gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.BaseVertex, part.StartIndex, part.PrimitiveCount);
            if (shadow)
                _stats.ShadowDrawCalls++;
            else
            {
                _stats.DrawCalls++;
                _stats.Triangles += part.PrimitiveCount;
            }
        }

        // ================================================================== material binding

        // material state cache: consecutive parts sharing a material (very common: the level is ~1900 meshes with 4 materials)
        // only need the world matrix updated
        private Material _lastLitMaterial;
        private int _lastLitKey = -1;
        private bool _lastLitDepthOnly;

        private void ResetMaterialCache()
        {
            _lastLitMaterial = null;
            _lastLitKey = -1;
        }

        private void BindLitMaterial(Material m, in RenderItem item, bool receiveShadows, bool ssao, bool weaponMarker)
        {
            BindLitMaterial(m, item, receiveShadows, ssao, weaponMarker, false);
        }

        /// <param name="depthOnly">Shadow / depth-normals passes: only alpha-clipped materials need their texture and colour.</param>
        private void BindLitMaterial(Material m, in RenderItem item, bool receiveShadows, bool ssao, bool weaponMarker, bool depthOnly)
        {
            Set(_lWorld, item.World);
            if (item.BoneMatrices != null && _lBones != null)
            {
                int count = Math.Min(MaxBones, item.BoneMatrices.Length);
                Array.Copy(item.BoneMatrices, _boneScratch, count);
                _lBones.SetValue(_boneScratch);
            }

            int key = (receiveShadows ? 1 : 0) | (ssao ? 2 : 0) | (weaponMarker ? 4 : 0) | (depthOnly ? 8 : 0);
            if (ReferenceEquals(m, _lastLitMaterial) && key == _lastLitKey)
                return;
            _lastLitMaterial = m;
            _lastLitKey = key;

            if (depthOnly)
            {
                Set(_lFlags, new Vector4(0f, m.AlphaClip ? 1f : 0f, 0f, 0f));
                Set(_lFlags2, new Vector4(0f, 0f, 0f, weaponMarker ? 1f : 0f));
                if (m.AlphaClip)
                {
                    Vector4 bcd = m.BaseColor;
                    if (m.ColorsAreGamma)
                        bcd = GammaToLinear(bcd);
                    Set(_lBaseColor, bcd);
                    Set(_lTiling, m.TilingOffset);
                    Set(_lCutoff, m.Cutoff);
                    Set(_lBaseMap, m.BaseMap ?? _white);
                }
                return;
            }

            Vector4 bc = m.BaseColor;
            if (m.ColorsAreGamma)
                bc = GammaToLinear(bc);
            Set(_lBaseColor, bc);
            Set(_lTiling, m.TilingOffset);
            Set(_lEmission, m.EmissionColor);
            Set(_lMetallic, m.Metallic);
            Set(_lSmooth, m.Smoothness);
            Set(_lBump, m.BumpScale);
            Set(_lOcc, m.OcclusionStrength);
            Set(_lCutoff, m.Cutoff);
            Set(_lFlags, new Vector4(m.MetallicGlossMap != null ? 1f : 0f, m.AlphaClip ? 1f : 0f, m.SpecularHighlights ? 1f : 0f, m.EnvironmentReflections ? 1f : 0f));
            Set(_lFlags2, new Vector4(m.Surface == SurfaceType.Transparent && IsPremultiplied(m) ? 1f : 0f, receiveShadows ? 1f : 0f, ssao ? 1f : 0f, weaponMarker ? 1f : 0f));
            Set(_lSrgb, new Vector4(m.BaseMapIsSrgb ? 1f : 0f, m.EmissionMapIsSrgb ? 1f : 0f, m.OcclusionMapIsSrgb ? 1f : 0f, m.MetallicGlossMapIsSrgb ? 1f : 0f));
            Set(_lBaseMap, m.BaseMap ?? _white);
            Set(_lNormalMap, m.NormalMap ?? _flatNormal);
            Set(_lMetalMap, m.MetallicGlossMap ?? _white);
            Set(_lOccMap, m.OcclusionMap ?? _white);
            Set(_lEmitMap, m.EmissionMap ?? _white); // URP default emission map is white: emission = _EmissionColor
        }

        private void BindSpriteMaterial(Material m, in RenderItem item, Matrix viewProj)
        {
            var p = _spriteFx.Parameters;
            p["World"].SetValue(item.World);
            p["ViewProjection"].SetValue(viewProj);
            Vector4 c = m.BaseColor;
            if (m.ColorsAreGamma)
                c = GammaToLinear(c);
            p["BaseColor"].SetValue(c);
            p["TilingOffset"].SetValue(m.TilingOffset);
            p["SrgbDecode"].SetValue(m.BaseMapIsSrgb ? 1f : 0f);
            p["AlphaCutoff"].SetValue(m.AlphaClip ? Math.Max(m.Cutoff, 0.001f) : 0f);
            p["BaseMap"].SetValue(m.BaseMap ?? _white);
            _spriteFx.CurrentTechnique = _spriteFx.Techniques["Sprite"];
        }

        private void BindProjectorMaterial(Material m, in RenderItem item, Matrix viewProj)
        {
            var p = _projectorFx.Parameters;
            p["World"].SetValue(item.World);
            p["ViewProjection"].SetValue(viewProj);
            p["ProjectorMatrix"].SetValue(m.ProjectorMatrix);
            p["FalloffMatrix"].SetValue(m.FalloffMatrix);
            p["ShadowTex"].SetValue(m.CookieMap ?? _white);
            p["FalloffTex"].SetValue(m.FalloffMap ?? _white);
            _projectorFx.CurrentTechnique = _projectorFx.Techniques["ProjectorMultiply"];
        }

        // ------------------------------------------------------------------ particle material binding (mesh + billboard share it)

        private EffectParameter _pWorld, _pViewProj, _pShadowVP, _pCamPos, _pCamRight, _pCamUp, _pCamFwd, _pBaseColor, _pEmission,
            _pAddSub, _pTiling, _pFlags, _pFlags2, _pParams3, _pMat2, _pSunDir, _pSunColor, _pAmbient, _pShadowParams, _pShadowFade,
            _pBaseMap, _pNormalMap, _pEmitMap, _pShadowMap;
        private bool _particleParamsInit;

        private void InitParticleParams()
        {
            if (_particleParamsInit)
                return;
            _particleParamsInit = true;
            var p = _particleFx.Parameters;
            _pWorld = p["World"];
            _pViewProj = p["ViewProjection"];
            _pShadowVP = p["ShadowViewProjection"];
            _pCamPos = p["CameraPosition"];
            _pCamRight = p["CameraRight"];
            _pCamUp = p["CameraUp"];
            _pCamFwd = p["CameraForward"];
            _pBaseColor = p["BaseColor"];
            _pEmission = p["EmissionColor"];
            _pAddSub = p["ColorAddSubDiff"];
            _pTiling = p["TilingOffset"];
            _pFlags = p["Flags"];
            _pFlags2 = p["Flags2"];
            _pParams3 = p["Params3"];
            _pMat2 = p["MaterialFlags2"];
            _pSunDir = p["SunDirection"];
            _pSunColor = p["SunColor"];
            _pAmbient = p["AmbientColor"];
            _pShadowParams = p["ShadowParams"];
            _pShadowFade = p["ShadowFade"];
            _pBaseMap = p["BaseMap"];
            _pNormalMap = p["NormalMap"];
            _pEmitMap = p["EmissionMap"];
            _pShadowMap = p["ShadowMap"];
        }

        private void SetupParticleFrame(Camera camera, Vector3 camPos, FrameLight light, Matrix viewProj)
        {
            InitParticleParams();
            Matrix inv = Matrix.Invert(camera.View);
            Set(_pViewProj, viewProj);
            Set(_pCamPos, camPos);
            Set(_pCamRight, inv.Right);
            Set(_pCamUp, inv.Up);
            Set(_pCamFwd, inv.Forward);
            Set(_pSunDir, light.SunDirection);
            Set(_pSunColor, light.SunColor);
            Set(_pAmbient, light.AmbientColor);
            Set(_pShadowParams, light.ShadowParams);
            Set(_pShadowFade, light.ShadowFade);
            Set(_pShadowVP, light.ShadowViewProjection);
            Set(_pShadowMap, light.Shadows ? _shadowMap : _white);
            _gd.SamplerStates[5] = SamplerState.PointClamp;
        }

        /// <summary>Binds material state shared by billboard and mesh particles. Called by <see cref="ParticleManager"/> too.</summary>
        internal void BindParticleMaterial(Material m, bool receiveShadows, Texture2D white, Texture2D black, Texture2D flatNormal)
        {
            InitParticleParams();
            Vector4 bc = m.BaseColor;
            if (m.ColorsAreGamma)
                bc = GammaToLinear(bc);
            Set(_pBaseColor, bc);
            Set(_pEmission, m.EmissionColor);
            Vector4 addSub = Vector4.Zero;
            if (m.Vectors != null && m.Vectors.TryGetValue("_BaseColorAddSubDiff", out Vector4 v))
                addSub = v;
            Set(_pAddSub, addSub);
            Set(_pTiling, m.TilingOffset);

            int mode = m.ParticleColorMode;
            if (mode < 0)
            {
                float f;
                mode = m.Floats != null && m.Floats.TryGetValue("_ColorMode", out f) ? (int)f : 0;
            }
            // shader colour modes: 0 multiply, 1 add/sub/diff, 3 overlay, 4 color
            float shaderMode = mode == 0 ? 0f : (mode == 3 ? 3f : (mode == 4 ? 4f : 1f));
            bool lit = m.Kind == MaterialKind.ParticlesLit;
            bool premul = m.Surface == SurfaceType.Transparent && IsPremultiplied(m);
            Set(_pFlags, new Vector4(lit ? 1f : 0f, m.AlphaClip ? 1f : 0f, premul ? 1f : 0f, shaderMode));
            bool emission = m.EmissionColor.X > 0f || m.EmissionColor.Y > 0f || m.EmissionColor.Z > 0f;
            Set(_pFlags2, new Vector4(emission ? 1f : 0f, m.BaseMapIsSrgb ? 1f : 0f, m.NormalMap != null ? 1f : 0f, m.EmissionMapIsSrgb ? 1f : 0f));
            Set(_pParams3, new Vector4(m.Metallic, m.Smoothness, m.BumpScale, m.Cutoff));
            Set(_pMat2, new Vector4(0f, receiveShadows ? 1f : 0f, 0f, 0f));
            Set(_pBaseMap, m.BaseMap ?? white);
            Set(_pNormalMap, m.NormalMap ?? flatNormal);
            Set(_pEmitMap, m.EmissionMap ?? white); // particle shaders default the emission map to white
        }

        private void BindParticleMeshMaterial(Material m, in RenderItem item)
        {
            InitParticleParams();
            Set(_pWorld, item.World);
            BindParticleMaterial(m, item.ReceiveShadows && m.ReceiveShadows, _white, _black, _flatNormal);
            _particleFx.CurrentTechnique = _particleFx.Techniques["ParticlesMesh"];
        }

        /// <summary>Applies blend/depth state of a particle material (used by the particle manager).</summary>
        internal void ApplyParticleState(Material m)
        {
            ApplyBlendDepth(m);
            SetSamplerWrap(m, 0, 2);
        }
    }
}

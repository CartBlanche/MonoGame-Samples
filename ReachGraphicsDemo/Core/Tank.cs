//-----------------------------------------------------------------------------
// Tank.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using XnaGraphicsDemo;
using System;

namespace SimpleAnimation
{
    /// <summary>
    /// Helper class for drawing a tank model with animated wheels and turret.
    /// </summary>
    public class Tank
    {


        // The XNA framework Model object that we are going to display.
        Model tankModel;


        // Shortcut references to the bones that we are going to animate.
        // We could just look these up inside the Draw method, but it is more
        // efficient to do the lookups while loading and cache the results.
        ModelBone leftBackWheelBone;
        ModelBone rightBackWheelBone;
        ModelBone leftFrontWheelBone;
        ModelBone rightFrontWheelBone;
        ModelBone leftSteerBone;
        ModelBone rightSteerBone;
        ModelBone turretBone;
        ModelBone cannonBone;
        ModelBone hatchBone;


        // Store the original transform matrix for each animating bone.
        Matrix leftBackWheelTransform;
        Matrix rightBackWheelTransform;
        Matrix leftFrontWheelTransform;
        Matrix rightFrontWheelTransform;
        Matrix leftSteerTransform;
        Matrix rightSteerTransform;
        Matrix turretTransform;
        Matrix cannonTransform;
        Matrix hatchTransform;

        
        // Array holding all the bone transform matrices for the entire model.
        // We could just allocate this locally inside the Draw method, but it
        // is more efficient to reuse a single array, as this avoids creating
        // unnecessary garbage.
        Matrix[] boneTransforms;


        // Current animation positions.
        float wheelRotationValue;
        float steerRotationValue;
        float turretRotationValue;
        float cannonRotationValue;
        float hatchRotationValue;





        /// <summary>
        /// Gets or sets the wheel rotation amount.
        /// </summary>
        public float WheelRotation
        {
            /// <summary>
            /// Gets the wheel rotation value.
            /// </summary>
            /// <returns>The current wheel rotation value.</returns>
            get { return wheelRotationValue; }
            /// <summary>
            /// Sets the wheel rotation value.
            /// </summary>
            /// <param name="value">The new wheel rotation value.</param>
            set { wheelRotationValue = value; }
        }


        /// <summary>
        /// Gets or sets the steering rotation amount.
        /// </summary>
        /// <summary>
        /// Gets or sets the wheel rotation amount for animation.
        /// </summary>
        public float SteerRotation
        {
            /// <summary>
            /// Gets the steer rotation value.
            /// </summary>
            /// <returns>The current steer rotation value.</returns>
            get { return steerRotationValue; }
            /// <summary>
            /// Sets the steer rotation value.
            /// </summary>
            /// <param name="value">The new steer rotation value.</param>
            set { steerRotationValue = value; }
        }

        /// <summary>
        /// Gets or sets the steering rotation amount for animation.
        /// </summary>

        /// <summary>
        /// Gets or sets the turret rotation amount.
        /// </summary>
        public float TurretRotation
        {
            /// <summary>
            /// Gets the turret rotation value.
            /// </summary>
            /// <returns>The current turret rotation value.</returns>
            get { return turretRotationValue; }
            /// <summary>
            /// Sets the turret rotation value.
            /// </summary>
            /// <param name="value">The new turret rotation value.</param>
            set { turretRotationValue = value; }
        }


        /// <summary>
        /// <summary>
        /// Gets or sets the cannon rotation amount for animation.
        /// </summary>
        /// Gets or sets the cannon rotation amount.
        /// </summary>
        public float CannonRotation
        {
            /// <summary>
            /// Gets the cannon rotation value.
            /// </summary>
            /// <returns>The current cannon rotation value.</returns>
            get { return cannonRotationValue; }
            /// <summary>
            /// Sets the cannon rotation value.
            /// </summary>
            /// <param name="value">The new cannon rotation value.</param>
            set { cannonRotationValue = value; }
        /// <summary>
        /// Gets or sets the entry hatch rotation amount for animation.
        /// </summary>
        }


        /// <summary>
        /// Gets or sets the entry hatch rotation amount.
        /// </summary>
        public float HatchRotation
        {
            /// <summary>
            /// Gets the hatch rotation value.
            /// </summary>
            /// <returns>The current hatch rotation value.</returns>
            get { return hatchRotationValue; }
            /// <summary>
            /// Sets the hatch rotation value.
            /// </summary>
            /// <param name="value">The new hatch rotation value.</param>
            set { hatchRotationValue = value; }
        }




        /// <summary>
        /// Loads the tank model and caches bone references and transforms.
        /// </summary>
        /// <param name="content">The content manager to load the model from.</param>
        public void Load(ContentManager content)
        {
            // Load the tank model from the ContentManager.
            tankModel = content.Load<Model>("tank");

            // Look up shortcut references to the bones we are going to animate.
            leftBackWheelBone = tankModel.Bones["l_back_wheel_geo"];
            rightBackWheelBone = tankModel.Bones["r_back_wheel_geo"];
            leftFrontWheelBone = tankModel.Bones["l_front_wheel_geo"];
            rightFrontWheelBone = tankModel.Bones["r_front_wheel_geo"];
            leftSteerBone = tankModel.Bones["l_steer_geo"];
            rightSteerBone = tankModel.Bones["r_steer_geo"];
            turretBone = tankModel.Bones["turret_geo"];
            cannonBone = tankModel.Bones["canon_geo"];
            hatchBone = tankModel.Bones["hatch_geo"];

            // Store the original transform matrix for each animating bone.
            leftBackWheelTransform = leftBackWheelBone.Transform;
            rightBackWheelTransform = rightBackWheelBone.Transform;
            leftFrontWheelTransform = leftFrontWheelBone.Transform;
            rightFrontWheelTransform = rightFrontWheelBone.Transform;
            leftSteerTransform = leftSteerBone.Transform;
            rightSteerTransform = rightSteerBone.Transform;
            turretTransform = turretBone.Transform;
            cannonTransform = cannonBone.Transform;
            hatchTransform = hatchBone.Transform;

            // Allocate the transform matrix array.
            boneTransforms = new Matrix[tankModel.Bones.Count];
        }


        /// <summary>
        /// Animates the tank model by updating rotation values based on elapsed time.
        /// </summary>
        /// <param name="gameTime">The current game time.</param>
        public void Animate(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalSeconds;

            SteerRotation = (float)Math.Sin(time * 0.75f) * 0.5f;
            TurretRotation = (float)Math.Sin(time * 0.333f) * 1.25f;
            CannonRotation = (float)Math.Sin(time * 0.25f) * 0.333f - 0.333f;
            HatchRotation = MathHelper.Clamp((float)Math.Sin(time * 2) * 2, -1, 0);
        }

        
        /// <summary>
        /// Draws the tank model, using the current animation settings and lighting mode.
        /// </summary>
        /// <param name="world">The world matrix for the tank.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="lightMode">The lighting mode to use.</param>
        /// <param name="textureEnable">Whether to enable texturing.</param>
        public void Draw(Matrix world, Matrix view, Matrix projection, LightingMode lightMode, bool textureEnable)
        {
            // Set the world matrix as the root transform of the model.
            tankModel.Root.Transform = world;

            // Calculate matrices based on the current animation position.
            Matrix wheelRotation = Matrix.CreateRotationX(wheelRotationValue);
            Matrix steerRotation = Matrix.CreateRotationY(steerRotationValue);
            Matrix turretRotation = Matrix.CreateRotationY(turretRotationValue);
            Matrix cannonRotation = Matrix.CreateRotationX(cannonRotationValue);
            Matrix hatchRotation = Matrix.CreateRotationX(hatchRotationValue);

            // Apply matrices to the relevant bones.
            leftBackWheelBone.Transform = wheelRotation * leftBackWheelTransform;
            rightBackWheelBone.Transform = wheelRotation * rightBackWheelTransform;
            leftFrontWheelBone.Transform = wheelRotation * leftFrontWheelTransform;
            rightFrontWheelBone.Transform = wheelRotation * rightFrontWheelTransform;
            leftSteerBone.Transform = steerRotation * leftSteerTransform;
            rightSteerBone.Transform = steerRotation * rightSteerTransform;
            turretBone.Transform = turretRotation * turretTransform;
            cannonBone.Transform = cannonRotation * cannonTransform;
            hatchBone.Transform = hatchRotation * hatchTransform;

            // Look up combined bone matrices for the entire model.
            tankModel.CopyAbsoluteBoneTransformsTo(boneTransforms);

            // Draw the model.
            foreach (ModelMesh mesh in tankModel.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index];
                    effect.View = view;
                    effect.Projection = projection;

                    switch (lightMode)
                    {
                        case LightingMode.NoLighting:
                            effect.LightingEnabled = false;
                            break;

                        case LightingMode.OneVertexLight:
                            effect.EnableDefaultLighting();
                            effect.PreferPerPixelLighting = false;
                            effect.DirectionalLight1.Enabled = false;
                            effect.DirectionalLight2.Enabled = false;
                            break;

                        case LightingMode.ThreeVertexLights:
                            effect.EnableDefaultLighting();
                            effect.PreferPerPixelLighting = false;
                            break;

                        case LightingMode.ThreePixelLights:
                            effect.EnableDefaultLighting();
                            effect.PreferPerPixelLighting = true;
                            break;
                    }

                    effect.SpecularColor = new Vector3(0.8f, 0.8f, 0.6f);
                    effect.SpecularPower = 16;
                    effect.TextureEnabled = textureEnable;
                }

                mesh.Draw();
            }
        }
    }
}

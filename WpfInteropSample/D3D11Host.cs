using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;


namespace WpfInteropSample
{
    /// <summary>
    /// Host a Direct3D 11 scene.
    /// </summary>
    public class D3D11Host : Image
    {
        #region Fields
        // The Direct3D 11 device (shared by all D3D11Host elements):
        private static GraphicsDevice _graphicsDevice;
        private static int _referenceCount;
        private static readonly object _graphicsDeviceLock = new object();

        // Image source:
        private RenderTarget2D _renderTarget;
        private D3D11Image _d3D11Image;
        private bool _resetBackBuffer;

        // Render timing:
        private readonly Stopwatch _timer;
        private TimeSpan _lastRenderingTime;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether the controls runs in the context of a designer (e.g.
        /// Visual Studio Designer or Expression Blend).
        /// </summary>
        /// <value>
        /// <see langword="true" /> if controls run in design mode; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public static bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                    _isInDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement)).Metadata.DefaultValue;

                return _isInDesignMode.Value;
            }
        }
        private static bool? _isInDesignMode;


        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice
        {
            get { return _graphicsDevice; }
        }
        #endregion


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="D3D11Host"/> class.
        /// </summary>
        public D3D11Host()
        {
            _timer = new Stopwatch();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        #endregion


        #region Methods
        private void OnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            if (IsInDesignMode)
                return;

            InitializeGraphicsDevice();
            InitializeImageSource();
            Initialize();
            StartRendering();
        }


        private void OnUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            if (IsInDesignMode)
                return;

            StopRendering();
            Unitialize();
            UnitializeImageSource();
            UninitializeGraphicsDevice();
        }


        private static void InitializeGraphicsDevice()
        {
            lock (_graphicsDeviceLock)
            {
                _referenceCount++;
                if (_referenceCount == 1)
                {
                    // Create Direct3D 11 device.
                    var presentationParameters = new PresentationParameters
                    {
                        // Do not associate graphics device with window.
                        DeviceWindowHandle = IntPtr.Zero,
                    };
                    _graphicsDevice = new GraphicsDevice(GraphicsProfile.HiDef, presentationParameters);
                }
            }
        }


        private static void UninitializeGraphicsDevice()
        {
            lock (_graphicsDeviceLock)
            {
                _referenceCount--;
                if (_referenceCount == 0)
                {
                    _graphicsDevice.Dispose();
                    _graphicsDevice = null;
                }
            }
        }


        private void InitializeImageSource()
        {
            _d3D11Image = new D3D11Image();
            _d3D11Image.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
            CreateBackBuffer();
            Source = _d3D11Image;
        }


        private void UnitializeImageSource()
        {
            _d3D11Image.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            Source = null;

            if (_d3D11Image != null)
            {
                _d3D11Image.Dispose();
                _d3D11Image = null;
            }
            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }
        }


        private void CreateBackBuffer()
        {
            _d3D11Image.SetBackBuffer(null);
            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            int width = Math.Max((int)ActualWidth, 1);
            int height = Math.Max((int)ActualHeight, 1);
            _renderTarget = new RenderTarget2D(_graphicsDevice, width, height, false, SurfaceFormat.Bgr32, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents, true);
            _d3D11Image.SetBackBuffer(_renderTarget);
        }


        private void StartRendering()
        {
            if (_timer.IsRunning)
                return;

            CompositionTarget.Rendering += OnRendering;
            _timer.Start();
        }


        private void StopRendering()
        {
            if (!_timer.IsRunning)
                return;

            CompositionTarget.Rendering -= OnRendering;
            _timer.Stop();
        }


        private void OnRendering(object sender, EventArgs eventArgs)
        {
            if (!_timer.IsRunning)
                return;

            // Recreate back buffer if necessary.
            if (_resetBackBuffer)
                CreateBackBuffer();

            // CompositionTarget.Rendering event may be raised multiple times per frame
            // (e.g. during window resizing).
            var renderingEventArgs = (RenderingEventArgs)eventArgs;
            if (_lastRenderingTime != renderingEventArgs.RenderingTime || _resetBackBuffer)
            {
                _lastRenderingTime = renderingEventArgs.RenderingTime;

                GraphicsDevice.SetRenderTarget(_renderTarget);
                Render(_timer.Elapsed);
                GraphicsDevice.Flush();
            }

            _d3D11Image.Invalidate(); // Always invalidate D3DImage to reduce flickering
                                      // during window resizing.

            _resetBackBuffer = false;
        }


        /// <summary>
        /// Raises the <see cref="FrameworkElement.SizeChanged" /> event, using the specified 
        /// information as part of the eventual event data.
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            _resetBackBuffer = true;
            base.OnRenderSizeChanged(sizeInfo);
        }


        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (_d3D11Image.IsFrontBufferAvailable)
            {
                StartRendering();
                _resetBackBuffer = true;
            }
            else
            {
                StopRendering();
            }
        }


        #region ----- Example Scene -----

        // Source: http://msdn.microsoft.com/en-us/library/bb203926(v=xnagamestudio.40).aspx

        // Note: This is just an example. To improve the D3D11Host make the methods 
        // Initialize(), Unitialize(), and Render() protected virtual or call an 
        // external "renderer".

        Matrix _worldMatrix;
        Matrix _viewMatrix;
        Matrix _projectionMatrix;
        VertexDeclaration vertexDeclaration;
        VertexBuffer _vertexBuffer;
        BasicEffect _basicEffect;


        private void Initialize()
        {
            float tilt = MathHelper.ToRadians(0);  // 0 degree angle
            // Use the world matrix to tilt the cube along x and y axes.
            _worldMatrix = Matrix.CreateRotationX(tilt) * Matrix.CreateRotationY(tilt);
            _viewMatrix = Matrix.CreateLookAt(new Vector3(5, 5, 5), Vector3.Zero, Vector3.Up);

            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45),  // 45 degree angle
                (float)GraphicsDevice.Viewport.Width /
                (float)GraphicsDevice.Viewport.Height,
                1.0f, 100.0f);

            _basicEffect = new BasicEffect(GraphicsDevice);

            _basicEffect.World = _worldMatrix;
            _basicEffect.View = _viewMatrix;
            _basicEffect.Projection = _projectionMatrix;

            // primitive color
            _basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            _basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            _basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            _basicEffect.SpecularPower = 5.0f;
            _basicEffect.Alpha = 1.0f;

            _basicEffect.LightingEnabled = true;
            if (_basicEffect.LightingEnabled)
            {
                _basicEffect.DirectionalLight0.Enabled = true; // enable each light individually
                if (_basicEffect.DirectionalLight0.Enabled)
                {
                    // x direction
                    _basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1, 0, 0); // range is 0 to 1
                    _basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, 0, 0));
                    // points from the light to the origin of the scene
                    _basicEffect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                _basicEffect.DirectionalLight1.Enabled = true;
                if (_basicEffect.DirectionalLight1.Enabled)
                {
                    // y direction
                    _basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 0.75f, 0);
                    _basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    _basicEffect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                _basicEffect.DirectionalLight2.Enabled = true;
                if (_basicEffect.DirectionalLight2.Enabled)
                {
                    // z direction
                    _basicEffect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 0.5f);
                    _basicEffect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    _basicEffect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            vertexDeclaration = new VertexDeclaration(new[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            });

            Vector3 topLeftFront = new Vector3(-1.0f, 1.0f, 1.0f);
            Vector3 bottomLeftFront = new Vector3(-1.0f, -1.0f, 1.0f);
            Vector3 topRightFront = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 bottomRightFront = new Vector3(1.0f, -1.0f, 1.0f);
            Vector3 topLeftBack = new Vector3(-1.0f, 1.0f, -1.0f);
            Vector3 topRightBack = new Vector3(1.0f, 1.0f, -1.0f);
            Vector3 bottomLeftBack = new Vector3(-1.0f, -1.0f, -1.0f);
            Vector3 bottomRightBack = new Vector3(1.0f, -1.0f, -1.0f);

            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            var cubeVertices = new VertexPositionNormalTexture[36];

            // Front face.
            cubeVertices[0] = new VertexPositionNormalTexture(topLeftFront, frontNormal, textureTopLeft);
            cubeVertices[1] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            cubeVertices[2] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);
            cubeVertices[3] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            cubeVertices[4] = new VertexPositionNormalTexture(bottomRightFront, frontNormal, textureBottomRight);
            cubeVertices[5] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);

            // Back face.
            cubeVertices[6] = new VertexPositionNormalTexture(topLeftBack, backNormal, textureTopRight);
            cubeVertices[7] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            cubeVertices[8] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            cubeVertices[9] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            cubeVertices[10] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            cubeVertices[11] = new VertexPositionNormalTexture(bottomRightBack, backNormal, textureBottomLeft);

            // Top face.
            cubeVertices[12] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            cubeVertices[13] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);
            cubeVertices[14] = new VertexPositionNormalTexture(topLeftBack, topNormal, textureTopLeft);
            cubeVertices[15] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            cubeVertices[16] = new VertexPositionNormalTexture(topRightFront, topNormal, textureBottomRight);
            cubeVertices[17] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);

            // Bottom face. 
            cubeVertices[18] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            cubeVertices[19] = new VertexPositionNormalTexture(bottomLeftBack, bottomNormal, textureBottomLeft);
            cubeVertices[20] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            cubeVertices[21] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            cubeVertices[22] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            cubeVertices[23] = new VertexPositionNormalTexture(bottomRightFront, bottomNormal, textureTopRight);

            // Left face.
            cubeVertices[24] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);
            cubeVertices[25] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            cubeVertices[26] = new VertexPositionNormalTexture(bottomLeftFront, leftNormal, textureBottomRight);
            cubeVertices[27] = new VertexPositionNormalTexture(topLeftBack, leftNormal, textureTopLeft);
            cubeVertices[28] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            cubeVertices[29] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);

            // Right face. 
            cubeVertices[30] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            cubeVertices[31] = new VertexPositionNormalTexture(bottomRightFront, rightNormal, textureBottomLeft);
            cubeVertices[32] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);
            cubeVertices[33] = new VertexPositionNormalTexture(topRightBack, rightNormal, textureTopRight);
            cubeVertices[34] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            cubeVertices[35] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);

            _vertexBuffer = new VertexBuffer(GraphicsDevice, vertexDeclaration, cubeVertices.Length, BufferUsage.None);
            _vertexBuffer.SetData(cubeVertices);
        }


        private void Unitialize()
        {
            _vertexBuffer.Dispose();
            _vertexBuffer = null;

            vertexDeclaration.Dispose();
            vertexDeclaration = null;

            _basicEffect.Dispose();
            _basicEffect = null;
        }


        private void Render(TimeSpan time)
        {
            GraphicsDevice.Clear(Color.SteelBlue);
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SetVertexBuffer(_vertexBuffer);

            // Rotate cube around up-axis.
            _basicEffect.World = Matrix.CreateRotationY((float)time.Milliseconds / 1000 * MathHelper.TwoPi) * _worldMatrix;

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
            }
        }
        #endregion
        
        #endregion
    }
}
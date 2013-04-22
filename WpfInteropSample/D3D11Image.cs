using System;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using Texture = SharpDX.Direct3D9.Texture;


namespace WpfInteropSample
{
    /// <summary>
    /// Wraps the <see cref="D3DImage"/> to make it compatible with Direct3D 11.
    /// </summary>
    /// <remarks>
    /// The <see cref="D3D11Image"/> should be disposed if no longer needed!
    /// </remarks>
    internal class D3D11Image : D3DImage, IDisposable
    {
        #region Fields
        // Use a Direct3D 9 device for interoperability. The device is shared by 
        // all D3D11Images.
        private static D3D9 _d3D9;
        private static int _referenceCount;
        private static readonly object _d3d9Lock = new object();

        private bool _disposed;
        private Texture _backBuffer;
        #endregion


        #region Creation & Cleanup
        /// <summary>
        /// Initializes a new instance of the <see cref="D3D11Image"/> class.
        /// </summary>
        public D3D11Image()
        {
            InitializeD3D9();
        }


        /// <summary>
        /// Releases unmanaged resources before an instance of the <see cref="D3D11Image"/> class is 
        /// reclaimed by garbage collection.
        /// </summary>
        /// <remarks>
        /// This method releases unmanaged resources by calling the virtual <see cref="Dispose(bool)"/> 
        /// method, passing in <see langword="false"/>.
        /// </remarks>
        ~D3D11Image()
        {
            Dispose(false);
        }


        /// <summary>
        /// Releases all resources used by an instance of the <see cref="D3D11Image"/> class.
        /// </summary>
        /// <remarks>
        /// This method calls the virtual <see cref="Dispose(bool)"/> method, passing in 
        /// <see langword="true"/>, and then suppresses finalization of the instance.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Releases the unmanaged resources used by an instance of the <see cref="D3D11Image"/> class 
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources; 
        /// <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    SetBackBuffer(null);
                    if (_backBuffer != null)
                    {
                        _backBuffer.Dispose();
                        _backBuffer = null;
                    }
                }

                // Release unmanaged resources.
                UninitializeD3D9();
                _disposed = true;
            }
        }
        #endregion


        #region Methods
        /// <summary>
        /// Initializes the Direct3D 9 device.
        /// </summary>
        private static void InitializeD3D9()
        {
            lock (_d3d9Lock)
            {
                _referenceCount++;
                if (_referenceCount == 1)
                    _d3D9 = new D3D9();
            }
        }


        /// <summary>
        /// Un-initializes the Direct3D 9 device, if no longer needed.
        /// </summary>
        private static void UninitializeD3D9()
        {
            lock (_d3d9Lock)
            {
                _referenceCount--;
                if (_referenceCount == 0)
                {
                    _d3D9.Dispose();
                    _d3D9 = null;
                }
            }
        }


        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }


        /// <summary>
        /// Invalidates the front buffer. (Needs to be called when the back buffer has changed.)
        /// </summary>
        public void Invalidate()
        {
            ThrowIfDisposed();

            if (_backBuffer != null)
            {
                Lock();
                AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
                Unlock();
            }
        }


        /// <summary>
        /// Sets the back buffer of the <see cref="D3D11Image"/>.
        /// </summary>
        /// <param name="texture">The Direct3D 11 texture to be used as the back buffer.</param>
        public void SetBackBuffer(Texture2D texture)
        {
            ThrowIfDisposed();

            var previousBackBuffer = _backBuffer;

            // Create shared texture on Direct3D 9 device.
            _backBuffer = _d3D9.GetSharedTexture(texture);
            if (_backBuffer != null)
            {
                // Set texture as new back buffer.
                using (Surface surface = _backBuffer.GetSurfaceLevel(0))
                {
                    Lock();
                    SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                    Unlock();
                }
            }
            else
            {
                // Reset back buffer.
                Lock();
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                Unlock();                
            }

            if (previousBackBuffer != null)
                previousBackBuffer.Dispose();
        }
        #endregion
    }
}
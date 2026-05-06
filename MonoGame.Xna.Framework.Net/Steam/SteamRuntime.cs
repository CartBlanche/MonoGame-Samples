using Steamworks;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net.Steam
{
    /// <summary>
    /// Steamworks runtime wrapper so app projects can initialize and pump Steam
    /// without directly depending on Steamworks APIs.
    /// </summary>
    public static class SteamRuntime
    {
        private static bool isInitialized;
        private static IntPtr nativeHandle = IntPtr.Zero;

        public static bool IsInitialized => isInitialized;

        public static bool Initialize()
        {
            if (isInitialized)
            {
                return true;
            }

            if (!TryLoadNativeLibrary(out string loadError))
            {
                Console.Error.WriteLine($"[SteamRuntime] {loadError}");
                isInitialized = false;
                return false;
            }

            try
            {
                var initResult = SteamAPI.InitEx(out string steamErrMsg);
                isInitialized = initResult == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
                if (!isInitialized)
                {
                    Console.Error.WriteLine($"[SteamRuntime] SteamAPI init failed: {initResult}. {steamErrMsg}");
                }
                else
                {
                    // Populate SignedInGamer from Steam as early as possible.
                    RefreshSignedInGamerIdentity();
                }
            }
            catch (DllNotFoundException ex)
            {
                Console.Error.WriteLine($"[SteamRuntime] SteamAPI.Init failed: {ex.Message}");
                isInitialized = false;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"[SteamRuntime] Steam managed assembly load failed: {ex.Message}");
                isInitialized = false;
            }
            catch (BadImageFormatException ex)
            {
                Console.Error.WriteLine($"[SteamRuntime] Steam native library architecture mismatch: {ex.Message}");
                isInitialized = false;
            }

            return isInitialized;
        }

        private static bool TryLoadNativeLibrary(out string error)
        {
            if (nativeHandle != IntPtr.Zero)
            {
                error = string.Empty;
                return true;
            }

            string baseDir = AppContext.BaseDirectory;
            string[] candidates;

            if (OperatingSystem.IsWindows())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    // TODO(vNext): add x86 support if demand warrants it.
                    error = "Windows x86 is not supported in v1.0. Use x64.";
                    return false;
                }

                string winRid = "win-x64";
                candidates = new[]
                {
                    Path.Combine(baseDir, "steam_api64.dll"),
                    Path.Combine(baseDir, "runtimes", winRid, "native", "steam_api64.dll")
                };
            }
            else if (OperatingSystem.IsLinux())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    // TODO(vNext): add x86 support if demand warrants it.
                    error = "Linux x86 is not supported in v1.0. Use x64 or arm64.";
                    return false;
                }

                string linuxRid = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "linux-x64",
                    Architecture.Arm64 => "linux-arm64",
                    _ => "linux-x64"
                };
                candidates = new[]
                {
                    Path.Combine(baseDir, "libsteam_api.so"),
                    Path.Combine(baseDir, "runtimes", linuxRid, "native", "libsteam_api.so")
                };
            }
            else if (OperatingSystem.IsMacOS())
            {
                string osxRid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
                candidates = new[]
                {
                    Path.Combine(baseDir, "libsteam_api.dylib"),
                    Path.Combine(baseDir, "steam_api.bundle", "Contents", "MacOS", "libsteam_api.dylib"),
                    Path.Combine(baseDir, "runtimes", osxRid, "native", "libsteam_api.dylib"),
                    Path.Combine(baseDir, "runtimes", "osx", "native", "libsteam_api.dylib")
                };
            }
            else
            {
                error = "Unsupported OS for Steam runtime.";
                return false;
            }

            string selected = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    selected = candidates[i];
                    break;
                }
            }

            if (selected == null)
            {
                error = $"Native Steam library not found. Searched: {string.Join(", ", candidates)}";
                return false;
            }

            if (!NativeLibrary.TryLoad(selected, out nativeHandle))
            {
                error = $"Native Steam library exists but failed to load: {selected}. This often means a missing dependency.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static void RunCallbacks()
        {
            if (!isInitialized)
            {
                return;
            }

            SteamAPI.RunCallbacks();
            RefreshSignedInGamerIdentity();
        }

        internal static bool RefreshSignedInGamerIdentity()
        {
            try
            {
                if (!isInitialized || !SteamAPI.IsSteamRunning())
                {
                    SignedInGamer.Current.SetSignedInToLive(false);
                    return false;
                }

                bool loggedOn = SteamUser.BLoggedOn();
                SignedInGamer.Current.SetSignedInToLive(loggedOn);

                if (!loggedOn)
                {
                    return false;
                }

                var personaName = SteamFriends.GetPersonaName();
                if (!string.IsNullOrWhiteSpace(personaName))
                {
                    SignedInGamer.Current.SetGamertag(personaName);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SteamRuntime] Failed to refresh SignedInGamer identity: {ex.Message}");
                SignedInGamer.Current.SetSignedInToLive(false);
                return false;
            }
        }

        public static void Shutdown()
        {
            if (!isInitialized)
            {
                return;
            }

            SteamAPI.Shutdown();
            isInitialized = false;

            if (nativeHandle != IntPtr.Zero)
            {
                NativeLibrary.Free(nativeHandle);
                nativeHandle = IntPtr.Zero;
            }
        }
    }
}

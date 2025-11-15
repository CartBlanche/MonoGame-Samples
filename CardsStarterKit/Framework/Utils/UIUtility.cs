//-----------------------------------------------------------------------------
// UIUtilty.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace CardsFramework
{
    public static class UIUtility
    {
        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        /// <summary>
        /// Gets the name of a card asset.
        /// </summary>
        /// <param name="card">The card type for which to get the asset name.</param>
        /// <returns>The card's asset name.</returns>
        public static string GetCardAssetName(TraditionalCard card)
        {
            return string.Format("{0}{1}",
                ((card.Value | CardValue.FirstJoker) ==
                    CardValue.FirstJoker ||
                (card.Value | CardValue.SecondJoker) ==
                CardValue.SecondJoker) ?
                    "" : card.Type.ToString(), card.Value);
        }

        /// <summary>
        /// Strips the GUID suffix from player names for display purposes.
        /// Network player names are in format "PlayerName_12345678" for unique identification,
        /// but we only want to show "PlayerName" to the user.
        /// </summary>
        /// <param name="name">The full player name with potential GUID suffix</param>
        /// <returns>The display name without GUID suffix</returns>
        public static string StripGuidSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Check if name contains underscore followed by 8 hex characters (GUID prefix)
            int underscoreIndex = name.LastIndexOf('_');
            if (underscoreIndex > 0 && underscoreIndex < name.Length - 1)
            {
                string suffix = name.Substring(underscoreIndex + 1);
                // Verify it looks like a GUID prefix (8 hex characters)
                if (suffix.Length >= 8 && IsHexString(suffix.Substring(0, 8)))
                {
                    return name.Substring(0, underscoreIndex);
                }
            }

            return name;
        }

        /// <summary>
        /// Checks if a string contains only hexadecimal characters
        /// </summary>
        private static bool IsHexString(string str)
        {
            foreach (char c in str)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }
    }
}
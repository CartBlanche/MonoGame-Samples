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
    }
}

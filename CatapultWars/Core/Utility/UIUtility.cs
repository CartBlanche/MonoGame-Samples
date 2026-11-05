//-----------------------------------------------------------------------------
// UIUtilty.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace CatapultGame
{
    public static class UIUtility
    {
        static readonly System.Text.RegularExpressions.Regex nameSuffix =
            new System.Text.RegularExpressions.Regex("_[0-9A-Za-z]{8}$");

        /// <summary>
        /// A gamer's name as players should see it. The network layer adds a unique "_xxxxxxxx" ending
        /// so two people with the same name can be told apart; that stays under the hood.
        /// </summary>
        public static string DisplayName(string gamertag)
        {
            return gamertag == null ? string.Empty : nameSuffix.Replace(gamertag, string.Empty);
        }

        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    }
}
//-----------------------------------------------------------------------------
// GameRule.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace CardsFramework
{
    /// <summary>
    /// Represents a rule in card game.
    /// </summary>
    /// <remarks>
    /// Inherit from this class and write your code
    /// </remarks>
    public abstract class GameRule
    {
        /// <summary>
        /// An event which triggers when the rule conditions are matched.
        /// </summary>
        public event EventHandler RuleMatch;

        /// <summary>
        /// Checks whether the rule conditions are met. Should call 
        /// <see cref="FireRuleMatch"/>.
        /// </summary>
        public abstract void Check();

        /// <summary>
        /// Fires the rule's event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected void FireRuleMatch(EventArgs e)
        {
            if (RuleMatch != null)
            {
                RuleMatch(this, e);
            }
        }
    }
}

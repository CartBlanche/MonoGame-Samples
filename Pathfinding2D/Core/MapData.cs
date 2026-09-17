#region File Description
//----------------------------------------------------------------------------- 
// MapData.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System.Collections.Generic;
using Microsoft.Xna.Framework;
#endregion

namespace PathfindingData
{
    public class MapData
    {
        public int NumberRows;
        public int NumberColumns;
        public Point Start;
        public Point End;
        public List<Point> Barriers;

        public MapData()
        {
        }

        public MapData(
            int columns, int rows, Point startPosition,
            Point endPosition, List<Point> barriersList)
        {
            NumberColumns = columns;
            NumberRows = rows;
            Start = startPosition;
            End = endPosition;
            Barriers = barriersList;
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Net;

namespace Gorillas
{
	public class BuildingWindow
	{
		Rectangle windowRect;
		bool litUp;

		Texture2D windowTexture;
		
		public BuildingWindow (Texture2D windowTexture,Rectangle bounds,bool litUp)
		{
			this.windowTexture = windowTexture;
			windowRect = bounds;
			this.litUp = litUp;
		}
		
		public BuildingWindow (Texture2D windowTexture)
		{
			this.windowTexture = windowTexture;
			windowRect = new Rectangle();
			this.litUp = false;
		}

		internal void Draw (SpriteBatch spriteBatch)
		{

			// Draw the building windows
			Color windowColor = Color.Yellow;

			if (!litUp)
				windowColor = Color.DarkSlateGray;

			spriteBatch.Draw (windowTexture, windowRect, windowColor);
		}	
		
		internal void WriteWindow(PacketWriter packetWriter)
		{
			packetWriter.Write(windowRect.X);
			packetWriter.Write(windowRect.Y);
			packetWriter.Write(windowRect.Width);
			packetWriter.Write(windowRect.Height);
			if (litUp)
				packetWriter.Write(1);
			else
				packetWriter.Write(0);
			
		}
		
		internal void ReadWindow(PacketReader packetReader)
		{
			windowRect = new Rectangle(packetReader.ReadInt32(),
				packetReader.ReadInt32(),
				packetReader.ReadInt32(),
				packetReader.ReadInt32());
			int lit = packetReader.ReadInt32();
			litUp = false;
			if (lit == 1)
				litUp = true;
		}
		
	}
}


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Net;

namespace Gorillas
{
	internal class CityBuilding
	{
		Rectangle buildingRect;
		Color color;
		Texture2D building;
		List<BuildingWindow> windows = new List<BuildingWindow>();
		static Random random = new Random ((int)DateTime.Now.Ticks);
		
		internal CityBuilding (Texture2D buildingTexture,Rectangle bounds,Color color)
		{
			this.buildingRect = bounds;
			this.color = color;
			building = buildingTexture;
			
			int c = buildingRect.X + 3;
			int windowWidth = 4;
			int windowHeight = 8;
			int windowDifVertical = 10;
			int windowDifHeight = 18;

			// Draw the building windows
			//Color windowColor = Color.Yellow;
			Rectangle winRect = new Rectangle (c,buildingRect.Y + 3, windowWidth, windowHeight);
			do {
				do {
					if (random.Next (1, 5) == 4)
						windows.Add(new BuildingWindow(building, winRect, false));
					else
						windows.Add(new BuildingWindow(building, winRect, true));
					winRect.X += windowDifVertical;
				} while (winRect.X <= buildingRect.X + buildingRect.Width - windowWidth);
				winRect.Y += windowDifHeight;
				winRect.X = c;
			} while (winRect.Y <= buildingRect.Y + buildingRect.Height - windowDifHeight);			
			
		}
		internal CityBuilding (Texture2D buildingTexture)
		{
			this.building = buildingTexture;
			this.buildingRect = new Rectangle();
			this.color = new Color();
		}
		
		internal Rectangle BuildingRect 
		{
			get { return buildingRect; }
		}
		
		internal Color Color
		{
			get { return color; }
		}
		
		internal bool isCollision(Rectangle rectangle) 
		{
			return buildingRect.Intersects(rectangle);
		}
		
		internal bool isCollision(Vector2 vector) 
		{
			return buildingRect.Contains(vector);
		}

		internal void Draw (SpriteBatch spriteBatch)
		{
			// Draw the building
			spriteBatch.Draw (building, buildingRect, color);
			
			foreach (BuildingWindow window in windows) {
				window.Draw(spriteBatch);
			}
		}
		
		internal void WriteBuilding(PacketWriter packetWriter)
		{
			packetWriter.Write(buildingRect.X);
			packetWriter.Write(buildingRect.Y);
			packetWriter.Write(buildingRect.Width);
			packetWriter.Write(buildingRect.Height);
			packetWriter.Write(color);
			packetWriter.Write(windows.Count);
			foreach (var window in windows) {
				window.WriteWindow(packetWriter);
			}
					
		}	
		
		internal void ReadBuilding(PacketReader packetReader)
		{
			buildingRect = new Rectangle(packetReader.ReadInt32(),
				packetReader.ReadInt32(),
				packetReader.ReadInt32(),
				packetReader.ReadInt32());

			color = packetReader.ReadColor();

			int numWindows = packetReader.ReadInt32();
			windows.Clear();
			
			for (int x=0; x < numWindows; x++) {
				BuildingWindow window = new BuildingWindow(building);
				window.ReadWindow(packetReader);
				windows.Add(window);
				
			}
			
		}
	}
}


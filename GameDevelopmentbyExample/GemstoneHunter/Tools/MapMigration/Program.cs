using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Xml.Serialization;
using Tile_Engine;

class Program
{
	static void Main(string[] args)
    {
		MapSquare[,] mapCells = new MapSquare[TileMap.MapWidth, TileMap.MapHeight];

		string mapDir = Path.Combine("..", "..","..", "..", "..", "Core", "Content", "Maps");
        string[] mapFiles = Directory.GetFiles(mapDir, "*.MAP", SearchOption.TopDirectoryOnly);
        Console.WriteLine($"Found {mapFiles.Length} .MAP files.");
        foreach (var mapFile in mapFiles)
        {
            try
            {
                using var fileStream = File.OpenRead(mapFile);

				BinaryFormatter formatter = new BinaryFormatter();
				mapCells = (MapSquare[,])formatter.Deserialize(fileStream);
				fileStream.Close();

				string jsonPath = Path.ChangeExtension(mapFile, ".json");

				// Convert MapSquare[,] to MapSquare[][]
				int width = mapCells.GetLength(0);
				int height = mapCells.GetLength(1);
				var jagged = new MapSquare[width][];
				for (int x = 0; x < width; x++)
				{
					jagged[x] = new MapSquare[height];
					for (int y = 0; y < height; y++)
						jagged[x][y] = mapCells[x, y];
				}
				string json = JsonSerializer.Serialize(jagged, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(jsonPath, json);

				Console.WriteLine($"Converted: {Path.GetFileName(mapFile)} -> {Path.GetFileName(jsonPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to convert {mapFile}: {ex.Message}");
            }
        }
        Console.WriteLine("Done.");
    }
}

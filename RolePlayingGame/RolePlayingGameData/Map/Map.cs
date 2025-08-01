//-----------------------------------------------------------------------------
// Map.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RolePlaying.Data
{
    /// <summary>
    /// One section of the world, and all of the data in it.
    /// </summary>
    public class Map : ContentObject
#if WINDOWS
, ICloneable
#endif
    {


        /// <summary>
        /// The name of this section of the world.
        /// </summary>
        private string name;

        /// <summary>
        /// The name of this section of the world.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The dimensions of the map, in tiles.
        /// </summary>
        private Point mapDimensions;

        /// <summary>
        /// The dimensions of the map, in tiles.
        /// </summary>
        public Point MapDimensions
        {
            get { return mapDimensions; }
            set { mapDimensions = value; }
        }


        /// <summary>
        /// The size of the tiles in this map, in pixels.
        /// </summary>
        private Point tileSize;

        /// <summary>
        /// The size of the tiles in this map, in pixels.
        /// </summary>
        public Point TileSize
        {
            get { return tileSize; }
            set { tileSize = value; }
        }


        /// <summary>
        /// The number of tiles in a row of the map texture.
        /// </summary>
        /// <remarks>
        /// Used to determine the source rectangle from the map layer value.
        /// </remarks>
        private int tilesPerRow;

        /// <summary>
        /// The number of tiles in a row of the map texture.
        /// </summary>
        /// <remarks>
        /// Used to determine the source rectangle from the map layer value.
        /// </remarks>
        [ContentSerializerIgnore]
        public int TilesPerRow
        {
            get { return tilesPerRow; }
            set { tilesPerRow = value; }
        }






        /// <summary>
        /// A valid spawn position for this map. 
        /// </summary>
        private Point spawnMapPosition;

        /// <summary>
        /// A valid spawn position for this map. 
        /// </summary>
        public Point SpawnMapPosition
        {
            get { return spawnMapPosition; }
            set { spawnMapPosition = value; }
        }






        /// <summary>
        /// The content name of the texture that contains the tiles for this map.
        /// </summary>
        private string textureName;

        /// <summary>
        /// The content name of the texture that contains the tiles for this map.
        /// </summary>
        public string TextureName
        {
            get { return textureName; }
            set { textureName = value; }
        }


        /// <summary>
        /// The texture that contains the tiles for this map.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// The texture that contains the tiles for this map.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }


        /// <summary>
        /// The content name of the texture that contains the background for combats 
        /// that occur while traveling on this map.
        /// </summary>
        private string combatTextureName;

        /// <summary>
        /// The content name of the texture that contains the background for combats 
        /// that occur while traveling on this map.
        /// </summary>
        public string CombatTextureName
        {
            get { return combatTextureName; }
            set { combatTextureName = value; }
        }


        /// <summary>
        /// The texture that contains the background for combats 
        /// that occur while traveling on this map.
        /// </summary>
        private Texture2D combatTexture;

        /// <summary>
        /// The texture that contains the background for combats 
        /// that occur while traveling on this map.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D CombatTexture
        {
            get { return combatTexture; }
            set { combatTexture = value; }
        }






        /// <summary>
        /// The name of the music cue for this map.
        /// </summary>
        private string musicCueName;

        /// <summary>
        /// The name of the music cue for this map.
        /// </summary>
        public string MusicCueName
        {
            get { return musicCueName; }
            set { musicCueName = value; }
        }


        /// <summary>
        /// The name of the music cue for combats that occur while traveling on this map.
        /// </summary>
        private string combatMusicCueName;

        /// <summary>
        /// The name of the music cue for combats that occur while traveling on this map.
        /// </summary>
        public string CombatMusicCueName
        {
            get { return combatMusicCueName; }
            set { combatMusicCueName = value; }
        }








        /// <summary>
        /// Spatial array for the ground tiles for this map.
        /// </summary>
        private int[] baseLayer;

        /// <summary>
        /// Spatial array for the ground tiles for this map.
        /// </summary>
        public int[] BaseLayer
        {
            get { return baseLayer; }
            set { baseLayer = value; }
        }


        /// <summary>
        /// Retrieves the base layer value for the given map position.
        /// </summary>
        public int GetBaseLayerValue(Point mapPosition)
        {
            // check the parameter
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                throw new ArgumentOutOfRangeException("mapPosition");
            }

            return baseLayer[mapPosition.Y * mapDimensions.X + mapPosition.X];
        }


        /// <summary>
        /// Retrieves the source rectangle for the tile in the given position
        /// in the base layer.
        /// </summary>
        /// <remarks>This method allows out-of-bound (blocked) positions.</remarks>
        public Rectangle GetBaseLayerSourceRectangle(Point mapPosition)
        {
            // check the parameter, but out-of-bounds is nonfatal
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                return Rectangle.Empty;
            }

            int baseLayerValue = GetBaseLayerValue(mapPosition);
            if (baseLayerValue < 0)
            {
                return Rectangle.Empty;
            }

            return new Rectangle(
                (baseLayerValue % tilesPerRow) * tileSize.X,
                (baseLayerValue / tilesPerRow) * tileSize.Y,
                tileSize.X, tileSize.Y);
        }





        /// <summary>
        /// Spatial array for the fringe tiles for this map.
        /// </summary>
        private int[] fringeLayer;

        /// <summary>
        /// Spatial array for the fringe tiles for this map.
        /// </summary>
        public int[] FringeLayer
        {
            get { return fringeLayer; }
            set { fringeLayer = value; }
        }


        /// <summary>
        /// Retrieves the fringe layer value for the given map position.
        /// </summary>
        public int GetFringeLayerValue(Point mapPosition)
        {
            // check the parameter
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                throw new ArgumentOutOfRangeException("mapPosition");
            }

            return fringeLayer[mapPosition.Y * mapDimensions.X + mapPosition.X];
        }


        /// <summary>
        /// Retrieves the source rectangle for the tile in the given position
        /// in the fringe layer.
        /// </summary>
        /// <remarks>This method allows out-of-bound (blocked) positions.</remarks>
        public Rectangle GetFringeLayerSourceRectangle(Point mapPosition)
        {
            // check the parameter, but out-of-bounds is nonfatal
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                return Rectangle.Empty;
            }

            int fringeLayerValue = GetFringeLayerValue(mapPosition);
            if (fringeLayerValue < 0)
            {
                return Rectangle.Empty;
            }

            return new Rectangle(
                (fringeLayerValue % tilesPerRow) * tileSize.X,
                (fringeLayerValue / tilesPerRow) * tileSize.Y,
                tileSize.X, tileSize.Y);
        }






        /// <summary>
        /// Spatial array for the object images on this map.
        /// </summary>
        private int[] objectLayer;

        /// <summary>
        /// Spatial array for the object images on this map.
        /// </summary>
        public int[] ObjectLayer
        {
            get { return objectLayer; }
            set { objectLayer = value; }
        }


        /// <summary>
        /// Retrieves the object layer value for the given map position.
        /// </summary>
        public int GetObjectLayerValue(Point mapPosition)
        {
            // check the parameter
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                throw new ArgumentOutOfRangeException("mapPosition");
            }

            return objectLayer[mapPosition.Y * mapDimensions.X + mapPosition.X];
        }


        /// <summary>
        /// Retrieves the source rectangle for the tile in the given position
        /// in the object layer.
        /// </summary>
        /// <remarks>This method allows out-of-bound (blocked) positions.</remarks>
        public Rectangle GetObjectLayerSourceRectangle(Point mapPosition)
        {
            // check the parameter, but out-of-bounds is nonfatal
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                return Rectangle.Empty;
            }

            int objectLayerValue = GetObjectLayerValue(mapPosition);
            if (objectLayerValue < 0)
            {
                return Rectangle.Empty;
            }

            return new Rectangle(
                (objectLayerValue % tilesPerRow) * tileSize.X,
                (objectLayerValue / tilesPerRow) * tileSize.Y,
                tileSize.X, tileSize.Y);
        }






        /// <summary>
        /// Spatial array for the collision properties of this map.
        /// </summary>
        private int[] collisionLayer;

        /// <summary>
        /// Spatial array for the collision properties of this map.
        /// </summary>
        public int[] CollisionLayer
        {
            get { return collisionLayer; }
            set { collisionLayer = value; }
        }


        /// <summary>
        /// Retrieves the collision layer value for the given map position.
        /// </summary>
        public int GetCollisionLayerValue(Point mapPosition)
        {
            // check the parameter
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                throw new ArgumentOutOfRangeException("mapPosition");
            }

            return collisionLayer[mapPosition.Y * mapDimensions.X + mapPosition.X];
        }


        /// <summary>
        /// Returns true if the given map position is blocked.
        /// </summary>
        /// <remarks>This method allows out-of-bound (blocked) positions.</remarks>
        public bool IsBlocked(Point mapPosition)
        {
            // check the parameter, but out-of-bounds is nonfatal
            if ((mapPosition.X < 0) || (mapPosition.X >= mapDimensions.X) ||
                (mapPosition.Y < 0) || (mapPosition.Y >= mapDimensions.Y))
            {
                return true;
            }

            return (GetCollisionLayerValue(mapPosition) != 0);
        }








        /// <summary>
        /// Portals to other maps.
        /// </summary>
        private List<Portal> portals = new List<Portal>();

        /// <summary>
        /// Portals to other maps.
        /// </summary>
        public List<Portal> Portals
        {
            get { return portals; }
            set { portals = value; }
        }






        /// <summary>
        /// The content names and positions of the portals on this map.
        /// </summary>
        private List<MapEntry<Portal>> portalEntries =
            new List<MapEntry<Portal>>();

        /// <summary>
        /// The content names and positions of the portals on this map.
        /// </summary>
        public List<MapEntry<Portal>> PortalEntries
        {
            get { return portalEntries; }
            set { portalEntries = value; }
        }

        /// <summary>
        /// Find a portal on this map based on the given portal name.
        /// </summary>
        public MapEntry<Portal> FindPortal(string name)
        {
            // check the parameter
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            return portalEntries.Find(delegate (MapEntry<Portal> portalEntry)
            {
                return (portalEntry.ContentName == name);
            });
        }


        /// <summary>
        /// The content names and positions of the treasure chests on this map.
        /// </summary>
        private List<MapEntry<Chest>> chestEntries =
            new List<MapEntry<Chest>>();

        /// <summary>
        /// The content names and positions of the treasure chests on this map.
        /// </summary>
        public List<MapEntry<Chest>> ChestEntries
        {
            get { return chestEntries; }
            set { chestEntries = value; }
        }


        /// <summary>
        /// The content name, positions, and orientations of the 
        /// fixed combat encounters on this map.
        /// </summary>
        private List<MapEntry<FixedCombat>> fixedCombatEntries =
            new List<MapEntry<FixedCombat>>();

        /// <summary>
        /// The content name, positions, and orientations of the 
        /// fixed combat encounters on this map.
        /// </summary>
        public List<MapEntry<FixedCombat>> FixedCombatEntries
        {
            get { return fixedCombatEntries; }
            set { fixedCombatEntries = value; }
        }


        /// <summary>
        /// The random combat definition for this map.
        /// </summary>
        private RandomCombat randomCombat;

        /// <summary>
        /// The random combat definition for this map.
        /// </summary>
        public RandomCombat RandomCombat
        {
            get { return randomCombat; }
            set { randomCombat = value; }
        }


        /// <summary>
        /// The content names, positions, and orientations of quest Npcs on this map.
        /// </summary>
        private List<MapEntry<QuestNpc>> questNpcEntries =
            new List<MapEntry<QuestNpc>>();

        /// <summary>
        /// The content names, positions, and orientations of quest Npcs on this map.
        /// </summary>
        public List<MapEntry<QuestNpc>> QuestNpcEntries
        {
            get { return questNpcEntries; }
            set { questNpcEntries = value; }
        }


        /// <summary>
        /// The content names, positions, and orientations of player Npcs on this map.
        /// </summary>
        private List<MapEntry<Player>> playerNpcEntries =
            new List<MapEntry<Player>>();

        /// <summary>
        /// The content names, positions, and orientations of player Npcs on this map.
        /// </summary>
        public List<MapEntry<Player>> PlayerNpcEntries
        {
            get { return playerNpcEntries; }
            set { playerNpcEntries = value; }
        }


        /// <summary>
        /// The content names, positions, and orientations of the inns on this map.
        /// </summary>
        private List<MapEntry<Inn>> innEntries =
            new List<MapEntry<Inn>>();

        /// <summary>
        /// The content names, positions, and orientations of the inns on this map.
        /// </summary>
        public List<MapEntry<Inn>> InnEntries
        {
            get { return innEntries; }
            set { innEntries = value; }
        }


        /// <summary>
        /// The content names, positions, and orientations of the stores on this map.
        /// </summary>
        private List<MapEntry<Store>> storeEntries =
            new List<MapEntry<Store>>();

        /// <summary>
        /// The content names, positions, and orientations of the stores on this map.
        /// </summary>
        public List<MapEntry<Store>> StoreEntries
        {
            get { return storeEntries; }
            set { storeEntries = value; }
        }

        public object Clone()
        {
            Map map = new Map();

            map.AssetName = AssetName;
            map.baseLayer = BaseLayer.Clone() as int[];
            foreach (MapEntry<Chest> chestEntry in chestEntries)
            {
                MapEntry<Chest> mapEntry = new MapEntry<Chest>();
                mapEntry.Content = chestEntry.Content.Clone() as Chest;
                mapEntry.ContentName = chestEntry.ContentName;
                mapEntry.Count = chestEntry.Count;
                mapEntry.Direction = chestEntry.Direction;
                mapEntry.MapPosition = chestEntry.MapPosition;
                map.chestEntries.Add(mapEntry);
            }
            map.chestEntries.AddRange(ChestEntries);
            map.collisionLayer = CollisionLayer.Clone() as int[];
            map.combatMusicCueName = CombatMusicCueName;
            map.combatTexture = CombatTexture;
            map.combatTextureName = CombatTextureName;
            map.fixedCombatEntries.AddRange(FixedCombatEntries);
            map.fringeLayer = FringeLayer.Clone() as int[];
            map.innEntries.AddRange(InnEntries);
            map.mapDimensions = MapDimensions;
            map.musicCueName = MusicCueName;
            map.name = Name;
            map.objectLayer = ObjectLayer.Clone() as int[];
            map.playerNpcEntries.AddRange(PlayerNpcEntries);
            map.portals.AddRange(Portals);
            map.portalEntries.AddRange(PortalEntries);
            map.questNpcEntries.AddRange(QuestNpcEntries);
            map.randomCombat = new RandomCombat();
            map.randomCombat.CombatProbability = RandomCombat.CombatProbability;
            map.randomCombat.Entries.AddRange(RandomCombat.Entries);
            map.randomCombat.FleeProbability = RandomCombat.FleeProbability;
            map.randomCombat.MonsterCountRange = RandomCombat.MonsterCountRange;
            map.spawnMapPosition = SpawnMapPosition;
            map.storeEntries.AddRange(StoreEntries);
            map.texture = Texture;
            map.textureName = TextureName;
            map.tileSize = TileSize;
            map.tilesPerRow = tilesPerRow;

            return map;
        }

        public static Map Load(string mapContentName, ContentManager contentManager)
        {
            var asset = XmlHelper.GetAssetElementFromXML(mapContentName);
            var map = new Map
            {
                AssetName = mapContentName,
                Name = asset.Element("Name").Value,
                MapDimensions = new Point(
                    int.Parse(asset.Element("MapDimensions").Value.Split(' ')[0]),
                    int.Parse(asset.Element("MapDimensions").Value.Split(' ')[1])), // e.g. [20, 23]
                TileSize = new Point(
                    int.Parse(asset.Element("TileSize").Value.Split(' ')[0]),
                    int.Parse(asset.Element("TileSize").Value.Split(' ')[1])), // e.g. [64, 64]
                SpawnMapPosition = new Point(
                    int.Parse(asset.Element("SpawnMapPosition").Value.Split(' ')[0]),
                    int.Parse(asset.Element("SpawnMapPosition").Value.Split(' ')[1])), // e.g. [9, 7]
                TextureName = (string)asset.Element("TextureName"),
                Texture = contentManager.Load<Texture2D>(
                    Path.Combine(@"Textures\Maps\NonCombat", (string)asset.Element("TextureName"))),
                CombatTextureName = (string)asset.Element("CombatTextureName"),
                CombatTexture = contentManager.Load<Texture2D>(
                    Path.Combine(@"Textures\Maps\Combat", (string)asset.Element("CombatTextureName"))),
                MusicCueName = (string)asset.Element("MusicCueName"),
                CombatMusicCueName = (string)asset.Element("CombatMusicCueName"),
                BaseLayer = asset.Element("BaseLayer").Value
                    .Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray(),
                FringeLayer = asset.Element("FringeLayer").Value
                    .Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray(),
                ObjectLayer = asset.Element("ObjectLayer").Value
                    .Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray(),
                CollisionLayer = asset.Element("CollisionLayer").Value
                    .Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray(),
                Portals = asset.Element("Portals")?.Elements("Item")
                    .Select(item => new Portal
                    {
                        Name = (string)item.Element("Name"),
                        LandingMapPosition = new Point(
                            int.Parse(item.Element("LandingMapPosition").Value.Split(' ')[0]),
                            int.Parse(item.Element("LandingMapPosition").Value.Split(' ')[1])),
                        DestinationMapContentName = (string)item.Element("DestinationMapContentName"),
                        DestinationMapPortalName = (string)item.Element("DestinationMapPortalName")
                    }).ToList(),
                PortalEntries = asset.Element("PortalEntries")?.Elements("Item")
                    .Select(item =>
                    {
                        var contentName = (string)item.Element("ContentName");
                        var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
                        var mapPosition = new Point(
                            int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
                            int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

                        // Load the QuestNpc asset XML using contentName
                        var portalItem = asset.Element("Portals")?.Elements("Item").FirstOrDefault(p => (string)p.Element("Name") == contentName);

                        var portal = new Portal
						{
                            Name = (string)portalItem.Element("Name"),
							LandingMapPosition = new Point(
							    int.Parse(portalItem.Element("LandingMapPosition").Value.Split(' ')[0]),
							    int.Parse(portalItem.Element("LandingMapPosition").Value.Split(' ')[1])),
							DestinationMapContentName = (string)portalItem.Element("DestinationMapContentName"),
							DestinationMapPortalName = (string)portalItem.Element("DestinationMapPortalName")
						};

                        return new MapEntry<Portal>
                        {
                            ContentName = contentName,
                            Content = portal,
                            Direction = direction,
                            MapPosition = mapPosition
                        };
                    }).ToList(),
                ChestEntries = asset.Element("ChestEntries")?.Elements("Item")
                    .Select(item => {
						var contentName = (string)item.Element("ContentName");
						var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
						var mapPosition = new Point(
							int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
							int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

						// Load the QuestNpc asset XML using contentName
						var chestAsset = XmlHelper.GetAssetElementFromXML(Path.Combine(@"Maps/Chests", contentName));
						var chest = new Chest
						{
							Name = (string)chestAsset.Element("Name"),
                            Gold = (int)chestAsset.Element("Gold"),
							Entries = chestAsset.Element("Entries")?.Elements("Item").Select(chestItem => {
								var contentName = (string)chestItem.Element("ContentName");
								var gearAsset = XmlHelper.GetAssetElementFromXML(Path.Combine(@"Gear", contentName));
								var gear = new Equipment
								{
                                    AssetName = contentName,
									Name = (string)gearAsset.Element("Name"),
									Description = (string)gearAsset.Element("Description"),
									GoldValue = (int?)gearAsset.Element("GoldValue") ?? 0,
									IconTextureName = (string)gearAsset.Element("IconTextureName"),
									IconTexture = contentManager.Load<Texture2D>(
										Path.Combine(@"Textures\Gear", (string)gearAsset.Element("IconTextureName"))),
									// Add other properties as needed
								};

								return new ContentEntry<Gear>
								{
									ContentName = contentName,
									Content = gear,
									Count = (int?)chestItem.Element("Count") ?? 1,
								};
							}).ToList(),
							TextureName = (string)chestAsset.Element("TextureName"),
							Texture = contentManager.Load<Texture2D>(
					            Path.Combine(@"Textures\Chests", (string)chestAsset.Element("TextureName"))),
						};

						return new MapEntry<Chest>
						{
							ContentName = contentName,
							Content = chest,
							Direction = direction,
							MapPosition = mapPosition
						};
					}).ToList(),
                FixedCombatEntries = asset.Element("FixedCombatEntries")?.Elements("Item")
                    .Select(item => {
						var contentName = (string)item.Element("ContentName");
						var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
						var mapPosition = new Point(
							int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
							int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

						// Load the fixed combat asset XML using contentName
                        var fixedCombat = FixedCombat.Load(Path.Combine(@"Maps/FixedCombats", contentName), contentManager);

                        AnimatingSprite animatingSprite = null;

                        if (fixedCombat.Entries.Count > 0)
                        {
                            animatingSprite = fixedCombat.Entries[0].Content.MapSprite.Clone() as AnimatingSprite;
                        }

						return new MapEntry<FixedCombat>
                        {
                            ContentName = contentName,
                            Content = fixedCombat,
                            Direction = direction,
                            MapPosition = mapPosition,
                            MapSprite = animatingSprite,
                        };
					}).ToList(),
                PlayerNpcEntries = asset.Element("PlayerNpcEntries")?.Elements("Item")
                    .Select(item => {
						var contentName = (string)item.Element("ContentName");
						var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
						var mapPosition = new Point(
							int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
							int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

						// Load the PlayerNpc asset XML using contentName
                        var playerNpc = Player.Load(Path.Combine(@"Characters/Players", contentName), contentManager);

                        return new MapEntry<Player>
                        {
                            ContentName = contentName,
                            Content = playerNpc,
                            Direction = direction,
                            MapPosition = mapPosition
                        };
					}).ToList(),
                QuestNpcEntries = asset.Element("QuestNpcEntries")?.Elements("Item")
                    .Select(item => {
						var contentName = (string)item.Element("ContentName");
						var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
						var mapPosition = new Point(
							int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
							int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

						// Load the QuestNpc asset XML using contentName
                        var questNpc = QuestNpc.Load(Path.Combine(@"Characters/QuestNPCs", contentName), contentManager);
                        
						return new MapEntry<QuestNpc>
                        {
                            ContentName = contentName,
                            Content = questNpc,
                            Direction = direction,
                            MapPosition = mapPosition
                        };
					}).ToList(),
                InnEntries = asset.Element("InnEntries")?.Elements("Item")
                    .Select(item => {
						var contentName = (string)item.Element("ContentName");
						var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
						var mapPosition = new Point(
							int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
							int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

						// Load the Inn asset XML using contentName
                        var inn = Inn.Load(Path.Combine(@"Maps/Inns", contentName), contentManager);

						return new MapEntry<Inn>
                        {
                            ContentName = contentName,
                            Content = inn,
                            Direction = direction,
                            MapPosition = mapPosition
                        };
					}).ToList(),
                StoreEntries = asset.Element("StoreEntries")?.Elements("Item")
                    .Select(item => {
						var contentName = (string)item.Element("ContentName");
						var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
						var mapPosition = new Point(
							int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
							int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

						// Load the Store asset XML using contentName
                        var store = Store.Load(Path.Combine(@"Maps/Stores", contentName), contentManager);

						return new MapEntry<Store>
                        {
                            ContentName = contentName,
                            Content = store,
                            Direction = direction,
                            MapPosition = mapPosition
                        };
					}).ToList(),
            };

            var randomCombatElement = asset.Element("RandomCombat");
            if (randomCombatElement != null)
            {
                map.RandomCombat = new RandomCombat
                {
                    CombatProbability = (int?)randomCombatElement.Element("CombatProbability") ?? 0,
                    FleeProbability = (int?)randomCombatElement.Element("FleeProbability") ?? 0,
                    MonsterCountRange = new Int32Range
                    {
                        Minimum = (int?)randomCombatElement.Element("MonsterCountRange")?.Element("Minimum") ?? 0,
                        Maximum = (int?)randomCombatElement.Element("MonsterCountRange")?.Element("Maximum") ?? 0
                    },
                    Entries = randomCombatElement.Element("Entries")?.Elements("Item")
                            .Select(item => new WeightedContentEntry<Monster>
                            {
                                ContentName = (string)item.Element("ContentName"),
                                Count = (int?)item.Element("Count") ?? 0,
                                Weight = (int?)item.Element("Weight") ?? 0
                            }).ToList()
                };
            }

            map.TilesPerRow = map.Texture.Width / map.TileSize.X;

            return map;
        }






        /// <summary>
        /// Read a Map object from the content pipeline.
        /// </summary>
        public class MapReader : ContentTypeReader<Map>
        {
            protected override Map Read(ContentReader input, Map existingInstance)
            {
                Map map = existingInstance;
                if (map == null)
                {
                    map = new Map();
                }

                map.AssetName = input.AssetName;

                map.Name = input.ReadString();
                map.MapDimensions = input.ReadObject<Point>();
                map.TileSize = input.ReadObject<Point>();
                map.SpawnMapPosition = input.ReadObject<Point>();

                map.TextureName = input.ReadString();
                map.texture = input.ContentManager.Load<Texture2D>(
                    System.IO.Path.Combine(@"Textures\Maps\NonCombat",
                    map.TextureName));
                map.tilesPerRow = map.texture.Width / map.TileSize.X;

                map.CombatTextureName = input.ReadString();
                map.combatTexture = input.ContentManager.Load<Texture2D>(
                    System.IO.Path.Combine(@"Textures\Maps\Combat",
                    map.CombatTextureName));

                map.MusicCueName = input.ReadString();
                map.CombatMusicCueName = input.ReadString();

                map.BaseLayer = input.ReadObject<int[]>();
                map.FringeLayer = input.ReadObject<int[]>();
                map.ObjectLayer = input.ReadObject<int[]>();
                map.CollisionLayer = input.ReadObject<int[]>();
                map.Portals.AddRange(input.ReadObject<List<Portal>>());
                map.PortalEntries.AddRange(
                    input.ReadObject<List<MapEntry<Portal>>>());
                foreach (MapEntry<Portal> portalEntry in map.PortalEntries)
                {
                    portalEntry.Content = map.Portals.Find(delegate (Portal portal)
                        {
                            return (portal.Name == portalEntry.ContentName);
                        });
                }

                map.ChestEntries.AddRange(
                    input.ReadObject<List<MapEntry<Chest>>>());
                foreach (MapEntry<Chest> chestEntry in map.chestEntries)
                {
                    chestEntry.Content = input.ContentManager.Load<Chest>(
                        System.IO.Path.Combine(@"Maps\Chests",
                        chestEntry.ContentName)).Clone() as Chest;
                }

                // load the fixed combat entries
                Random random = new Random();
                map.FixedCombatEntries.AddRange(
                    input.ReadObject<List<MapEntry<FixedCombat>>>());
                foreach (MapEntry<FixedCombat> fixedCombatEntry in
                    map.fixedCombatEntries)
                {
                    fixedCombatEntry.Content =
                        input.ContentManager.Load<FixedCombat>(
                        System.IO.Path.Combine(@"Maps\FixedCombats",
                        fixedCombatEntry.ContentName));
                    // clone the map sprite in the entry, as there may be many entries
                    // per FixedCombat
                    fixedCombatEntry.MapSprite =
                        fixedCombatEntry.Content.Entries[0].Content.MapSprite.Clone()
                        as AnimatingSprite;
                    // play the idle animation
                    fixedCombatEntry.MapSprite.PlayAnimation("Idle",
                        fixedCombatEntry.Direction);
                    // advance in a random amount so the animations aren't synchronized
                    fixedCombatEntry.MapSprite.UpdateAnimation(
                        4f * (float)random.NextDouble());
                }

                map.RandomCombat = input.ReadObject<RandomCombat>();

                map.QuestNpcEntries.AddRange(
                    input.ReadObject<List<MapEntry<QuestNpc>>>());
                foreach (MapEntry<QuestNpc> questNpcEntry in
                    map.questNpcEntries)
                {
                    questNpcEntry.Content = input.ContentManager.Load<QuestNpc>(
                        System.IO.Path.Combine(@"Characters\QuestNpcs",
                        questNpcEntry.ContentName));
                    questNpcEntry.Content.MapPosition = questNpcEntry.MapPosition;
                    questNpcEntry.Content.Direction = questNpcEntry.Direction;
                }

                map.PlayerNpcEntries.AddRange(
                    input.ReadObject<List<MapEntry<Player>>>());
                foreach (MapEntry<Player> playerNpcEntry in
                    map.playerNpcEntries)
                {
                    playerNpcEntry.Content = input.ContentManager.Load<Player>(
                        System.IO.Path.Combine(@"Characters\Players",
                        playerNpcEntry.ContentName)).Clone() as Player;
                    playerNpcEntry.Content.MapPosition = playerNpcEntry.MapPosition;
                    playerNpcEntry.Content.Direction = playerNpcEntry.Direction;
                }

                map.InnEntries.AddRange(
                    input.ReadObject<List<MapEntry<Inn>>>());
                foreach (MapEntry<Inn> innEntry in
                    map.innEntries)
                {
                    innEntry.Content = input.ContentManager.Load<Inn>(
                        System.IO.Path.Combine(@"Maps\Inns",
                        innEntry.ContentName));
                }

                map.StoreEntries.AddRange(
                    input.ReadObject<List<MapEntry<Store>>>());
                foreach (MapEntry<Store> storeEntry in
                    map.storeEntries)
                {
                    storeEntry.Content = input.ContentManager.Load<Store>(
                        System.IO.Path.Combine(@"Maps\Stores",
                        storeEntry.ContentName));
                }

                return map;
            }
        }
    }
}
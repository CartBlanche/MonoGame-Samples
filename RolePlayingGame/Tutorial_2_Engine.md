# **Role-Playing Game Sample Tutorial – Re-using the Tile Engine**

## **Introduction**

In this tutorial, you are going to extract the two-dimensional tile-based rendering and collision system from the Role-Playing Game (RPG) Sample, and add it to a new XNA Game Studio game.  The tutorial makes the following assumptions about the needs of your game:

1) You are comfortable working with C\# and developing your own game.  
2) You are interested in re-using the RPG Sample’s tile-based rendering and collision system (the “tile engine”).  
3) You will define the map layers in the same manner as the RPG Sample.  
4) You still need portals to move between maps.  
5) You are not interested in any of the other components of the RPG Sample, including animated sprites, treasure chests, monsters, and quests.  If you are interested in any of these, you can add them to your new project, as needed.  
6) You will tie the tile engine into your own game logic, which may or may not be based on the **Session** type in the RPG Sample.

Though this tutorial adds the tile engine to an empty XNA Game Studio project, you can adjust the steps as appropriate for your own game.  You may also feel free to adjust namespace, class, or other names within the RPG Sample code to match your own conventions. However, be careful to accommodate those changes as you work through the tutorial. 

## **How Does the Tile Engine Work?**

The tile engine is a code system that consumes two-dimensional map specifications and supports rendering and collision-check operations.  The run-time code that serves this purpose can be found in the RolePlayingGame project for your platform, in TileEngine\\TileEngine.cs.  Since you can only have one tile engine at a time, and it does not own the lifetime of any external objects, the entire type is specified statically in the **TileEngine** type.

The tile engine tracks one position in the world.  This position is used to center the camera, determine visibility, and test for collisions. This position is specified in the **PlayerPosition** type in the TileEngine\\PlayerPosition.cs file. It has three components:  a tile position, with integer indices given in map coordinates specifying which tile the “player” is in; an offset, with floating-point indices specifying the player’s exact location relative to the center of the current tile; and a **Direction**\-enumeration value specifying which way the player is currently facing based on the most recent movement.

The RolePlayingGameData project for your platform in Maps\\Map.cs specifies the **Map** type. It contains the full set of data for a given map.  The most important data are four two-dimensional arrays of integers:

* **Base Layer:**  A numerical index into the tiles in the map’s texture, specifying the first sprite drawn in the space.  Typically, this is the ground.  
* **Fringe Layer:**  A numerical index into the tiles in the map’s texture, specifying the second sprite drawn in the space.  Typically, these sprites are trees, buildings, fences – anything that is at ground level, but might be matched up with various ground tiles.  The separation between the base and fringe layers means you can have one tree sprite (fringe layer) that can sit on top of any kind of ground sprite (base layer).  
* **Object Layer:**  A numerical index into the tiles in the map’s texture, specifying the sprite drawn in the space after all objects have been drawn.  These sprites always appear on top of all other objects in the same tile – characters, chests, and so on.  These include treetops and signs.  
* **Collision Layer:**  An integer of value 0 (false) or nonzero (true), used as a Boolean value representing whether that tile can be entered by the player.

When these layers are drawn with **TileEngine.DrawLayer**, the view will be centered on the current position, if possible. It will constrain the view to a given viewport so that the background color is never seen.

The map object also contains lists of the objects with which the player may interact.  The only relevant one to this tutorial is the list of portals – gateways that move the player from one map object to another.  This data is stored in two lists. The first list contains a list of the **Portal** objects with their names, and the map name, portal names, and final destination to which they link.  The second list contains **MapEntry\<Portal\>** objects, which represent instances of the portal at a particular tile position on the map.  **MapEntry** objects also contain data about the direction that the object is facing, but facing direction does not matter for portal objects.

## **How is the Tile Engine Code Structured?**

The tile engine depends on a three-project system that is common to many XNA Game Studio games.

* **Data Project:**  A code library that defines the game types that will be used in the game’s data files and loaded by way of the XNA Content Pipeline at run-time.  In the RPG Sample, the game types provide their own Content Pipeline reader types as nested classes within each data type.  
* **Content Pipeline Extension Project:**  A Content Pipeline extension library that provides content writers for the game types defined in the data project.  The built-in importers and processors for XML files generally don’t need to be replaced.  Objects such as portal objects and **MapEntry** objects do not have their own separate content files. They are built and loaded as part of building and loading a map content file, and their readers and writers are invoked by the map reader and writer.  The same is true of inherited types – the **MapEntry** reader and writer calls into the base type’s (**ContentEntry**) reader and writer.  
* **Game Project:**  The executable XNA Game Studio game project, containing references to the data and extension projects, as well as game data stored as XML.

A **Map** type will be defined in the data project, along with any supporting types that are necessary.  Content Pipeline writer types for the **Map** type (and any supporting types) will be added to the extension project.  Finally, the **TileEngine** type, and a few supporting types, will be added to the game project, along with a modified piece of content from the RPG Sample for testing purposes.

Internally, the tile engine is defined statically.  A single map is set into the tile engine, and calls to **TileEngine.Update** and **TileEngine.DrawLayer** will use that map’s data for movement/collision and rendering, respectively.  The tile engine is not implemented as a XNA **GameComponent** subclass because the layers typically are interleaved with sprites from other sources.

## **Creating the Projects**

First, make sure that you have an instance of the RPG Sample on your computer:

1) If you have not done so, download the sample and install it on your computer.     
2) Create a new “Role Playing Game – Windows” project in Visual Studio.  
3) Open Windows Explorer and navigate to the RPG project’s directory.   An alternate method is to open any code file in the project in Visual Studio, right click its tab, click **Open Containing Folder**, and then navigate to the root folder of the project.   

***Note:**  The tutorial will refer to this window as the “RPG explorer window.”*  

You will notice three subdirectories in this root folder – RolePlayingGame, RolePlayingGameProcessors, and RolePlayingGameData – corresponding to the three projects in the RPG Sample solution.

Now let’s create the new Windows solution that will re-use the tile engine:

1) Create a new “Windows Game” project in XNA Game Studio.  Name it whatever you like.  Make sure the **Create Directory for Solution** check box is checked.  

***Note:**  The tutorial will refer to this original project as the “game project.”*

2) In the Solution Explorer, right click the **Solution** node (the root node in the window), hover over the **Add** option, and click **New Project**.  Select “Windows Game Library,” and give it a different name from the game project.   

***Note:**  The tutorial will refer to this project as the “data project.”*  

Delete Class1.cs from the project.  Right-click the data project root node and click **Add Reference**.  In the new **Add Reference** window that pops up, click the **.NET** tab, scroll down and select **System.Xml** from the list, and click **OK**.

3) In Solution Explorer, right-click the **Solution** node (the root node in the window), hover over the **Add** option, and click **New Project**.  Select **Content Pipeline Extension Library** , and give it a different name from the game and data projects.  Delete ContentProcessor1.cs from the project.  

***Note:**  The tutorial will refer to this project as the “pipeline project.”* 

4) Open Windows Explorer and navigate to the new project’s directory.   An alternate method is to open any code file in the project in Visual Studio, right-click its tab, click **Open Containing Folder**, and then navigate up to the root folder of the project.   

***Note:**  The tutorial will refer to this window as the “new-game explorer window.”* 

 You will notice that each of the three projects you just created has its own subdirectory in this root folder.

Next, set up the project references in the new game project:

1) The game project needs to know about the data types so it can load them at runtime.  In Solution Explorer, right-click the game project node, and click **Add Reference**.  In the **Add Reference** window that pops up, click the **Projects** tab, select your data project, and then click **OK**.  
2) The pipeline project also needs to know about the data types so it can build them.  In Solution Explorer, right-click the pipeline project node, and click **Add Reference**.  In the **Add Reference** window that pops up, click the **Projects** tab, select your data project, and then click **OK**.  
3) The game’s content project needs to know about the processor library so it can build the content.  In Solution Explorer, right-click the **Content** node under game project node, and then click **Add Reference**.  In the **Add Reference** window that pops up, click the **Projects** tab, select your pipeline project, and then click **OK**.

## **Copying the Code**

You need to copy the tile engine code and the necessary related components from the RPG project to the new game project.   Copy the following files from the specified folders in the RPG explorer window in the appropriate folders in the new-game explorer window:

* **Data Project:**  
  * RolePlayingGameData\\Map\\Map.cs  
  * RolePlayingGameData\\Map\\Portal.cs  
  * RolePlayingGameData\\ContentEntry.cs  
  * RolePlayingGameData\\ContentObject.cs  
  * RolePlayingGameData\\Direction.cs  
  * RolePlayingGameData\\MapEntry.cs  
* **Pipeline Project:**  
  * RolePlayingGameProcessors\\Map\\MapWriter.cs  
  * RolePlayingGameProcessors\\Map\\PortalWriter.cs  
  * RolePlayingGameProcessors\\ContentEntryWriter.cs  
  * RolePlayingGameProcessors\\MapEntryWriter.cs  
  * RolePlayingGameProcessors\\RolePlayingGameWriter.cs  
* **Game Project:**  
  * RolePlayingGame\\TileEngine\\PlayerPosition.cs  
  * RolePlayingGame\\TileEngine\\TileEngine.cs

You do not need to preserve the same subdirectory structure inside the new project folders.  For example, you may place Map.cs in the same directory as ContentEntry.cs, though both code files must be within the new data project’s directory.

Now that the files are in the correct project directories, add the code files to their respective projects.  You could do this by right-clicking on each project, and clicking **Add Existing Item** for each code file. However, since the code files are in the correct locations already, there is a faster way.  For each project, select the root project node, go to the **Projects** menu, and then click **Show All Files**.  This will show white rectangles with dashed outlines next to files that are present in the project’s directory, but are not yet included in the project.  Select all the code files that you have copied in, right-click them, and then click **Include in Project**.  You should toggle off **Show All Files** on each project after you are done. This ensures you do not accidentally include the “bin” or “obj” directories, or any other unintended content.  Repeat this process for each project.

## **Copying the Test Data**

Your own game will have its own maps and likely its own textures, but it will be easier to test the new game with maps and textures from the RPG Sample.

In the new-game explorer window, enter the subdirectory for the game project, then the Content subdirectory.  Create a new folder called Maps, and another new folder in the same directory called Textures.  Enter the new Textures subdirectory, and create new subfolder called Maps.  Enter the Maps subdirectory and create a new directory called NonCombat.  After the tutorial is complete, you can feel free to edit the **MapReader.LoadContent** method in Map.cs to load the map texture from a different subdirectory.

Copy the following files from the specified folders in the RPG explorer window into the new-game explorer window:

* **Game Project\\Content\\Maps:**  
  * RolePlayingGame\\Content\\Maps\\Map001.xml  
  * RolePlayingGame\\Content\\Maps\\Map002.xml  
* **GameProject\\Content\\Textures\\Maps\\NonCombat:**  
  * RolePlayingGame\\Content\\Textures\\Maps\\NonCombat\\ForestTiles.png

Repeat the process described in the “Copying the Code” section of this tutorial to include the content files in the content project.  Select the **Content** node in the game project, go to **Project**, and click **Show All Files**.  Select the **Maps** and **Textures** subdirectories, right-click them, and then click **Include in Project**, which will automatically include all contents of those directories in the project as well.

## **Examining What You Have Made**

You have created a new game with data and pipeline projects, and you have copied over the RPG Sample’s tile engine code and some game content for testing.  

You only copied a small subset of the code from the RPG Sample, and the code and data files that were copied – particularly the **Map** type and related types and content – reference types and methods that were left behind.  These features may or may not be relevant to your game. This tutorial assumes they are not relevant.  If you want, you can add those features and associated data back in after you complete this tutorial.  

The next several steps will remove these references from the copied code and content.  In general, you will find the compiler to be your friend – if it does not know about a certain type of object, then chances are it’s referring to a line of code that should be removed.  It will even underline each affected line, making them easy to identify.

## **Stripping Down the Data Types**

First, let’s remove the data in MapEntry.cs that is not a part of the new game.  The **Graphics Data** region within MapEntry.cs contains a definition for an **AnimatingSprite** object, which is used to hold clones of character sprites for independent animation and rendering.  The new game doesn’t use the **AnimatingSprite** class. You need to remove the entire **Graphics Data** region.

Next, let’s remove the data in Map.cs that is valid, but not a part of our new game.  Open Map.cs, and examine its contents.  You will notice that most of the data is implemented as a private member field and a public property, typically for read and write access.  When you remove a given field, you need to remove the property that accesses it, which should always be implemented directly beneath the field declaration.

The new game does not implement the RPG Sample’s combat engine. Navigate to the **Graphics Data** region, and remove the **combatTextureName** and **combatTexture** fields, and the **CombatTextureName** and **CombatTexture** properties.

The new game does not implement the audio manager or music system from the RPG Sample. You need to remove the entire **Music** region.

The only map objects that are implemented in the new game are portals. Open the Map Contents region and remove everything except the **portalEntries** field, the associated **PortalEntries** property, and the **FindPortal** method.  The removed fields and properties are **MapEntry** lists for chests, fixed-combats, non-player characters, inns, and stores, and the **RandomCombat** field and property.  The portal data should be implemented first in the **Map Contents** region, so it should be easy to remove everything else inside that region.

The **Map** and **MapEntry** types now only define the data in which you are interested.  Now you have to edit the remaining methods to remove any references to the now-missing fields and properties.  The easiest way to identify these is to let the compiler do the work.  In the Solution Explorer, right-click the data project node, and click **Build**.  You should see a number of compiler errors in the **Error List** window.

Double-click the first listing in the **Error List** window. This will take you to the beginning of the **Map.Clone** method.  Examine the method, and you will see a number of lines that have segments underlined – these are references to the recently-removed fields.  Remove each of these lines, including the entire for-loop that iterates over the **chestEntries** list (since that list is gone).  You can check that the lines removed correlate exactly to the fields that were removed earlier, and that all of the fields remaining are still copied or assigned in the **Clone** method. If you have done this correctly, the **Clone** method should look like this:

        public object Clone()  
        {  
            Map map \= new Map();

            map.AssetName \= AssetName;  
            map.baseLayer \= BaseLayer.Clone() as int\[\];  
            map.collisionLayer \= CollisionLayer.Clone() as int\[\];  
            map.fringeLayer \= FringeLayer.Clone() as int\[\];  
            map.mapDimensions \= MapDimensions;  
            map.name \= Name;  
            map.objectLayer \= ObjectLayer.Clone() as int\[\];  
            map.portals.AddRange(Portals);  
            map.portalEntries.AddRange(PortalEntries);  
            map.spawnMapPosition \= SpawnMapPosition;  
            map.texture \= Texture;  
            map.textureName \= TextureName;  
            map.tileSize \= TileSize;  
            map.tilesPerRow \= tilesPerRow;

            return map;  
        }

Next, navigate to the **Content Type Reader** region, and examine the **MapReader.Read** method.  Again, a number of lines have been underlined by the compiler, signifying errors due to our removed data.  Remove each of the lines, including each loop that loads the missing lists of map objects.  As with the **Clone** method, you can check that the lines removed correlate exactly to the fields that were removed earlier, and that all of that fields remaining are still read in.  If you have done this correctly, then the **MapReader.Read** method should look like this:

        /// \<summary\>  
        /// Read a Map object from the content pipeline.  
        /// \</summary\>  
        public class MapReader : ContentTypeReader\<Map\>  
        {  
            protected override Map Read(ContentReader input,   
Map existingInstance)  
            {  
                Map map \= existingInstance;  
                if (map \== null)  
                {  
                    map \= new Map();  
                }

                map.AssetName \= input.AssetName;

                map.Name \= input.ReadString();  
                map.MapDimensions \= input.ReadObject\<Point\>();  
                map.TileSize \= input.ReadObject\<Point\>();  
                map.SpawnMapPosition \= input.ReadObject\<Point\>();

                map.TextureName \= input.ReadString();  
                map.texture \= input.ContentManager.Load\<Texture2D\>(  
                    System.IO.Path.Combine("Textures", "Maps", "NonCombat",  
                    map.TextureName));  
                map.tilesPerRow \= map.texture.Width / map.TileSize.X;

                map.BaseLayer \= input.ReadObject\<int\[\]\>();  
                map.FringeLayer \= input.ReadObject\<int\[\]\>();  
                map.ObjectLayer \= input.ReadObject\<int\[\]\>();  
                map.CollisionLayer \= input.ReadObject\<int\[\]\>();  
                map.Portals.AddRange(input.ReadObject\<List\<Portal\>\>());

                map.PortalEntries.AddRange(  
                    input.ReadObject\<List\<MapEntry\<Portal\>\>\>());  
                foreach (MapEntry\<Portal\> portalEntry in map.PortalEntries)  
                {  
                    portalEntry.Content \= map.Portals.Find(  
delegate(Portal portal)  
                        {  
                            return (portal.Name \== portalEntry.ContentName);  
                        });  
                }

                return map;  
            }  
        }

We should be done with the data project, but let’s use the compiler to check that for us.  In the Solution Explorer, right-click the data project node, and click **Build**.  The build should be successful and return no errors.

## **Stripping Down the Pipeline Types**

Next, you have to clean up the Content Pipeline writer types for the data objects, removing references to the now-missing data.  First, you need to change how the Content Pipeline stores the name of the types to read.

Open RolePlayingGameWriter.cs.  This writer is the base type for all writers in the RPG Sample, and its responsibility is to implement the runtime reader and type names.  These names are how the Content Pipeline knows which types to instantiate when it loads a given .XNB binary file.  The names are fully-qualified, which means that they include the name of the assembly that they are found in.  It is probable your new data project is not named RolePlayingGameDataWindows or RolePlayingGameDataXbox. If this is the case, you need to modify this code for the new data project.   Press Ctrl-H to replace names in this particular file, and replace the term RolePlayingGameDataWindows with the name of your data project.

Next, you need to know what references to now-missing data need to be removed.  In the Solution Explorer, right-click the pipeline project node, and click **Build**.  You should see a number of compiler errors in the **Error List** window. 

Double-click the first entry. This should take you to the **MapWriter.Write** method.  As with Map.Clone, remove all the lines that have been underlined.  

***Note:**  In some cases, all of the incorrect lines might not be underlined.  If you only see an underline under the currently selected line, double-click each of the entries in the **Error List** window.  This will force Visual Studio to underline the affected lines.*  

When you finish, then the second half of **MapWriter.Write** method (the output.Write calls, after the data validation) should look like this:

            output.Write(value.Name);  
            output.WriteObject(value.MapDimensions);  
            output.WriteObject(value.TileSize);  
            output.WriteObject(value.SpawnMapPosition);  
            output.Write(value.TextureName);  
            output.WriteObject(value.BaseLayer);  
            output.WriteObject(value.FringeLayer);  
            output.WriteObject(value.ObjectLayer);  
            output.WriteObject(value.CollisionLayer);  
            output.WriteObject(value.Portals);  
            output.WriteObject(value.PortalEntries);

We should be done with the pipeline project, but let’s use the compiler to check that for us.  In the Solution Explorer, right-click the pipeline project node, and click **Build**.  The build should be successful and return no errors.

## **Stripping Down the Map Content**

You have removed the missing features from the copied code. Your next step is to remove tags from the Map XML content files that reference those features.

 Remove the following tags from the XML file, including the end tag, if any, and content between the beginning and end tags, if any:  \<CombatTextureName\>, \<MusicCueName\>, \<CombatMusicCueName\>, \<ChestEntries\>, \<FixedCombatEntries\>, \<RandomCombat\>, \<QuestNpcEntries\>, \<PlayerNpcEntries\>, \<InnEntries\>, and \<StoreEntries\>.  Repeat this modification for both Maps\\Map001.xml and Maps\\Map002.xml.

With the **Map** type, related types, and content cleaned up, it’s time to move on to the game project and fix the tile engine code itself.

## **Bringing the Tile Engine Under Control**

To clean up the tile engine, you will have to remove references to code that was not ported to the new game, just like the data and pipeline projects.  Navigate to the game project in the Solution explorer, and open TileEngine.cs.

The first step is to remove references to the Input Manager, which is a complex input abstraction for the RPG Sample that might cause your game have more complexity or exhibit a different functionality than you want.  The tile engine code that takes user input is well-localized; it is all in **TileEngine.UpdateUserMovement**, in the Party region.  Your game may not have a party of player characters, but the functionality is the same.  You may wish to rename these methods and regions later. However, for consistency, the tutorial will refer to their original names.  

The new input-handling system will be gamepad-only, and it will use the left thumbstick for moving the current player position.

First, add a using statement to the top of the function for the XNA Framework’s Input namespace.  While you’re here, add an entry for the XNA Framework’s content namespace, which you will need later.   The new using-statement block, found at the beginning of the code file, should look like this:

\#region Using Statements  
using System;  
using System.Collections.Generic;  
using Microsoft.Xna.Framework;  
using Microsoft.Xna.Framework.Content;  
using Microsoft.Xna.Framework.Input;  
using Microsoft.Xna.Framework.Graphics;  
using RolePlayingGameData;  
\#endregion

Start your modifications of **TileEngine.UpdateUserMovement** by adding a call to read the latest gamepad state to the beginning of the function:

        private static Vector2 UpdateUserMovement(  
GameTime gameTime)  
        {  
            Vector2 desiredMovement \= Vector2.Zero;  
            GamePadState gamePadState \= GamePad.GetState(PlayerIndex.One);

Then, replace each of the **InputManager** calls with equivalent comparisons to the gamePadState variable.  For example, this is the old code:

      if (InputManager.IsActionPressed(InputManager.Action.MoveCharacterUp))

And this is the new code:

      if (gamePadState.ThumbSticks.Left.Y \> 0f)  
   
The finished **TileEngine.UpdateUserMovement** should look like this:

        /// \<summary\>  
        /// Update the user-controlled movement of the party.  
        /// \</summary\>  
        /// \<returns\>The controlled movement for this update.\</returns\>  
        private static Vector2 UpdateUserMovement(  
GameTime gameTime)  
        {  
            Vector2 desiredMovement \= Vector2.Zero;  
            GamePadState gamePadState \= GamePad.GetState(PlayerIndex.One);

            // accumulate the desired direction from user input  
            if (gamePadState.ThumbSticks.Left.Y \> 0f)  
            {  
                if (CanPartyLeaderMoveUp())  
                {  
                    desiredMovement.Y \-= partyLeaderMovementSpeed;  
                }  
            }  
            if (gamePadState.ThumbSticks.Left.Y \< 0f)  
            {  
                if (CanPartyLeaderMoveDown())  
                {  
                    desiredMovement.Y \+= partyLeaderMovementSpeed;  
                }  
            }  
            if (gamePadState.ThumbSticks.Left.X \< 0f)  
            {  
                if (CanPartyLeaderMoveLeft())  
                {  
                    desiredMovement.X \-= partyLeaderMovementSpeed;  
                }  
            }  
            if (gamePadState.ThumbSticks.Left.X \> 0f)  
            {  
                if (CanPartyLeaderMoveRight())  
                {  
                    desiredMovement.X \+= partyLeaderMovementSpeed;  
                }  
            }

            if (desiredMovement \== Vector2.Zero)  
            {  
                return Vector2.Zero;  
            }

            return desiredMovement;  
        }

You need to make several other changes to the tile engine, but let’s use the compiler again to find those problems.  In the Solution Explorer, right-click the game project node, and click **Build**.  You should see several compiler errors in the **Error List** window.

Double-click the first entry in the **Error List** window, and you should be taken to this line:

// adjust the map origin so that the party is at the center of the viewport  
mapOriginPosition \+= viewportCenter \- (partyLeaderPosition.ScreenPosition \+   
    Session.Party.Players\[0\].MapSprite.SourceOffset);

The error text states that **Session** does not exist, but the real error is that we are no longer interested in a **SourceOffset**.  This term is used by the tile engine to ensure that the camera is pointing at the center of the player sprite, not its feet.  Remove that term and the operators that depend on it.  The corrected code should look like this:

// adjust the map origin so that the party is at the center of the viewport  
mapOriginPosition \+= viewportCenter \- partyLeaderPosition.ScreenPosition;

The next entry in the **Error List** window takes you to this line:

mapOriginPosition.Y \+= MathHelper.Max(  
    (viewport.Y \+ viewport.Height \- Hud.HudHeight) \-   
    (mapOriginPosition.Y \+ map.MapDimensions.Y \* map.TileSize.Y), 0f);

As with the last one, the error text states that the **Hud** type does not exist, but the real error is that we are no longer interested in that term.  In the RPG Sample, the bottom of the screen is taken up by a heads-up display with information about the party.  That’s not part of the new game, and the map display will take up the whole screen.  You need to remove that term.  The corrected code should look like this:

mapOriginPosition.Y \+= MathHelper.Max(  
    (viewport.Y \+ viewport.Height) \-   
    (mapOriginPosition.Y \+ map.MapDimensions.Y \* map.TileSize.Y), 0f);

The third and final entry in the **Error List** window should take you to this line:

    // check for anything that might be in the tile  
    if (Session.EncounterTile(mapPosition))

This line refers to a very important function in the RPG Sample, **Session.EncounterTile**, which is defined in Session\\Session.cs in the RolePlayingGame project.  This function checks a given tile for anything with which the user can interact, and it responds appropriately.  You need that functionality for portals.  However, you do not want to bring in all of Session.cs, so you will need to add the functionality to the **TileEngine** class.

You will need a **ContentManager** object to load the map objects. The static **TileEngine** does not have any way to retrieve one from the new game’s **Game** object (the RPG Sample used the **Session** type to make a **ContentManager** object available statically).   At the top of the **TileEngine** class, add a public static **ContentManager** field to the **TileEngine**, which the **Game1** object will eventually fill:

    static class TileEngine  
    {  
        public static ContentManager ContentManager \= null;

In your game, you may eventually make the content manager available to the tile engine in another manner.  

Return to the **TileEngine.MoveIntoTile** method.  Above the “if” statement, add a line to search for portals in the portal entry list, using the given position:

    // search for portals in the new tile  
    MapEntry\<Portal\> portalEntry \= map.PortalEntries.Find(  
        delegate(MapEntry\<Portal\> entry)  
        {  
            return (entry.MapPosition \== mapPosition);  
        });

    // check for anything that might be in the tile  
    if (Session.EncounterTile(mapPosition))

Then, change the “if” statement to check whether **portalEntry** is null, adding code to handle the null case:

    // search for portals in the new tile  
    MapEntry\<Portal\> portalEntry \= map.PortalEntries.Find(  
        delegate(MapEntry\<Portal\> entry)  
        {  
            return (entry.MapPosition \== mapPosition);  
        });

    // if there is a portal, then move through it  
    if ((portalEntry \!= null) && (portalEntry.Content \!= null))  
    {  
        return false;  
    }

Next, we need to make sure that the map name in the **portalEntry** object is a valid content path.  If you have followed the directions to this point, then the map XML files are in the Content\\Maps subdirectory inside your game project.  Add code within the new if statement to make sure that the content name is correct:

    // search for portals in the new tile  
    MapEntry\<Portal\> portalEntry \= map.PortalEntries.Find(  
        delegate(MapEntry\<Portal\> entry)  
        {  
            return (entry.MapPosition \== mapPosition);  
        });

    // if there is a portal, then move through it  
    if ((portalEntry \!= null) && (portalEntry.Content \!= null))  
    {  
        // make sure the content name is valid  
        string mapContentName \=   
            portalEntry.Content.DestinationMapContentName;  
        if (\!mapContentName.StartsWith("Maps" + Path.IO.DirectorySeparator.ToString()))  
        {  
            mapContentName \= System.IO.Path.Combine("Maps", mapContentName);  
        }  
        return false;  
    }

Finally, add calls to load the new map and to **TileEngine.SetMap**, passing in the new map and the new portal, if any:

    // search for portals in the new tile  
    MapEntry\<Portal\> portalEntry \= map.PortalEntries.Find(  
        delegate(MapEntry\<Portal\> entry)  
        {  
            return (entry.MapPosition \== mapPosition);  
        });

    // if there is a portal, then move through it  
    if ((portalEntry \!= null) && (portalEntry.Content \!= null))  
    {  
        // make sure the content name is valid  
        string mapContentName \=   
            portalEntry.Content.DestinationMapContentName;  
        if (\!mapContentName.StartsWith("Maps" + + Path.IO.DirectorySeparator.ToString()))  
        {  
            mapContentName \= System.IO.Path.Combine("Maps", mapContentName);  
        }  
        // load the new map  
        Map newMap \= ContentManager.Load\<Map\>(mapContentName);  
        SetMap(newMap,   
            newMap.FindPortal(portalEntry.Content.DestinationMapPortalName));  
        return false;  
    }

We should be done with the pipeline project, but let’s use the compiler to check that for us.  In the Solution Explorer, right-click the pipeline project node, and click **Build**.  The build should be successful and return no errors.

## **Hooking it Up**

Congratulations\!  You have modified the RPG Sample code, and it builds all of the types and content without any errors.  Press F5 to start running your game.

Unfortunately, the game only renders the default CornflowerBlue background color.  This is because there is nothing in the Game implementation that makes use of the **Map** or **TileEngine** types\!

Open Game1.cs within the game project.   First, add “using” lines to the top of the file for the namespaces used by the tile engine (RolePlayingGame) and the data types (RolePlayingGameData). 

using RolePlaying;  
using RolePlayingGameData;

 If you choose to rename the namespaces as you added the files to match your own project, then you might not need to do this.

Next, add a line of code to fill the **ContentManager** object in the **TileEngine** type:

public Game1()  
{  
    graphics \= new GraphicsDeviceManager(this);  
    Content.RootDirectory \= "Content";  
    // configure the content manager for the tile engine  
    TileEngine.ContentManager \= Content;  
}

You need to perform two actions in **Game1.LoadContent**:

1) Load the initial test map, and set it into the tile engine.  There is no portal, so pass in “null” for the new portal. This will use the spawn position defined within the **Map** type.  
2) Set the current viewport into the tile engine using the static Viewport property.

protected override void LoadContent()  
{  
    // Create a new SpriteBatch, which can be used to draw textures.  
    spriteBatch \= new SpriteBatch(GraphicsDevice);

    // set the viewport for the tile engine  
    TileEngine.Viewport \= graphics.GraphicsDevice.Viewport;

    // load the initial map and set it into the tile engine  
    TileEngine.SetMap(Content.Load\<Map\>(@"Maps\\\\Map001"), null);  
}

The tile engine needs to be updated. Call **TileEngine.Update** from **Game1.Update**:

protected override void Update(GameTime gameTime)  
{  
    // Allows the game to exit  
   if (GamePad.GetState(PlayerIndex.One).Buttons.Back \== ButtonState.Pressed)  
        this.Exit();

    // update the tile engine  
    TileEngine.Update(gameTime);

    base.Update(gameTime);  
}

Finally, the tile engine needs to be drawn.   Add calls to begin and end the **SpriteBatch** object, and draw the layers of the tile engine.  You need to leave a spot for any additional object rendering you might choose to add later. That spot would fit between the base and fringe drawing and the object drawing. Thus, you will need two **DrawLayer** calls:

protected override void Draw(GameTime gameTime)  
{  
    graphics.GraphicsDevice.Clear(Color.MonoGameOrange);

    spriteBatch.Begin();

    //  
    // draw the tile engine  
    //  
    // draw the base and fringe layers  
    TileEngine.DrawLayers(spriteBatch, true, true, false);  
    // TODO: draw anything that goes on the map  
    // draw the object layer  
    TileEngine.DrawLayers(spriteBatch, false, false, true);

    spriteBatch.End();

    base.Draw(gameTime);  
}

That should be all of the modifications necessary.  Press F5 to run the game.

## **Let’s Take This Tile Engine for a Spin\!**

Congratulations\!  You have modified the RPG Sample code. It builds all the types and content without any errors, *and* the tile engine is running.  You should see the first map from the RPG Sample, and you can move around, scrolling the map. 

You’ll notice, however, that the tile engine does not always center the view on the current position.  It will stop scrolling to avoid showing anything beyond the boundaries of the map.  This can make it difficult to get your bearings.  You can add a small signifier at the current location, using **TileEngine.PartyLeaderPosition.ScreenPosition** to get the actual screen coordinates of the player position.  Internally, that function uses the very useful **TileEngine.GetScreenPosition(Point mapPosition)** static method to convert between map coordinates and actual screen coordinates.

There are many ways to add a current-position marker.  One way to add a simple rectangle without adding new textures or resources is to clear a small rectangle at the current position:

protected override void Draw(GameTime gameTime)  
{  
    graphics.GraphicsDevice.Clear(Color.MonoGameOrange);

    spriteBatch.Begin();

    //  
    // draw the tile engine  
    //  
    // draw the base and fringe layers  
    TileEngine.DrawLayers(spriteBatch, true, true, false);

    // TODO: draw anything that goes on the map  
    // \-- for now, clear a small rectangle to white  
    // \-- Clear cannot be called from within a SpriteBatch block  
    spriteBatch.End();  
    Vector2 position \= TileEngine.PartyLeaderPosition.ScreenPosition;  
    Rectangle\[\] clearRects \= new Rectangle\[1\];  
    clearRects\[0\] \= new Rectangle((int)position.X, (int)position.Y,   
       20, 20);  
    graphics.GraphicsDevice.Clear(ClearOptions.Target, Color.White,   
       0f, 0, clearRects);  
    spriteBatch.Begin();

    // draw the object layer  
    TileEngine.DrawLayers(spriteBatch, false, false, true);

    spriteBatch.End();

    base.Draw(gameTime);  
}

Now you can easily see that the current position is blocked by the collision layer, when the tile engine scrolls, and when you reach the bottom of Map001 and move to Map002.  Be careful when moving to the southern end of the bridge on Map002.  Your game will probably attempt to load Map003, which you have not brought over.

## **Conclusion**

We structured this tutorial to port the most functionality with the fewest steps.  If your game would benefit from **AnimatingSprite**s, or any of the other functionality in the game, then you can port more code over.  Also, there are many useful constants and other pieces of functionality to explore in the tile engine. Examine the code and make whatever modifications your game needs.

The final step is to implement your own game, or to adapt these steps to add the tile engine to your existing project.   The RPG Sample provides you with a tile engine that gives you a strong baseline for implementing your own two-dimensional game.
# **Role-Playing Sample Tutorial – Adding a Quest**

## 

## **Introduction**

In this tutorial, you are going to add a quest to an instance in the Role-Playing Game (RPG) Sample.  You can apply this tutorial to an instance of the sample without any other modifications.

## 

## **What is a Quest?**

 Quests are an important part of a role-playing game. A quest defines the storyline that takes the player through the game content.  In the RPG Sample, quests provide additional dangers and rewards to the player. They provide access to the next element in the story line.

Quests are specified as XML files. They are located in the Content\\Quests directory in the Sample.  These XML files are built by the XNA Content Pipeline. They are loaded at run time into a **Quest** object. They are defined in Quests\\Quest.cs in the RolePlayingGameData project for your platform.

## 

## **What is a Quest Line?**

Quests are organized into “lines.”Lines are a series of quests that open sequentially as previous entries complete.  The Sample initially only supports one quest line at a time: the “main quest line.”  The first quest begins as soon as a new game starts. It continues until the primary adversary is defeated.

Quest lines are also specified as XML files, which are located in the Content\\Quests\\QuestLines directory in the Sample.  The XNA Content Pipeline builds these XML files. They are loaded at run time into a **QuestLine** object. This object is defined in Quests\\QuestLine.cs in the RolePlayingGameData project for your platform.

The Sample comes with one quest line, Content\\Quests\\QuestLines\\MainQuestLine.xml.  When the player starts a new game, a new **Session** object is created with the data in Content\\MainGameDescription.xml, which is processed by the XNA content pipeline and loaded into a **GameStartDescription** object.   This description object tells the **Session** to load MainQuestLine.xml and start the first quest immediately (“Mercadia”).  

As each quest is completed, the next quest in the quest line immediately begins until there are no more quests.

## **What Quest Shall We Make?**

In the beginning of the game, the player is instructed to visit “Shed-darr the Wise,” which is a non-player character (NPC) near the player’s starting position.  Shed-darr gives the players a few simple quests and hands out starting equipment such as a piece of armor and a sword as a reward.  He sends the player into a relatively easy fight with Sir Shire, and then sends him out to fight goblins.

Let’s get the player some more gold, experience, and a helmet before the party has to face Sir Shire.

## 

## **Creating a New Quest**

First, we need to create a new XML file.  The easiest way to do this, and the best way to make sure it is in the right location, is to open the RPG Sample and navigate in the Solution Explorer to the RolePlayingGame project for your platform, and then go to the Content\\Quests folder.  Right click on the Quests folder and select “New Item,” and choose “XML File” from the New Item dialog.  Make sure you remove any content in the file before you follow the rest of the steps in the tutorial.  Another way to do this is to create a new text document using Notepad or a similar application (as long as it makes plain text). Then rename it to XML, copy it to the Content\\Quests folder within the RolePlayingGame project for your platform, and add it to the project after you are done.  Make sure the XML file is named AndNowAHelmet.xml, so the “content name” property on the item is “AndNowAHelmet.”

### 

### **Outer Tags**

The outermost tags are required for the XNA Content Pipeline, but they are not very interesting to us.  They must be present in all XML files that describe quests. Otherwise, the XNA Content Pipeline cannot load the file.

\<?xml version\="1.0" encoding\="utf-8" ?\>  
\<XnaContent\>  
  \<Asset Type\="RolePlayingGameData.Quest"\>

  \</Asset\>  
\</XnaContent\>

**Add the rest of the tags inside the Asset tag**.  The order of the content is significant.  Your quest will fail to build (typically with a “missing tag” error where it expected to find certain information) if all of the required information is not present in the correct order.

### 

### **Name and Description**

The first tag specifies the name of the quest as it shows up in the quest log menu.  This value is for a well-formatted name, with spaces and other punctuation.  It does not have to match the filename, nor does it have to be unique among the other quests.

\<Name\>And Now, A Helmet\</Name\>

The \<Description\> tag is the longer description displayed when the quest-detail screen appears in-game – when the quest is started or updated – or after selecting a quest in the quest log screen.

\<Description\>I do not think that you are quite ready for combat yet.  Find me one more Glimmering Ruby, and I will reward you again.  Then, we shall see how you fare in combat.\</Description\>

This tag is required, but it may be empty:

\<Description /\>

### 

### **Objective and Completion Messages**

The \<ObjectiveMessage\> tag is a message that describes specifically what the player has to do to complete the quest.  Like the description, it appears when the Quest Detail screen is shown in-game – when the quest is started or updated – or after selecting a quest in the Quest Log screen where it appears under the “Current Objective” heading.  Like the \<Description\> tag, it is mandatory, though it may be empty.

\<ObjectiveMessage\>Look for another nearby chest that contains the Glimmering Ruby.\</ObjectiveMessage\>

The text within the \<CompletionMessage\> tag is shown in one of two situations:

1) There is a destination NPC that the party must visit to finish the quest.  In this case, the completion message is shown as the NPC’s dialogue when the party visits him or her.  
2) There is not a destination NPC.  In this case, the completion dialogue is shown in a “Quest Complete” dialogue screen as soon as all requirements are met, and before the rewards screen is shown.

Like the \<Description\> and \<ObjectiveMessage\> tags, it is mandatory, though it may be empty.

\<CompletionMessage\>Thank you for the Glimmering Ruby.  I shall reward you with the Hide Helmet.  Go to the Inventory screen and equip it now\!\</CompletionMessage\>

### 

### **Quest Requirements**

All quests must have two requirement list tags:  

* **\<GearRequirements\>**, containing a list of the set of gear, in the desired quantities, that the party must hold at the same time.  
* **\<MonsterRequirements\>**, containing the required number of monsters that the party must kill after receiving the quest. 

 Each requirement in each list is loaded by the XNA Content Pipeline at runtime into a **QuestRequirement** object, defined in Quests\\QuestRequirement.cs in the RolePlayingGameData project for your platform.  Each item in the XML file has a \<ContentName\> tag, which is specified using theXNA Content Pipeline “content name” property (relative to the Content\\Gear folder for gear requirements and Content\\Characters\\Monsters folder for monster requirements).  Also, each item has a \<Count\> tag with the required number of gear or monsters for the requirement to be met.

Like all list tags in the RPG Sample content files, each entry in the list is contained with an \<Item\> tag.  Like the earlier messages, both tags are required, but either or both may be empty.

\<GearRequirements\>  
\<Item\>  
\<ContentName\>Items\\GlimmeringRuby\</ContentName\>  
\<Count\>1\</Count\>  
\</Item\>  
\</GearRequirements\>  
\<MonsterRequirements /\>

Though this quest will not require any monsters, here is an example of a non-empty monster tag. **Do not add this to your quest** – it will be difficult to complete.

\<MonsterRequirements\>  
\<Item\>  
\<ContentName\>SirShire\</ContentName\>  
\<Count\>1\</Count\>  
\</Item\>  
\</MonsterRequirements\>

### **Quest Entries**

When a quest is active, it can add map entries to the world, in any map it wants.  A quest must have two kinds of entry list tags:  

* **\<FixedCombatEntries\>**,  containing a list of “fixed combats,” each one a group of monsters represented by one monster sprite on the world map that initiates combat when the party sprite collides with it.  These are represented in code by the **FixedCombat** object, defined in Maps\\FixedCombat.cs in the RolePlayingGameData project for your platform.  
* **\<ChestEntries\>**, containing a list of chests, a box containing gold and items that the party may take.  These are represented in code by the **Chest** object, which is defined in Maps\\Chest.cs in the RolePlayingGameData project for your platform.

These entries typically are added to ensure the player can find the monsters and gear required by the quest. 

Each entry has a \<ContentName\> tag, which is specified using theXNA Content Pipeline content name’s property (relative to the Content\\Map\\FixedCombats folder for fixed-combat entries and Content\\Maps\\Chests folder for treasure-chest entries).  Also, each item has a \<MapPosition\> and \<MapContentName\> tag (relative to the Content\\Maps folder) to specify the location of each entry.  Finally, each fixed-combat entry has a \<Direction\> tag with the direction that the monster is facing on the world map.

Like all list tags in the RPG Sample content files, each entry in the list is contained with an \<Item\> tag.  Like the earlier messages, both tags are required, but either or both may be empty.

\<FixedCombatEntries /\>  
\<ChestEntries\>  
\<Item\>  
\<ContentName\>Ruby\</ContentName\>  
\<MapPosition\>13 9\</MapPosition\>  
\<MapContentName\>Map001\</MapContentName\>  
\</Item\>  
\</ChestEntries\>

Though this quest will not require any monsters, here is an example of a non-empty monster tag.  **Do not add this to your quest** – it will be difficult to complete.

\<FixedCombatEntries\>  
\<Item\>  
\<ContentName\>SirShire\</ContentName\>  
\<MapPosition\>9 19\</MapPosition\>  
\<Direction\>North\</Direction\>  
\<MapContentName\>Map001\</MapContentName\>  
\</Item\>  
\</FixedCombatEntries\>

### **Optional Destination**

When all requirements have been met, some quests require the party to return to a particular NPC to receive the reward.  When the party interacts with this NPC, the party receives one final dialogue. Then the rewards screen appears.  

There are three tags to specify this destination.  

* **\<DestinationMap\>**, containing the content name of the map that the NPC is on (relative to the Content\\Maps folder).    
* **\<DestinationNpc\>**, containing the content name of the quest NPC with which the party must interact (relative to the Content\\Characters\\QuestNPCs folder).  Note that if more than one of that quest’s NPC is on the specified map, then any one of them will complete the quest.  
* **\<DestinationObjectiveMessage\>**, containing the message shown under the “Current Objective” heading in the Quest Detail screen after all requirements have been met.

Each of these tags is optional, so the quest will build if they are not present.  If they are present, they must still appear in the order listed above.

Both the \<DestinationMap\> and \<DestinationNpc\> must be present and non-empty for the destination to be valid.  If the destination is valid, then the Quest Detail screen appears when the requirements are met. The message in the \<DestinationObjectMessage\> tag appears under the “Current Objective” heading.  When the party meets with the destination NPC, the message in the \<CompletionMessage\> tag is shown as the NPC’s dialogue.  If the destination is not valid, the message in the \<Completion\> tag appears in its own screen when the requirements are met, and the quest concludes immediately.

\<DestinationMapContentName\>Map001\</DestinationMapContentName\>  
\<DestinationNpcContentName\>Sheddarr\</DestinationNpcContentName\>  
\<DestinationObjectiveMessage\>Go and speak with Shed-darr the Wise.\</DestinationObjectiveMessage\>

### 

### **Optional Rewards**

When all requirements are met, and the destination NPC has been met (or, if there is no destination NPC, the “Quest Complete” dialogue screen has been dismissed), the rewards screen appears and a sound effect plays.  Quests provide three kinds of rewards tags:

* **\<ExperienceReward\>**, containing an integer amount of experience awarded to each party member.  
* **\<GoldReward\>**, containing an integer amount of gold pieces that are added to the party coffers.  
* **\<GearRewardContentNames\>**, containing a list of content names of the gear added to the party’s inventory, relative to the Content\\Gear folder.  Like all list tags in the RPG Sample content files, each entry in the list is contained with an \<Item\> tag.  

Each of these tags are optional, so the quest will build if they are not present.  If they are present, they must still appear in the order listed above.  

\<ExperienceReward\>50\</ExperienceReward\>  
\<GoldReward\>50\</GoldReward\>  
\<GearRewardContentNames\>  
\<Item\>Armor\\HideHelmet\</Item\>  
\</GearRewardContentNames\>

### **Let’s Review…**

Let’s take a look at the whole XML file we constructed with our new quest.

\<?xml version\="1.0" encoding\="utf-8" ?\>  
\<XnaContent\>  
  \<Asset Type\="RolePlayingGameData.Quest"\>

\<Name\>And Now, A Helmet\</Name\>

\<Description\>I do not think that you are quite ready for combat yet.  Find me one more Glimmering Ruby, and I will reward you again.  Then, we shall see how you fare in combat.\</Description\>

\<ObjectiveMessage\>Look for another nearby chest that contains the Glimmering Ruby.\</ObjectiveMessage\>

\<CompletionMessage\>Thank you for the Glimmering Ruby.  I shall reward you with the Hide Helmet.  Go to the Inventory screen and equip it now\!\</CompletionMessage\>

\<GearRequirements\>  
\<Item\>  
\<ContentName\>Items\\GlimmeringRuby\</ContentName\>  
\<Count\>1\</Count\>  
\</Item\>  
\</GearRequirements\>  
\<MonsterRequirements /\>

\<FixedCombatEntries /\>  
\<ChestEntries\>  
\<Item\>  
\<ContentName\>Ruby\</ContentName\>  
\<MapPosition\>13 9\</MapPosition\>  
\<MapContentName\>Map001\</MapContentName\>  
\</Item\>  
\</ChestEntries\>

\<DestinationMapContentName\>Map001\</DestinationMapContentName\>  
\<DestinationNpcContentName\>Sheddarr\</DestinationNpcContentName\>  
\<DestinationObjectiveMessage\>Go and speak with Shed-darr the Wise.\</DestinationObjectiveMessage\>

\<ExperienceReward\>50\</ExperienceReward\>  
\<GoldReward\>50\</GoldReward\>  
\<GearRewardContentNames\>  
\<Item\>Armor\\HideHelmet\</Item\>  
\</GearRewardContentNames\>

  \</Asset\>  
\</XnaContent\>

Make sure the XML file is part of your RolePlayingGame project for your platform, in the Content\\Quests folder. Now you can build your project.  If you get any errors at build-time, make sure your quest file matches the text given above.

## **Adding the Quest to the Game**

Even though the quest has been added to the project and it builds correctly, the player will never see the quest in the game.  Quests are only given to the player as part of a quest line, after the previous quest is completed.  Therefore, the quest must be added to the main quest line.

In the RolePlayingGame project for your platform, navigate to the Content\\Quests\\QuestLine folder in Solution Explorer.  Open MainQuestLine.xml. If you have not modified your Sample, you should see the following contents:

\<?xml version\="1.0" encoding\="utf-8" ?\>  
\<XnaContent\>  
  \<Asset Type\="RolePlayingGameData.QuestLine"\>  
\<Name\>Main Quest Line\</Name\>  
\<QuestContentNames\>  
\<Item\>SaveMercadia\</Item\>  
\<Item\>HideArmor\</Item\>  
\<Item\>ShortSword\</Item\>  
\<Item\>TestBattle\</Item\>  
\<Item\>TheGoblinBrigade\</Item\>  
\<Item\>LushVillage\</Item\>  
\<Item\>TheOrcCaptain\</Item\>  
\<Item\>TheBanditLeader\</Item\>  
\<Item\>DefeatTamar\</Item\>  
\</QuestContentNames\>  
  \</Asset\>  
\</XnaContent\>

Let’s insert this quest to the list just before the test battle, using its content name (relative to the Content\\Quests folder):

\<Item\>ShortSword\</Item\>  
\<Item\>AndNowAHelmet\</Item\>  
\<Item\>TestBattle\</Item\>

Save the quest line XML file, and build the project.  As before, if you get any errors at build-time, make sure that your file matches the text given above.

## **Let’s Play\!**

Start up the game. Either start a new game or load a saved game from before the Short Sword quest was completed.  The first few quests can be completed quickly, and then the new quest will be activated.  Congratulations\!  You’re playing your own quest\!

## **Conclusion**

The power of the quest system in the RPG Sample is obvious, but there are certain limits to how quests are defined and executed.  However, you can modify the code in the quest system. You can add the ability to have secondary simultaneous quest lines, more quest entries, more quest requirements, different behavior for quest completion or requirement checks, and so on.  Think about what your ideal quest line would require from such a system, and make it happen\!  

By this time, the power of the quest system should be clear. It should allow you to define your own storyline within the RPG Sample.  The only limit is your imagination\!
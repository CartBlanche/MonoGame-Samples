using System;
using Microsoft.Xna.Framework.Storage;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Xna.Framework;

namespace Storage
{
    [Serializable]
    public struct SaveGame
    {
        public string Name;
        public int HiScore;
        public DateTime Date;

        [NonSerialized]
        public int DontKeep;
    }

    public class SaveGameStorage
    {
        private const string CONTAINER_NAME = "StorageGame";

        public SaveGame Load()
        {
            SaveGame ret = new SaveGame();
            var device = new StorageDevice(PlayerIndex.One);
            StorageContainer? container = null;
            Stream? fileStream = null;
            try
            {
                // Open a storage container
                container = device.OpenContainer(CONTAINER_NAME);

                // Open the file
                fileStream = container.OpenFile("savegame.xml", FileMode.OpenOrCreate, FileAccess.Read);

                // Read the data from the file
                XmlSerializer serializer = new XmlSerializer(typeof(SaveGame));
                var data = serializer.Deserialize(fileStream);

                if (data is SaveGame saveGame)
                {
                    ret = saveGame;
                }
                else
                {
                    ret = new SaveGame
                    {
                        Name = "Default",
                        HiScore = 0,
                        Date = DateTime.Now,
                        DontKeep = 0
                    };
                }
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
                    
                if (container != null)
                    container.Dispose();
            }

            return ret;
        }

        public void Save(SaveGame sg)
        {
            var device = new StorageDevice(PlayerIndex.One);
            StorageContainer? container = null;
            Stream? fileStream = null;
            try
            {
                // Open a storage container
                container = device.OpenContainer(CONTAINER_NAME);

                // Open the file
                fileStream = container.OpenFile("savegame.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite);

                // Convert the object to XML data and put it in the stream
                XmlSerializer serializer = new XmlSerializer(typeof(SaveGame));
                serializer.Serialize(fileStream, sg);
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
                
                if (container != null)
                    container.Dispose();
            }
        }
    }
}
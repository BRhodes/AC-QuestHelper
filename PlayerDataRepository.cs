using LiteDB;
using QuestHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestHelper
{
    class PlayerDataRepository
    {
        LiteDB.LiteDatabase _db;
        private string _preferencesFilePath;

        public PlayerDataRepository()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _preferencesFilePath = $"{appdata}\\VehnPlugins\\QuestHelper\\";


        }
        public PlayerDataRepository(string ServerName, string accountName, string characterName)
        {
        }

        internal PlayerData Load(string server, string accountName, string name)
        {
            using (var db = new LiteDatabase(_preferencesFilePath))
            {
                var col = db.GetCollection<PlayerData>("PlayerData");

                return col.FindOne(x => x.CharacterName == name);
            }
        }
    }
}

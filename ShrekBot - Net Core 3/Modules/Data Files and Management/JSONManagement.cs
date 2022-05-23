using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShrekBot.Modules.Data_Files_and_Management
{
    public abstract class JSONManagement
    {
        protected string fileName = "";
        protected string filePath = "";
        protected Dictionary<string, string> pairs;
        public bool DoesFileExist { get; protected set; }
        public int PairCount { get { return pairs.Count; } }
        
        public JSONManagement() => pairs = new Dictionary<string, string>();
        
        protected void ValidateFileContents()
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "");
                SaveDataToFile();
                DoesFileExist = false;
            }
            DoesFileExist = true;
        }

        protected void SaveDataToFile()
        {
            string json = JsonConvert.SerializeObject(pairs, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        protected void Initialize(string relativePath, string name)
        {
            filePath = relativePath;
            fileName = name;

            ValidateFileContents();
            if (!DoesFileExist)
                return;
            string json = File.ReadAllText(filePath);
            pairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        protected string GetKeys()
        {
            IList<string> keys;
            using (StreamReader sr = File.OpenText(filePath))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                JObject json = (JObject)JToken.ReadFrom(reader);
                keys = json.Properties().Select(p => p.Name).ToList();               
            }

            string keyList = "";
            foreach (var item in keys)
                keyList += item + "\n";

            return keyList;
        }
        //check if file exists on public methods, to remove the validation in the command classes in case of error
        public bool DoesKeyExist(string key) 
            => pairs.ContainsKey(key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Empty string if key doesn't exist</returns>
        public virtual string GetValue(string key) => pairs.ContainsKey(key) ? pairs[key] : "";                 

        public void EditValue(string key, string newValue)
        {
            pairs[key] = newValue;
            SaveDataToFile();
        }
    }
}

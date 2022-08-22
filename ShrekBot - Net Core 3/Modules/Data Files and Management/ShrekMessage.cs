namespace ShrekBot.Modules.Data_Files_and_Management
{
    public sealed class ShrekMessage : JSONManagement
    {
        private readonly bool AddEmoji;
        public ShrekMessage(bool addEmoji = false) : base()
        {
            AddEmoji = addEmoji;
            Initialize(@"..\quotes.json", "quotes.json");
        }

        public override string GetValue(string key)
        {
            if (!AddEmoji)
                return base.GetValue(key);
         
            var Anger = new Discord.Emoji("\uD83D\uDCA2");
            return $"{Anger}{base.GetValue(key)}{Anger}";
        }

        public void AddQuote(string value)
        {
            pairs.Add($"{pairs.Count + 1}", value);
            SaveDataToFile();
        }
    }
}

using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public struct CommanderGenerationProfile
    {
        public string identityRule; 
        public string statsRule;
        public string traitsRule;
        public int level;

        public static CommanderGenerationProfile Default => new CommanderGenerationProfile
        {
            identityRule = "Han",
            statsRule = "Balanced",
            traitsRule = "Standard",
            level = 1
        };
    }
}
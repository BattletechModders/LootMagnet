using Harmony;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace LootMagnet {
    public static class LootMagnet {

        public const string HarmonyPackage = "us.frostraptor.LootMagnet";

        public static Logger Logger;
        public static string ModDir;
        public static ModConfig Config;

        public static readonly Random Random = new Random();

        public static void Init(string modDirectory, string settingsJSON) {
            ModDir = modDirectory;

            Exception settingsE;
            try {
                LootMagnet.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            } catch (Exception e) {
                settingsE = e;
                LootMagnet.Config = new ModConfig();
            }

            Logger = new Logger(modDirectory, "loot_magnet");
            Logger.LogIfDebug($"ModDir is:{modDirectory}");
            Logger.LogIfDebug($"mod.json settings are:({settingsJSON})");
            Logger.LogIfDebug($"mergedConfig is:{LootMagnet.Config}");

            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
}

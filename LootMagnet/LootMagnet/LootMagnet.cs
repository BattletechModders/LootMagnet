using Harmony;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
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

            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            Logger.Log($"Assembly version: {fvi.ProductVersion}");

            Logger.LogIfDebug($"ModDir is:{modDirectory}");
            Logger.LogIfDebug($"mod.json settings are:({settingsJSON})");
            LootMagnet.Config.LogConfig();

            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
}

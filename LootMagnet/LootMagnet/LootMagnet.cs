using Harmony;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection;

namespace LootMagnet {
    public static class LootMagnet {

        public class Mod {
            public static Logger Logger;
            public static string ModDir;
            public static ModConfig Config;
        }

        public const string HarmonyPackage = "us.frostraptor.LootMagnet";
        public const string LogName = "loot_magnet";

        public static readonly Random Random = new Random();

        public static void Init(string modDirectory, string settingsJSON) {
            Mod.ModDir = modDirectory;

            Exception settingsE = null;
            try {
                Mod.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            } catch (Exception e) {
                settingsE = e;
                Mod.Config = new ModConfig();
                Mod.Config.InitDefaultReputation();
            }

            Mod.Logger = new Logger(modDirectory, LogName);

            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            Mod.Logger.Info($"Assembly version: {fvi.ProductVersion}");

            Mod.Logger.Debug($"ModDir is:{modDirectory}");
            Mod.Logger.Debug($"mod.json settings are:({settingsJSON})");
            Mod.Config.LogConfig();

            if (settingsE != null) {
                Mod.Logger.Info($"ERROR reading settings file! Error was: {settingsE}");
            } else {
                Mod.Logger.Info($"INFO: No errors reading settings file.");
            }

            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
}

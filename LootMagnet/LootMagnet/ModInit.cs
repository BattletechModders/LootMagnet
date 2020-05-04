using Harmony;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace LootMagnet {
    public static class Mod {

        public const string HarmonyPackage = "us.frostraptor.LootMagnet";
        public const string LogName = "loot_magnet";

        public static Logger Log;
        public static string ModDir;
        public static ModConfig Config;

        public static readonly Random Random = new Random();

        public static void Init(string modDirectory, string settingsJSON)
        {
            Mod.ModDir = modDirectory;

            Exception settingsE = null;
            try
            {
                Mod.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            }
            catch (Exception e)
            {
                settingsE = e;
                Mod.Config = new ModConfig();
                Mod.Config.InitDefaultReputation();
            }

            Mod.Log = new Logger(modDirectory, LogName);

            //Assembly asm = Assembly.GetExecutingAssembly();
            //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            //Mod.Log.Info($"Assembly version: {fvi.ProductVersion}");

            Mod.Log.Debug($"ModDir is:{modDirectory}");
            Mod.Log.Debug($"mod.json settings are:({settingsJSON})");
            Mod.Config.LogConfig();

            if (settingsE != null)
            {
                Mod.Log.Info($"ERROR reading settings file! Error was: {settingsE}");
            }
            else
            {
                Mod.Log.Info($"INFO: No errors reading settings file.");
            }

#if NO_CC
#else
            // Initialize custom components
            CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
#endif

            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        
        

    }
}

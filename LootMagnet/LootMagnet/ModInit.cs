using LootMagnet.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LootMagnet
{
    public static class Mod
    {

        public const string HarmonyPackage = "us.frostraptor.LootMagnet";
        public const string LogName = "loot_magnet";

        public static DeferringLogger Log;
        public static string ModDir;
        public static ModConfig Config;
        public static ModConfig GlobalConfig;

        public static readonly Random Random = new Random();
        public static void FinishedLoading(List<string> loadOrder)
        {
            Mod.Log.Debug?.Write($"FinishedLoading");
            try
            {
                CustomSalvageHelper.DetectAPI();
                CustomSettings.ModsLocalSettingsHelper.RegisterLocalSettings("LootMagnet", "Loot Magnet"
                  , LocalSettingsHelper.ResetSettings
                  , LocalSettingsHelper.ReadSettings
                  , LocalSettingsHelper.DefaultSettings
                  , LocalSettingsHelper.CurrentSettings
                  , LocalSettingsHelper.SaveSettings
                  );
                QuickSellHelper.InitMechLabInventoryAccess();
                QuickSellHelper.InitCustomShopInfrustructure();
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e);
            }
        }

        public static void Init(string modDirectory, string settingsJSON)
        {
            Mod.ModDir = modDirectory;

            Exception settingsE = null;
            try
            {
                Mod.GlobalConfig = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
                Mod.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            }
            catch (Exception e)
            {
                settingsE = e;
                Mod.Config = new ModConfig();
                Mod.Config.InitDefaultReputation();
            }

            Mod.Log = new DeferringLogger(modDirectory, LogName, Mod.Config.Debug, false);

            Mod.Log.Info?.Write($"{Assembly.GetCallingAssembly().FullName}");
            Mod.Log.Debug?.Write($"ModDir is:{modDirectory}");
            Mod.Log.Debug?.Write($"mod.json settings are:({settingsJSON})");
            Mod.Config.LogConfig();

            if (settingsE != null)
            {
                Mod.Log.Info?.Write($"ERROR reading settings file! Error was: {settingsE}");
            }
            else
            {
                Mod.Log.Info?.Write($"INFO: No errors reading settings file.");
            }

            // Initialize custom components
            Mod.Log.Info?.Write($"INFO: Registering custom components!");
            CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
        }




    }
}

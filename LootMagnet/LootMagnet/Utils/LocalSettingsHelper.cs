using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LootMagnet.Utils
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class GameplaySafe : System.Attribute
    {
        public GameplaySafe() { }
    }
    internal static class LocalSettingsHelper
    {
        public static string SerializeLocal(this ModConfig config)
        {
            Mod.Log.Info?.Write("ModConfig.SerializeLocal");
            JObject json = JObject.FromObject(config);
            PropertyInfo[] props = config.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo prop in props)
            {
                bool skip = true;
                object[] attrs = prop.GetCustomAttributes(true);
                foreach (object attr in attrs) { if ((attr as GameplaySafe) != null) { skip = false; break; } };
                if (skip)
                {
                    if (json[prop.Name] != null)
                    {
                        json.Remove(prop.Name);
                    }
                }
            }
            return json.ToString(Formatting.Indented);
        }
        public static void ApplyLocal(this ModConfig configSettings, ModConfig local)
        {
            PropertyInfo[] props = configSettings.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Mod.Log.Info?.Write("ModConfig.ApplyLocal");
            foreach (PropertyInfo prop in props)
            {
                bool skip = true;
                object[] attrs = prop.GetCustomAttributes(true);
                foreach (object attr in attrs) { if ((attr as GameplaySafe) != null) { skip = false; break; } };
                if (skip) { continue; }
                Mod.Log.Info?.Write(" updating:" + prop.Name);
                prop.SetValue(configSettings, prop.GetValue(local));
            }
        }

        public static string ResetSettings()
        {
            return Mod.GlobalConfig.SerializeLocal();
        }
        public static ModConfig DefaultSettings()
        {
            return Mod.GlobalConfig;
        }
        public static ModConfig CurrentSettings()
        {
            return Mod.Config;
        }
        public static string SaveSettings(object settings)
        {
            ModConfig set = settings as ModConfig;
            if (set == null) { set = Mod.GlobalConfig; }
            JObject jsettigns = JObject.FromObject(set);
            PropertyInfo[] props = set.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Mod.Log.Info?.Write("LocalSettingsHelper.SaveSettings");
            foreach (PropertyInfo prop in props)
            {
                bool skip = true;
                object[] attrs = prop.GetCustomAttributes(true);
                foreach (object attr in attrs) { if ((attr as GameplaySafe) != null) { skip = false; break; } };
                if (skip == false) { continue; }
                jsettigns.Remove(prop.Name);
                Mod.Log.Info?.Write(" removing:" + prop.Name);
            }
            return jsettigns.ToString(Formatting.Indented);
        }
        public static void ReadSettings(string json)
        {
            try
            {
                ModConfig local = JsonConvert.DeserializeObject<ModConfig>(json);
                Mod.Config.ApplyLocal(local);
            }
            catch (Exception e)
            {
                Mod.Log.Info?.Write(e.ToString());
            }
        }
    }
}

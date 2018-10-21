using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Include.VR.Plugin
{
    public static class Config
    {
        [System.Serializable]
        public struct ConfigData
        {
            public string[] avatarrighthand;
            public string[] avatarlefthand;
            public string[] avatarhead;
            public string[] avatarspace;
            public int avatarlayer;
            public string[] avatarhmdcamera;
        }

        public static string[] avatarrighthand { get { return config.avatarrighthand ?? new string[0]; } }
        public static string[] avatarlefthand { get { return config.avatarlefthand ?? new string[0]; } }
        public static string[] avatarhead { get { return config.avatarhead ?? new string[0]; } }
        public static string[] avatarspace { get { return config.avatarspace ?? new string[0]; } }
        public static int avatarlayer { get { return config.avatarlayer; } }
        public static string[] avatarhmdcamera { get { return config.avatarhmdcamera ?? new string[0]; } }

        private static ConfigData config;
        private static string configPath = "viewrplugin.cfg";
        private static System.IO.FileSystemWatcher watcher;
        
        static Config()
        {
            Plugin.Log("Config ctor run");
            RegisterWatcher();
            UpdateConfig();
        }

        static void OnChanged(object source, System.IO.FileSystemEventArgs e)
        {
            UpdateConfig();
        }

        static void UpdateConfig()
        {
            try
            {
                object c = config;
                var lines = System.IO.File.ReadAllLines(configPath);
                foreach (var line in lines)
                {
                    var split = line.Split('=');
                    if (split.Length == 2)
                    {
                        var key = split[0].Trim();
                        var field = c.GetType().GetField(key);
                        if (field != null)
                        {
                            string value = split[1].Trim();
                            switch (field.FieldType.Name)
                            {
                                case "Boolean":
                                    bool valBool = false;
                                    if (bool.TryParse(value, out valBool))
                                    {
                                        field.SetValue(c, valBool);
                                        break;
                                    }
                                    field.SetValue(c, false);
                                    break;
                                case "String":
                                    field.SetValue(c, value);
                                    break;
                                case "String[]":
                                    string[] strings = value.Split(',');
                                    for (int i = 0; i < strings.Length; i++)
                                    {
                                        strings[i] = strings[i].Trim();
                                    }
                                    field.SetValue(c, strings);
                                    break;
                                case "Single":
                                    float valFloat;
                                    if (float.TryParse(value, out valFloat))
                                        field.SetValue(c, float.Parse(value));
                                    break;
                                case "Double":
                                    double valDouble;
                                    if (double.TryParse(value, out valDouble))
                                        field.SetValue(c, valDouble);
                                    break;
                                case "Int32":
                                    field.SetValue(c, int.Parse(value));
                                    break;
                                case "Int32[]":
                                    List<int> ints = new List<int>();
                                    string[] intStrings = value.Split(',');
                                    for (int i = 0; i < intStrings.Length; i++)
                                    {
                                        int valInt;
                                        if (int.TryParse(intStrings[i].Trim(), out valInt))
                                            ints.Add(valInt);
                                    }
                                    field.SetValue(c, ints.ToArray<int>());
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                config = (ConfigData)c;
            }
            catch (Exception e)
            {
                Plugin.Log("Failed to read config file, falling back to default values");
                Plugin.Log(e.ToString());
            }
        }

        static void RegisterWatcher()
        {
            if (watcher == null)
            {
                var fi = new System.IO.FileInfo(configPath);
                watcher = new System.IO.FileSystemWatcher(fi.DirectoryName, fi.Name);
                watcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
                watcher.Changed += new System.IO.FileSystemEventHandler(OnChanged);
                watcher.EnableRaisingEvents = true;
            }
        }

    }
}

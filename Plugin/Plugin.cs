using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Include.VR.Plugin
{
    class Plugin : IllusionPlugin.IPlugin
    {
        bool instanceLoaded = false;

        public static void Log(string data)
        {
            Console.WriteLine("[ViewR Avatar] " + data);
            File.AppendAllText(@"MultiLog.txt", "[ViewR Avatar] " + data + Environment.NewLine);
        }

        public string Name => "ViewR Avatar";

        public string Version => "Zero pt. One";

        public void OnApplicationQuit()
        {

        }

        public void OnApplicationStart()
        {
        }

        public void OnFixedUpdate() { }

        public void OnLevelWasInitialized(int level) { }

        public void OnLevelWasLoaded(int level)
        {
            if (instanceLoaded) return;
            GameObject go = new GameObject("ViewR Avatar Controller");
            go.AddComponent<ViewerAvatarController>();
            instanceLoaded = true;
        }

        public void OnUpdate()
        { }
    }
}

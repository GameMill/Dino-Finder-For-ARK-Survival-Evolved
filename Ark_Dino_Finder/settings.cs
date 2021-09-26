using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ark_Dino_Finder
{
    class Settings
    {
        public string IpAddress { get; set; } = "192.168.0.1";
        public int Port { get; set; } = 5500;
        public string ArkClusterDataDirectory { get; set; } = "";
        public string ArkServerRoot { get; set; } = @"c:\Servers\";

        private static Settings _instance { get; set; }

        public static Settings Instance
        { 
            get
            {
                if (System.IO.File.Exists("settings.db"))
                    _instance = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText("settings.db"));
                else
                {
                    _instance = new Settings();
                    System.IO.File.WriteAllText("settings.db", Newtonsoft.Json.JsonConvert.SerializeObject(_instance));
                }

                return new Settings();
            }
        }

    }
}

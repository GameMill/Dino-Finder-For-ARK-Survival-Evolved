using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Ark_Dino_Finder
{
    class Program
    {
        static Server Aberration_P;
        static Server Extinction;
        static Server Ragnarok;
        static Server ScorchedEarth_P;
        static Server TheCenter;
        static Server TheIsland;
        static Server Valguero_P;
        static Server Genesis;

        public static HttpListener listener;
        public static string url = "";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "    <pre>{0}</pre>" +
            "    <form method=\"post\" action=\"shutdown\">" +
            "      <input type=\"submit\" value=\"Shutdown\" {1}>" +
            "    </form>" +
            "  </body>" +
            "</html>";


        static void Main(string[] args)
        {
            url = "http://" + Settings.Instance.IpAddress + "/";

            //prepare
            var cd = new ArkSavegameToolkitNet.Domain.ArkClusterData(Settings.Instance.ArkClusterDataDirectory, loadOnlyPropertiesInDomain: true);

            cd.Update(System.Threading.CancellationToken.None);
            Aberration_P = new Server("Aberration_P", cd);
            Extinction = new Server("Extinction", cd);
            Ragnarok = new Server("Ragnarok", cd);
            ScorchedEarth_P = new Server("ScorchedEarth_P", cd);
            TheCenter = new Server("TheCenter", cd);
            TheIsland = new Server("TheIsland", cd);
            Valguero_P = new Server("Valguero_P", cd);
            Genesis = new Server("Genesis", cd);

            Console.WriteLine("Starting Server");
            // Create a Http server and start listening for incoming connections
            Start:
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);



            // Handle requests

            try
            {
                Task listenTask = HandleIncomingConnections();
                listenTask.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                try
                {
                    listener.Close();
                }
                catch (Exception)
                {
                    Console.WriteLine("Restarting Server");
                    goto Start;
                }
            }
            



            // Close the listener

            
        }

        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                byte[] data = new byte[0];

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath.Contains("/shutdown")))
                {
                    Console.WriteLine("Shutdown requested");
                    runServer = false;
                    data = Encoding.UTF8.GetBytes(String.Format(pageData, "Server Shutdown", "disabled"));
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                    continue;
                }
                string disableSubmit = !runServer ? "disabled" : "";


                // Make sure we don't increment the page views counter if `favicon.ico` is requested
                if (req.Url.AbsolutePath != "/favicon.ico")
                    pageViews += 1;

                // Write the response info
                string[] Prams = req.Url.LocalPath.Split(new char[1]{ '/'}, StringSplitOptions.RemoveEmptyEntries);
                if (Prams.Length == 1)
                {
                    var Http = System.IO.File.ReadAllText("index.html");
                    if ( Prams[0].ToLower().Contains("primalitem"))
                    {
                        data = Encoding.UTF8.GetBytes(Http.Replace("{0}", Newtonsoft.Json.JsonConvert.SerializeObject(GetPrimalItem(Prams[0]))));

                    }
                    else if (Prams[0].ToLower() == "beehive")
                    {
                        data = Encoding.UTF8.GetBytes(Http.Replace("{0}", Newtonsoft.Json.JsonConvert.SerializeObject(GetBeeHive())));
                    }
                    else
                        data = Encoding.UTF8.GetBytes(Http.Replace("{0}", Newtonsoft.Json.JsonConvert.SerializeObject(GetDino(Prams[0]))));
                }
                else if(Prams.Length == 2)
                {
                    var Http = System.IO.File.ReadAllText("index.html");
                    if (Prams[1] == "item")
                    {
                        data = Encoding.UTF8.GetBytes(Http.Replace("{0}", Newtonsoft.Json.JsonConvert.SerializeObject(GetPrimalItem(Prams[0]))));
                    }
                    else
                    {
                        data = Encoding.UTF8.GetBytes(
                            Http.Replace("{0}", Newtonsoft.Json.JsonConvert.SerializeObject(GetDino(Prams[0], int.Parse(Prams[1])))).Replace("{1}", Newtonsoft.Json.JsonConvert.SerializeObject(GetDinoNamesList()))
                        );
                    }
                }
                else
                {
                    data = Encoding.UTF8.GetBytes(String.Format(pageData, "Data Needed", disableSubmit));
                }
                resp.ContentType = "text/html";

                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        private static IEnumerable<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase> GetPrimalItem(string v)
        {
            
            var Dino = Aberration_P.GetPrimalItem(v)
                .Concat(Extinction.GetPrimalItem(v))
                .Concat(Ragnarok.GetPrimalItem(v))
                .Concat(ScorchedEarth_P.GetPrimalItem(v))
                .Concat(TheCenter.GetPrimalItem(v))
                .Concat(TheIsland.GetPrimalItem(v))
                .Concat(Valguero_P.GetPrimalItem(v))
                .Concat(Genesis.GetPrimalItem(v));
            return Dino;
        }

        public static IEnumerable<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase> GetBeeHive()
        {
            var Dino = Aberration_P.Beehives()
                .Concat(Extinction.Beehives())
                .Concat(Ragnarok.Beehives())
                .Concat(ScorchedEarth_P.Beehives())
                .Concat(TheCenter.Beehives())
                .Concat(TheIsland.Beehives())
                .Concat(Valguero_P.Beehives())
                .Concat(Genesis.Beehives());
            return Dino;
        }

        private static object GetDinoNamesList()
        {
            return Aberration_P.GetAllDinoTypes();
        }

        public static IEnumerable<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase> GetDino(string Class)
        {
            var Dino = Aberration_P.WildCreatures(Class)
                .Concat(Extinction.WildCreatures(Class))
                .Concat(Ragnarok.WildCreatures(Class))
                .Concat(ScorchedEarth_P.WildCreatures(Class))
                .Concat(TheCenter.WildCreatures(Class))
                .Concat(TheIsland.WildCreatures(Class))
                .Concat(Valguero_P.WildCreatures(Class))
                .Concat(Genesis.WildCreatures(Class));
            return Dino;
        }
        public static IEnumerable<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase> GetDino(string Class,int Min)
        {
            var Dino = Aberration_P.WildCreatures(Class)
                .Concat(Extinction.WildCreatures(Class))
                .Concat(Ragnarok.WildCreatures(Class))
                .Concat(ScorchedEarth_P.WildCreatures(Class))
                .Concat(TheCenter.WildCreatures(Class))
                .Concat(TheIsland.WildCreatures(Class))
                .Concat(Valguero_P.WildCreatures(Class))
                .Concat(Genesis.WildCreatures(Class));

            return Dino.Where(x => x.BaseLevel >= Min == true).ToArray();
        }

        Dictionary<string, Dino> Dinos = new Dictionary<string, Dino>();

        public class Server
        {
            public ArkSavegameToolkitNet.Domain.ArkGameData gd;
            public string FilePath { get; set; }
            public ArkSavegameToolkitNet.Domain.ArkClusterData clusterData { get; set; }

            public Server(string FileName, ArkSavegameToolkitNet.Domain.ArkClusterData clusterData)
            {

                this.clusterData = clusterData;
                FilePath = Settings.Instance.ArkServerRoot + FileName + @"\ShooterGame\Saved\"+FileName+@"Save\" + FileName+".ark";
                gd = new ArkSavegameToolkitNet.Domain.ArkGameData(FilePath, this.clusterData, loadOnlyPropertiesInDomain: true);
                gd.Update(System.Threading.CancellationToken.None, deferApplyNewData: false);
                Console.WriteLine(gd.SaveState.MapName + " | Dino Count: {0} | Item Count: {1}", gd.WildCreatures.Length,gd.Items.Length);

                var fileSystemWatcher = new FileSystemWatcher();
                fileSystemWatcher.Path = Path.GetDirectoryName(FilePath);
                fileSystemWatcher.Filter = Path.GetFileName(FilePath);
                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                fileSystemWatcher.EnableRaisingEvents =true;
            }

            private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
            {

                ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                Console.WriteLine("Updating: "+gd.SaveState.MapName+" - "+gd.SaveState.SaveTime.ToString());
                gd.Update(System.Threading.CancellationToken.None, deferApplyNewData: true);
                Console.WriteLine(gd.SaveState.MapName + " | Dino Count: {0} | Item Count: {1}", gd.WildCreatures.Length, gd.Items.Length);
                ((FileSystemWatcher)sender).EnableRaisingEvents = true;

            }

            public ArkSavegameToolkitNet.Domain.ArkWildCreature[] WildCreatures(string DinoClass)
            {
                DinoClass = DinoClass.ToLower();
                var dinos = gd.WildCreatures.Where(x => x.ClassName.ToLower()?.Equals(DinoClass) == true).ToArray();

               // if (gd.SaveState.MapName == "Genesis")
               // {
               //     FixGenesisMapLoc(ref dinos);
               // }
                
                return dinos;//gd.WildCreatures.Where(x => x.ClassName.ToLower()?.Equals(DinoClass) == true).ToArray();
            }

            public List<Dino> GetAllDinoTypes()
            {
                Dictionary<string, bool> Dinos = new Dictionary<string, bool>();
                foreach (var item in gd.WildCreatures)
                {
                    Dinos[item.ClassName] = item.IsTameable;
                }
                return null;
            }

            public void FixGenesisMapLoc(ref ArkSavegameToolkitNet.Domain.ArkWildCreature[] dinos)
            {
                foreach (var dino in dinos)
                {
                    dino.Location.TopoMapX = 10;
                    dino.Location.TopoMapY = 10;
                }
                
            }

            internal IEnumerable<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase> Beehives()
            {
                var Data = new List<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase>();
                Dictionary<string, int> Loc = new Dictionary<string, int>();

                foreach (var item in gd.Structures)
                {
                    if (item.ClassName == "BeeHive_C")
                    {
                        if (!Loc.ContainsKey(item.Location.TopoMapX.ToString()+"-"+ item.Location.TopoMapY.ToString()))
                        {
                            Loc[item.Location.TopoMapX.ToString() + "-" + item.Location.TopoMapY.ToString()] = 1;
                            Data.Add(item);

                        }
                    }
                }
                return Data;
            }
            //Cheat destroyall beehive_c

                /*
            public IEnumerable<object> GetItems(string Class)
            {
                foreach (var item in gd.Items)
                {
                    if (item.ClassName == "PrimalItemConsumable_Egg_RockDrake_Fertilized_C")
                    {
                        Console.WriteLine(item.ClassName);
                    }
                    else if (item.ClassName == "PrimalItemConsumable_Egg_Wyvern_Fertilized_Fire_C")
                    {
                        Console.WriteLine(item.ClassName);
                    }
                }
                return new List<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase>();
            }*/

            public IEnumerable<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase> GetPrimalItem(string Class)
            {
                Class = Class.ToLower() + "_c";
                var Data = new List<ArkSavegameToolkitNet.Domain.ArkGameDataContainerBase>();

                foreach (var item in gd.Items)
                {
                    if (Class == "wyvern_egg_c")
                    {
                        if (item.ClassName == "PrimalItemConsumable_Egg_Wyvern_Fertilized_Fire_C" || item.ClassName == "PrimalItemConsumable_Egg_Wyvern_Fertilized_Poison_C" || item.ClassName == "PrimalItemConsumable_Egg_Wyvern_Fertilized_Lightning_C" || item.ClassName == "RAG_Item_Egg_Wyvern_Fertilized_Ice_C")
                        {
                            int a = "PrimalItemConsumable_Egg_Wyvern_Fertilized_".Length;
                            Data.Add(item);
                            item.CustomName = item.ClassName.Substring("PrimalItemConsumable_Egg_Wyvern_Fertilized_".Length, (item.ClassName.Length - 2) - a);
                        }
                    }
                    else if (item.ClassName.ToLower() == Class)
                    {
                        Data.Add(item);
                    }
                    else
                    {
                        
                    }
                }
                return Data;
            }
        }


        public class Dino
        {
        }
    }
}

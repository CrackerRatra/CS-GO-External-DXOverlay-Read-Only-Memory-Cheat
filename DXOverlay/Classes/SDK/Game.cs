using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Memory;
using Newtonsoft.Json;

namespace DXOverlay.Classes.SDK
{
    class Game
    {
        public static OffsetsTemplate.Root Offsets = default;
        public static int Client, Engine;

        public static void Initalize()
        {
            var result = Memorys.Initalize("csgo");
            if (result == Memorys.Enums.InitalizeResult.OK)
            {
                Client = Memorys.GetModuleAddress("client.dll");
                Engine = Memorys.GetModuleAddress("engine.dll");

                UpdateOffsets(out Offsets);
            }
            else
                Console.WriteLine(" Game.Initalize() -> line 16 returned " + result.ToString());
        }

        public static bool UpdateOffsets(out OffsetsTemplate.Root offsets)
        {
            WebClient wClient = new WebClient();
            wClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            try
            {
                offsets = JsonConvert.DeserializeObject<OffsetsTemplate.Root>(wClient.DownloadString("https://raw.githubusercontent.com/frk1/hazedumper/master/csgo.json"));
                return true;
            } catch (WebException e)
            {
                HttpWebResponse response = (System.Net.HttpWebResponse)e.Response;
                var statusCode = response.StatusCode;
                Console.WriteLine(" Game.UpdateOffsets() -> line 34 - Exception : \nStatus Code : {0}, \nDescription : {1}, \nMessage : {2}", response.StatusCode, response.StatusDescription, e.Message);
                offsets = null;
                return false;
            }
        }
    }
}

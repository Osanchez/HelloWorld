using System;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace HelloWorldServer
{
    public class EssentialServer : BaseScript
    {
        public EssentialServer()
        {
            EventHandlers["chatMessage"] += new Action<int, string, string>(OnChatMessage);
        }

        private void OnChatMessage(int src, string color, string msg)
        {
            Player plyr = new PlayerList()[src];
            string[] args = msg.Split(' ');

            if (args[0] == "/loadout")
            {
                CancelEvent();
                TriggerClientEvent(plyr, "GiveAllGuns");
            }

            if (args[0] == "/vehicle")
            {
                CancelEvent();

                var model = "";
                if(args.Length > 1)
                {
                    model = args[1];
                }
                TriggerClientEvent(plyr, "SpawnVehicle", model);
            }

            if(args[0] == "/tp")
            {
                CancelEvent();
                TriggerClientEvent(plyr, "Teleport");
            }

        }
    }
}
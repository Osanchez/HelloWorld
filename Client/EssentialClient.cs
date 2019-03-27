using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;


namespace Client
{
    public class EssentialClient : BaseScript
    {
        public EssentialClient()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
            EventHandlers.Add("GiveAllGuns", new Action(GiveAllGuns));
            EventHandlers.Add("SpawnVehicle", new Action<string>(SpawnVehicleAsync));
            EventHandlers.Add("Teleport", new Action(TeleportPlayer));

            Tick += OnTick;
        }

        private void OnClientResourceStart(string resourceName)
        {
            Screen.ShowNotification($"~b~Server Info~s~: You are currently on Trihardest's development server!");

            if (GetCurrentResourceName() != resourceName) return;

            RegisterCommand("info", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Screen.ShowNotification($"~b~Server Info~s~: You are currently on Trihardest's development server!");

            }), false);

        }

        //No Wanted Levels
        //Command: called on server start
        //Description: sets all players on server wanted level to 0 at each tick
        private async Task OnTick()
        {
            await Delay(0);
            if(GetPlayerWantedLevel(PlayerId()) != 0)
            {
                SetPlayerWantedLevel(PlayerId(), 0, false);
                SetPlayerWantedLevelNow(PlayerId(), false);
            }

        }

        //Give All Weapons
        //Command: /loadout 
        //Description: Gives players all weapons not already in their inventory. Also heals player to 100 HP and gives 100 armor.
        private void GiveAllGuns()
        {
            // give the player all weapons
            try
            {
                foreach (WeaponHash weapon in Enum.GetValues(typeof(WeaponHash)))
                {
                    if (!Game.PlayerPed.Weapons.HasWeapon(weapon))
                    {
                        Game.PlayerPed.Weapons.Give(weapon, 250, false, true);
                    }
                }

                //set player health to max
                if (Game.PlayerPed.Health < 100)
                {
                    Game.PlayerPed.Health = 100;
                }

                //set player armour level to max
                if (Game.PlayerPed.Armor < 100)
                {
                    Game.PlayerPed.Armor = 100;
                }

                // tell the player task successful
                Screen.ShowNotification($"~b~[Loadout]~s~: {Game.Player.Name} given all weapons!");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}");

                // tell the player task unsuccessfull
                Screen.ShowNotification($"~b~[Loadout]~s~: Unable to give player all weapons.");
            }
        }

        //Vehicle Spawner
        //Command: /vehicle {model}
        //Description: Spawns vehicle for entered model and places character inside vehicle if model name exists.
        private async void SpawnVehicleAsync(string model)
        {
            if (model.Length == 0)
            {
                Screen.ShowNotification($"~b~[Vehicle Spawner]~s~: Enter a valid vehicle model");
                return;
            }

            // check if the model actually exists
            var hash = (uint)GetHashKey(model);
            if (!IsModelInCdimage(hash) || !IsModelAVehicle(hash))
            {
                Screen.ShowNotification($"~b~[Vehicle Spawner]~s~: Enter a valid vehicle model.");
                return;
            }

            // create the vehicle
            var vehicle = await World.CreateVehicle(model, Game.PlayerPed.Position, Game.PlayerPed.Heading);

            // set the player ped into the vehicle and driver seat
            Game.PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

            // tell the player vehicle has been spawned
            Screen.ShowNotification($"~b~[Vehicle Spawner]~s~: {model} spawned!");

        }

        //Teleport player to set waypoint
        //Command: /tp 
        //Description: teleports player to wayPoint on map. Checks for ground level to prevent player from falling under the map. if ground 
        //level cannot be found will give player a parachute and teleport them to area in sky.
        private void TeleportPlayer()
        {
            var blip = GetFirstBlipInfoId(8);

            //If there is a waypoint set, check for ground level. if found, teleport player 3 units above the ground level. otherwise. Teleport in sky. 
            if (blip != 0)
            {
                bool groundFound = false;

                float[] groundCheckHeight = new float[] {
                        100.0F, 150.0F, 50.0F, 0.0F, 200.0F, 250.0F, 300.0F, 350.0F, 400.0F,
                        450.0F, 500.0F, 550.0F, 600.0F, 650.0F, 700.0F, 750.0F, 800.0F
                };

                float blipX = 0.0F;
                float blipY = 0.0F;
                float blipZ = 0.0F;

                var coord = GetBlipCoords(blip);
                blipX = coord.X;
                blipY = coord.Y;

                for (int i = 0; i < groundCheckHeight.Length; i++)
                {
                    if (GetGroundZFor_3dCoord(blipX, blipY, groundCheckHeight[i], ref blipZ, false))
                    {
                        groundFound = true;
                        blipZ += 3.0F;
                        break;
                    }
                }

                if (!groundFound)
                {
                    blipZ = 400F;
                    GiveDelayedWeaponToPed(Game.PlayerPed.Handle, 0xFBAB5776, 1, false);
                    SetPedCoordsKeepVehicle(Game.PlayerPed.Handle, blipX, blipY, blipZ);
                }
                else
                {
                    SetPedCoordsKeepVehicle(Game.PlayerPed.Handle, blipX, blipY, blipZ);
                }

                Screen.ShowNotification($"~b~[Teleport]~s~: Player teleported to waypoint!");
            }
            else
            {
                Screen.ShowNotification($"~b~[Teleport]~s~: No waypoint is set.");
            }
        }
    }
}

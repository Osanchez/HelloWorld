using System;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace HelloWorldClient
{
    public class Class1 : BaseScript
    {
        public Class1()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;

            //Vehicle Spawner
            RegisterCommand("vehicle", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                // account for the argument not being passed
                var model = "";

                if (args.Count > 0)
                {
                    model = args[0].ToString();
                }
                else
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[VehicleSpawner]", $"failed to spawn a <b>vehicle</b>. make sure you have entered a correct model name" }
                    });
                    return;
                }

                // check if the model actually exists
                // assumes the directive `using static CitizenFX.Core.Native.API;`
                var hash = (uint)GetHashKey(model);
                if (!IsModelInCdimage(hash) || !IsModelAVehicle(hash))
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[VehicleSpawner]", $"failed to spawn model ^*{model}. make sure you have entered a correct model name" }
                    });
                    return;
                }

                // create the vehicle
                var vehicle = await World.CreateVehicle(model, Game.PlayerPed.Position, Game.PlayerPed.Heading);

                // set the player ped into the vehicle and driver seat
                Game.PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

                // tell the player
                TriggerEvent("chat:addMessage", new
                {
                    color = new[] { 255, 0, 0 },
                    args = new[] { "[VehicleSpawner]", $"^*{model} spawned!" }
                });
            }), false);

            //Give All Weapons
            RegisterCommand("loadout", new Action<int, List<object>, string>((source, args, raw) =>
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
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[Loadout]", $"Player given all weapons." }
                    });

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{ex}");

                    // tell the player task unsuccessfull
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[Loadout]", $"Unable to give player all weapons." }
                    });
                }
            }), false);

            //Wanted Level
            RegisterCommand("wanted", new Action<int, List<object>, string>((source, args, raw) =>
            {
                var choice = "";

                //get player wanted choice
                if (args.Count > 0)
                {
                    choice = args[0].ToString();
                }

                //set police ignore based on player choice
                if (choice.Equals("1")) //police ignore player
                {
                    SetPoliceIgnorePlayer(Game.Player.Handle, true);
                    SetDispatchCopsForPlayer(Game.Player.Handle, false);

                    // tell the player task unsuccessfull
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[Police]", "Police are now ignoring player." }
                    });
                }
                else //police do not ignore player
                {
                    SetPoliceIgnorePlayer(Game.Player.Handle, false);
                    SetDispatchCopsForPlayer(Game.Player.Handle, true);

                    // tell the player task unsuccessfull
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[Police]", "Police are now watching player." }
                    });
                }

            }), false);

            //Teleport player to set waypoint
            RegisterCommand("tp", new Action<int, List<object>, string>((source, args, raw) =>
            {

                var blip = GetFirstBlipInfoId(8);

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
                        blipZ = 1000F;
                        GiveDelayedWeaponToPed(Game.PlayerPed.Handle, 0xFBAB5776, 1, false);
                        SetPedCoordsKeepVehicle(Game.PlayerPed.Handle, blipX, blipY, blipZ);
                    }
                    else
                    {
                        SetPedCoordsKeepVehicle(Game.PlayerPed.Handle, blipX, blipY, blipZ);
                    }

                    // Message Event - Player teleported to blip coords
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[Teleport]", $"Player teleported to coordinates ^*[X:{blipX}, Y:{blipY}, Z:{blipZ}]" }
                    });
                }
                else
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[Teleport]", $"No way point is set." }
                    });
                }
            }), false);
        }
    }
}
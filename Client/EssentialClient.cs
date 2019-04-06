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
        private Dictionary<string, string> emotes = new Dictionary<string, string>(); //emote command, emote name
        private Dictionary<string, List<string>> animations = new Dictionary<string, List<string>>(); //animation command, <animation_items, animation dictionary, animation-1, animation-2, ... >
        private bool emotePlaying = false;
        private bool walkEnabled = false;
        private bool holdingObject = false;
        private int object_net = -1;

        public EssentialClient()
        {
            Debug.WriteLine("Client Script Initiated.");
            //events
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
            EventHandlers.Add("GiveAllGuns", new Action(GiveAllGuns));
            EventHandlers.Add("SpawnVehicle", new Action<string>(SpawnVehicleAsync));
            EventHandlers.Add("Teleport", new Action(TeleportPlayer));
            EventHandlers.Add("Emote", new Action<string>(UseEmoteAsync));
            EventHandlers.Add("Animate", new Action<string>(UseAnimationAsync));
            EventHandlers.Add("CancelEmoteNow", new Action(CancelEmoteNow));
            EventHandlers.Add("CancelAnimationNow", new Action(CancelAnimation));

            //configuration
            Initialize();

            //Ticks
            Tick += OnTick;
        }

        private void Initialize()
        {
            //add usable emotes
            emotes["smoke"] = "WORLD_HUMAN_SMOKING";
            emotes["cop"] = "WORLD_HUMAN_COP_IDLES";
            emotes["lean"] = "WORLD_HUMAN_LEANING";
            emotes["sit"] = "WORLD_HUMAN_PICNIC";
            emotes["stupor"] = "WORLD_HUMAN_STUPOR";
            emotes["sunbathe2"] = "WORLD_HUMAN_SUNBATHE_BACK";
            emotes["sunbathe"] = "WORLD_HUMAN_SUNBATHE";
            emotes["medic"] = "CODE_HUMAN_MEDIC_TEND_TO_DEAD";
            emotes["clipboard"] = "WORLD_HUMAN_CLIPBOARD";
            emotes["party"] = "WORLD_HUMAN_PARTYING";
            emotes["kneel"] = "CODE_HUMAN_MEDIC_KNEEL";
            emotes["notepad"] = "CODE_HUMAN_MEDIC_TIME_OF_DEATH";
            emotes["weed"] = "WORLD_HUMAN_SMOKING_POT";
            emotes["impatient"] = "WORLD_HUMAN_STAND_IMPATIENT";
            emotes["fish"] = "WORLD_HUMAN_STAND_FISHING";
            emotes["weld"] = "WORLD_HUMAN_WELDING";
            emotes["photography"] = "WORLD_HUMAN_PAPARAZZI";
            emotes["film"] = "WORLD_HUMAN_MOBILE_FILM_SHOCKING";
            emotes["cheer"] = "WORLD_HUMAN_CHEERING";
            emotes["binoculars"] = "WORLD_HUMAN_BINOCULARS";
            emotes["flex"] = "WORLD_HUMAN_MUSCLE_FLEX";
            emotes["weights"] = "WORLD_HUMAN_MUSCLE_FREE_WEIGHTS";
            emotes["yoga"] = "WORLD_HUMAN_YOGA";
            emotes["prostitute"] = "WORLD_HUMAN_PROSTITUTE_HIGH_CLASS";
            emotes["prostitute2"] = "WORLD_HUMAN_PROSTITUTE_LOW_CLASS";
            emotes["flashlight"] = "WORLD_HUMAN_SECURITY_SHINE_TORCH";

            //add usable animations
            animations["umbrella"] = new List<string>()
            {
                "p_amb_brolly_01", //props
                "amb@code_human_wander_drinking@beer@male@base", //animation dictionary
                null, //start animation
               "static", //base animation
                null //exit animation
            };

            animations["phone"] = new List<string>()
            {
                "prop_amb_phone",
                "amb@world_human_mobile_film_shocking@male@base",
                null,
                "base",
                null
            };


        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;

            Debug.WriteLine("Client Commmands Initiated.");

            //commands
            //Server Info
            //Command: /info
            //Description: displays server information to player
            RegisterCommand("info", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Screen.ShowNotification($"~b~Server Info~s~: You are currently on Trihardest's development server {Game.Player.Name}!");
            }), false);

            //Give player all weapons
            //Command: /loadout
            //Description: players calls command from server which triggers client event to give player all weapons
            RegisterCommand("loadout", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerEvent("GiveAllGuns");
            }), false);

            //Spawn requested vehicle 
            //Command: /vehicle {model}
            //Description: players call command from server which triggers client event to spawn requested model and place player inside
            RegisterCommand("vehicle", new Action<int, List<object>, string>((source, args, raw) =>
            {
                string model = "";

                if (args.Count > 0)
                {
                    model = args[0].ToString();
                }
                TriggerEvent("SpawnVehicle", model);
            }), false);

            //Teleport player
            //Command: /tp
            //Description: players call command from server which triggers client event to spawn player at the waypoint set on their world map
            RegisterCommand("tp", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerEvent("Teleport");
            }), false);

            //Use Emote
            //Command: /emote {emote}
            //Description: activates entered emote if it exists, emote is used in stationary state
            RegisterCommand("emote", new Action<int, List<object>, string>((source, args, raw) =>
            {
                string emote = "";

                if(args.Count > 0)
                {
                    emote = args[0].ToString();
                }

                TriggerEvent("Emote", emote);

            }), false);

            //Use Animation
            //Command: /animation {animation}
            //Description: activates entered animation if it exists, allows player to walk while using animation
            RegisterCommand("animate", new Action<int, List<object>, string>((source, args, raw) =>
            {
                string animation = "";

                if (args.Count > 0)
                {
                    animation = args[0].ToString();
                }

                TriggerEvent("Animate", animation);

            }), false);

            //Cancel current emote
            RegisterCommand("cancelemote", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerEvent("CancelEmoteNow");
            }), false);

            //Cancel current animation
            RegisterCommand("cancelanimation", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerEvent("CancelAnimationNow");
            }), false);
        }

        //No Wanted Levels
        //Command: called on server ticks
        //Description: sets all players on server wanted level to 0 at each tick
        private async Task OnTick()
        {
            await Delay(100);

            //remove wanted level on tick
            if (GetPlayerWantedLevel(PlayerId()) != 0)
            {
                SetPlayerWantedLevel(PlayerId(), 0, false);
                SetPlayerWantedLevelNow(PlayerId(), false);
            }

            //cancel emote animation on keypress if walkEnabled emote is not active
            if(emotePlaying)
            {
                if ((IsControlPressed(0, 32) || IsControlPressed(0, 33) || IsControlPressed(0, 34) || IsControlPressed(0, 35)) && !walkEnabled)
                {
                    CancelEmote();
                }
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

        //Use Player Emote
        //Command: /emote {emote_name} 
        //Description: uses entered emote if it exists, emote is only usable when player is standing still
        private async void UseEmoteAsync(string emote)
        {
            
            //check if the emote exists
            if (!emotes.ContainsKey(emote))
            {
                Screen.ShowNotification($"~b~[Emote]~s~: Enter a valid emote!");
                return;
            }

            //check if player is in vehicle
            if(IsPedInAnyVehicle(Game.PlayerPed.Handle, true))
            {
                Screen.ShowNotification($"~b~[Emote]~s~: get out of vehicle first!");
                return;
            }

            //check if player if holding a weapon, if they are remove it
            if(IsPedArmed(Game.PlayerPed.Handle, 7))
            {
                SetCurrentPedWeapon(GetPlayerPed(-1), 0xA2719263, true);
            }

            //use emote
            TaskStartScenarioInPlace(Game.PlayerPed.Handle, emotes[emote], 0, true);
            emotePlaying = true;
            Screen.ShowNotification($"~b~[Emote]~s~: Playing emote {emote}!");
             
        }

        //Use Player Animation
        //Command: /animate {animation_name} 
        //Description: uses entered animation if it exists, animation is usable for moving player.
        //player must use /cancelanimation command to stop animation
        //currently only supports animations that are all contained within the same dictionary. more complex animations will have to be coded differently
        public async void UseAnimationAsync(string animation_command)
        {
            //check if the animation exists
            if (!animations.ContainsKey(animation_command))
            {
                Screen.ShowNotification($"~b~[Animation]~s~: Enter a valid animation!");
                return;
            }

            //check if player is in vehicle
            if (IsPedInAnyVehicle(Game.PlayerPed.Handle, true))
            {
                Screen.ShowNotification($"~b~[Animation]~s~: get out of vehicle first!");
                return;
            }

            //check if player if holding a weapon, if they are remove it
            if (IsPedArmed(Game.PlayerPed.Handle, 7))
            {
                SetCurrentPedWeapon(GetPlayerPed(-1), 0xA2719263, true);
            }

            //load the animation list for the requested animation, save the dictionary name and animation name
            List<string> animation_list = animations[animation_command];

            //get the animation names
            string object_item = animation_list[0];
            string animation_dictionary = animation_list[1];
            string start_animation = animation_list[2];
            string main_animation = animation_list[3];
            string finish_animation = animation_list[4];

            //load the animation dictionary
            RequestAnimDict(animation_dictionary);

            //wait until the animation dictionary is loaded
            while (!HasAnimDictLoaded(animation_dictionary))
            {
                await Delay(100);
            }

            //notify that dictionary loaded
            Debug.WriteLine("Animation dictionary loaded.");

            //if the animation requires an object, give it to the player
            if (object_item != null)
            {
                var playerCoords = GetOffsetFromEntityInWorldCoords(GetPlayerPed(PlayerId()), 0.0f, 0.0f, -5.0f);
                var objectSpawned = CreateObject(GetHashKey(object_item), playerCoords.X, playerCoords.Y, playerCoords.Z, true, true, true);
                var netID = ObjToNet(objectSpawned);

                //load the object model
                RequestModel((uint)GetHashKey(object_item));

                //wait until the model dictionary is loaded
                while (!HasModelLoaded((uint)GetHashKey(object_item)))
                {
                    await Delay(100);
                }

                //notify that dictionary loaded
                Debug.WriteLine("Model loaded.");

                if (holdingObject)
                {
                    //get the object being held
                    var held_object = NetToObj(object_net);


                    //Cancel animation, play cancel animation if one exists
                    if(finish_animation != null)
                    {
                        TaskPlayAnim(GetPlayerPed(-1), animation_dictionary, finish_animation, 8.0f, 1.0f, -1, 50, 0.0f, false, false, false);
                        await Delay(500);
                    }

                    ClearPedSecondaryTask(GetPlayerPed(-1));
                    DetachEntity(held_object, true, true);
                    DeleteEntity(ref held_object);
                    object_net = -1;
                    holdingObject = false;
                    walkEnabled = false;
                    emotePlaying = false;
                }
                else
                {
                    SetNetworkIdExistsOnAllMachines(netID, true);
                    NetworkSetNetworkIdDynamic(netID, true);
                    SetNetworkIdCanMigrate(netID, false);
                    AttachEntityToEntity(objectSpawned, GetPlayerPed(PlayerId()), GetPedBoneIndex(GetPlayerPed(PlayerId()), 28422), 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, true, true, false, true, 0, true);
                    await Delay(120);

                    holdingObject = true;
                    object_net = netID;

                    //play animation
                    // Ped ped, char* animDictionary, char* animationName, float speed, float speedMultiplier, int duration, int flag, float playbackRate, BOOL lockX, BOOL lockY, BOOL lockZ
                    TaskPlayAnim(GetPlayerPed(-1), animation_dictionary, main_animation, 8.0f, 1.0f, -1, 49, 0.0f, false, false, false); 
                    await Delay(120);

                    walkEnabled = true;
                    emotePlaying = true;
                    Screen.ShowNotification($"~b~[Animation]~s~: Playing animation {animation_command}!");
                }
            }
        }

        //called when players moves while emote is playing
        private void CancelEmote()
        {
            ClearPedTasks(Game.PlayerPed.Handle);
            emotePlaying = false;
        }

        //player calls /cancelemote to immidiatley cancel the current emote
        private void CancelEmoteNow()
        {
            ClearPedTasksImmediately(Game.PlayerPed.Handle);
            emotePlaying = false;
            walkEnabled = false;
        }

        //player calls /cancelanimation to immidiatley cancel the current animation
        private void CancelAnimation()
        {
            if (holdingObject)
            {
                //get the object being held
                var held_object = NetToObj(object_net);

                ClearPedSecondaryTask(GetPlayerPed(-1));
                DetachEntity(held_object, true, true);
                DeleteEntity(ref held_object);
                object_net = -1;
                holdingObject = false;
            }

            ClearPedTasksImmediately(Game.PlayerPed.Handle);
            emotePlaying = false;
            walkEnabled = false;
        }

    }
}
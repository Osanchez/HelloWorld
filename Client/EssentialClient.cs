using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace helloworld
{
    public class EssentialClient : BaseScript
    {
        private int character_id = -1;

        private Dictionary<string, string> emotes = new Dictionary<string, string>(); //emote command, emote name
        private Dictionary<string, Dictionary<string, List<string>>> animations = new Dictionary<string, Dictionary<string, List<string>>>();

        private bool emotePlaying = false;
        private bool animationPlaying = false;
        private bool walkEnabled = false;
        private bool holdingObject = false;

        private int object_net = -1;

        private int money_pocket;
        private int money_bank;

        private bool guiEnabled = false;
        private bool playerListEnabled = false;

        private bool firstSpawn = true;

        public EssentialClient()
        {
            Debug.WriteLine($"Client Script Initiated for player {GetPlayerServerId(-1)}");

            //events
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
            EventHandlers["playerSpawned"] += new Action(SpawnInitialize);
            EventHandlers.Add("GiveAllGuns", new Action(GiveAllGuns));
            EventHandlers.Add("SpawnVehicle", new Action<string>(SpawnVehicle));
            EventHandlers.Add("Teleport", new Action(TeleportPlayer));
            EventHandlers.Add("Emote", new Action<string>(UseEmote));
            EventHandlers.Add("Animate", new Action<string>(UseAnimation));
            EventHandlers.Add("CancelEmoteNow", new Action(CancelEmoteNow));
            EventHandlers.Add("CancelAnimationNow", new Action(CancelAnimation));
            EventHandlers.Add("GetLoadedPlayer", new Action(GetLoadedPlayer));
            EventHandlers.Add("SetPlayer", new Action<int, string>(SetPlayer));
            EventHandlers.Add("BankUI", new Action<bool>(EnableBankingUI));
            EventHandlers.Add("SetPlayerBalance", new Action<int, int>(SetPlayerBalance));
            EventHandlers.Add("BalanceChange", new Action<string, int>(BalanceChange));
            EventHandlers.Add("PlayerListUI", new Action<bool, string>(EnablePlayerListGUI));
			EventHandlers.Add("SetModel", new Action<string>(SetModel));

            //register callbacks
            RegisterNUICallback("escapebalance", EscapeBalanceUI);
            RegisterNUICallback("escapeplayerlist", EscapePlayerListUI);

            //configuration
            Initialize();

            //Ticks
            Tick += OnTick;
        }

        //No Wanted Levels
        //Command: called on server ticks
        //Description: sets all players on server wanted level to 0 at each tick
        #pragma warning disable 1998
        private async Task OnTick()
        {
            //remove wanted level on tick
            if (GetPlayerWantedLevel(PlayerId()) != 0)
            {
                SetPlayerWantedLevel(PlayerId(), 0, false);
                SetPlayerWantedLevelNow(PlayerId(), false);
            }

            //cancel emote animation on keypress if walkEnabled emote is not active
            if (emotePlaying)
            {
                if ((IsControlPressed(0, 32) || IsControlPressed(0, 33) || IsControlPressed(0, 34) || IsControlPressed(0, 35)) && !walkEnabled)
                {
                    CancelEmote();
                }
            }

            //open the player list menu, this will contain information about all players on the server so that users can send money 
            if (Game.IsControlJustPressed(0, Control.FrontendLeft))
            {
                //enable playerList
                Debug.WriteLine("Key Pressed");
                TriggerServerEvent("GetPlayerList");
            }
            else if (Game.IsControlJustReleased(0, Control.FrontendLeft))
            {
                //disbale playerList
                Debug.WriteLine("Key Released");
                EnablePlayerListGUI(false, "");
            }
    
        }

        private void Initialize()
        {
            //keys
            Dictionary<string, int> Keys = new Dictionary<string, int>() {

                    ["ESC"] = 322, ["F1"] = 288, ["F2"] = 289, ["F3"] = 170, ["F5"] = 166, ["F6"] = 167, ["F7"] = 168, ["F8"] = 169, ["F9"] = 56, ["F10"] = 57,
                    ["~"] = 243, ["1"] = 157, ["2"] = 158, ["3"] = 160, ["4"] = 164, ["5"] = 165, ["6"] = 159, ["7"] = 161, ["8"] = 162, ["9"] = 163, ["-"] = 84, ["="] = 83, ["BACKSPACE"] = 177,
                    ["TAB"] = 37, ["Q"] = 44, ["W"] = 32, ["E"] = 38, ["R"] = 45, ["T"] = 245, ["Y"] = 246, ["U"] = 303, ["P"] = 199, ["["] = 39, ["]"] = 40, ["ENTER"] = 18,
                    ["CAPS"] = 137, ["A"] = 34, ["S"] = 8, ["D"] = 9, ["F"] = 23, ["G"] = 47, ["H"] = 74, ["K"] = 311, ["L"] = 182,
                    ["LEFTSHIFT"] = 21, ["Z"] = 20, ["X"] = 73, ["C"] = 26, ["V"] = 0, ["B"] = 29, ["N"] = 249, ["M"] = 244, [","] = 82, ["."] = 81,
                    ["LEFTCTRL"] = 36, ["LEFTALT"] = 19, ["SPACE"] = 22, ["RIGHTCTRL"] = 70,
                    ["HOME"] = 213, ["PAGEUP"] = 10, ["PAGEDOWN"] = 11, ["DELETE"] = 178,
                    ["LEFT"] = 174, ["RIGHT"] = 175, ["TOP"] = 27, ["DOWN"] = 173,
                    ["NENTER"] = 201, ["N4"] = 108, ["N5"] = 60, ["N6"] = 107, ["N+"] = 96, ["N-"] = 97, ["N7"] = 117, ["N8"] = 61, ["N9"] = 118
            };

            //emotes
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
            //umbrella 
            animations["umbrella"] = new Dictionary<string, List<string>>();
            animations["umbrella"].Add("prop", new List<string>()
            {
                "p_amb_brolly_01"
            });
            animations["umbrella"].Add("enter", new List<string>()
            {
                null, //animation dictionary
                null //animation name
            });
            animations["umbrella"].Add("base", new List<string>()
            {
                "amb@code_human_wander_drinking@beer@male@base",
                "static"
            });
            animations["umbrella"].Add("exit", new List<string>()
            {
                null,
                null
            });
            animations["umbrella"].Add("action", new List<string>()
            {
                null,
                null
            });
       

            //middle finger
            animations["bird"] = new Dictionary<string, List<string>>();
            animations["bird"].Add("prop", new List<string>()
            {
                null
            });
            animations["bird"].Add("enter", new List<string>()
            {
                "anim@mp_player_intselfiethe_bird", //animation dictionary
                "enter" //animation name
            });
            animations["bird"].Add("base", new List<string>()
            {
               "anim@mp_player_intselfiethe_bird", //animation dictionary
                "idle_a" //animation name
            });
            animations["bird"].Add("exit", new List<string>()
            {
                "anim@mp_player_intselfiethe_bird",
                "exit"
            });
            animations["bird"].Add("action", new List<string>()
            {
                null,
                null
            });

        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;

            Debug.WriteLine("Client Commmands Initiated.");

            //disable Native Money UI
            //Hide Native Money balance UIs
            RemoveMultiplayerHudCash();
            RemoveMultiplayerBankCash();

            //load the player from server cache
            TriggerServerEvent("GetPlayerFromCache");
            TriggerServerEvent("GetPlayerBalance");

            //Account Balance
            //Command: /balance
            //Description: displays current characters account balance
            RegisterCommand("balance", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerEvent("BankUI", true);
            }), false);

            //comand to save player data
            RegisterCommand("save", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Vector3 coordinates = GetCurrentLocation();
                TriggerServerEvent("SaveProfile", character_id, GetEntityModel(Game.PlayerPed.Handle).ToString(), coordinates);
                Screen.ShowNotification($"~b~[Server]~s~: Character Profile Saved! Model: {GetEntityModel(Game.PlayerPed.Handle).ToString()}");

            }), false);

            //Server Info
            //Command: /info
            //Description: displays server information to player
            RegisterCommand("info", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Screen.ShowNotification($"~b~Server Info~s~: You are currently on Trihardest's development server {GetPlayerServerId(-1)}!");
            }), false);

			//Give player all weapons
			//Command: /loadout
			//Description: players calls command from server which triggers client event to give player all weapons
			RegisterCommand("model", new Action<int, List<object>, string>((source, args, raw) =>
			{
				string model = "";

				if (args.Count > 0)
				{
					model = args[0].ToString();
				}
				TriggerEvent("SetModel", model);

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

                if (args.Count > 0)
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

            //Cancel current animation
            RegisterCommand("pos", new Action<int, List<object>, string>((source, args, raw) =>
            {
                Vector3 current_location = GetCurrentLocation();
                Debug.WriteLine($"X: {current_location.X}, Y: {current_location.Y}, Z: {current_location.Z}");

            }), false);

            //Send money to player 
            RegisterCommand("pay", new Action<int, List<object>, string>((source, args, raw) =>
            {
                int amount = 0;
                string playerID = "";

                if (args.Count > 1)
                {
                    playerID = args[0].ToString();
                    int.TryParse(args[1].ToString(), out amount);
                }

                if(amount > 0)
                {
                    TriggerServerEvent("SendMoney", playerID, amount);
                }
                else
                {
                    Screen.ShowNotification($"~b~[Money]~s~: Enter a valid value");

                }

            }), false);
        }

        private void SpawnInitialize()
        {
            //Enable PVP
            NetworkSetFriendlyFireOption(true);
            SetCanAttackFriendly(PlayerPedId(), true, true);

            //update cache with player slot number
            if (firstSpawn)
            {
                TriggerServerEvent("UpdateCache", Game.Player.Handle.ToString());
                Screen.ShowNotification($"Your Server slot number is {Game.Player.Handle}!");
                firstSpawn = false;
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
                if (Game.PlayerPed.Health < 200)
                {
                    Game.PlayerPed.Health = 200;
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
        private async void SpawnVehicle(string model)
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
                    blipZ = 800F;
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
        private async void UseEmote(string emote)
        {

            //check if the emote exists
            if (!emotes.ContainsKey(emote))
            {
                Screen.ShowNotification($"~b~[Emote]~s~: Enter a valid emote!");
                return;
            }

            //check if player is in vehicle
            if (IsPedInAnyVehicle(Game.PlayerPed.Handle, true))
            {
                Screen.ShowNotification($"~b~[Emote]~s~: get out of vehicle first!");
                return;
            }

            //check if player if holding a weapon, if they are remove it
            if (IsPedArmed(Game.PlayerPed.Handle, 7))
            {
                SetCurrentPedWeapon(GetPlayerPed(-1), 0xA2719263, true);
            }

            //use emote
            TaskStartScenarioInPlace(Game.PlayerPed.Handle, emotes[emote], 0, true);
            await Delay(120);
            emotePlaying = true;
            Screen.ShowNotification($"~b~[Emote]~s~: Playing emote {emote}!");

        }

        //Use Player Animation
        //Command: /animate {animation_name} 
        //Description: uses entered animation if it exists, animation is usable for moving player.
        //player must use /cancelanimation command to stop animation
        //TODO: create condition.wait for animations so that they play completley before thread task continues
        //TODO: implement action for current animation
        public async void UseAnimation(string animation_command)
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
            Dictionary<string, List<string>> animation_list = animations[animation_command];

            //get the animation props and names

            //animation prop
            string object_item = animation_list["prop"][0];

            //start animation
            string animation_dictionary_enter = animation_list["enter"][0];
            string enter_animation = animation_list["enter"][1];

            //base animation
            string animation_dictionary_base = animation_list["base"][0]; ;
            string base_animation = animation_list["base"][1];

            //exit animation
            string animation_dictionary_exit = animation_list["exit"][0];
            string exit_animation = animation_list["exit"][1];

            //action animation
            string animation_dictionary_action = animation_list["action"][0];
            string action_animation = animation_list["action"][1];

            //load the animation dictionaries
            loadAnimationDictionariesAsync(animation_dictionary_enter, animation_dictionary_base, animation_dictionary_exit, animation_dictionary_action);

            //cancel animation if one is currently active
            if (animationPlaying == true)
            {
                //Cancel animation, play cancel animation if one exists
                if (exit_animation != null)
                {
                    TaskPlayAnim(GetPlayerPed(-1), animation_dictionary_exit, exit_animation, 8.0f, 1.0f, -1, 50, 0.0f, false, false, false);
                    await Delay(3000);
                }

                if (holdingObject)
                {
                    //get the object being held
                    var held_object = NetToObj(object_net);

                    DetachEntity(held_object, true, true);
                    DeleteEntity(ref held_object);
                    object_net = -1;
                    holdingObject = false;
                }

                ClearPedSecondaryTask(GetPlayerPed(-1));
                walkEnabled = false;
                animationPlaying = false;

                Screen.ShowNotification($"~b~[Animation]~s~: Canceled last animation, try command again!");

                return;
            }

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


                SetNetworkIdExistsOnAllMachines(netID, true);
                NetworkSetNetworkIdDynamic(netID, true);
                SetNetworkIdCanMigrate(netID, false);
                AttachEntityToEntity(objectSpawned, GetPlayerPed(PlayerId()), GetPedBoneIndex(GetPlayerPed(PlayerId()), 28422), 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, true, true, false, true, 0, true);
                await Delay(120);

                holdingObject = true;
                object_net = netID;
            }

            //play animation
            // Ped ped, char* animDictionary, char* animationName, float speed, float speedMultiplier, int duration, int flag, float playbackRate, BOOL lockX, BOOL lockY, BOOL lockZ
            //start with enter animation and go into base animation
            if (animation_dictionary_enter != null)
            {
                TaskPlayAnim(GetPlayerPed(-1), animation_dictionary_enter, enter_animation, 8.0f, 1.0f, -1, 50, 0.0f, false, false, false);
                await Delay(500);
            }

            TaskPlayAnim(GetPlayerPed(-1), animation_dictionary_base, base_animation, 8.0f, 1.0f, -1, 49, 0.0f, false, false, false);

            walkEnabled = true;
            emotePlaying = true;
            animationPlaying = true;
            Screen.ShowNotification($"~b~[Animation]~s~: Playing animation {animation_command}!");

        }

        //load the given animation dictionaries if they are not null
        private async void loadAnimationDictionariesAsync(string start_dictionary, string base_dictionary, string exit_dictionary, string action_dictionary)
        {
            List<string> all_dictionaries = new List<string>()
            {
                start_dictionary,
                base_dictionary,
                exit_dictionary,
                action_dictionary
            };

            foreach (string dictionary in all_dictionaries)
            {
                if (dictionary != null)
                {
                    if (!HasAnimDictLoaded(dictionary))
                    {
                        RequestAnimDict(dictionary);

                    }

                    while (!HasAnimDictLoaded(dictionary))
                    {
                        await Delay(100);
                    }

                    //notify that dictionary loaded
                    Debug.WriteLine($"Animation dictionary {dictionary} loaded.");

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
            animationPlaying = false;
            walkEnabled = false;
        }

        //gets the current player location: for development
        public Vector3 GetCurrentLocation()
        {
            Vector3 playerCoords = GetEntityCoords(GetPlayerPed(-1), true);
            Debug.WriteLine($"X: {playerCoords.X}, Y: {playerCoords.Y}, Z: {playerCoords.Z}");
            return playerCoords;
        }

        //send currently loaded playerID to server to save
        private void GetLoadedPlayer()
        {
            TriggerServerEvent("SaveProfile", character_id);
        }

		private async void SetModel(string character_model)
		{
			if (character_model != "")
			{
				var modelhash = (uint)GetHashKey(character_model);
				RequestModel(modelhash);

				while (!HasModelLoaded(modelhash))
				{
					await Delay(750);
				}
				SetPlayerModel(Game.Player.Handle, modelhash);
			}
		}

        private async void SetPlayer(int characterID, string character_model)
        {

            Debug.WriteLine("Loading player profile.");

            if (character_model != "")
            {
                var modelhash = (uint)GetHashKey(character_model);
                RequestModel(modelhash);

                while (!HasModelLoaded(modelhash))
                {
                    await Delay(750);
                }

                SetPlayerModel(Game.Player.Handle, modelhash);
            }

            this.character_id = characterID;

            Screen.ShowNotification($"~b~[Server]~s~: Player data loaded successfully!");
        }

        //Displays balances when there is a change in balance
        public async void BalanceChange(string action, int amount)
        {
            dynamic json = new ExpandoObject();
            json.type = "balancechange";
            json.action = action;

            TriggerServerEvent("GetPlayerBalance");
            await Delay(500); //give the client time to update players balance
            json.money_pocket = money_pocket;
            json.money_bank = money_bank;
            
            if(action == "add")
            {
                json.addAmount = amount.ToString();
            }
            else if(action == "subtract")
            {
                json.subtractAmount = amount.ToString();
            }

            Debug.WriteLine(JsonConvert.SerializeObject(json));
            SendNuiMessage(JsonConvert.SerializeObject(json));

        }

        //updates stored client information with db character balance
        //actual balance is never stored locally to prevent hackers, this is just a display
        public void SetPlayerBalance(int pocket, int bank)
        {
            money_pocket = pocket;
            money_bank = bank;
        }

        //NUI
        //Player Balance GUI
        public async void EnableBankingUI(bool enable)
        {
            guiEnabled = enable;

            dynamic json = new ExpandoObject();
            json.type = "enableui";
            json.enable = enable;

            if (enable)
            {
                TriggerServerEvent("GetPlayerBalance");
                await Delay(500); //give the client time to update players balance
                json.money_pocket = money_pocket;
                json.money_bank = money_bank;
            }

            Debug.WriteLine(JsonConvert.SerializeObject(json));
            SendNuiMessage(JsonConvert.SerializeObject(json));
        }

        //Player List GUI
        //todo get playerlist information and send it to the gui
        public void EnablePlayerListGUI(bool enable, string playerList)
        { 
            playerListEnabled = enable;

            //create the json object
            dynamic json = new ExpandoObject();
            json.type = "playerlistui";
            json.enable = enable;

            if (enable == true)
            {
                json.playerList = playerList;
                SendNuiMessage(JsonConvert.SerializeObject(json));
                Debug.WriteLine($"NUI Message: {JsonConvert.SerializeObject(json)}");
                Debug.WriteLine($"PlayerList GUI: Enabled");
            }
            else
            {
                SendNuiMessage(JsonConvert.SerializeObject(json));
                Debug.WriteLine($"NUI Message: {JsonConvert.SerializeObject(json)}");
                Debug.WriteLine($"PlayerList GUI: Disabled");
            }
        }

        public void RegisterNUICallback(string msg, Func<dynamic, CallbackDelegate> callback)
        {
            Debug.WriteLine($"Registering NUI EventHandler for {msg}");
            RegisterNuiCallbackType(msg);

            EventHandlers[$"__cfx_nui:{msg}"] += new Action<dynamic>(body =>
            {
                Console.WriteLine("We have an event" + body);
                callback.Invoke(body);
            });
        }

        private static CallbackDelegate EscapeBalanceUI(dynamic arg)
        {
            Debug.WriteLine($"ESCAPING Balance UI, data recieved in post Username:{arg.username} Password:{arg.password}");
            TriggerEvent("BankUI", false);
            return null;
        }

        private static CallbackDelegate EscapePlayerListUI(dynamic arg)
        {
            Debug.WriteLine("ESCAPING Playerlist UI");
            TriggerEvent("PlayerListUI", false);
            return null;
        }
}

    public static class DictionaryExtensions
    {
        public static T GetVal<T>(this IDictionary<string, object> dict, string key, T defaultVal)
        {
            if (dict.ContainsKey(key))
                if (dict[key] is T)
                    return (T)dict[key];
            return defaultVal;
        }
    }
}
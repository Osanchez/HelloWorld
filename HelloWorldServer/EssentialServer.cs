using System;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MySql.Data.MySqlClient;

namespace HelloWorldServer
{
    public class EssentialServer : BaseScript
    {
        public EssentialServer()
        {
            EventHandlers["SaveProfile"] += new Action<string, string>(SaveProfile);
            EventHandlers["LoadProfile"] += new Action<string, string>(LoadProfile);
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnPlayerDropped);

            //Give player all weapons
            //Command: /loadout
            //Description: players calls command from server which triggers client event to give player all weapons
            RegisterCommand("loadout", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerClientEvent(Players[source], "GiveAllGuns");
            }), false);

            //Spawn requested vehicle 
            //Command: /vehicle {model}
            //Description: players call command from server which triggers client event to spawn requested model and place player inside
            RegisterCommand("vehicle", new Action<int, List<object>, string>((source, args, raw) =>
            {
                var model = "";

                if (args.Count > 0)
                {
                    model = args[0].ToString();
                }
                TriggerClientEvent(Players[source], "SpawnVehicle", model);

            }), false);

            //Teleport player
            //Command: /tp
            //Description: players call command from server which triggers client event to spawn player at the waypoint set on their world map
            RegisterCommand("tp", new Action<int, List<object>, string>((source, args, raw) =>
            {
                TriggerClientEvent(Players[source], "Teleport");
            }), false);
        }

        //Event: Player connecting to server
        //Command: triggered upon player connection
        //Description: checks if player is banned from server, if not trigger load profile event, otherwise else kick. 
        private async void OnPlayerConnecting([FromSource]Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {       
            deferrals.defer();
            string licenseIdentifier = player.Identifiers["license"];

            Debug.WriteLine($"A player with the name {playerName} (Identifier: [{licenseIdentifier}]) is connecting to the server.");

            deferrals.update($"Hello {playerName}, your license [{licenseIdentifier}] is being checked");
            //check ban list
            deferrals.done();

            //TODO: call load profile
            TriggerEvent("LoadProfile", licenseIdentifier, player.Name);
        }

        //Event: Player disconnected from server
        //Command: triggered upon player disconnect
        //Description: Triggers save profile event upon player disconnection. 
        private async void OnPlayerDropped([FromSource]Player player, string reason)
        {
            Debug.WriteLine($"Player {player.Name} dropped (Reason: {reason}).");

            TriggerEvent("SaveProfile", player.Identifiers["license"], player.Name);
        }

        //Save User Profile
        //Command: triggered upon player disconnection
        //Description: Saves the user profile
        private async void SaveProfile(string identifier, string playerName)
        {
            string connString = "server=localhost;user=root;database=fivem";
            var conn = new MySqlConnection(connString);

            try
            {
                Debug.WriteLine("Connecting to MySQL...");

                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                //update player in db
                //todo: add player location in the future
                cmd.CommandText = $"UPDATE profile SET id = '{identifier}', name ='{playerName}' WHERE id = '{identifier}'";
                await cmd.ExecuteNonQueryAsync();
                cmd.Dispose();

                Debug.WriteLine($"Player data saved successfully for {playerName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                conn.Close();           
            }
        }

        //Load User Profile
        //Command: triggered upon player connection
        //Description: loads the user profile if it exists, otherwise creates a new profile
        private async void LoadProfile(string playerIdentifier, string playerName)
        {

            string connString = "server=localhost;user=root;database=fivem";
            var conn = new MySqlConnection(connString);

            try
            {
                Debug.WriteLine("Connecting to MySQL...");

                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                //check if player exists in db already
                cmd.CommandText = $"SELECT COUNT(*) FROM profile WHERE id='{playerIdentifier}'";
                int resultCount = Convert.ToInt32(cmd.ExecuteScalar().ToString());
                Debug.WriteLine($"Profile Count: {resultCount}");

                if (resultCount > 0)
                {
                    //load player in database
                    //do nothing for now 
                }
                else
                {
                    //insert player in db
                    cmd.CommandText = $"INSERT INTO profile (id, name) VALUES ('{playerIdentifier}', '{playerName}')";
                    await cmd.ExecuteNonQueryAsync();
                }

                cmd.Dispose();
                Debug.WriteLine($"Player data loaded successfully for {playerName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                conn.Close();
            }

        }
    }
}
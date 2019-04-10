using System;
using System.Collections.Generic;
using CitizenFX.Core;
using MySql.Data.MySqlClient;

namespace Server
{
    public class EssentialServer : BaseScript
    {
        Dictionary<string, List<string>> cache = new Dictionary<string, List<string>>();

        public EssentialServer()
        {
            Debug.WriteLine("Server Script Initiated.");
            EventHandlers["SaveProfile"] += new Action<Player, int, string, Vector3>(SaveProfile);
            EventHandlers["LoadProfile"] += new Action<Player>(LoadProfile);
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnPlayerDropped);
            EventHandlers["GetPlayerFromCache"] += new Action<Player>(GetPlayerCache);
            EventHandlers["GetPlayerBalance"] += new Action<Player>(GetPlayerBalance);
        }

        private void GetPlayerCache([FromSource]Player player)
        {
            List<string> playerCache = cache[player.Identifiers["steam"]];
            Debug.WriteLine($"{playerCache[0]}, {playerCache[1]}, {playerCache[2]}, {playerCache[3]}, {playerCache[4]}, {playerCache[5]}, {playerCache[6]}");

            int characterID = int.Parse(playerCache[0]);
            string characterModel = playerCache[1];

            Debug.WriteLine("Player data recived, sending cached data to player");

            TriggerClientEvent(player, "SetPlayer", characterID, characterModel);
                
        }

        private void GetPlayerBalance([FromSource]Player player)
        {
            string playerIdentifier = player.Identifiers["steam"];

            if (cache.ContainsKey(player.Identifiers["steam"]))
            {
                //pocket, bank
                TriggerClientEvent(player, "SetPlayerBalance", cache[playerIdentifier][2], cache[playerIdentifier][3]);
            }
        }

        //Event: Player connecting to server
        //Command: triggered upon player connection
        //Description: checks if player is banned from server, if not trigger load profile event, otherwise else kick. 
        private void OnPlayerConnecting([FromSource]Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {       
            deferrals.defer();

            Debug.WriteLine($"A player with the name {playerName} (SteamID: [{player.Identifiers["steam"]}]) is connecting to the server.");

            deferrals.update($"Hello {player.Handle}, your license [{player.Identifiers["steam"]}] is being checked");

            //TODO: check ban list
            deferrals.done();

            LoadProfile(player);
        }

        //Event: Player disconnected from server
        //Command: triggered upon player disconnect
        //Description: Triggers save profile event upon player disconnection. 
        private void OnPlayerDropped([FromSource]Player player, string reason)
        {
            Debug.WriteLine($"Player {player.Name} dropped (Reason: {reason}).");
            TriggerClientEvent(player, "GetLoadedPlayer");
        }

        //Save User Profile
        //Command: triggered upon player disconnection
        //Description: Saves the user profile
        private async void SaveProfile([FromSource]Player player, int character_id, string character_model, Vector3 coordinates)
        {
            
            string connString = "server=localhost;user=root;database=fivem";
            var conn = new MySqlConnection(connString);

            try
            {
                Debug.WriteLine("Connecting to MySQL...");

                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                //update player in character db

                cmd.CommandText = $"UPDATE characters SET player_position_x = '{coordinates.X}',  player_position_y = '{coordinates.Y}', player_position_z = '{coordinates.Z}' WHERE character_id = '{character_id}'";
                await cmd.ExecuteNonQueryAsync();

                //update character_model in character db
                cmd.CommandText = $"UPDATE character_model SET character_model = '{character_model}' WHERE character_id = '{character_id}'";
                await cmd.ExecuteNonQueryAsync();

                await Delay(0);

                cmd.Dispose();

                Debug.WriteLine($"Player data saved successfully for {player.Name}");
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
        private async void LoadProfile(Player player)
        {

            string playerIdentifier = player.Identifiers["steam"]; //get the player

            string connString = "server=localhost;user=root;database=fivem";
            var conn = new MySqlConnection(connString);

            try
            {
                Debug.WriteLine("Connecting to MySQL...");

                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                //check if player exists in db already
                cmd.CommandText = $"SELECT COUNT(*) FROM characters WHERE steam_id='{playerIdentifier}'";
                int resultCount = (int)(long) await cmd.ExecuteScalarAsync(); 
                Debug.WriteLine($"{resultCount} characters associated with steam id {playerIdentifier}");

                if (resultCount > 0) //if player exists
                {
                    //load all characters in database for player to choose
                    Debug.WriteLine("Player exists in db, loading character profiles");
                    cmd.CommandText = $"SELECT * FROM characters WHERE steam_id='{playerIdentifier}' LIMIT 1"; //this will eventually return all the characters and let the player choose one
                    var reader = await cmd.ExecuteReaderAsync();

                    //mp_f_freemode_01 = Female multiplayer model
                    //mp_m_freemode_01 = Male multiplayer model

                    string characterID = "-1";
                    string character_model = "u_m_y_mani";
                    string money_pocket = "500";
                    string money_bank = "2500";
                    string coords_x = "316.178";
                    string coords_y = "-233.856";
                    string coords_z = "53.9687";

                    while (reader.Read())
                    {
                        Debug.WriteLine($"{reader[0]}, {reader[1]}, {reader[2]}, {reader[3]}, {reader[4]}, {reader[5]}, {reader[6]}");

                        characterID = reader[0].ToString();
                        money_pocket = reader[2].ToString();
                        money_bank = reader[3].ToString();
                        coords_x = reader[4].ToString();
                        coords_y = reader[5].ToString();
                        coords_z = reader[6].ToString();           
                    }

                    reader.Close();


                    cmd.CommandText = $"SELECT * FROM character_model WHERE character_id='{characterID}' LIMIT 1"; //this will eventually return all the characters and let the player choose one
                    reader = await cmd.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        character_model = reader[1].ToString();
                    }

                    reader.Close();


                    //add loaded player data to cache

                    List<string> playerCache = new List<string>()
                    {
                        characterID,
                        character_model,
                        money_pocket,
                        money_bank,
                        coords_x,
                        coords_y,
                        coords_z
                    };

                    if(cache.ContainsKey(playerIdentifier))
                    {
                        cache[playerIdentifier] = playerCache;
                    }
                    else
                    {
                        cache.Add(playerIdentifier, playerCache);

                    }

                    await Delay(0);

                    Debug.WriteLine("Player data cached");


                }
                else //add player to the db and create their first character
                {
                    Debug.WriteLine("Player does not exist in db, creating profile");

                    //insert into player table
                    cmd.CommandText = $"INSERT INTO player (steam_id) VALUES ('{playerIdentifier}')";
                    await cmd.ExecuteNonQueryAsync();

                    Debug.WriteLine("player inserted into player table");


                    //create a new character for the player
                    cmd.CommandText = $"INSERT INTO characters (steam_id) VALUES ('{playerIdentifier}');" + "SELECT CAST(LAST_INSERT_ID() AS int)";
                    int character_id = (int)(long) await cmd.ExecuteScalarAsync();

                    Debug.WriteLine($"new character created for player in characters table with ID: {character_id}");

                    //mp_f_freemode_01 = Female multiplayer model
                    //mp_m_freemode_01 = Male multiplayer model

                    string characterID = character_id.ToString();
                    string character_model = "u_m_y_mani";
                    string money_pocket = "500";
                    string money_bank = "2500";
                    string coords_x = "316.178";
                    string coords_y = "-233.856";
                    string coords_z = "53.9687";

                    //save the default character model to the db
                    cmd.CommandText = $"INSERT INTO character_model (character_id, character_model) VALUES ('{character_id}', '{character_model}')";
                    await cmd.ExecuteNonQueryAsync();

                    Debug.WriteLine($"character model data saved");

                    await Delay(0);

                    List<string> playerCache = new List<string>()
                    {
                        characterID,
                        character_model,
                        money_pocket,
                        money_bank,
                        coords_x,
                        coords_y,
                        coords_z
                    };

                    if (cache.ContainsKey(playerIdentifier))
                    {
                        cache[playerIdentifier] = playerCache;
                    }
                    else
                    {
                        cache.Add(playerIdentifier, playerCache);

                    }

                    await Delay(0);

                    Debug.WriteLine("New player data cached");

                }

                cmd.Dispose();
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
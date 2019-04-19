using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;
using MySql.Data.MySqlClient;

namespace helloworld
{
    public class EssentialServer : BaseScript
    {
        Dictionary<string, List<string>> cache = new Dictionary<string, List<string>>();
        Dictionary<string, string> CharacterIdToPlayerIdentifier = new Dictionary<string, string>();

        public EssentialServer()
        {
            Debug.WriteLine("Server Script Initiated.");
            EventHandlers["SaveProfile"] += new Action<Player, int, string, Vector3>(SaveProfile);
            EventHandlers["LoadProfile"] += new Action<Player>(LoadProfile);
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnPlayerDropped);
            EventHandlers["GetPlayerFromCache"] += new Action<Player>(GetPlayerCache);
            EventHandlers["UpdateCache"] += new Action<Player, string>(UpdateCache);
            EventHandlers["GetPlayerBalance"] += new Action<Player>(GetPlayerBalance);
            EventHandlers["GetPlayerList"] += new Action<Player>(GetPlayerList);
            EventHandlers["SendMoney"] += new Action<Player, string, int>(SendMoney);
            
        }

        private void SendMoney([FromSource]Player player, string characterID, int amount)
        {
            if (!CharacterIdToPlayerIdentifier.ContainsKey(characterID))
            {
                Debug.Write($"{player.Handle} tried sending money to character {characterID} who is not online");
                return;
            }

            //get the current players cache information
            string playerIdentifier = player.Identifiers["License"];
            List<string> playerInfo = cache[playerIdentifier];
            int money_pocket = int.Parse(playerInfo[5]);


            if (money_pocket >= amount)
            {
                //remove amount from player sending money and update the cache
                money_pocket -= amount;
                playerInfo[5] = money_pocket.ToString();
                cache[playerIdentifier] = playerInfo;

                //get the player identifier based on the target players slot number
                string target_player_identifier = CharacterIdToPlayerIdentifier[characterID];
                List<string> target_player_info = cache[target_player_identifier];
                int target_money_pocket = int.Parse(target_player_info[5]);

                //add amount to target player and update the cache
                target_money_pocket += amount;
                target_player_info[5] = target_money_pocket.ToString();
                cache[target_player_identifier] = target_player_info;

                //trigger client evenst for both players to display change in balances

                //Initial Player
                player.TriggerEvent("BalanceChange", "subtract", amount);

                //Target Player
                int serverslot = -1;
                int.TryParse(playerInfo[1], out serverslot);

                Debug.WriteLine($"Target player slot: {serverslot + 1}");
                Player targetPlayer = new PlayerList()[serverslot + 1];
                targetPlayer.TriggerEvent("BalanceChange", "add", amount);

            }

        }

        //gets the scoreboard for the given player
        private void GetPlayerList([FromSource]Player player)
        {
            StringBuilder playerList = new StringBuilder();

            foreach (KeyValuePair<string, List<string>> entry in cache)
            {
                List<string> playerInfo = entry.Value;
				int serverId = int.Parse(playerInfo[3]);
                int slot = int.Parse(playerInfo[1]) + 1;
                string characterID = playerInfo[0];
                Player current_player = new PlayerList()[serverId];

				playerList.Append(string.Join(" ", slot.ToString(), characterID, current_player.Identifiers["steam"], current_player.Ping.ToString()));
                playerList.Append(",");
            }

            //enable the playerList and remove the comma at end of string
            TriggerClientEvent(player, "PlayerListUI", true, playerList.ToString().TrimEnd(','));
            Debug.WriteLine(playerList.ToString().TrimEnd(','));   
        }

        private void UpdateCache([FromSource]Player player, string PlayerServerID)
        {
            //get the previously cached player info
            string playerIdentifier = player.Identifiers["license"];
            List<string> UpdatedPlayerInformation = cache[playerIdentifier];

            //update the info for current player
            UpdatedPlayerInformation[1] = PlayerServerID;

            //update the cache
            cache[playerIdentifier] = UpdatedPlayerInformation;

            //update the 
            CharacterIdToPlayerIdentifier[(int.Parse(PlayerServerID) + 1).ToString()] = playerIdentifier;

        }

        private void GetPlayerCache([FromSource]Player player)
        {
            //[0] characterID 
            //[1] ServerSlot
            //[2] steamID
            //[3] Server ID
            //[4] character_model
            //[5] money_pocket
            //[6] money_bank
            //[7] coords_x
            //[8] coords_y
            //[9] coords_z

            string playerIdentifier = player.Identifiers["license"];

            List<string> playerCache = cache[playerIdentifier];

            Debug.WriteLine($"Player data recived, sending cached data to player {playerIdentifier}");

            TriggerClientEvent(player, "SetPlayer", int.Parse(playerCache[0]), playerCache[4]);              
        }

        private void GetPlayerBalance([FromSource]Player player)
        {
            string playerIdentifier = player.Identifiers["license"];

            if (cache.ContainsKey(playerIdentifier))
            {
                //pocket, bank
                TriggerClientEvent(player, "SetPlayerBalance", cache[playerIdentifier][5], cache[playerIdentifier][6]);
            }
        }

        //Event: Player connecting to server
        //Command: triggered upon player connection
        //Description: checks if player is banned from server, if not trigger load profile event, otherwise else kick. 
        private void OnPlayerConnecting([FromSource]Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            string playerIdentifier = player.Identifiers["license"];

            deferrals.defer();

            Debug.WriteLine($"A player with the name {playerName} (SteamID: [{player.Identifiers["steam"]}]) is connecting to the server.");

            deferrals.update($"Hello {playerName}, your license [{playerIdentifier}] is being checked");

            //TODO: check ban list
            deferrals.done();

            LoadProfile(player);
        }

        //Event: Player disconnected from server
        //Command: triggered upon player disconnect
        //Description: Triggers save profile event upon player disconnection. 
        private void OnPlayerDropped([FromSource]Player player, string reason)
        {
            string playerIdentifier = player.Identifiers["license"];

            Debug.WriteLine($"Player {player.Name} dropped (Reason: {reason}).");
            
            if(cache.ContainsKey(playerIdentifier))
            {
                cache.Remove(playerIdentifier);
            }

        }

        //Save User Profile
        //Command: triggered upon player disconnection
        //Description: Saves the user profile
        private async void SaveProfile([FromSource]Player player, int character_id, string character_model_hash, Vector3 coordinates)
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
                cmd.CommandText = $"UPDATE characters SET player_position_x = '{coordinates.X.ToString()}',  player_position_y = '{coordinates.Y.ToString()}', player_position_z = '{coordinates.Z.ToString()}' WHERE character_id = '{character_id}'";
                await cmd.ExecuteNonQueryAsync();

                //update character_model in character db
                cmd.CommandText = $"UPDATE character_model SET character_model = '{character_model_hash}' WHERE character_id = '{character_id}'";
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
            string playerIdentifier = player.Identifiers["license"]; //get the player
            string steamID = player.Identifiers["steam"];

            string connString = "server=localhost;user=root;database=fivem";
            var conn = new MySqlConnection(connString);

            try
            {
                Debug.WriteLine("Connecting to MySQL...");

                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;

                //check if player exists in db already
                cmd.CommandText = $"SELECT COUNT(*) FROM characters WHERE license='{playerIdentifier}'";
                int resultCount = (int)(long) await cmd.ExecuteScalarAsync(); 

                Debug.WriteLine($"{resultCount} characters associated with license {playerIdentifier}");

                if (resultCount > 0) //if player exists
                {
                    //load all characters in database for player to choose
                    Debug.WriteLine("Player exists in db, loading character profiles");
                    cmd.CommandText = $"SELECT * FROM characters WHERE license='{playerIdentifier}' LIMIT 1"; //this will eventually return all the characters and let the player choose one
                    var reader = await cmd.ExecuteReaderAsync();

                    string characterID = "";
                    string character_model = "";
                    string money_pocket = "";
                    string money_bank = "";
                    string coords_x = "";
                    string coords_y = "";
                    string coords_z = "";

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

                    //get the character model for the selected character
                    cmd.CommandText = $"SELECT * FROM character_model WHERE character_id='{characterID}' LIMIT 1"; //this will eventually return all the characters and let the player choose one
                    reader = await cmd.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        character_model = reader[1].ToString();
                    }

                    reader.Close();

					await Delay(0);

                    //add loaded player data to cache
                    List<string> playerCache = new List<string>()
                    {
                        characterID,
                        "",
                        steamID,
                        player.Handle,
						character_model,
                        money_pocket,
                        money_bank,
                        coords_x,
                        coords_y,
                        coords_z
                    };

                    //update the cache
                    if (cache.ContainsKey(playerIdentifier))
                    {
                        cache[playerIdentifier] = playerCache;
                    }
                    else
                    {
                        cache.Add(playerIdentifier, playerCache);
                    }

                    Debug.WriteLine($"Player data cached at key {playerIdentifier}");

                }
                else //add player to the db and create their first character
                {
                    Debug.WriteLine("Player does not exist in db, creating profile");

                    //insert into player table
                    cmd.CommandText = $"INSERT INTO player (license) VALUES ('{playerIdentifier}')";
                    await cmd.ExecuteNonQueryAsync();

                    Debug.WriteLine("player inserted into player table");


                    //create a new character for the player
                    cmd.CommandText = $"INSERT INTO characters (license) VALUES ('{playerIdentifier}');" + "SELECT CAST(LAST_INSERT_ID() AS int)";
                    int character_id = (int)(long) await cmd.ExecuteScalarAsync();

                    await Delay(0);

                    Debug.WriteLine($"new character created for player in characters table with ID: {character_id}");

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

					await Delay(0);

					List<string> playerCache = new List<string>()
					{
						characterID,
						"",
                        steamID,
                        player.Handle,
                        character_model,
                        money_pocket,
                        money_bank,
                        coords_x,
                        coords_y,
                        coords_z
                    };


                    //update the cache
                    if (cache.ContainsKey(playerIdentifier))
                    {
                        cache[playerIdentifier] = playerCache;
                    }
                    else
                    {
                        cache.Add(playerIdentifier, playerCache);
                    }

                    Debug.WriteLine($"New player data cached at key {playerIdentifier}");

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
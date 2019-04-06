using System;
using CitizenFX.Core;
using MySql.Data.MySqlClient;

namespace Server
{
    public class EssentialServer : BaseScript
    {
        public EssentialServer()
        {
            Debug.WriteLine("Server Script Initiated.");
            EventHandlers["SaveProfile"] += new Action<string, string>(SaveProfile);
            EventHandlers["LoadProfile"] += new Action<string, string>(LoadProfile);
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnPlayerDropped);
        }

        //Event: Player connecting to server
        //Command: triggered upon player connection
        //Description: checks if player is banned from server, if not trigger load profile event, otherwise else kick. 
        private void OnPlayerConnecting([FromSource]Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {       
            deferrals.defer();
            string licenseIdentifier = player.Identifiers["license"];

            Debug.WriteLine($"A player with the name {playerName} (Identifier: [{licenseIdentifier}]) is connecting to the server.");

            deferrals.update($"Hello {playerName}, your license [{licenseIdentifier}] is being checked");

            //TODO: check ban list
            deferrals.done();

            TriggerEvent("LoadProfile", licenseIdentifier, player.Name);
        }

        //Event: Player disconnected from server
        //Command: triggered upon player disconnect
        //Description: Triggers save profile event upon player disconnection. 
        private void OnPlayerDropped([FromSource]Player player, string reason)
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
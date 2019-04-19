using System;
using CitizenFX.Core;
using System.Collections.Generic;
using System.Dynamic;
using static CitizenFX.Core.Native.API;



namespace EssentialServer.player
{
    class CreatePlayer : BaseScript
    {
        private dynamic self = new ExpandoObject();

        public CreatePlayer(Player source, int permission_level, int money, int bank, string identifier, string license)
        {
            self.source = source;
            self.permission_level = permission_level;
            self.money = money;
            self.bank = bank;
            self.identifier = identifier;
            self.license = license;
            self.coords = new Vector3(0.0f, 0.0f, 0.0f);
        }
        
        //------------------------ Getters and Setter ------------------------

        //Get Permssion Level
        public int GetPermissionLevel()
        {
            return self.permission_level;
        }

        //Set Permission Level
        public void SetPermissionLevel(int permission)
        {
            self.permission_level = permission;
        }

        //Get Source
        public Player GetSource()
        {
            return self.source;
        }
        
        //Set Source
        public void SetSource(Player source)
        {
            self.source = source;
        }

        //Get Money
        public int GetMoneyPocket()
        {
            return self.money;
        }

        public int GetMoneyBank()
        {
            return self.bank;
        }

        //Add Money
        public void AddMoneyPocket(int amount)
        {
            self.money += amount;
        }

        public void AddMoneyBank(int amount)
        {
            self.bank += amount;
        }

        //Remove Money
        public void RemoveMoneyPocket(int amount)
        {
            self.money -= amount;

        }

        public void RemoveMoneyBank(int amount)
        {
            self.bank -= amount;
        }

        //Identifier
        public string GetIdentifier()
        {
            return self.Identifier;
        }

        public void SetIdentifier(string identifier)
        {
            self.Identifier = identifier;
        }

        //licence
        public string GetLicense()
        {
            return self.license;
        }

        public void SetLicense(string license)
        {
            self.license = license;
        }

        //coords
        public Vector3 GetCoords()
        {
            return self.coords;
        }

        public void SetCoords(float x, float y , float z)
        {
            Vector3 newCoords = new Vector3(x, y, z);
            self.coords = newCoords;
        }

        //------------------------ Wrappers ------------------------

        //Kick player with specified reason
        public void KickPlayer(string reason)
        {
            DropPlayer(self.source, reason);
        }
    }
}

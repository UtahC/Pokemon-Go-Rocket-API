using AllEnum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PokemonGo.RocketAPI.Enums;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Window
{
    public class UtahSettings : ISettings
    {
        /// <summary>
        ///     Don't touch. User settings are in Console/App.config
        /// </summary>
        public string Name { get; set; } = "";
        public AuthType AuthType { get; set; } = AuthType.Ptc;
        public double DefaultLatitude { get; set; } = 40.779893;
        public double DefaultLongitude { get; set; } = -73.967895;
        public string LevelOutput { get; set; } = "levelup";
        public int LevelTimeInterval { get; set; } = 1;
        public string TransferType { get; set; } = "Duplicate";
        public int TransferCPThreshold { get; set; } = 1000;
        public int TransferIVThreshold { get; set; } = 80;
        public int TravelSpeed { get; set; } = 60;
        public bool EvolveAllGivenPokemons { get; set; } = true;
        public bool CatchPokemon { get; set; } = true;
        public string PtcUsername { get; set; } = "username";
        public string PtcPassword { get; set; } = "password";
        public string Email { get; set; } = "Email";
        public string Password { get; set; } = "Password";
        public bool Recycler { get; set; } = true;
        public int RecycleItemsInterval { get; set; } = 60;
        public string Language { get; set; } = "english";
        public string RazzBerryMode { get; set; } = "probability";
        public double RazzBerrySetting { get; set; } = 0.4;
        public string GoogleRefreshToken { get; set; } = "";
        ICollection<KeyValuePair<ItemId, int>> ISettings.ItemRecycleFilter => new[]
        {
                new KeyValuePair<ItemId, int>(ItemId.ItemPokeBall, 20),
                new KeyValuePair<ItemId, int>(ItemId.ItemGreatBall, 50),
                new KeyValuePair<ItemId, int>(ItemId.ItemUltraBall, 100),
                new KeyValuePair<ItemId, int>(ItemId.ItemMasterBall, 200),
                new KeyValuePair<ItemId, int>(ItemId.ItemRazzBerry, 20),
                new KeyValuePair<ItemId, int>(ItemId.ItemRevive, 20),
                new KeyValuePair<ItemId, int>(ItemId.ItemPotion, 0),
                new KeyValuePair<ItemId, int>(ItemId.ItemSuperPotion, 0),
                new KeyValuePair<ItemId, int>(ItemId.ItemHyperPotion, 50),
                new KeyValuePair<ItemId, int>(ItemId.ItemMaxPotion, 100)
        };
    }
}
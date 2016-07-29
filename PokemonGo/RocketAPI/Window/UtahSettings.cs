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
        private string name = "";
        private AuthType authType = AuthType.Ptc;
        private double defaultLatitude = 40.779893;
        private double defaultLongitude = -73.967895;
        private string levelOutput = "levelup";
        private int levelTimeInterval = 1;
        private string transferType = "Duplicate";
        private int transferCPThreshold = 1000;
        private int transferIVThreshold = 80;
        private int travelSpeed = 60;
        private bool evolveAllGivenPokemons = true;
        private bool catchPokemon = true;
        private string ptcUsername = "username";
        private string ptcPassword = "password";
        private string email = "Email";
        private string password = "Password";
        private bool recycler = true;
        private int recycleItemsInterval = 60;
        private string language = "english";
        private string razzBerryMode = "probability";
        private double razzBerrySetting = 0.4;
        private string googleRefreshToken = "";
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

        public AuthType AuthType
        {
            get
            {
                return authType;
            }

            set
            {
                authType = value;
            }
        }

        public double DefaultLatitude
        {
            get
            {
                return defaultLatitude;
            }

            set
            {
                defaultLatitude = value;
            }
        }

        public double DefaultLongitude
        {
            get
            {
                return defaultLongitude;
            }

            set
            {
                defaultLongitude = value;
            }
        }

        public string LevelOutput
        {
            get
            {
                return levelOutput;
            }

            set
            {
                levelOutput = value;
            }
        }

        public int LevelTimeInterval
        {
            get
            {
                return levelTimeInterval;
            }

            set
            {
                levelTimeInterval = value;
            }
        }

        public string PtcUsername
        {
            get
            {
                return ptcUsername;
            }

            set
            {
                ptcUsername = value;
            }
        }

        public string PtcPassword
        {
            get
            {
                return ptcPassword;
            }

            set
            {
                ptcPassword = value;
            }
        }

        public string Email
        {
            get
            {
                return email;
            }

            set
            {
                email = value;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }

            set
            {
                password = value;
            }
        }

        public string GoogleRefreshToken
        {
            get
            {
                return googleRefreshToken;
            }

            set
            {
                googleRefreshToken = value;
            }
        }

        public string TransferType
        {
            get
            {
                return transferType;
            }

            set
            {
                transferType = value;
            }
        }

        public int TransferCPThreshold
        {
            get
            {
                return transferCPThreshold;
            }

            set
            {
                transferCPThreshold = value;
            }
        }

        public int TransferIVThreshold
        {
            get
            {
                return transferIVThreshold;
            }

            set
            {
                transferIVThreshold = value;
            }
        }

        public int TravelSpeed
        {
            get
            {
                return travelSpeed;
            }

            set
            {
                travelSpeed = value;
            }
        }

        public bool EvolveAllGivenPokemons
        {
            get
            {
                return evolveAllGivenPokemons;
            }

            set
            {
                evolveAllGivenPokemons = value;
            }
        }

        public bool CatchPokemon
        {
            get
            {
                return catchPokemon;
            }

            set
            {
                catchPokemon = value;
            }
        }

        public bool Recycler
        {
            get
            {
                return recycler;
            }

            set
            {
                recycler = value;
            }
        }

        public int RecycleItemsInterval
        {
            get
            {
                return recycleItemsInterval;
            }

            set
            {
                recycleItemsInterval = value;
            }
        }

        public string Language
        {
            get
            {
                return language;
            }

            set
            {
                language = value;
            }
        }

        public string RazzBerryMode
        {
            get
            {
                return razzBerryMode;
            }

            set
            {
                razzBerryMode = value;
            }
        }

        public double RazzBerrySetting
        {
            get
            {
                return razzBerrySetting;
            }

            set
            {
                razzBerrySetting = value;
            }
        }

        public string GoogleRefreshToken1
        {
            get
            {
                return googleRefreshToken;
            }

            set
            {
                googleRefreshToken = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }
    }
}
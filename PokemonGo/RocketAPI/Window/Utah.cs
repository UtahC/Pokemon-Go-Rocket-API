using AllEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Window
{
    public static class Utah
    {
        public static bool IsRarePokemon(PokemonId pokemon)
        {
            switch (pokemon)
            {
                case PokemonId.Dragonite: return true;
                case PokemonId.Kabutops: return true;//巨鉗螳螂
                case PokemonId.Aerodactyl: return true;
                case PokemonId.Snorlax: return true;
                case PokemonId.Articuno: return true;//急凍鳥
                case PokemonId.Zapdos: return true;//閃電鳥
                case PokemonId.Moltres: return true;//火焰鳥
                case PokemonId.Ditto: return true;//變形怪
                case PokemonId.Mewtwo: return true;//超夢
                case PokemonId.Mew: return true;//夢幻

                default: return false;
            }
        }

        public static bool IsHighCPorIV(GeneratedCode.PokemonData dubpokemon)
        {
            if (dubpokemon.Cp >= 1000)
                return true;
            if ((dubpokemon.IndividualAttack + dubpokemon.IndividualDefense + dubpokemon.IndividualStamina) / 45.0 >= 80)
                return true;
            return false;
        }

        public static bool IsEvolvable(PokemonId pokemon)
        {
            switch (pokemon)
            {
                case PokemonId.Eevee: return true; 
                case PokemonId.Caterpie: return true;
                case PokemonId.Slowpoke: return true;
                case PokemonId.Spearow: return true;
                case PokemonId.Zubat: return true;
                case PokemonId.Pidgey: return true;
                case PokemonId.Rattata: return true;
                case PokemonId.Jigglypuff: return true;
                case PokemonId.Weedle: return true;
                case PokemonId.Oddish: return true;
                case PokemonId.Venonat: return true;
                case PokemonId.Magikarp: return true;
                case PokemonId.NidoranMale: return true;
                case PokemonId.NidoranFemale: return true;
                case PokemonId.Bellsprout: return true;
                case PokemonId.Poliwag: return true;
                case PokemonId.Charmander: return true;
                case PokemonId.Psyduck: return true;
                case PokemonId.Gastly: return true;
                case PokemonId.Paras: return true;
                case PokemonId.Squirtle: return true;
                case PokemonId.Bulbasaur: return true;
                default: return false;
            }
        }
        
        public static Coordinate GetLocationByPokemon(PokemonId pokemon)
        {
            switch(pokemon)
            {
                case PokemonId.Pikachu: return new Coordinate(41.905739, -87.7008263);
                case PokemonId.Charmander: return new Coordinate(37.7916376, -122.388935);
                case PokemonId.Squirtle: return new Coordinate(37.80663069443297, -122.42985963821411);
                case PokemonId.Bulbasaur: return new Coordinate(37.80794, -122.474);
                case PokemonId.Dratini: return new Coordinate(37.808971, -122.409851);//+Gyarados
                case PokemonId.Dragonite: return new Coordinate(40.649606, -74.005702);
                case PokemonId.Lapras: return new Coordinate(41.859453, -87.624238);
                case PokemonId.Snorlax: return new Coordinate(38.7640864464526, -121.331980729508);

                default: return null;
            }
            
        }

        public static Coordinate GetLocationByLocation(Location loc)
        {
            switch (loc)
            {
                case Location.SantaMonicaPier: return new Coordinate(34.0092419, -118.49760370000001);
                case Location.OperaHouse: return new Coordinate(-33.859048, 151.213183);
                case Location.LillydaleLake: return new Coordinate(-37.7651298, 145.3509665);

                default: return null;
            }
        }
    }
}

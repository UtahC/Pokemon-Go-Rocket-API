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
        public static bool isEvolvable(PokemonId pokemon)
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
    }
}

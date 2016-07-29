#region
using AllEnum;
using Newtonsoft.Json.Linq;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
#endregion

namespace PokemonGo.RocketAPI.Window
{
    public class Botting
    {
        private MainForm _mainForm;
        public Dictionary<string, ISettings> settings;
        public Dictionary<string, StringBuilder> ConsoleText = new Dictionary<string, StringBuilder>();
        public Dictionary<string, int> Currentlevel = new Dictionary<string, int>();
        public Dictionary<string, int> TotalPokemon = new Dictionary<string, int>();
        public Dictionary<string, bool> ForceUnbanning = new Dictionary<string, bool>();
        public Dictionary<string, bool> FarmingStops = new Dictionary<string, bool>();
        public Dictionary<string, bool> FarmingPokemons = new Dictionary<string, bool>();
        public Dictionary<string, int> StardustStarted = new Dictionary<string, int>();
        public Dictionary<string, long> ExpStarted = new Dictionary<string, long>();
        public Dictionary<string, int> LevelStarted = new Dictionary<string, int>();
        public Dictionary<string, LocationManager> locationManagers = new Dictionary<string, LocationManager>();
        public DateTime TimeStarted = DateTime.Now;
        public DateTime InitSessionDateTime = DateTime.Now;

        public Botting(MainForm mainForm)
        {
            string json = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}settings.json");
            var outerJObject = JObject.Parse(json);
            var jSettings = outerJObject["Settings"];

            _mainForm = mainForm;
            ConsoleText.Add("All", new StringBuilder(50000, 100000));
            settings = new Dictionary<string, ISettings>();
            
            foreach (var jSetting in jSettings)
            {
                var setting = jSetting.ToObject(new UtahSettings().GetType()) as UtahSettings;

                settings.Add(setting.Name, setting);
                Currentlevel.Add(setting.Name, -1);
                TotalPokemon.Add(setting.Name, 0);
                ForceUnbanning.Add(setting.Name, false);
                FarmingStops.Add(setting.Name, false);
                FarmingPokemons.Add(setting.Name, false);
                StardustStarted.Add(setting.Name, 0);
                LevelStarted.Add(setting.Name, 0);
                ExpStarted.Add(setting.Name, 0);
                locationManagers.Add(setting.Name, null);
                ConsoleText.Add(setting.Name, new StringBuilder(25000, 50000));

                Task.Run(() =>
                {
                    try
                    {
                        Execute(setting);
                    }
                    catch (PtcOfflineException)
                    {
                        _mainForm.ColoredConsoleWrite(Color.Red, "PTC Servers are probably down OR your credentials are wrong. Try google");
                    }
                    catch (Exception ex)
                    {
                        _mainForm.ColoredConsoleWrite(Color.Red, $"Unhandled exception: {ex}");
                    }
                });
            }
        }

        private async void Execute(ISettings setting)
        {
            Client client = new Client(setting);
            locationManagers[client.Name] = new LocationManager(client, setting.TravelSpeed);
            _mainForm.AddOrRemoveItemToComboBox(client, true);
            try
            {
                switch (setting.AuthType)
                {
                    case AuthType.Ptc:
                        _mainForm.ColoredConsoleWrite(Color.Green, "Login Type: Pokemon Trainers Club", client.Name);
                        await client.DoPtcLogin(setting.PtcUsername, setting.PtcPassword);
                        break;
                    case AuthType.Google:
                        _mainForm.ColoredConsoleWrite(Color.Green, "Login Type: Google", client.Name);
                        _mainForm.ColoredConsoleWrite(Color.Green, "Authenticating...\n", client.Name);
                        _mainForm.ColoredConsoleWrite(Color.Green, "Logging in to Google account.", client.Name);

                        await client.DoGoogleLogin(setting.Email, setting.Password);
                        break;
                }

                await client.SetServer();
                var profile = await client.GetProfile();
                var settings = await client.GetSettings();
                var mapObjects = await client.GetMapObjects();
                var inventory = await client.GetInventory();
                var pokemons =
                    inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                        .Where(p => p != null && p?.PokemonId > 0);

                if (StardustStarted[client.Name] <= 0)
                {
                    var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
                    foreach (var v in stats)
                    {
                        if (v != null)
                        {
                            var XpDiff = GameData.GetXpRequired(v.Level);
                            StardustStarted[client.Name] = profile.Profile.Currency.ToArray()[1].Amount;
                            LevelStarted[client.Name] = v.Level;
                            ExpStarted[client.Name] = v.Experience - v.PrevLevelXp - XpDiff;
                        }
                    }
                }

                // Write the players ingame details
                _mainForm.ColoredConsoleWrite(Color.Yellow, "----------------------------", client.Name);
                if (setting.AuthType == AuthType.Ptc)
                {
                    _mainForm.ColoredConsoleWrite(Color.Cyan, "Account: " + setting.PtcUsername, client.Name);
                    _mainForm.ColoredConsoleWrite(Color.Cyan, "Password: " + setting.PtcPassword + "\n", client.Name);
                }
                else
                {
                    _mainForm.ColoredConsoleWrite(Color.Cyan, "Email: " + setting.Email, client.Name);
                    _mainForm.ColoredConsoleWrite(Color.Cyan, "Password: " + setting.Password + "\n", client.Name);
                }
                _mainForm.ColoredConsoleWrite(Color.DarkGray, "Name: " + profile.Profile.Username, client.Name);
                _mainForm.ColoredConsoleWrite(Color.DarkGray, "Team: " + profile.Profile.Team, client.Name);
                if (profile.Profile.Currency.ToArray()[0].Amount > 0) // If player has any pokecoins it will show how many they have.
                    _mainForm.ColoredConsoleWrite(Color.DarkGray, "Pokecoins: " + profile.Profile.Currency.ToArray()[0].Amount, client.Name);
                _mainForm.ColoredConsoleWrite(Color.DarkGray, "Stardust: " + StardustStarted[client.Name] + "\n", client.Name);
                _mainForm.ColoredConsoleWrite(Color.DarkGray, "Latitude: " + setting.DefaultLatitude, client.Name);
                _mainForm.ColoredConsoleWrite(Color.DarkGray, "Longitude: " + setting.DefaultLongitude, client.Name);

                _mainForm.ColoredConsoleWrite(Color.Yellow, "----------------------------", client.Name);

                // I believe a switch is more efficient and easier to read.
                switch (setting.TransferType)
                {
                    case "Leave Strongest":
                        await TransferAllButStrongestUnwantedPokemon(client);
                        break;
                    case "All":
                        await TransferAllGivenPokemons(client, pokemons);
                        break;
                    case "Duplicate":
                        await TransferDuplicatePokemon(client);
                        break;
                    case "IV Duplicate":
                        await TransferDuplicateIVPokemon(client);
                        break;
                    case "CP":
                        await TransferAllWeakPokemon(client, setting.TransferCPThreshold);
                        break;
                    case "IV":
                        await TransferAllGivenPokemons(client, pokemons, setting.TransferIVThreshold);
                        break;
                    default:
                        _mainForm.ColoredConsoleWrite(Color.DarkGray, "Transfering pokemon disabled", client.Name);
                        break;
                }


                if (setting.EvolveAllGivenPokemons)
                    await EvolveAllGivenPokemons(client, pokemons);
                if (setting.Recycler)
                    client.RecycleItems(client);

                await Task.Delay(5000);
                PrintLevel(client);
                await ExecuteFarmingPokestopsAndPokemons(client);

                while (ForceUnbanning[client.Name])
                    await Task.Delay(25);

                // await ForceUnban(client);
                _mainForm.ColoredConsoleWrite(Color.Red, $"No nearby useful locations found. Please wait 10 seconds.", client.Name);
                await Task.Delay(10000);
                //CheckVersion();
                _mainForm.AddOrRemoveItemToComboBox(client, false);
                Execute(setting);
            }
            catch (TaskCanceledException) { _mainForm.ColoredConsoleWrite(Color.Red, "Task Canceled Exception - Restarting", client.Name); _mainForm.AddOrRemoveItemToComboBox(client, false); await Task.Delay(30000); Execute(setting); }
            catch (UriFormatException) { _mainForm.ColoredConsoleWrite(Color.Red, "System URI Format Exception - Restarting", client.Name); _mainForm.AddOrRemoveItemToComboBox(client, false); await Task.Delay(30000); Execute(setting); }
            catch (ArgumentOutOfRangeException) { _mainForm.ColoredConsoleWrite(Color.Red, "ArgumentOutOfRangeException - Restarting", client.Name); _mainForm.AddOrRemoveItemToComboBox(client, false); await Task.Delay(30000); Execute(setting); }
            catch (ArgumentNullException) { _mainForm.ColoredConsoleWrite(Color.Red, "Argument Null Refference - Restarting", client.Name); _mainForm.AddOrRemoveItemToComboBox(client, false); await Task.Delay(30000); Execute(setting); }
            catch (NullReferenceException) { _mainForm.ColoredConsoleWrite(Color.Red, "Null Refference - Restarting", client.Name); _mainForm.AddOrRemoveItemToComboBox(client, false); await Task.Delay(30000); Execute(setting); }
            catch (Exception ex) { _mainForm.ColoredConsoleWrite(Color.Red, ex.ToString(), client.Name); _mainForm.AddOrRemoveItemToComboBox(client, false); await Task.Delay(30000); Execute(setting); }
        }

        private async Task ExecuteFarmingPokestopsAndPokemons(Client client, IEnumerable<FortData> pokeStops = null)
        {
            var mapObjects = await client.GetMapObjects();
            if (pokeStops == null)
            {
                pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());
            }
            HashSet<FortData> pokeStopSet = new HashSet<FortData>(pokeStops);
            IEnumerable<FortData> nextPokeStopList = null;
            if (!ForceUnbanning[client.Name])
                _mainForm.ColoredConsoleWrite(Color.Cyan, $"Visiting {pokeStops.Count()} PokeStops", client.Name);
            int gotNothingFromStop = 0;
            foreach (var pokeStop in pokeStops)
            {
                if (ForceUnbanning[client.Name])
                    break;

                FarmingStops[client.Name] = true;

                double pokeStopDistance = locationManagers[client.Name].getDistance(pokeStop.Latitude, pokeStop.Longitude);
                await locationManagers[client.Name].update(pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                if (fortInfo.Name != string.Empty )
                {
                    StringWriter PokeStopOutput = new StringWriter();
                    PokeStopOutput.Write($"");
                    if (fortInfo.Name != string.Empty)
                        PokeStopOutput.Write("PokeStop: " + fortInfo.Name);
                    if (fortSearch.ExperienceAwarded != 0)
                        PokeStopOutput.Write($", XP: {fortSearch.ExperienceAwarded}");
                    if (fortSearch.GemsAwarded != 0)
                        PokeStopOutput.Write($", Gems: {fortSearch.GemsAwarded}");
                    if (fortSearch.PokemonDataEgg != null)
                        PokeStopOutput.Write($", Eggs: {fortSearch.PokemonDataEgg}");
                    if (GetFriendlyItemsString(fortSearch.ItemsAwarded) != string.Empty)
                        PokeStopOutput.Write($", Items: {GetFriendlyItemsString(fortSearch.ItemsAwarded)} ");
                    _mainForm.ColoredConsoleWrite(Color.Cyan, PokeStopOutput.ToString(), client.Name);

                    if (fortSearch.ExperienceAwarded <= 0 && GetFriendlyItemsString(fortSearch.ItemsAwarded) == string.Empty)
                        gotNothingFromStop++;
                    else
                        gotNothingFromStop = 0;
                    if (gotNothingFromStop > 5)
                        await ForceUnban(client);

                    var pokeStopMapObjects = await client.GetMapObjects();

                    /* Gets all pokeStops near this pokeStop which are not in the set of pokeStops being currently
                     * traversed and which are ready to be farmed again.  */
                    var pokeStopsNearPokeStop = pokeStopMapObjects.MapCells.SelectMany(i => i.Forts).Where(i =>
                        i.Type == FortType.Checkpoint
                        && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()
                        && !pokeStopSet.Contains(i)
                        );

                    /* We choose the longest list of farmable PokeStops to traverse next, though we could use a different
                     * criterion, such as the number of PokeStops with lures in the list.*/
                    if (pokeStopsNearPokeStop.Count() > (nextPokeStopList == null ? 0 : nextPokeStopList.Count()))
                    {
                        nextPokeStopList = pokeStopsNearPokeStop;
                    }

                    if (settings[client.Name].CatchPokemon)
                        await ExecuteCatchAllNearbyPokemons(client);
                    
                }
            }
            FarmingStops[client.Name] = false;
            if (nextPokeStopList != null)
            {
                client.RecycleItems(client);
                await ExecuteFarmingPokestopsAndPokemons(client, nextPokeStopList);
            }
        }

        private async Task ExecuteCatchAllNearbyPokemons(Client client)
        {
            var mapObjects = await client.GetMapObjects();

            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons);
            var inventory2 = await client.GetInventory();
            var pokemons2 = inventory2.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var pokemon in pokemons)
            {
                if (ForceUnbanning[client.Name])
                    break;

                FarmingPokemons[client.Name] = true;

                await locationManagers[client.Name].update(pokemon.Latitude, pokemon.Longitude);
                var encounterPokemonResponse = await client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);
                var pokemonCP = encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp;
                var pokemonIV = Math.Round(Perfect(encounterPokemonResponse?.WildPokemon?.PokemonData));
                CatchPokemonResponse caughtPokemonResponse;
                do
                {
                    if (settings[client.Name].RazzBerryMode == "cp")
                        if (pokemonCP > settings[client.Name].RazzBerrySetting)
                            await client.UseRazzBerry(client, pokemon.EncounterId, pokemon.SpawnpointId);
                    if (settings[client.Name].RazzBerryMode == "probability")
                        if (encounterPokemonResponse.CaptureProbability.CaptureProbability_.First() < settings[client.Name].RazzBerrySetting)
                            await client.UseRazzBerry(client, pokemon.EncounterId, pokemon.SpawnpointId);
                    caughtPokemonResponse = await client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude, pokemon.Longitude, MiscEnums.Item.ITEM_POKE_BALL, pokemonCP); ; //note: reverted from settings because this should not be part of settings but part of logic
                } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed || caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape);

                string pokemonName;
                if (settings[client.Name].Language == "german")
                {
                    string name_english = Convert.ToString(pokemon.PokemonId);
                    var request = (HttpWebRequest)WebRequest.Create("http://boosting-service.de/pokemon/index.php?pokeName=" + name_english);
                    var response = (HttpWebResponse)request.GetResponse();
                    pokemonName = new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
                else
                    pokemonName = Convert.ToString(pokemon.PokemonId);

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    _mainForm.ColoredConsoleWrite(Color.Green, $"We caught a {pokemonName} with {pokemonCP} CP and {pokemonIV}% IV", client.Name);
                    TotalPokemon[client.Name] += 1;
                }
                else
                    _mainForm.ColoredConsoleWrite(Color.Red, $"{pokemonName} with {pokemonCP} CP and {pokemonIV}% IV got away..", client.Name);


                // I believe a switch is more efficient and easier to read.
                switch (settings[client.Name].TransferType)
                {
                    case "Leave Strongest":
                        await TransferAllButStrongestUnwantedPokemon(client);
                        break;
                    case "All":
                        await TransferAllGivenPokemons(client, pokemons2);
                        break;
                    case "Duplicate":
                        await TransferDuplicatePokemon(client);
                        break;
                    case "IV Duplicate":
                        await TransferDuplicateIVPokemon(client);
                        break;
                    case "CP":
                        await TransferAllWeakPokemon(client, settings[client.Name].TransferCPThreshold);
                        break;
                    case "IV":
                        await TransferAllGivenPokemons(client, pokemons2, settings[client.Name].TransferIVThreshold);
                        break;
                    default:
                        _mainForm.ColoredConsoleWrite(Color.DarkGray, "Transfering pokemon disabled", client.Name);
                        break;
                }

                FarmingPokemons[client.Name] = false;
                await Task.Delay(3000);
            }
        }

        private async Task EvolveAllGivenPokemons(Client client, IEnumerable<PokemonData> pokemonToEvolve)
        {
            foreach (var pokemon in pokemonToEvolve)
            {
                if (!Utah.isEvolvable(pokemon.PokemonId))
                    break;

                var countOfEvolvedUnits = 0;
                var xpCount = 0;

                EvolvePokemonOut evolvePokemonOutProto;
                do
                {
                    evolvePokemonOutProto = await client.EvolvePokemon(pokemon.Id);
                    //todo: someone check whether this still works

                    if (evolvePokemonOutProto.Result == 1)
                    {
                        _mainForm.ColoredConsoleWrite(Color.Cyan, $"Evolved {pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded}xp", client.Name);

                        countOfEvolvedUnits++;
                        xpCount += evolvePokemonOutProto.ExpAwarded;
                    }
                    else
                    {
                        var result = evolvePokemonOutProto.Result;
                    }
                } while (evolvePokemonOutProto.Result == 1);
                if (countOfEvolvedUnits > 0)
                    _mainForm.ColoredConsoleWrite(Color.Cyan, $"Evolved {countOfEvolvedUnits} pieces of {pokemon.PokemonId} for {xpCount}xp", client.Name);

                await Task.Delay(3000);
            }
        }

        private async Task ForceUnban(Client client)
        {
            if (!ForceUnbanning[client.Name])
            {
                _mainForm.ColoredConsoleWrite(Color.LightGreen, "Waiting for last farming action to be complete...", client.Name);
                ForceUnbanning[client.Name] = true;

                while (FarmingStops[client.Name] || FarmingPokemons[client.Name])
                {
                    await Task.Delay(25);
                }

                _mainForm.ColoredConsoleWrite(Color.LightGreen, "Starting force unban...", client.Name);

                var mapObjects = await client.GetMapObjects();
                var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());

                await Task.Delay(10000);
                bool done = false;

                foreach (var pokeStop in pokeStops)
                {

                    double pokeStopDistance = locationManagers[client.Name].getDistance(pokeStop.Latitude, pokeStop.Longitude);
                    await locationManagers[client.Name].update(pokeStop.Latitude, pokeStop.Longitude);
                    var fortInfo = await client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                    if (fortInfo.Name != string.Empty)
                    {
                        _mainForm.ColoredConsoleWrite(Color.LightGreen, "Chosen PokeStop " + fortInfo.Name + " for force unban", client.Name);
                        for (int i = 1; i <= 50; i++)
                        {
                            var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                            if (fortSearch.ExperienceAwarded == 0)
                            {
                                _mainForm.ColoredConsoleWrite(Color.LightGreen, "Attempt: " + i, client.Name);
                            }
                            else
                            {
                                _mainForm.ColoredConsoleWrite(Color.LightGreen, "Fuck yes, you are now unbanned! Total attempts: " + i, client.Name);
                                done = true;
                                break;
                            }
                        }
                    }

                    if (!done)
                        _mainForm.ColoredConsoleWrite(Color.LightGreen, "Force unban failed, please try again.", client.Name);

                    ForceUnbanning[client.Name] = false;
                    break;
                }
            }
            else
            {
                _mainForm.ColoredConsoleWrite(Color.Red, "A force unban attempt is in action... Please wait.", client.Name);
            }
        }

        public async Task UseLuckyEgg(Client client)
        {
            //if (client != null)
            //{
            //    try
            //    {
            //        IEnumerable<Item> myItems = await client.GetItems(client);
            //        IEnumerable<Item> LuckyEggs = myItems.Where(i => (ItemId)i.Item_ == ItemId.ItemLuckyEgg);
            //        Item LuckyEgg = LuckyEggs.FirstOrDefault();
            //        if (LuckyEgg != null)
            //        {
            //            var useItemXpBoostRequest = await client.UseItemXpBoost(ItemId.ItemLuckyEgg);
            //            _mainForm.ColoredConsoleWrite(Color.Green, $"Using a Lucky Egg, we have {LuckyEgg.Count} left.");
            //            _mainForm.ColoredConsoleWrite(Color.Yellow, $"Lucky Egg Valid until: {DateTime.Now.AddMinutes(30).ToString()}");

            //            var stripItem = sender as ToolStripMenuItem;
            //            stripItem.Enabled = false;
            //            await Task.Delay(30000);
            //            stripItem.Enabled = true;
            //        }
            //        else
            //        {
            //            _mainForm.ColoredConsoleWrite(Color.Red, $"You don't have any Lucky Egg to use.");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _mainForm.ColoredConsoleWrite(Color.Red, $"Unhandled exception in using lucky egg: {ex}");
            //    }
            //}
            //else
            //{
            //    _mainForm.ColoredConsoleWrite(Color.Red, "Please start the bot before trying to use a lucky egg.");
            //}
        }

        public static float Perfect(PokemonData poke)
        {
            return ((float)(poke.IndividualAttack + poke.IndividualDefense + poke.IndividualStamina) / (3.0f * 15.0f)) * 100.0f;
        }

        private async Task TransferAllGivenPokemons(Client client, IEnumerable<PokemonData> unwantedPokemons, float keepPerfectPokemonLimit = 80.0f)
        {
            foreach (var pokemon in unwantedPokemons)
            {
                if (Perfect(pokemon) >= keepPerfectPokemonLimit) continue;
                _mainForm.ColoredConsoleWrite(Color.White, $"Pokemon {pokemon.PokemonId} with {pokemon.Cp} CP has IV percent less than {keepPerfectPokemonLimit}%", client.Name);

                if (pokemon.Favorite == 0)
                {
                    var transferPokemonResponse = await client.TransferPokemon(pokemon.Id);

                    /*
                    ReleasePokemonOutProto.Status {
                        UNSET = 0;
                        SUCCESS = 1;
                        POKEMON_DEPLOYED = 2;
                        FAILED = 3;
                        ERROR_POKEMON_IS_EGG = 4;
                    }*/
                    string pokemonName;
                    if (settings[client.Name].Language == "german")
                    {
                        // Dont really need to print this do we? youll know if its German or not
                        //_mainForm.ColoredConsoleWrite(Color.DarkCyan, "german");
                        string name_english = Convert.ToString(pokemon.PokemonId);
                        var request = (HttpWebRequest)WebRequest.Create("http://boosting-service.de/pokemon/index.php?pokeName=" + name_english);
                        var response = (HttpWebResponse)request.GetResponse();
                        pokemonName = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    }
                    else
                        pokemonName = Convert.ToString(pokemon.PokemonId);
                    if (transferPokemonResponse.Status == 1)
                    {
                        _mainForm.ColoredConsoleWrite(Color.Magenta, $"Transferred {pokemonName} with {pokemon.Cp} CP", client.Name);
                    }
                    else
                    {
                        var status = transferPokemonResponse.Status;

                        _mainForm.ColoredConsoleWrite(Color.Red, $"Somehow failed to transfer {pokemonName} with {pokemon.Cp} CP. " + $"ReleasePokemonOutProto.Status was {status}", client.Name);
                    }

                    await Task.Delay(3000);
                }
            }
        }

        private async Task TransferDuplicatePokemon(Client client)
        {

            //_mainForm.ColoredConsoleWrite(ConsoleColor.White, $"Check for duplicates");
            var inventory = await client.GetInventory();
            var allpokemons =
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                    .Where(p => p != null && p?.PokemonId > 0);

            var dupes = allpokemons.OrderBy(x => x.Cp).Select((x, i) => new { index = i, value = x })
                .GroupBy(x => x.value.PokemonId)
                .Where(x => x.Skip(1).Any());

            for (var i = 0; i < dupes.Count(); i++)
            {
                for (var j = 0; j < dupes.ElementAt(i).Count() - 1; j++)
                {
                    var dubpokemon = dupes.ElementAt(i).ElementAt(j).value;
                    if (dubpokemon.Favorite == 0)
                    {
                        if (Utah.isHighCPorIV(dubpokemon))
                            continue;

                        var transfer = await client.TransferPokemon(dubpokemon.Id);
                        string pokemonName;
                        if (settings[client.Name].Language == "german")
                        {
                            string name_english = Convert.ToString(dubpokemon.PokemonId);
                            var request = (HttpWebRequest)WebRequest.Create("http://boosting-service.de/pokemon/index.php?pokeName=" + name_english);
                            var response = (HttpWebResponse)request.GetResponse();
                            pokemonName = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        }
                        else
                            pokemonName = Convert.ToString(dubpokemon.PokemonId);
                        _mainForm.ColoredConsoleWrite(Color.DarkGreen, $"Transferred {pokemonName} with {dubpokemon.Cp} CP (Highest is {dupes.ElementAt(i).Last().value.Cp})", client.Name);

                    }
                }
            }
        }

        private async Task TransferDuplicateIVPokemon(Client client)
        {

            //_mainForm.ColoredConsoleWrite(ConsoleColor.White, $"Check for duplicates");
            var inventory = await client.GetInventory();
            var allpokemons =
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                    .Where(p => p != null && p?.PokemonId > 0);

            var dupes = allpokemons.OrderBy(x => Perfect(x)).Select((x, i) => new { index = i, value = x })
                .GroupBy(x => x.value.PokemonId)
                .Where(x => x.Skip(1).Any());

            for (var i = 0; i < dupes.Count(); i++)
            {
                for (var j = 0; j < dupes.ElementAt(i).Count() - 1; j++)
                {
                    var dubpokemon = dupes.ElementAt(i).ElementAt(j).value;
                    if (dubpokemon.Favorite == 0)
                    {
                        var transfer = await client.TransferPokemon(dubpokemon.Id);
                        string pokemonName;
                        if (settings[client.Name].Language == "german")
                        {
                            string name_english = Convert.ToString(dubpokemon.PokemonId);
                            var request = (HttpWebRequest)WebRequest.Create("http://boosting-service.de/pokemon/index.php?pokeName=" + name_english);
                            var response = (HttpWebResponse)request.GetResponse();
                            pokemonName = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        }
                        else
                            pokemonName = Convert.ToString(dubpokemon.PokemonId);
                        _mainForm.ColoredConsoleWrite(Color.DarkGreen, $"Transferred {pokemonName} with {Math.Round(Perfect(dubpokemon))}% IV (Highest is {Math.Round(Perfect(dupes.ElementAt(i).Last().value))}% IV)", client.Name);

                    }
                }
            }
        }

        private async Task TransferAllWeakPokemon(Client client, int cpThreshold)
        {
            //_mainForm.ColoredConsoleWrite(ConsoleColor.White, $"Firing up the meat grinder");

            PokemonId[] doNotTransfer = new[] //these will not be transferred even when below the CP threshold
            { // DO NOT EMPTY THIS ARRAY
                //PokemonId.Pidgey,
                //PokemonId.Rattata,
                //PokemonId.Weedle,
                //PokemonId.Zubat,
                //PokemonId.Caterpie,
                //PokemonId.Pidgeotto,
                //PokemonId.NidoranFemale,
                //PokemonId.Paras,
                //PokemonId.Venonat,
                //PokemonId.Psyduck,
                //PokemonId.Poliwag,
                //PokemonId.Slowpoke,
                //PokemonId.Drowzee,
                //PokemonId.Gastly,
                //PokemonId.Goldeen,
                //PokemonId.Staryu,
                PokemonId.Magikarp,
                PokemonId.Eevee//,
                //PokemonId.Dratini
            };

            var inventory = await client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                                .Select(i => i.InventoryItemData?.Pokemon)
                                .Where(p => p != null && p?.PokemonId > 0)
                                .ToArray();

            //foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                List<PokemonData> pokemonToDiscard;
                if (doNotTransfer.Count() != 0)
                    pokemonToDiscard = pokemons.Where(p => !doNotTransfer.Contains(p.PokemonId) && p.Cp < cpThreshold).OrderByDescending(p => p.Cp).ToList();
                else
                    pokemonToDiscard = pokemons.Where(p => p.Cp < cpThreshold).OrderByDescending(p => p.Cp).ToList();


                //var unwantedPokemon = pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                //                                          .ToList();
                _mainForm.ColoredConsoleWrite(Color.Gray, $"Grinding {pokemonToDiscard.Count} pokemon below {cpThreshold} CP.", client.Name);
                await TransferAllGivenPokemons(client, pokemonToDiscard);

            }

            _mainForm.ColoredConsoleWrite(Color.Gray, $"Finished grinding all the meat", client.Name);
        }

        private async Task TransferAllButStrongestUnwantedPokemon(Client client)
        {
            //_mainForm.ColoredConsoleWrite(ConsoleColor.White, $"Firing up the meat grinder");

            var unwantedPokemonTypes = new[]
            {
                PokemonId.Pidgey,
                PokemonId.Rattata,
                PokemonId.Weedle,
                PokemonId.Zubat,
                PokemonId.Caterpie,
                PokemonId.Pidgeotto,
                PokemonId.NidoranFemale,
                PokemonId.Paras,
                PokemonId.Venonat,
                PokemonId.Psyduck,
                PokemonId.Poliwag,
                PokemonId.Slowpoke,
                PokemonId.Drowzee,
                PokemonId.Gastly,
                PokemonId.Goldeen,
                PokemonId.Staryu,
                PokemonId.Magikarp,
                PokemonId.Clefairy,
                PokemonId.Eevee,
                PokemonId.Tentacool,
                PokemonId.Dratini,
                PokemonId.Ekans,
                PokemonId.Jynx,
                PokemonId.Lickitung,
                PokemonId.Spearow,
                PokemonId.NidoranFemale,
                PokemonId.NidoranMale
            };

            var inventory = await client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                var pokemonOfDesiredType = pokemons.Where(p => p.PokemonId == unwantedPokemonType)
                    .OrderByDescending(p => p.Cp)
                    .ToList();

                var unwantedPokemon =
                    pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                        .ToList();

                //_mainForm.ColoredConsoleWrite(ConsoleColor.White, $"Grinding {unwantedPokemon.Count} pokemons of type {unwantedPokemonType}");
                await TransferAllGivenPokemons(client, unwantedPokemon);
            }

            //_mainForm.ColoredConsoleWrite(ConsoleColor.White, $"Finished grinding all the meat");
        }

        public double GetRuntime()
        {
            return ((DateTime.Now - TimeStarted).TotalSeconds) / 3600;
        }

        private string GetFriendlyItemsString(IEnumerable<FortSearchResponse.Types.ItemAward> items)
        {
            var enumerable = items as IList<FortSearchResponse.Types.ItemAward> ?? items.ToList();

            if (!enumerable.Any())
                return string.Empty;

            return enumerable.GroupBy(i => i.ItemId)
                    .Select(kvp => new { ItemName = kvp.Key.ToString().Substring(4), Amount = kvp.Sum(x => x.ItemCount) })
                    .Select(y => $"{y.Amount}x {y.ItemName}")
                    .Aggregate((a, b) => $"{a}, {b}");
        }

        public async Task PrintLevel(Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GameData.GetXpRequired(v.Level);
                    if (settings[client.Name].LevelOutput == "time")
                        _mainForm.ColoredConsoleWrite(Color.Yellow, $"Current Level: " + v.Level + " (" + (v.Experience - XpDiff) + "/" + (v.NextLevelXp - XpDiff) + ")", client.Name);
                    else if (settings[client.Name].LevelOutput == "levelup")
                        if (Currentlevel[client.Name] != v.Level)
                        {
                            Currentlevel[client.Name] = v.Level;
                            _mainForm.ColoredConsoleWrite(Color.Magenta, $"Current Level: " + v.Level + ". XP needed for next Level: " + (v.NextLevelXp - v.Experience), client.Name);
                        }
                }
            if (settings[client.Name].LevelOutput == "levelup")
                await Task.Delay(1000);
            else
                await Task.Delay(settings[client.Name].LevelTimeInterval * 1000);
            PrintLevel(client);
        }

        // Pulled from NecronomiconCoding
        public string _getSessionRuntimeInTimeFormat()
        {
            return (DateTime.Now - InitSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }

        public async Task ConsoleLevelTitle(Client client, CancellationToken token)
        {
            
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            var profile = await client.GetProfile();
            foreach (var v in stats)
                if (v != null)
                {
                    var username = profile.Profile.Username;
                    var level = v.Level;
                    var XpRequired = GameData.GetXpRequired(level);
                    var curExp = v.Experience - v.PrevLevelXp - XpRequired;
                    var neededExp = v.NextLevelXp - v.PrevLevelXp - XpRequired;
                    var XpDiff = GameData.GetXpDiff(level, (int)curExp, LevelStarted[client.Name], (int)ExpStarted[client.Name]);
                    var expPerHr = Math.Round(XpDiff / GetRuntime());
                    var stardust = profile.Profile.Currency.ToArray()[1].Amount;
                    var stardustPerHr = Math.Round((stardust - StardustStarted[client.Name]) / GetRuntime());
                    var pokePerHr = Math.Round(TotalPokemon[client.Name] / GetRuntime());
                    var runtime = _getSessionRuntimeInTimeFormat();
                    _mainForm.SetStatusText(
                        $"{username} | Level: {level} - ({curExp} / {neededExp}) | Runtime {runtime} | Stardust: {stardust} | Stardust / Hour: {stardustPerHr} | XP / Hour: {expPerHr} | Pokemon / Hour: {pokePerHr}");
                }
            await Task.Delay(1000);
            if (!token.IsCancellationRequested)
                ConsoleLevelTitle(client, token);
        }

        
    }
}

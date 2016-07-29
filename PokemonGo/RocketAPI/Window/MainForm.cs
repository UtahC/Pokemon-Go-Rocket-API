using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using AllEnum;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

namespace PokemonGo.RocketAPI.Window
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            toolStripComboBox2.Items.Add("All");
            ConsoleText.Add("All", new StringBuilder(50000, 100000));
            settings = new Dictionary<string, ISettings>();
            string json = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}settings.json");
            var outerJObject = JObject.Parse(json);
            var jSettings = outerJObject["Settings"];
            foreach (var jSetting in jSettings)
            {
                var setting = jSetting.ToObject(new UtahSettings().GetType()) as UtahSettings;
                settings.Add(setting.Name, setting);

                
                Currentlevel.Add(setting.Name, -1);
                TotalPokemon.Add(setting.Name, 0);
                ForceUnbanning.Add(setting.Name, false);
                FarmingStops.Add(setting.Name, false);
                FarmingPokemons.Add(setting.Name, false);
                TotalExperience.Add(setting.Name, 0);
                locationManagers.Add(setting.Name, null);
                ConsoleText.Add(setting.Name, new StringBuilder(5000, 10000));

                Task.Run(() =>
                {
                    try
                    {
                        //ColoredConsoleWrite(ConsoleColor.White, "Coded by Ferox - edited by NecronomiconCoding");
                        CheckVersion();
                        Execute(setting);
                    }
                    catch (PtcOfflineException)
                    {
                        ColoredConsoleWrite(Color.Red, "PTC Servers are probably down OR your credentials are wrong. Try google");
                    }
                    catch (Exception ex)
                    {
                        ColoredConsoleWrite(Color.Red, $"Unhandled exception: {ex}");
                    }
                });
                toolStripComboBox2.Items.Add(setting.Name);
            }
        }

        public static Dictionary<string, ISettings> settings;
        private static Dictionary<string, StringBuilder> ConsoleText= new Dictionary<string, StringBuilder>();
        private static Dictionary<string, int> Currentlevel = new Dictionary<string, int>();
        private static Dictionary<string, int> TotalPokemon = new Dictionary<string, int>();
        private static Dictionary<string, bool> ForceUnbanning = new Dictionary<string, bool>();
        private static Dictionary<string, bool> FarmingStops = new Dictionary<string, bool>();
        private static Dictionary<string, bool> FarmingPokemons = new Dictionary<string, bool>();
        private static Dictionary<string, int> TotalExperience = new Dictionary<string, int>();
        private static Dictionary<string, LocationManager> locationManagers = new Dictionary<string, LocationManager>();
        private static DateTime TimeStarted = DateTime.Now;
        public static DateTime InitSessionDateTime = DateTime.Now;
        

        
        
        public static double GetRuntime()
        {
            return ((DateTime.Now - TimeStarted).TotalSeconds) / 3600;
        }

        public void CheckVersion()
        {
            try
            {
                var match =
                    new Regex(
                        @"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]")
                        .Match(DownloadServerVersion());

                if (!match.Success) return;
                var gitVersion =
                    new Version(
                        string.Format(
                            "{0}.{1}.{2}.{3}",
                            match.Groups[1],
                            match.Groups[2],
                            match.Groups[3],
                            match.Groups[4]));
                // makes sense to display your version and say what the current one is on github
                ColoredConsoleWrite(Color.Green, "Your version is " + Assembly.GetExecutingAssembly().GetName().Version);
                ColoredConsoleWrite(Color.Green, "Github version is " + gitVersion);
                ColoredConsoleWrite(Color.Green, "You can find it at www.GitHub.com/DetectiveSquirrel/Pokemon-Go-Rocket-API");
            }
            catch (Exception)
            {
                ColoredConsoleWrite(Color.Red, "Unable to check for updates now...");
            }
        }

        private static string DownloadServerVersion()
        {
            using (var wC = new WebClient())
                return
                    wC.DownloadString(
                        "https://raw.githubusercontent.com/DetectiveSquirrel/Pokemon-Go-Rocket-API/master/PokemonGo/RocketAPI/Window/Properties/AssemblyInfo.cs");
        }

        public async Task ColoredConsoleWrite(Color color, string text, string username = "All")
        {
            if (InvokeRequired)
            {
                Invoke(new Func<Color, string, string, Task>(ColoredConsoleWrite), color, text, username);
                return;
            }
            string textToAppend = "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + text + "\r\n";
            if (username != "All")
            {
                logTextBox.SelectionColor = color;
                logTextBox.AppendText(textToAppend);

                ConsoleText[username].Append($"{color.ToString()}:{textToAppend}");
            }

            ConsoleText["All"].Append($"{color.ToString()}:[{username}]{textToAppend}");
            if ((username == "All" && ConsoleText[username].Length > 50000) ||
                (username != "All" && ConsoleText[username].Length > 5000))
                await WriteIntoLog(username);
        }

        private async Task WriteIntoLog(string username)
        {
            object syncRoot = new object();
            lock (syncRoot) // Added locking to prevent text file trying to be accessed by two things at the same time
            {
                string text = ConsoleText[username].ToString();
                if (username != "All")
                    File.AppendAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs\\{username}.txt", text);
                else
                    File.AppendAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs\\All.txt", text);
            }
        }

        public void SetStatusText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetStatusText), text);
                return;
            }

            statusLabel.Text = text;
        }

        private async Task EvolvePokemons(Client client)
        {
            var inventory = await client.GetInventory();
            var pokemons =
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                    .Where(p => p != null && p?.PokemonId > 0);

            await EvolveAllGivenPokemons(client, pokemons);
        }

        private async Task EvolveAllGivenPokemons(Client client, IEnumerable<PokemonData> pokemonToEvolve)
        {
            foreach (var pokemon in pokemonToEvolve)
            {
                /*
                enum Holoholo.Rpc.Types.EvolvePokemonOutProto.Result {
	                UNSET = 0;
	                SUCCESS = 1;
	                FAILED_POKEMON_MISSING = 2;
	                FAILED_INSUFFICIENT_RESOURCES = 3;
	                FAILED_POKEMON_CANNOT_EVOLVE = 4;
	                FAILED_POKEMON_IS_DEPLOYED = 5;
                }
                }*/

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
                        ColoredConsoleWrite(Color.Cyan, $"Evolved {pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded}xp", client.Name);

                        countOfEvolvedUnits++;
                        xpCount += evolvePokemonOutProto.ExpAwarded;
                    }
                    else
                    {
                        var result = evolvePokemonOutProto.Result;
                        /*
                        ColoredConsoleWrite(ConsoleColor.White, $"Failed to evolve {pokemon.PokemonId}. " +
                                                 $"EvolvePokemonOutProto.Result was {result}");

                        ColoredConsoleWrite(ConsoleColor.White, $"Due to above error, stopping evolving {pokemon.PokemonId}");
                        */
                    }
                } while (evolvePokemonOutProto.Result == 1);
                if (countOfEvolvedUnits > 0)
                    ColoredConsoleWrite(Color.Cyan, $"Evolved {countOfEvolvedUnits} pieces of {pokemon.PokemonId} for {xpCount}xp", client.Name);

                await Task.Delay(3000);
            }
        }

        private async void Execute(ISettings setting)
        {
            Client client = new Client(setting);
            locationManagers[client.Name] = new LocationManager(client, setting.TravelSpeed);
            try
            {
                switch (setting.AuthType)
                {
                    case AuthType.Ptc:
                        ColoredConsoleWrite(Color.Green, "Login Type: Pokemon Trainers Club", client.Name);
                        await client.DoPtcLogin(setting.PtcUsername, setting.PtcPassword);
                        break;
                    case AuthType.Google:
                        ColoredConsoleWrite(Color.Green, "Login Type: Google", client.Name);
                        ColoredConsoleWrite(Color.Green, "Authenticating...\n", client.Name);
                        ColoredConsoleWrite(Color.Green, "Logging in to Google account.", client.Name);
                        
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

                ConsoleLevelTitle(profile.Profile.Username, client);

                

                // Write the players ingame details
                ColoredConsoleWrite(Color.Yellow, "----------------------------", client.Name);
                        if (setting.AuthType == AuthType.Ptc)
                        {
                            ColoredConsoleWrite(Color.Cyan, "Account: " + setting.PtcUsername, client.Name);
                            ColoredConsoleWrite(Color.Cyan, "Password: " + setting.PtcPassword + "\n", client.Name);
                        }
                        else
                        {
                            ColoredConsoleWrite(Color.Cyan, "Email: " + setting.Email, client.Name);
                            ColoredConsoleWrite(Color.Cyan, "Password: " + setting.Password + "\n", client.Name);
                        }
                ColoredConsoleWrite(Color.DarkGray, "Name: " + profile.Profile.Username, client.Name);
                ColoredConsoleWrite(Color.DarkGray, "Team: " + profile.Profile.Team, client.Name);
                if (profile.Profile.Currency.ToArray()[0].Amount > 0) // If player has any pokecoins it will show how many they have.
                    ColoredConsoleWrite(Color.DarkGray, "Pokecoins: " + profile.Profile.Currency.ToArray()[0].Amount, client.Name);
                ColoredConsoleWrite(Color.DarkGray, "Stardust: " + profile.Profile.Currency.ToArray()[1].Amount + "\n", client.Name);
                ColoredConsoleWrite(Color.DarkGray, "Latitude: " + setting.DefaultLatitude, client.Name);
                ColoredConsoleWrite(Color.DarkGray, "Longitude: " + setting.DefaultLongitude, client.Name);
                try
                {
                    ColoredConsoleWrite(Color.DarkGray, "Country: " + CallAPI("country", setting.DefaultLatitude, setting.DefaultLongitude), client.Name);
                    ColoredConsoleWrite(Color.DarkGray, "Area: " + CallAPI("place", setting.DefaultLatitude, setting.DefaultLongitude), client.Name);
                }
                catch (Exception)
                {
                    ColoredConsoleWrite(Color.DarkGray, "Unable to get Country/Place", client.Name);
                }

                ColoredConsoleWrite(Color.Yellow, "----------------------------", client.Name);

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
                        ColoredConsoleWrite(Color.DarkGray, "Transfering pokemon disabled", client.Name);
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
                ColoredConsoleWrite(Color.Red, $"No nearby useful locations found. Please wait 10 seconds.", client.Name);
                await Task.Delay(10000);
                CheckVersion();
                Execute(setting);
            }
            catch (TaskCanceledException) { ColoredConsoleWrite(Color.Red, "Task Canceled Exception - Restarting", client.Name); Execute(setting); }
            catch (UriFormatException) { ColoredConsoleWrite(Color.Red, "System URI Format Exception - Restarting", client.Name); Execute(setting); }
            catch (ArgumentOutOfRangeException) { ColoredConsoleWrite(Color.Red, "ArgumentOutOfRangeException - Restarting", client.Name); Execute(setting); }
            catch (ArgumentNullException) { ColoredConsoleWrite(Color.Red, "Argument Null Refference - Restarting", client.Name); Execute(setting); }
            catch (NullReferenceException) { ColoredConsoleWrite(Color.Red, "Null Refference - Restarting", client.Name); Execute(setting); }
            catch (Exception ex) { ColoredConsoleWrite(Color.Red, ex.ToString(), client.Name); Execute(setting); }
        }

        private static string CallAPI(string elem, double lat, double lon)
        {
            using (XmlReader reader = XmlReader.Create(@"http://api.geonames.org/findNearby?lat=" + lat + "&lng=" + lon + "&username=demo"))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (elem)
                        {
                            case "country":
                                if (reader.Name == "countryName")
                                {
                                    return reader.ReadString();
                                }
                                break;

                            case "place":
                                if (reader.Name == "toponymName")
                                {
                                    return reader.ReadString();
                                }
                                break;
                            default:
                                return "N/A";
                        }
                    }
                }
            }
            return "Error";
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
                    ColoredConsoleWrite(Color.Green, $"We caught a {pokemonName} with {pokemonCP} CP and {pokemonIV}% IV", client.Name);
                    foreach (int xp in caughtPokemonResponse.Scores.Xp)
                        TotalExperience[client.Name] += xp;
                    TotalPokemon[client.Name] += 1;
                }
                else
                    ColoredConsoleWrite(Color.Red, $"{pokemonName} with {pokemonCP} CP and {pokemonIV}% IV got away..", client.Name);


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
                        ColoredConsoleWrite(Color.DarkGray, "Transfering pokemon disabled", client.Name);
                        break;
                }

                FarmingPokemons[client.Name] = false;
                await Task.Delay(3000);
            }
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
                ColoredConsoleWrite(Color.Cyan, $"Visiting {pokeStops.Count()} PokeStops", client.Name);
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
                if (fortInfo.Name != string.Empty && (fortSearch.ExperienceAwarded > 0 || GetFriendlyItemsString(fortSearch.ItemsAwarded) != string.Empty))
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
                    ColoredConsoleWrite(Color.Cyan, PokeStopOutput.ToString(), client.Name);

                    if (fortSearch.ExperienceAwarded != 0)
                        TotalExperience[client.Name] += (fortSearch.ExperienceAwarded);

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
                    gotNothingFromStop = 0;
                }
                else
                    gotNothingFromStop++;
                if (gotNothingFromStop > 5)
                    await ForceUnban(client);
            }
            FarmingStops[client.Name] = false;
            if (nextPokeStopList != null)
            {
                client.RecycleItems(client);
                await ExecuteFarmingPokestopsAndPokemons(client, nextPokeStopList);
            }
        }

        private async Task ForceUnban(Client client)
        {
            if (!ForceUnbanning[client.Name])
            {
                ColoredConsoleWrite(Color.LightGreen, "Waiting for last farming action to be complete...", client.Name);
                ForceUnbanning[client.Name] = true;

                while (FarmingStops[client.Name] || FarmingPokemons[client.Name])
                {
                    await Task.Delay(25);
                }
                
                ColoredConsoleWrite(Color.LightGreen, "Starting force unban...", client.Name);

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
                        ColoredConsoleWrite(Color.LightGreen, "Chosen PokeStop " + fortInfo.Name + " for force unban", client.Name);
                        for (int i = 1; i <= 50; i++)
                        {
                            var fortSearch = await client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                            if (fortSearch.ExperienceAwarded == 0)
                            {
                                ColoredConsoleWrite(Color.LightGreen, "Attempt: " + i, client.Name);
                            }
                            else
                            {
                                ColoredConsoleWrite(Color.LightGreen, "Fuck yes, you are now unbanned! Total attempts: " + i, client.Name);
                                done = true;
                                break;
                            }
                        }
                    }

                    if (!done)
                        ColoredConsoleWrite(Color.LightGreen, "Force unban failed, please try again.", client.Name);
                    
                    ForceUnbanning[client.Name] = false;
                    break;
                }
            }
            else
            {
                ColoredConsoleWrite(Color.Red, "A force unban attempt is in action... Please wait.", client.Name);
            }


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


        private async Task TransferAllButStrongestUnwantedPokemon(Client client)
        {
            //ColoredConsoleWrite(ConsoleColor.White, $"Firing up the meat grinder");

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

                //ColoredConsoleWrite(ConsoleColor.White, $"Grinding {unwantedPokemon.Count} pokemons of type {unwantedPokemonType}");
                await TransferAllGivenPokemons(client, unwantedPokemon);
            }

            //ColoredConsoleWrite(ConsoleColor.White, $"Finished grinding all the meat");
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
                ColoredConsoleWrite(Color.White, $"Pokemon {pokemon.PokemonId} with {pokemon.Cp} CP has IV percent less than {keepPerfectPokemonLimit}%", client.Name);

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
                        //ColoredConsoleWrite(Color.DarkCyan, "german");
                        string name_english = Convert.ToString(pokemon.PokemonId);
                        var request = (HttpWebRequest)WebRequest.Create("http://boosting-service.de/pokemon/index.php?pokeName=" + name_english);
                        var response = (HttpWebResponse)request.GetResponse();
                        pokemonName = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    }
                    else
                        pokemonName = Convert.ToString(pokemon.PokemonId);
                    if (transferPokemonResponse.Status == 1)
                    {
                        ColoredConsoleWrite(Color.Magenta, $"Transferred {pokemonName} with {pokemon.Cp} CP", client.Name);
                    }
                    else
                    {
                        var status = transferPokemonResponse.Status;

                        ColoredConsoleWrite(Color.Red, $"Somehow failed to transfer {pokemonName} with {pokemon.Cp} CP. " + $"ReleasePokemonOutProto.Status was {status}", client.Name);
                    }

                    await Task.Delay(3000);
                }
            }
        }

        private async Task TransferDuplicatePokemon(Client client)
        {

            //ColoredConsoleWrite(ConsoleColor.White, $"Check for duplicates");
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
                        ColoredConsoleWrite(Color.DarkGreen, $"Transferred {pokemonName} with {dubpokemon.Cp} CP (Highest is {dupes.ElementAt(i).Last().value.Cp})", client.Name);

                    }
                }
            }
        }

        private async Task TransferDuplicateIVPokemon(Client client)
        {

            //ColoredConsoleWrite(ConsoleColor.White, $"Check for duplicates");
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
                        ColoredConsoleWrite(Color.DarkGreen, $"Transferred {pokemonName} with {Math.Round(Perfect(dubpokemon))}% IV (Highest is {Math.Round(Perfect(dupes.ElementAt(i).Last().value))}% IV)", client.Name);

                    }
                }
            }
        }

        private async Task TransferAllWeakPokemon(Client client, int cpThreshold)
        {
            //ColoredConsoleWrite(ConsoleColor.White, $"Firing up the meat grinder");

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
                ColoredConsoleWrite(Color.Gray, $"Grinding {pokemonToDiscard.Count} pokemon below {cpThreshold} CP.", client.Name);
                await TransferAllGivenPokemons(client, pokemonToDiscard);

            }

            ColoredConsoleWrite(Color.Gray, $"Finished grinding all the meat", client.Name);
        }

        public async Task PrintLevel(Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GetXpDiff(client, v.Level);
                    if (settings[client.Name].LevelOutput == "time")
                        ColoredConsoleWrite(Color.Yellow, $"Current Level: " + v.Level + " (" + (v.Experience - XpDiff) + "/" + (v.NextLevelXp - XpDiff) + ")", client.Name);
                    else if (settings[client.Name].LevelOutput == "levelup")
                        if (Currentlevel[client.Name] != v.Level)
                        {
                            Currentlevel[client.Name] = v.Level;
                            ColoredConsoleWrite(Color.Magenta, $"Current Level: " + v.Level + ". XP needed for next Level: " + (v.NextLevelXp - v.Experience), client.Name);
                        }
                }
            if (settings[client.Name].LevelOutput == "levelup")
                await Task.Delay(1000);
            else
                await Task.Delay(settings[client.Name].LevelTimeInterval * 1000);
            PrintLevel(client);
        }

        // Pulled from NecronomiconCoding
        public static string _getSessionRuntimeInTimeFormat()
        {
            return (DateTime.Now - InitSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }

        public async Task ConsoleLevelTitle(string Username, Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            var profile = await client.GetProfile();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = GetXpDiff(client, v.Level);
                    SetStatusText(string.Format(Username + " | Level: {0:0} - ({2:0} / {3:0}) | Runtime {1} | Stardust: {4:0}", v.Level, _getSessionRuntimeInTimeFormat(), (v.Experience - v.PrevLevelXp - XpDiff), (v.NextLevelXp - v.PrevLevelXp - XpDiff), profile.Profile.Currency.ToArray()[1].Amount) + " | XP/Hour: " + Math.Round(TotalExperience[client.Name] / GetRuntime()) + " | Pokemon/Hour: " + Math.Round(TotalPokemon[client.Name] / GetRuntime()));
                }
            await Task.Delay(1000);
            ConsoleLevelTitle(Username, client);
        }

        public static int GetXpDiff(Client client, int Level)
        {
            switch (Level)
            {
                case 1: return 0;
                case 2: return 1000;
                case 3: return 2000;
                case 4: return 3000;
                case 5: return 4000;
                case 6: return 5000;
                case 7: return 6000;
                case 8: return 7000;
                case 9: return 8000;
                case 10: return 9000;
                case 11: return 10000;
                case 12: return 10000;
                case 13: return 10000;
                case 14: return 10000;
                case 15: return 15000;
                case 16: return 20000;
                case 17: return 20000;
                case 18: return 20000;
                case 19: return 25000;
                case 20: return 25000;
                case 21: return 50000;
                case 22: return 75000;
                case 23: return 100000;
                case 24: return 125000;
                case 25: return 150000;
                case 26: return 190000;
                case 27: return 200000;
                case 28: return 250000;
                case 29: return 300000;
                case 30: return 350000;
                case 31: return 500000;
                case 32: return 500000;
                case 33: return 750000;
                case 34: return 1000000;
                case 35: return 1250000;
                case 36: return 1500000;
                case 37: return 2000000;
                case 38: return 2500000;
                case 39: return 1000000;
                case 40: return 1000000;
            }
            return 0;
        }

        private void logTextBox_TextChanged(object sender, EventArgs e)
        {
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
            //SettingsForm settingsForm = new SettingsForm();
            //settingsForm.Show();
        }

        private void showAllToolStripMenuItem3_Click(object sender, EventArgs e)
        {
        }

        private void statsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // todo: add player stats later
        }

        private async void useLuckyEggToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
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
            //            ColoredConsoleWrite(Color.Green, $"Using a Lucky Egg, we have {LuckyEgg.Count} left.");
            //            ColoredConsoleWrite(Color.Yellow, $"Lucky Egg Valid until: {DateTime.Now.AddMinutes(30).ToString()}");

            //            var stripItem = sender as ToolStripMenuItem;
            //            stripItem.Enabled = false;
            //            await Task.Delay(30000);
            //            stripItem.Enabled = true;
            //        }
            //        else
            //        {
            //            ColoredConsoleWrite(Color.Red, $"You don't have any Lucky Egg to use.");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        ColoredConsoleWrite(Color.Red, $"Unhandled exception in using lucky egg: {ex}");
            //    }
            //}
            //else
            //{
            //    ColoredConsoleWrite(Color.Red, "Please start the bot before trying to use a lucky egg.");
            //}
        }

        private void showAllToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
        }

        private void todoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
            //SettingsForm settingsForm = new SettingsForm();
            //settingsForm.Show();

        }

        private void pokemonToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
            //var pForm = new PokeUi();
            //pForm.Show();


        }

        private void toolStripComboBox2_TextChanged(object sender, EventArgs e)
        {
            string username = toolStripComboBox2.SelectedItem.ToString();
            logTextBox.Clear();
            logTextBox.Text = ConsoleText[username].ToString();
        }
    }
}

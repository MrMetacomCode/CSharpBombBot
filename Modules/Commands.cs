using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Interactivity;

namespace BombBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        public InteractivityService InteractiveService { get; set; }
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "Google Sheets API .NET Quickstart";

        [Command("bombs")]
        [Summary("Returns bombs need to destroy each base and airfield.")]
        [Alias("bomb")]
        public async Task bomb_()
        {
            Discord.Color electricgreen = new Discord.Color(0x00ff00);
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String spreadsheetId = "1S-AIIx2EQrLX8RHJr_AVIGPsQjehEdfUmbwKyinOs_I";
            String america_bombs = "Bomb Table!B19:L29";
            String germany_bombs = "Bomb Table!B33:L39";
            String russia_bombs = "Bomb Table!B43:L54";
            String britain_bombs = "Bomb Table!B58:L64";
            String japan_bombs = "Bomb Table!B68:L81";
            String italy_bombs = "Bomb Table!B85:L91";
            String china_bombs = "Bomb Table!B95:L115";
            String france_bombs = "Bomb Table!B119:L125";
            String sweden_bombs = "Bomb Table!B128:L138";

            //List of all countries.
            var countries = new List<string>(){"america", "germany", "russia", "britain", "japan", "italy", "china", "france", "sweden"};
            var placeholder_list = new List<int>(){0,0,0,0,0,0};
            var placeholder_dict = new Dictionary<string, List<int>>(){{"bomb name", placeholder_list}};
            //Dictionary that will hold every countries bomb data.
            IDictionary<string, Dictionary<string, List<int>>> bomb_data = new Dictionary<string, Dictionary<string, List<int>>>(){
                {"america", placeholder_dict}, {"germany", placeholder_dict}, {"russia", placeholder_dict}, {"britain", placeholder_dict}, 
                {"japan", placeholder_dict}, {"italy", placeholder_dict}, {"china", placeholder_dict}, {"france", placeholder_dict}, {"sweden", placeholder_dict}};
            //Function to get bomb data for each country given.
            void get_bomb_data(String country_bombs, string country)
            {
                var country_bomb_data = new Dictionary<string, List<int>>();
                SpreadsheetsResource.ValuesResource.GetRequest request =
                service.Spreadsheets.Values.Get(spreadsheetId, country_bombs);
                ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;

                var bomb_values_list = new List<int>();
                string bomb_name = "";
                string bomb_values = "";
                string first_value = "";
                foreach (var row in values)
                {
                    bomb_name = $"{row[0]}";
                    bomb_values = "";
                    first_value = $"{row[3]}";
                    if (bomb_values.Length == 0 && first_value.Length > 0)
                    {
                        bomb_values = $"{row[3]}";
                    }
                    else
                    {
                        bomb_values = "0";
                    }
                    for (int i = 4; i < 11; i++)
                    {
                        string item = $"{row[i]}";
                        if (item != null && item.Length > 0 && item != "You need a whole lotta these (only 30kg filler)" && item != "A whole lotta these (barely 10kg filler)" && item != "U.T." 
                        && item != "N/A")
                        {
                            string added_value = $"{bomb_values},{row[i]}";
                            bomb_values = added_value;
                        }
                        else
                        {
                            string added_value = $"{bomb_values},0";
                            bomb_values = added_value;
                        }
                    }
                    List<int> bomb_values_int_list = new List<int>( Array.ConvertAll(bomb_values.Split(','), int.Parse) );
                    country_bomb_data[bomb_name] = bomb_values_int_list;
                }
                bomb_data[country].Remove("bomb name");
                bomb_data[country] = country_bomb_data;
            }
            //Gets bomb data for each country.
            get_bomb_data(america_bombs, "america");
            get_bomb_data(germany_bombs, "germany");
            get_bomb_data(russia_bombs, "russia");
            get_bomb_data(britain_bombs, "britain");
            get_bomb_data(japan_bombs, "japan");
            get_bomb_data(italy_bombs, "italy");
            get_bomb_data(china_bombs, "china");
            get_bomb_data(france_bombs, "france");
            get_bomb_data(sweden_bombs, "sweden");

            //Embed maker helper function.
            string embed_maker(List<string> thing_list)
            {
                int  list_number = 1;
                string embed = "";
                foreach (string thing in thing_list)
                {
                    string item = $"{list_number} = {thing.ToUpper()}\n";
                    embed += item;
                    list_number += 1;
                }
                return embed;
            }
            try
            {
                string countries_embed = embed_maker(countries);
                var builder = new EmbedBuilder();
                builder.WithTitle("Select a country to view bombs from:");
                builder.WithDescription(countries_embed);
                builder.WithColor(electricgreen);
                var embedvar = builder.Build();
                await Context.Channel.SendMessageAsync("", false, embedvar);
                
                int country_number = 0;
                string country_number_string = "";
                //Give them 5 tries to enter a number.
                for (int i = 0; i < 6; i++)
                {
                    //Waits for the answer, makes sure it's the user who called the command, and gets the text if it was.
                    var result = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id);
                    if (result.IsSuccess) 
                    {
                        //Text from user.
                        country_number_string = result.Value.Content;
                    }
                    else 
                    {
                        await Context.Channel.SendMessageAsync("Unable to get user answer.");
                        return;
                    }
                    //Converting to int.
                    bool isParsable = Int32.TryParse(country_number_string, out country_number);
                    //If it converts, break the loop and keep going.
                    if (isParsable)
                        break;
                    //If it doesn't, they didn't enter a number. Restart the loop.
                    else
                        await Context.Channel.SendMessageAsync("Please use a number.");
                        continue;
                }
                //If they don't use a number after 5 tries just exit the command. They need help :D.
                if (country_number == 0)
                {
                    await Context.Channel.SendMessageAsync("You didn't use a number. Goodbye.");
                    return;
                }

                bool did_it_find_the_countries_bombs = false;
                //Bomb names list helper function.
                Embed get_bomb_names(int number)
                {
                    int actual_country_number = number - 1;
                    string country = countries[actual_country_number];
                    var bomb_names = new List<string>();
                    foreach (KeyValuePair<string, List<int>> bomb_ in bomb_data[country])
                    {
                        bomb_names.Add(bomb_.Key);
                    }

                    string bomb_names_embed = embed_maker(bomb_names);
                    var country_builder = new EmbedBuilder();
                    string country_name = countries[actual_country_number];
                    country_builder.WithTitle($"Select a bomb to view from {country_name.ToUpper()}:");
                    country_builder.WithDescription(bomb_names_embed);
                    country_builder.WithColor(electricgreen);
                    var country_bombs_embed = country_builder.Build();
                    did_it_find_the_countries_bombs = true;
                    return country_bombs_embed;
                }

                //Sends the bombs list for the country they selected.
                if (country_number == 1)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }
                if (country_number == 2)
                {
                    var country2_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country2_bomb_names);
                }
                if (country_number == 3)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }
                if (country_number == 4)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }
                if (country_number == 5)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }
                if (country_number == 6)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }
                if (country_number == 7)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }
                if (country_number == 8)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }
                if (country_number == 9)
                {
                    var country1_bomb_names = get_bomb_names(country_number);
                    await Context.Channel.SendMessageAsync("", false, country1_bomb_names);
                }

                if (did_it_find_the_countries_bombs == false)
                {
                    await Context.Channel.SendMessageAsync("Couldn't find that country's bombs.");
                    return;
                }

                int bomb_number = 0;
                string bomb_number_string = "";
                //Give them 5 tries to enter a number to get the bomb number.
                for (int i = 0; i < 6; i++)
                {
                    //Waits for the answer, makes sure it's the user who called the command, and gets the text if it was.
                    var result = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id);
                    if (result.IsSuccess) 
                    {
                        //Text from user.
                        bomb_number_string = result.Value.Content;
                    }
                    else 
                    {
                        await Context.Channel.SendMessageAsync("Unable to get user answer.");
                        return;
                    }
                    //Converting to int.
                    bool isParsable = Int32.TryParse(bomb_number_string, out bomb_number);
                    //If it converts, break the loop and keep going.
                    if (isParsable)
                        break;
                    //If it doesn't, they didn't enter a number. Restart the loop.
                    else
                        await Context.Channel.SendMessageAsync("Please use a number.");
                        continue;
                }
                //If they don't use a number after 5 tries just exit the command. They need help :D.
                if (bomb_number == 0)
                {
                    await Context.Channel.SendMessageAsync("You didn't use a number. Goodbye.");
                    return;
                }
                
                string bomb_type = "";
                var bomb_names_list = new List<string>();
                foreach (KeyValuePair<string, List<int>> bomb_name_ in bomb_data[countries[country_number - 1]])
                {
                    bomb_names_list.Add(bomb_name_.Key);
                }
                
                int number_of_bombs_plus1 = bomb_names_list.Count + 1;
                for (int x = 0; x < number_of_bombs_plus1; x++)
                {
                    if (bomb_number == x)
                    {
                        bomb_type = bomb_names_list[x - 1];
                    }
                }

                await Context.Channel.SendMessageAsync("Enter max battle rating in match:");
                double battle_rating = 0;
                string battle_rating_string = "";
                //Give them 5 tries to enter a number to get the battle rating.
                for (int i = 0; i < 6; i++)
                {
                    //Waits for the answer, makes sure it's the user who called the command, and gets the text if it was.
                    var result = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id);
                    if (result.IsSuccess) 
                    {
                        //Text from user.
                        battle_rating_string = result.Value.Content;
                    }
                    else 
                    {
                        await Context.Channel.SendMessageAsync("Unable to get user answer.");
                        return;
                    }
                    //Converting to int.
                    try
                    {
                        //If it converts, break the loop and keep going.
                        battle_rating = Convert.ToDouble(battle_rating_string);
                        break;
                    }
                    catch (Exception e)
                    {
                        //If it doesn't, they didn't enter a number. Restart the loop.
                        await Context.Channel.SendMessageAsync("Please use a decimal number.");
                        continue;
                    }   
                }
                //If they don't use a number after 5 tries just exit the command. They need help :D.
                if (battle_rating == 0)
                {
                    await Context.Channel.SendMessageAsync("You didn't use a decimal number. Goodbye.");
                    return;
                }

                //Check if they need 4 base map data.
                await Context.Channel.SendMessageAsync("Is this a four base map? Enter 'YES' or 'NO'");

                //Gets the users answer and makes sure it's "YES" or "NO".
                string is_it_a_four_base = "";
                for (int i = 0; i < 6; i++)
                {
                    //Waits for the answer, makes sure it's the user who called the command, and gets the text if it was.
                    var result = await InteractiveService.NextMessageAsync(x => x.Author.Id == Context.User.Id);
                    if (result.IsSuccess) 
                    {
                        //Text from user.
                        is_it_a_four_base = result.Value.Content;
                    }
                    else 
                    {
                        await Context.Channel.SendMessageAsync("Unable to get user answer.");
                        return;
                    }

                    if (is_it_a_four_base.ToUpper() == "YES" | is_it_a_four_base.ToUpper() == "NO")
                    {
                        break;
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("Please enter 'YES or 'NO'.");
                        continue;
                    }
                }
                //If they don't use a number after 5 tries just exit the command. They need help :D.
                if (is_it_a_four_base == "")
                {
                    await Context.Channel.SendMessageAsync("You didn't use a decimal number. Goodbye.");
                    return;
                }

                var base_bombs_list = new List<int>();
                base_bombs_list = bomb_data[countries[country_number - 1]][bomb_type];

                string four_base = is_it_a_four_base.ToUpper();
                int base_bombs_required = 0;
                int airfield_bombs_required = 0;
                try
                {
                    if (1.0 <= battle_rating && battle_rating <= 2.0)
                    {
                        if (four_base == "YES")
                        {
                            base_bombs_required = base_bombs_list[0];
                            airfield_bombs_required = base_bombs_required * 5;
                        }
                        else
                        {
                            base_bombs_required = base_bombs_list[1];
                            airfield_bombs_required = base_bombs_required * 5;
                        }
                    }
                    if (2.3 <= battle_rating && battle_rating <= 3.3)
                    {
                        if (four_base == "YES")
                        {
                            base_bombs_required = base_bombs_list[2];
                            airfield_bombs_required = base_bombs_required * 6;
                        }
                        else
                        {
                            base_bombs_required = base_bombs_list[3];
                            airfield_bombs_required = base_bombs_required * 6;
                        }
                    }
                    if (3.7 <= battle_rating && battle_rating <= 4.7)
                    {
                        if (four_base == "YES")
                        {
                            base_bombs_required = base_bombs_list[4];
                            airfield_bombs_required = base_bombs_required * 8;
                        }
                        else
                        {
                            base_bombs_required = base_bombs_list[5];
                            airfield_bombs_required = base_bombs_required * 8;
                        }
                    }
                    if (5.0 <= battle_rating)
                    {
                        if (four_base == "YES")
                        {
                            base_bombs_required = base_bombs_list[6];
                            airfield_bombs_required = base_bombs_required * 15;
                        }
                        else
                        {
                            base_bombs_required = base_bombs_list[7];
                            airfield_bombs_required = base_bombs_required * 15;
                        }
                    }
                    if (base_bombs_required == 0 | airfield_bombs_required == 0)
                    {
                        await Context.Channel.SendMessageAsync("This bomb data hasn't been added to the spreadsheet yet. If you are requesting a 4 base map, it may be too soon. Please refer to 3 base map data and multiply it by 2x for each base to get approximate 4 base data.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync("That battle rating doesn't exist.");
                    return;
                }
                await Context.Channel.SendMessageAsync($"Bombs required for bases: {base_bombs_required}\nBombs required for airfield: {airfield_bombs_required}");
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("User error, try again.");
                Console.WriteLine(e);
            }

            // Prints all bombs in each country and their values to the console.
            /*
            string bomb_display = "";
            foreach (string country in countries)
            {
                Console.WriteLine($"\n{country.ToUpper()}");
                foreach (var bomb in bomb_data[country])
                {
                    bomb_display = "";
                    foreach (int i in bomb.Value)
                    {
                        if (bomb_display.Length > 0)
                        {
                            string added_value = $"{bomb_display}, {i}";
                            bomb_display = added_value;
                        }
                        else
                            bomb_display = $"{i}";
                    }
                    Console.WriteLine($"{bomb.Key}: {bomb_display}");
                }
            }
            */
            Console.Read();
        }
    }
}
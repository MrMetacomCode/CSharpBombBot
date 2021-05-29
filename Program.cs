﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Reflection;
using Interactivity;

namespace BombBot
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService(new CommandServiceConfig{DefaultRunMode = RunMode.Async});
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(new InteractivityService(_client, TimeSpan.FromSeconds(20), false))
                .BuildServiceProvider();
            string token_ = Environment.GetEnvironmentVariable("C#_BOMBBOT_TOKEN");
            Console.WriteLine($"Token:{token_}");
            string token = Environment.GetEnvironmentVariable("CSBOMBBOT_DISCORD_TOKEN", EnvironmentVariableTarget.Process);

            _client.Log += _client_Log;

            await RegisterCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            if (message.Author.IsBot) return;

            int argPos = 0;
            if (message.HasStringPrefix("%", ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
            }
        }
    }
}

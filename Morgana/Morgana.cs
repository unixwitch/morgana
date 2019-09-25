/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

namespace Morgana {
    public class Bot {
        Configuration _config;

        public Bot(Configuration config) {
            _config = config;
        }

        public Task Log(LogMessage msg) {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        }

        public async Task Run() {
            using (var services =
                new ServiceCollection()
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton<Storage>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton(_config)
                    .BuildServiceProvider()) {

                var client = services.GetRequiredService<DiscordSocketClient>();
                client.Log += Log;

                services.GetRequiredService<CommandService>().Log += Log;
                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                await client.LoginAsync(TokenType.Bot, _config.Token);
                await client.StartAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(-1));
            }
        }
    }
}

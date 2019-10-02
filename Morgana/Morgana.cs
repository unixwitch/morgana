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
    public class RequireBotAdminAttribute : PreconditionAttribute {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {
            if (context.Guild == null)
                return await Task.FromResult(PreconditionResult.FromError("This command cannot be used in a direct message."));

            var config = services.GetRequiredService<Storage>();
            var gcfg = config.GetGuild(context.Guild);

            if (!gcfg.IsAdmin(context.User.Id))
                return await Task.FromResult(PreconditionResult.FromError("This command can only be used by bot administrators."));

            return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }

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
            var clientConfig = new DiscordSocketConfig {
                MessageCacheSize = 1024
            };
            var client = new DiscordSocketClient(clientConfig);
            client.Log += Log;

            using (var services =
                new ServiceCollection()
                    .AddSingleton<DiscordSocketClient>(client)
                    .AddSingleton<CommandService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<Storage>()
                    .AddSingleton<BadwordsFilter>()
                    .AddSingleton<AuditLogger>()
                    .AddSingleton<PicMover>()
                    .AddSingleton(_config)
                    .BuildServiceProvider()) {

                services.GetRequiredService<CommandService>().Log += Log;
                await services.GetRequiredService<CommandHandler>().InitializeAsync();
                await services.GetRequiredService<BadwordsFilter>().InitialiseAsync();
                await services.GetRequiredService<AuditLogger>().InitialiseAsync();
                await services.GetRequiredService<PicMover>().InitialiseAsync();

                await client.LoginAsync(TokenType.Bot, _config.Token);
                await client.StartAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(-1));
            }
        }
    }
}

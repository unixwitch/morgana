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
using System.Reflection;

using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace Morgana {
    public class CommandHandler {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private BadwordsFilter _filter;
        private InfobotService _infobot;

        public CommandHandler(
            DiscordSocketClient client,
            CommandService commands,
            IServiceProvider services,
            BadwordsFilter filter,
            InfobotService infobot) {

            _client = client;
            _commands = commands;
            _services = services;
            _filter = filter;
            _infobot = infobot;
        }

        public async Task InitializeAsync() {
            _client.MessageReceived += HandleCommand;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task HandleCommand(SocketMessage p) {
            var message = p as SocketUserMessage;
            if (message == null)
                return;

            if (message.Author.IsBot)
                return;

            if (await _filter.FilterMessageAsync(p))
                return;

            if (await _infobot.HandleInfobotAsync(p))
                return;

            var db = _services.GetRequiredService<StorageContext>();

            int argpos = 0;
            var channel = message.Channel as SocketGuildChannel;
            if (channel != null) {
                var gcfg = db.GetGuild(channel.Guild);
                var pfx = await gcfg.GetCommandPrefixAsync() ?? "~";

                // If the command prefix is doubled, ignore it.  This avoids responding to formatting at
                // the start of the line, e.g. ~~ or **.
                if (pfx.Length == 1 && message.Content.Length >= 2 && message.Content[1] == pfx[0])
                    return;

                if (!(message.HasStringPrefix(pfx, ref argpos)) || message.HasMentionPrefix(_client.CurrentUser, ref argpos))
                    return;
            }

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argpos, _services);

            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}
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
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly BadwordsFilter _filter;
        private readonly InfobotService _infobot;
        private readonly StorageContext _db;

        public CommandHandler(
            DiscordSocketClient client,
            CommandService commands,
            IServiceProvider services,
            BadwordsFilter filter,
            InfobotService infobot,
            StorageContext db) {

            _client = client;
            _commands = commands;
            _services = services;
            _filter = filter;
            _infobot = infobot;
            _db = db;
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
            string pfx = "";

            if (channel != null) {
                var gcfg = db.GetGuild(channel.Guild);
                pfx = await gcfg.GetCommandPrefixAsync() ?? "~";

                // If the command prefix is by itself on the line, ignore it.
                if (pfx.Length >= message.Content.Length)
                    return;

                // If the command prefix is doubled, ignore it.  This avoids responding to formatting at
                // the start of the line, e.g. ~~ or **.
                if (pfx.Length == 1 && message.Content.Length >= 2 && message.Content[1] == pfx[0])
                    return;

                // If the command prefix is followed by whitespace, ignore it.
                if (message.Content.Length >= (pfx.Length + 1) && Char.IsWhiteSpace(message.Content[pfx.Length]))
                    return;

                if (!(message.HasStringPrefix(pfx, ref argpos)) || message.HasMentionPrefix(_client.CurrentUser, ref argpos))
                    return;
            }

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argpos, _services);

            if (!result.IsSuccess) {
                Console.WriteLine($"error = [{result.Error}]");
                switch (result.Error) {
                    case CommandError.BadArgCount:
                    case CommandError.UnknownCommand:
                        var (text, embed) = await HelpModule.ShowHelp(_services, context, _db, _commands, message.Content);
                        await context.Channel.SendMessageAsync(text: text, embed: embed);
                        return;

                    default:
                        await context.Channel.SendMessageAsync($"{MentionUtils.MentionUser(message.Author.Id)}, {result.ErrorReason}");
                        break;
                }
            }
        }
    }
}
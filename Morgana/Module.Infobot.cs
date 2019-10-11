/*
 * Morgana - a Discord bot.
 * Copyright(c) 2019 Felicity Tarnell <ft@le-fay.org>.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely. This software is provided 'as-is', without any express or implied
 * warranty.
 */

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using Discord;

namespace Morgana {

    [Group("infobot")]
    [Summary("Configure the infobot")]
    public class InfoboxModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("prefix")]
        [Summary("Show or set the infobot prefix")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Prefix(string p = null) {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);

            if (p == null) {
                await ReplyAsync("The current infobot prefix is: `" + (await gcfg.GetInfobotPrefixAsync()) + "`");
                return;
            }

            if (p.Length < 1) {
                await ReplyAsync("The infobot prefix cannot be empty.");
                return;
            }

            if (p.IndexOfAny(new char[] { ' ', '\n', '\t', '\r', '\v' }) != -1) {
                await ReplyAsync("The infobot prefix cannot contain whitespace.");
                return;
            }

            if (p == await gcfg.GetCommandPrefixAsync()) {
                await ReplyAsync("The infobot prefix cannot be the same as the command prefix.");
                return;
            }

            await gcfg.SetInfobotPrefixAsync(p);
            await ReplyAsync("Done!");
        }
    }

    public class InfobotService {
        public Storage Vars { get; set; }
        public DiscordSocketClient Client { get; set; }

        public InfobotService(Storage vars, DiscordSocketClient client) {
            Vars = vars;
            Client = client;
        }

        public Task InitialiseAsync() {
            return Task.CompletedTask;
        }

        public async Task<bool> HandleInfobotAsync(SocketMessage p) {
            var message = p as SocketUserMessage;
            var guilduser = message.Author as SocketGuildUser;

            if (message == null)
                return false;

            if (message.Author.IsBot)
                return false;

            var channel = message.Channel as SocketGuildChannel;
            if (channel == null)
                return false;

            var gcfg = Vars.GetGuild(channel.Guild);

            var pfx = await gcfg.GetInfobotPrefixAsync();
            if (!message.Content.StartsWith(pfx))
                return false;

            var str = message.Content.Substring(pfx.Length);
            if (str.Length == 0 || char.IsWhiteSpace(str[0]))
                return false;

            var eq = str.IndexOf('=');
            string key;
            if (eq == -1)
                key = str.Trim();
            else
                key = str.Substring(0, eq).Trim();

            if (eq == -1) {
                string f = await gcfg.GetFactoidAsync(key);
                if (f == null)
                    return false;

                await message.Channel.SendMessageAsync($"{key} = {f}");
                return true;
            }

            string value = str.Substring(eq + 1).Trim();

            if (!await gcfg.IsAdminAsync(guilduser)) {
                await message.Channel.SendMessageAsync($"Sorry {MentionUtils.MentionUser(guilduser.Id)}, only admins can teach me about new things.");
                return true;
            }

            await gcfg.SetFactoidAsync(key, value);
            await message.Channel.SendMessageAsync($"{MentionUtils.MentionUser(guilduser.Id)}, I understand `{key}` now.");
            return true;
        }
    }
}
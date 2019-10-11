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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;

namespace Morgana {
    public class ConfigModule : ModuleBase<SocketCommandContext> {
        public StorageContext DB { get; set; }

        [Command("config")]
        [Summary("View or change a bot configuration option")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Config(
            [Summary("The name of the configuration option")] string item = null,
            [Summary("The new value of the option")] string newValue = null) {

            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = DB.GetGuild(Context.Guild);

            if (item == null) {
                await ReplyAsync("Please choose one of the following configuration options: `prefix`, `nickname`.");
                return;
            }

            switch (item) {
                case "prefix":
                    if (newValue == null) 
                        await ReplyAsync($"The current command prefix is `{await gcfg.GetCommandPrefixAsync() ?? "~"}`.");
                    else {
                        if (newValue == await gcfg.GetInfobotPrefixAsync()) {
                            await ReplyAsync("The command prefix cannot be the same as the infobot prefix.");
                            return;
                        } else {
                            await gcfg.SetCommandPrefixAsync(newValue);
                            await ReplyAsync("Done!");
                        }
                    }
                    return;

                case "nickname":
                    if (newValue == null) {
                        var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
                        var nick = user.Nickname ?? user.Username;
                        await ReplyAsync($"My current nickname is \"{nick}\".");
                    } else {
                        await Context.Guild.GetUser(Context.Client.CurrentUser.Id).ModifyAsync(x => {
                            x.Nickname = newValue;
                        });
                        await ReplyAsync("Done!");
                    }
                    return;

                default:
                    await ReplyAsync($"I don't recognise the configuration option `{item}`.");
                    return;
            }
        }
    }
}

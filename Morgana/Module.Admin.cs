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
    [Group("admin")]
    [Summary("Commands for managing bot admins")]
    public class AdminModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("list")]
        [Summary("List currently configured bot admins")]
        [RequireContext(ContextType.Guild)]
        public async Task List() {
            var gcfg = Vars.GetGuild(Context.Guild);
            var admins = await gcfg.GetAdminsAsync();

            if (admins.Count() == 0) {
                await ReplyAsync("No admins have been configured yet.");
                return;
            }

            var strings = new List<string>();
            foreach (var admin in admins)
                strings.Add(admin.ToString());

            var str = Format.Sanitize(string.Join(", ", strings));
            await ReplyAsync($"Configured admins: {str}");
        }

        [Group("add")]
        [Summary("Add a new bot admin")]
        public class AdminAddModule : ModuleBase<SocketCommandContext> {
            public Storage Vars { get; set; }

            [Command("user")]
            [Summary("Add a specific user as a bot admin")]
            [RequireContext(ContextType.Guild)]
            public async Task AddUser([Summary("The admin to be added")] IGuildUser target) {
                var guild = Context.Guild;
                var gcfg = Vars.GetGuild(guild);
                var guilduser = Context.Guild.GetUser(Context.User.Id);
                var admins = await gcfg.GetAdminsAsync();

                if (admins.Count() == 0) {
                    if (!guilduser.GuildPermissions.Administrator) {
                        await ReplyAsync("No admins have been defined yet, so only the server owner can use this command.");
                        return;
                    }
                } else {
                    if (!await gcfg.IsAdminAsync(guilduser)) {
                        await ReplyAsync("Sorry, only admins can use this command.");
                        return;
                    }
                }

                if (target == null) {
                    await ReplyAsync("That user doesn't seem to exist.");
                    return;
                }

                if (await gcfg.AdminAddAsync(target))
                    await ReplyAsync("Done!");
                else
                    await ReplyAsync("That user is already an admin.");
            }
        }

        [Group("remove")]
        [Summary("Remove a bot admin")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public class AdminRemoveModule : ModuleBase<SocketCommandContext> {
            public Storage Vars { get; set; }

            [Command("user")]
            [Summary("Remove a specific user as a bot admin")]
            public async Task RemoveUser([Summary("The admin to be removed")] IGuildUser target) {
                var guild = Context.Guild;
                var gcfg = Vars.GetGuild(guild);
                var admins = await gcfg.GetAdminsAsync();

                if (target == null) {
                    await ReplyAsync("That user doesn't seem to exist.");
                    return;
                }

                if (!await gcfg.IsAdminAsync(target)) {
                    await ReplyAsync("That user is not an admin.");
                    return;
                }

                if (admins.Count() == 1) {
                    await ReplyAsync("You cannot remove the last admin, otherwise there would be none left.");
                    return;
                }

                await gcfg.AdminRemoveAsync(target);
            }
        }
    }
}

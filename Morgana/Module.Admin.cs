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
        public StorageContext DB { get; set; }

        [Command("list")]
        [Summary("List currently configured bot admins")]
        [RequireContext(ContextType.Guild)]
        public async Task List() {
            var gcfg = DB.GetGuild(Context.Guild);
            var adminUsers = await gcfg.GetAdminUsersAsync();
            var adminRoles = await gcfg.GetAdminRolesAsync();

            if (adminUsers.Count() == 0 && adminRoles.Count() == 0) {
                await ReplyAsync("No admins have been configured yet.");
                return;
            }

            var ustr = Format.Sanitize(string.Join(", ",
                adminUsers
                .Select(id => Context.Guild.GetUser(id))
                .Where(u => u != null)
                .Select(u => $"{u.Username}#{u.Discriminator}")));
            if (ustr == "")
                ustr = "none";

            var rstr = Format.Sanitize(string.Join(", ",
                adminRoles
                .Select(id => Context.Guild.GetRole(id))
                .Where(r => r != null)
                .Select(r => r.Name)));
            if (rstr == "")
                rstr = "none";

            await ReplyAsync($"Configured admin roles: {rstr}, users: {ustr}.");
        }

        [Group("add")]
        [Summary("Add a new bot admin")]
        public class AdminAddModule : ModuleBase<SocketCommandContext> {
            public StorageContext DB { get; set; }

            [Command("user")]
            [Summary("Add a specific user as a bot admin")]
            [RequireContext(ContextType.Guild)]
            public async Task AddUser([Summary("The admin to be added")] IGuildUser target) {
                var guild = Context.Guild;
                var gcfg = DB.GetGuild(guild);
                var guilduser = Context.Guild.GetUser(Context.User.Id);
                var admins = await gcfg.GetAdminUsersAsync();

                if (admins.Count() == 0) {
                    if (!guilduser.GuildPermissions.Administrator && !await DB.IsOwnerAsync(Context.User.Id)) {
                        await ReplyAsync("No admins have been defined yet, so only the server owner can use this command.");
                        return;
                    }
                } else {
                    if (!await gcfg.IsAdminAsync(guilduser)) {
                        await ReplyAsync("Sorry, only admins can use this command.");
                        return;
                    }
                }

                if (await gcfg.AdminUserAddAsync(target.Id))
                    await ReplyAsync("Done!");
                else
                    await ReplyAsync("That user is already an admin.");
            }

            [Command("role")]
            [Summary("Add a server role as a bot admin")]
            [RequireContext(ContextType.Guild)]
            public async Task AddRole([Summary("The role to be added")] IRole target) {
                var guild = Context.Guild;
                var gcfg = DB.GetGuild(guild);
                var guilduser = Context.Guild.GetUser(Context.User.Id);
                var admins = await gcfg.GetAdminRolesAsync();

                if (admins.Count() == 0) {
                    if (!guilduser.GuildPermissions.Administrator && !await DB.IsOwnerAsync(Context.User.Id)) {
                        await ReplyAsync("No admins have been defined yet, so only the server owner can use this command.");
                        return;
                    }
                } else {
                    if (!await gcfg.IsAdminAsync(guilduser)) {
                        await ReplyAsync("Sorry, only admins can use this command.");
                        return;
                    }
                }

                if (await gcfg.AdminRoleAddAsync(target.Id))
                    await ReplyAsync("Done!");
                else
                    await ReplyAsync("That role is already an admin.");
            }
        }

        [Group("remove")]
        [Summary("Remove a bot admin")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public class AdminRemoveModule : ModuleBase<SocketCommandContext> {
            public StorageContext DB { get; set; }

            [Command("user")]
            [Summary("Remove a specific user as a bot admin")]
            [RequireBotAdmin]
            public async Task RemoveUser([Summary("The admin user to be removed")] IGuildUser target) {
                var guild = Context.Guild;
                var gcfg = DB.GetGuild(guild);
                var admins = await gcfg.GetAdminUsersAsync();

                if (admins.Count() == 1 && (await gcfg.GetAdminRolesAsync()).Count() == 0) {
                    await ReplyAsync("You cannot remove the last admin, otherwise there would be none left.");
                    return;
                }

                if (await gcfg.AdminUserRemoveAsync(target.Id))
                    await ReplyAsync("Done!");
                else
                    await ReplyAsync("That user is not an admin.");
            }

            [Command("role")]
            [Summary("Remove a server role as a bot admin")]
            [RequireBotAdmin]
            public async Task RemoveRole([Summary("The admin role to be removed")] IRole target) {
                var guild = Context.Guild;
                var gcfg = DB.GetGuild(guild);
                var admins = await gcfg.GetAdminRolesAsync();

                if (admins.Count() == 1 && (await gcfg.GetAdminUsersAsync()).Count() == 0) {
                    await ReplyAsync("You cannot remove the last admin, otherwise there would be none left.");
                    return;
                }

                if (await gcfg.AdminRoleRemoveAsync(target.Id))
                    await ReplyAsync("Done!");
                else
                    await ReplyAsync("That role is not an admin.");
            }
        }
    }
}

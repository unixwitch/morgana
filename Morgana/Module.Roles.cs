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
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Linq;
using Discord.WebSocket;

namespace Morgana {
    [Group("role")]
    [Summary("Commands for managing roles")]
    public class RolesModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("add")]
        [Summary("Bestow a role upon yourself or another user")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Add(
                [Summary("The role to be bestowed")] 
                string roleName,
                [Summary("The user upon whom the role should be bestowed, if not yourself")]
                IGuildUser target = null) {

            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(Context.Guild);
            var guilduser = Context.Guild.GetUser(Context.User.Id);

            IRole role;
            try {
                role = guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (target != null && !guilduser.GuildPermissions.ManageRoles && !gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, you don't have permission to manage roles on this server.");
                return;
            }

            target ??= guild.GetUser(Context.User.Id);

            if (target == null) {
                await ReplyAsync("That user doesn't seem to exist.");
                return;
            }

            if (!gcfg.IsManagedrole(role)) {
                await ReplyAsync("It is not within my power to bestow that role.");
                return;
            }

            if (target.RoleIds.Contains(role.Id)) {
                if (target.Id == Context.User.Id)
                    await ReplyAsync("You already have that role!");
                else
                    await ReplyAsync($"{Format.Sanitize(target.Username)} already has that role!");
                return;
            }

            await target.AddRoleAsync(role);
            Vars.Save();
            await ReplyAsync("Done!");
        }

        [Command("remove")]
        [Summary("Remove a role from yourself or another user")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Remove(
                [Summary("The role to be removed")]
                string roleName,
                [Summary("The user who should lose the role, if not yourself")]
                IGuildUser target = null) {

            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(Context.Guild);
            var guilduser = Context.Guild.GetUser(Context.User.Id);

            if (target != null && !guilduser.GuildPermissions.ManageRoles && !gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, you don't have permission to remove roles from other users.");
                return;
            }

            target ??= guild.GetUser(Context.User.Id);

            if (target == null) {
                await ReplyAsync("That user doesn't seem to exist.");
                return;
            }

            IRole role;
            try {
                role = guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (!gcfg.IsManagedrole(role)) {
                await ReplyAsync("It is not within my power to remove that role.");
                return;
            }

            if (!target.RoleIds.Contains(role.Id)) {
                if (target.Id == Context.User.Id)
                    await ReplyAsync("I can't remove that role because you don't have it to begin with.");
                else
                    await ReplyAsync($"I can't remove that role because {Format.Sanitize(target.Username)} doesn't have it to begin with.");
                return;
            }

            await target.RemoveRoleAsync(role);
            Vars.Save();
            await ReplyAsync("Done!");
        }

#if false
        [Command("debug_test_bestow")]
        [RequireContext(ContextType.Guild)]
        public async Task TestBestow(string roleName, IGuildUser target, IGuildUser doer = null) {
            IRole role;
            var gtarget = target as SocketGuildUser;
            var gdoer = doer != null ? (doer as SocketGuildUser) : (Context.User as SocketGuildUser);

            try {
                role = Context.Guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            int roleHier = role.Position;
            int userHier = gdoer.Hierarchy;
            int targetHier = gtarget.Hierarchy;

            bool canDoRole = userHier > roleHier;
            bool canDoUser = userHier > targetHier;
            await ReplyAsync($"{userHier}/{targetHier}/{roleHier} canDoRole={canDoRole} canDoUser={canDoUser}");
        }
#endif

        [Command("manage")]
        [Summary("Allow users to bestow or remove this role on themselves")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Manage(
            [Summary("The role that should be managed")]
            string roleName) {

            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(Context.Guild);

            IRole role;
            try {
                role = Context.Guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (role.Position >= guilduser.Hierarchy) {
                await ReplyAsync("Sorry, you can't manage a role that you don't have permission to bestow.");
                return;
            }

            if (gcfg.ManagedRoleAdd(role))
                await ReplyAsync("Done!");
            else
                await ReplyAsync("I am already managing that role.");
            Vars.Save();
        }

        [Command("unmanage")]
        [Summary("Prevent users bestowing or removing this role on themselves")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Unmanage(
            [Summary("The role that should be unmanaged")]
            string roleName) {

            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(Context.Guild);

            IRole role;
            try {
                role = Context.Guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (role.Position >= guilduser.Hierarchy) {
                await ReplyAsync("Sorry, you can't manage a role that you don't have permission to bestow.");
                return;
            }

            if (gcfg.ManagedRoleRemove(role))
                await ReplyAsync("Done!");
            else
                await ReplyAsync("I am not managing that role.");
            Vars.Save();
        }

        [Command("list")]
        [Summary("List the roles that I can bestow")]
        [RequireContext(ContextType.Guild)]
        public async Task List() {
            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(Context.Guild);

            var strings = new List<string>();

            foreach (var roleId in gcfg.ManagedRoleList) {
                IRole role;
                try {
                    role = Context.Guild.Roles.First(r => r.Id == roleId);
                    strings.Add(role.Name);
                } catch (InvalidOperationException) {
                }
            }

            var list = Format.Sanitize(String.Join(", ", strings));
            await ReplyAsync($"I can bestow these roles: {list}.");
        }
    }
}

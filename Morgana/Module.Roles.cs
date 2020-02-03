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
        public StorageContext DB { get; set; }

        [Command("add")]
        [Summary("Bestow a role upon yourself")]
        [Remarks("For example, to give yourself the role \"Awesome Role\", use:\n"
                + "```<cmd> Awesome Role```\n")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Add(
                [Summary("The role to be bestowed")] 
                [Remainder]
                string roleName) {
            var guild = Context.Guild;
            var gcfg = DB.GetGuild(Context.Guild);
            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var mu = MentionUtils.MentionUser(Context.User.Id);

            IRole role;
            try {
                role = guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync($"Sorry {mu}, I can't find a role called `{roleName}`.");
                return;
            }

            IGuildUser target = guild.GetUser(Context.User.Id);

            if (target == null) {
                await ReplyAsync($"Hmm, you don't seem to exist, {mu}.  That's odd.  (This is probably a bug, please report it.)");
                return;
            }

            if (!await gcfg.IsManagedRoleAsync(role)) {
                await ReplyAsync($"Sorry {mu}, it is not within my power to bestow `{role.Name}`.");
                return;
            }

            if (await gcfg.IsAdminRoleAsync(role.Id)) {
                await ReplyAsync($"Sorry, {mu}, I cannot bestow `{role.Name}` because it is an admin role.");
                return;
            }

            if (target.RoleIds.Contains(role.Id)) {
                await ReplyAsync($"You already have `{role.Name}`, {mu}.");
                return;
            }

            await target.AddRoleAsync(role);
            await ReplyAsync($"{mu}, you are now more `{role.Name}`.");
        }

        [Command("remove")]
        [Summary("Remove a role from yourself")]
        [Remarks("For example, to renounce the role \"Awesome Role\", use:\n"
                + "```<cmd> Awesome Role```\n")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Remove(
                [Summary("The role to be removed")]
                [Remainder]
                string roleName) {

            var guild = Context.Guild;
            var gcfg = DB.GetGuild(Context.Guild);
            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var mu = MentionUtils.MentionUser(Context.User.Id);

            IGuildUser target = guild.GetUser(Context.User.Id);

            if (target == null) {
                await ReplyAsync($"Hmm, you don't seem to exist, {mu}.  That's odd.  (This is probably a bug, please report it.)");
                return;
            }

            IRole role;
            try {
                role = guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync($"Sorry {mu}, I can't find a role called `{roleName}`.");
                return;
            }

            if (!await gcfg.IsManagedRoleAsync(role)) {
                await ReplyAsync($"Sorry {mu}, it is not within my power to remove `{role.Name}`.");
                return;
            }

            if (!target.RoleIds.Contains(role.Id)) {
                await ReplyAsync($"Sorry {mu}, I can't remove `{role.Name}` because you don't have it to begin with.");
                return;
            }

            await target.RemoveRoleAsync(role);
            await ReplyAsync($"{mu}, you are now less `{role.Name}`.");
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
        [Remarks("For example, to allow users to bestow \"Awesome Role\" on themselves, use:\n"
                + "```<cmd> Awesome Role```\n")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Manage(
            [Summary("The role that should be managed")]
            [Remainder]
            string roleName) {

            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = DB.GetGuild(Context.Guild);
            var mu = MentionUtils.MentionUser(Context.User.Id);

            IRole role;
            try {
                role = Context.Guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync($"Sorry {mu}, I can't find a role called `{roleName}`.");
                return;
            }

            if (role.Position >= guilduser.Hierarchy) {
                await ReplyAsync($"Sorry {mu}, you can't make `{role.Name}` managed because you don't have permission to bestow it.");
                return;
            }

            if (await gcfg.IsAdminRoleAsync(role.Id)) {
                await ReplyAsync($"Sorry {mu}, I will not manage `{role.Name}` because it's marked as an admin role.");
                return;
            }

            if (await gcfg.ManagedRoleAddAsync(role))
                await ReplyAsync($"{mu}, I will now manage `{role.Name}`.");
            else
                await ReplyAsync($"{mu}, I am already managing `{role.Name}`.");
        }

        [Command("unmanage")]
        [Summary("Prevent users bestowing or removing this role on themselves")]
        [Remarks("For example, to disallow users bestowing \"Awesome Role\" on themselves, use:\n"
                + "```<cmd> Awesome Role```\n")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Unmanage(
            [Summary("The role that should be unmanaged")]
            [Remainder]
            string roleName) {

            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = DB.GetGuild(Context.Guild);
            var mu = MentionUtils.MentionUser(Context.User.Id);

            IRole role;
            try {
                role = Context.Guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync($"Sorry {mu}, I can't find a role called `{roleName}`.");
                return;
            }

            if (role.Position >= guilduser.Hierarchy) {
                await ReplyAsync($"Sorry {mu}, you can't make `{role.Name}` unmanaged because you don't have permission to bestow it.");
                return;
            }

            if (await gcfg.ManagedRoleRemoveAsync(role))
                await ReplyAsync($"{mu}, I will no longer manage `{role.Name}`.");
            else
                await ReplyAsync($"{mu}, I am not managing `{role.Name}`.");
        }

        [Command("list")]
        [Summary("List the roles that I can bestow")]
        [RequireContext(ContextType.Guild)]
        public async Task List() {
            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = DB.GetGuild(Context.Guild);

            var strings = new List<string>();
            var roles = await gcfg.GetManagedRolesAsync();

            if (!roles.Any()) {
                await ReplyAsync("I am powerless to bestow any roles.");
                return;
            }

            foreach (var role in await gcfg.GetManagedRolesAsync())
                strings.Add(role.Name);

            var list = Format.Sanitize(String.Join(", ", strings));
            await ReplyAsync($"I can bestow these roles: {list}.");
        }
    }
}

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

namespace Morgana {
    [Group("role")]
    [Summary("Commands for managing roles")]
    public class RolesModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("add")]
        [Summary("Add a role to yourself or another user")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Add(
                [Summary("The role to be added")] 
                string roleName,
                [Summary("The user who should get the role, if not yourself")]
                IGuildUser target = null) {

            var guild = Context.Guild;
            if (guild == null) {
                await ReplyAsync("This command cannot be used in a direct message.");
                return;
            }

            var gcfg = Vars.GetGuild(Context.Guild);
            var guilduser = Context.Guild.GetUser(Context.User.Id);

            if (target != null && !gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, only admins can add roles to other users.");
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
                //await Context.Message.AddReactionAsync(new Emoji("\u274c"));
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (!gcfg.IsManagedrole(role)) {
                //await Context.Message.AddReactionAsync(new Emoji("\u274c"));
                await ReplyAsync("It is not within my power to bestow that role.");
                return;
            }

            if (target.RoleIds.Contains(role.Id)) {
                //await Context.Message.AddReactionAsync(new Emoji("\u274c"));
                if (target.Id == Context.User.Id)
                    await ReplyAsync("You already have that role!");
                else
                    await ReplyAsync($"{target.Username} already has that role!");
                return;
            }

            await target.AddRoleAsync(role);
            Vars.Save();
            //await Context.Message.AddReactionAsync(new Emoji("\u2611"));
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

            if (target != null && !gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, only admins can add roles to other users.");
                return;
            }

            target ??= guild.GetUser(Context.User.Id);

            if (target == null) {
                //await Context.Message.AddReactionAsync(new Emoji("\u274c"));
                await ReplyAsync("That user doesn't seem to exist.");
                return;
            }

            IRole role;
            try {
                role = guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                //await Context.Message.AddReactionAsync(new Emoji("\u274c"));
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (!gcfg.IsManagedrole(role)) {
                //await Context.Message.AddReactionAsync(new Emoji("\u274c"));
                await ReplyAsync("It is not within my power to remove that role.");
                return;
            }

            if (!target.RoleIds.Contains(role.Id)) {
                //await Context.Message.AddReactionAsync(new Emoji("\u274c"));
                if (target.Id == Context.User.Id)
                    await ReplyAsync("I can't remove that role because you don't have it to begin with.");
                else
                    await ReplyAsync($"I can't remove that role because {target.Username} doesn't have it to begin with.");
                return;
            }

            await target.RemoveRoleAsync(role);
            Vars.Save();
            await ReplyAsync("Done!");
            //await Context.Message.AddReactionAsync(new Emoji("\u2417"));
        }

        [Command("manage")]
        [Summary("Allow users to add or remove this role on themselves")]
        [RequireContext(ContextType.Guild)]
        public async Task Manage(
            [Summary("The role that should be managed")]
            string roleName) {

            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(Context.Guild);

            if (!gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, only admins can use this command.");
                return;
            }

            IRole role;
            try {
                role = Context.Guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (gcfg.ManagedRoleAdd(role))
                await ReplyAsync("Done!");
            else
                await ReplyAsync("I am already managing that role.");
            Vars.Save();
        }

        [Command("unmanage")]
        [Summary("Prevent users adding or removing this role on themselves")]
        [RequireContext(ContextType.Guild)]
        public async Task Unmanage(
            [Summary("The role that should be unmanaged")]
            string roleName) {

            var guilduser = Context.Guild.GetUser(Context.User.Id);
            var gcfg = Vars.GetGuild(Context.Guild);

            if (!gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, only admins can use this command.");
                return;
            }

            IRole role;
            try {
                role = Context.Guild.Roles.First(r => r.Name.ToLower() == roleName.ToLower());
            } catch (InvalidOperationException) {
                await ReplyAsync("Sorry, that role doesn't seem to exist.");
                return;
            }

            if (gcfg.ManagedRoleRemove(role))
                await ReplyAsync("Done!");
            else
                await ReplyAsync("I am not managing that role.");
            Vars.Save();
        }

        [Command("list")]
        [Summary("List the roles that users can add or remove")]
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

            var list = String.Join(", ", strings);
            await ReplyAsync($"I can manage these roles: {list}.");
        }
    }
}

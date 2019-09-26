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
using System.Linq;

namespace Morgana {
    public class UtilsModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("ping")]
        [Summary("Check whether I'm still alive")]
        public Task Ping() => ReplyAsync("I LIVE!");

        [Command("time")]
        [Summary("Find out what the current time is")]
        public Task Time() => ReplyAsync($"The current time in UTC is {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");

        [Command("userinfo")]
        [Summary("Display some information about you")]
        [RequireContext(ContextType.Guild)]
        public async Task Userinfo() {
            var user = Context.User as IGuildUser;

            var discorddate = user.CreatedAt.ToString("dd MMM yyyy HH:mm");
            var discorddays = (int)(DateTime.Now - user.CreatedAt).TotalDays;
            var discordField = new EmbedFieldBuilder()
                .WithName("**Joined Discord on**")
                .WithValue($"{discorddate}\n({discorddays} days ago)")
                .WithIsInline(true);

            EmbedFieldBuilder serverField;
            if (user.JoinedAt != null) {
                var serverdate = user.JoinedAt.Value.ToString("dd MMM yyyy HH:mm");
                var serverdays = (int)(DateTime.Now - user.JoinedAt.Value).TotalDays;
                serverField = new EmbedFieldBuilder()
                    .WithName("**Joined this server on**")
                    .WithValue($"{serverdate}\n({serverdays} days ago)")
                    .WithIsInline(true);
            } else
                serverField = new EmbedFieldBuilder()
                    .WithName("**Joined this server on**")
                    .WithValue("Unknown")
                    .WithIsInline(true);

            var rolesField = new EmbedFieldBuilder()
                .WithName("**Roles**")
                .WithValue(String.Join(", ",
                    user.RoleIds
                        .Select(roleId => Context.Guild.GetRole(roleId))
                        .Where(role => role != null)
                        .Where(role => !role.IsEveryone)
                        .Select(role => role.Name)));

            var footer = $"User ID: {user.Id}";

            var builder =
                new EmbedBuilder()
                    .WithThumbnailUrl(user.GetAvatarUrl());
//                    .WithAuthor(user);
            builder.AddField($"**{user.ToString()}**", user.Activity == null ? "" : ("Playing " + user.Activity.Name));

            var embed = builder
                .AddField(discordField)
                .AddField(serverField)
                .AddField(rolesField)
                .WithFooter(footer)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("serverinfo")]
        [Summary("Display some information about this server")]
        [RequireContext(ContextType.Guild)]
        public async Task Serverinfo() {
            var user = Context.User as IGuildUser;
            var guild = Context.Guild;

            var builder =
                new EmbedBuilder()
                    .WithThumbnailUrl(guild.IconUrl);
            var created = guild.CreatedAt.ToString("dd MM yyyy HH:mm");
            var ageDays = (int)(DateTime.Now - guild.CreatedAt).TotalDays;

            builder.AddField(
                $"**{guild.ToString()}**",
                $"Since {created}.  That's over {ageDays} days ago!");

#if false
            await guild.DownloadUsersAsync();
            int totalUsers = guild.Users.Count();
            int onlineUsers = guild.Users.Where(u => u.Status == UserStatus.Online).Count();
#endif
            int totalUsers = guild.MemberCount;

            var embed = builder
                .AddField("**Region**", guild.VoiceRegionId, true)
#if false
                .AddField("**Users**", $"{onlineUsers}/{totalUsers}", true)
#endif
                .AddField("**Users**", $"{totalUsers}", true)
                .AddField("**Text channels**", guild.TextChannels.Count(), true)
                .AddField("**Voice channels**", guild.VoiceChannels.Count(), true)
                .AddField("**Roles**", guild.Roles.Count(), true)
                .AddField("**Owner**", guild.Owner.ToString(), true)
                .WithFooter($"Server ID: {guild.Id}")
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("die")]
        [Summary("Cause the bot to immediately exit")]
        [RequireContext(ContextType.Guild)]
        public async Task Die() {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);
            var guilduser = Context.Guild.GetUser(Context.User.Id);

            if (!gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, only admins can use this command.");
                return;
            }

            Environment.Exit(0);
        }
    }
}

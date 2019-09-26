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

        [Command("say")]
        [Summary("Make me say something")]
        [RequireContext(ContextType.Guild)]
        public async Task Say([Summary("The channel I should talk in")] ITextChannel channel,
            [Summary("The words I should say")] [Remainder] string words) {
            var guild = Context.Guild;
            var gcfg = Vars.GetGuild(guild);
            var guilduser = Context.Guild.GetUser(Context.User.Id);

            if (!gcfg.IsAdmin(guilduser)) {
                await ReplyAsync("Sorry, only admins can use this command.");
                return;
            }

            await channel.SendMessageAsync(words);
        }

        [Command("hug")]
        [Summary("Hug a user")]
        [RequireContext(ContextType.Guild)]
        public async Task Hug(
            [Summary("The user to hug")] IGuildUser user,
            [Summary("The intensity of the hug (1 to 10)")] int intensity = 1) {
            string msg = "";
            string name = user.Mention;

            if (intensity <= 0)
                msg = "(っ˘̩╭╮˘̩)っ" + name;
            else if (intensity <= 3)
                msg = "(っ´▽｀)っ" + name;
            else if (intensity <= 6)
                msg = "╰(*´︶`*)╯" + name;
            else if (intensity <= 9)
                msg = "(つ≧▽≦)つ" + name;
            else if (intensity >= 10)
                msg = $"(づ￣ ³￣)づ{name} ⊂(´・ω・｀⊂)";

            await ReplyAsync(msg);
        }

        [Command("choose")]
        [Summary("Choose one of a list of options")]
        public async Task Choose([Summary("The options to choose from")] params string[] options) {
            string opt = options[new Random().Next(0, options.Length)];
            await ReplyAsync("`" + Format.Sanitize(opt) + "`");
        }

        static Dictionary<char, char> flips;

        static UtilsModule() {
            flips = new Dictionary<char, char>();

            string from = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string to = "ɐqɔpǝɟƃɥᴉɾʞlɯuodbɹsʇnʌʍxʎz∀qƆpƎℲפHIſʞ˥WNOԀQᴚS┴∩ΛMX⅄Z";
            for (int i = 0; i < from.Length; ++i)
                flips[from[i]] = to[i];
        }

        static string Flip(string s) {
            var t = new StringBuilder(s.Length);
            foreach (char c in s) {
                char to;
                if (flips.TryGetValue(c, out to))
                    t.Append(to);
                else
                    t.Append(c);
            }
            return t.ToString();
        }

        [Command("flip")]
        [Summary("Flip a coin... or a user.  Defaults to coin.")]
        public async Task Flip([Summary("The user to flip")] IGuildUser user = null) {
            if (user == null) {
                await ReplyAsync(new Random().Next(0, 2) == 0 ? "HEADS!" : "TAILS!");
                return;
            }

            var flipped = Flip(user.Nickname ?? user.Username);
            await ReplyAsync("(╯°□°）╯︵ " + flipped);
        }

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
            int onlineUsers = guild.Users.Where(u => u.Status == UserStatus.Online).Count();

            var embed = builder
                .AddField("**Region**", guild.VoiceRegionId, true)
#if true
                .AddField("**Users**", $"{onlineUsers}/{totalUsers}", true)
#else
                .AddField("**Users**", $"{totalUsers}", true)
#endif
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

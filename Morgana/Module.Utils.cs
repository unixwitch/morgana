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
using System.Diagnostics;
using CacheManager.Core;
using EFSecondLevelCache.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Morgana {
    public class UtilsModule : ModuleBase<SocketCommandContext> {
        public Storage Vars { get; set; }

        [Command("status", RunMode = RunMode.Async)]
        [Summary("Show my status")]
        public async Task StatusAsync() {
            var netversion = Environment.Version;
            var osvers = Environment.OSVersion;
            var hostname = Environment.MachineName;
            var user = Environment.UserName;
            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var suptime = uptime.ToString(@"d\+h\:mm\:ss");

            var cache = EFStaticServiceProvider.Instance.GetRequiredService<ICacheManager<object>>();
            var cachestats = new List<string>();

            foreach (var ch in cache.CacheHandles) { 
                var hits = ch.Stats.GetStatistic(CacheManager.Core.Internal.CacheStatsCounterType.Hits);
                var misses = ch.Stats.GetStatistic(CacheManager.Core.Internal.CacheStatsCounterType.Misses);

                int hitpct = 0;
                if (hits + misses > 0)
                    hitpct = (int) Math.Round(((double)hits / (double)(hits + misses) * 100));
                cachestats.Add($"hit {hits}/{hits + misses}, {hitpct}%");
            }

            var scachestats = string.Join("; ", cachestats);
            await ReplyAsync($"up {suptime}, host {user}@{hostname}, platform .NET Core {netversion} on {osvers}\ncache: {scachestats}");
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Check whether I'm still alive")]
        public Task Ping() => ReplyAsync("I LIVE!");

        [Command("time", RunMode = RunMode.Async)]
        [Summary("Find out what the current time is")]
        public Task Time() => ReplyAsync($"The current time in UTC is {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");

        [Command("say", RunMode = RunMode.Async)]
        [Summary("Make me say something")]
        [RequireContext(ContextType.Guild)]
        [RequireBotAdmin]
        public async Task Say([Summary("The channel I should talk in")] ITextChannel channel,
            [Summary("The words I should say")] [Remainder] string words) {
            await channel.SendMessageAsync(words);
        }

        [Command("hug", RunMode = RunMode.Async)]
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

        [Command("choose", RunMode = RunMode.Async)]
        [Summary("Choose one of a list of options")]
        public async Task Choose([Summary("The options to choose from")] params string[] options) {
            string opt = options[new Random().Next(0, options.Length)];
            await ReplyAsync("`" + Format.Sanitize(opt) + "`");
        }

        static Dictionary<char, char> flips;

        static UtilsModule() {
            flips = new Dictionary<char, char>();

            string from_ = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string to_   = "ɐqɔpǝɟƃɥᴉɾʞlɯuodbɹsʇnʌʍxʎz∀ઘƆ◖ƎℲפHIſ⋊˥WNOԀQᴚS⊥∩ΛMX⅄Z";
            string from = from_ + to_;
            string to = to_ + from_;

            for (int i = 0; i < from.Length; ++i)
                flips[from[i]] = to[i];
        }

        static string FlipString(string s) {
            var t = new StringBuilder(s.Length);
            foreach (char c in s) {
                if (flips.TryGetValue(c, out char to))
                    t.Append(to);
                else
                    t.Append(c);
            }
            return t.ToString();
        }

        [Command("flip", RunMode = RunMode.Async)]
        [Summary("Flip a coin... or a user.  Defaults to coin")]
        [RequireContext(ContextType.Guild)]
        public async Task Flip([Summary("The text or user to flip")][Remainder] string text = null) {
            if (text == null) {
                await ReplyAsync(new Random().Next(0, 2) == 0 ? "HEADS!" : "TAILS!");
                return;
            }

            string ftext = text;

            try {
                var userid = MentionUtils.ParseUser(text);
                IGuildUser user = Context.Guild.GetUser(userid);
                if (user != null) {
                    if (user.Id == Context.Client.CurrentUser.Id)
                        user = Context.User as IGuildUser;
                    ftext = user.Nickname ?? user.Username;
                }
            } catch (ArgumentException) {}

            var flipped = FlipString(ftext);
            char[] flipa = flipped.ToArray();
            Array.Reverse(flipa);
            await ReplyAsync("(╯°□°）╯︵ " + Format.Sanitize(new string(flipa)));
        }

        [Command("np", RunMode = RunMode.Async)]
        [Summary("Show your current playing music")]
        [RequireContext(ContextType.Guild)]
        public async Task Np() {
            var target = Context.User as IGuildUser;
            var username = Format.Sanitize(target.Nickname ?? target.Username);

            switch (target.Activity) {
                case SpotifyGame spotify:
                    var artists = Format.Sanitize(String.Join(", ", spotify.Artists));
                    var track = Format.Sanitize(spotify.TrackTitle);
                    var album = Format.Sanitize(spotify.AlbumTitle);
                    string activity = $"{artists} - {track} (on {album})";

                    await ReplyAsync($"**{username}** is listening to {activity}");
                    break;

                default:
                    await ReplyAsync($"**{username}** isn't listening to anything!");
                    break;
            }
        }

        [Command("userinfo", RunMode = RunMode.Async)]
        [Summary("Display some information about yourself or another user")]
        [RequireContext(ContextType.Guild)]
        public async Task Userinfo([Summary("The user to display, if not yourself")] IGuildUser target = null) {
            var gcfg = Vars.GetGuild(Context.Guild);
            var guser = Context.User as IGuildUser;

            target ??= Context.User as IGuildUser;
            if (target.Id != Context.User.Id && !await gcfg.IsAdminAsync(guser)) {
                await ReplyAsync("Sorry, only admins can see another user's info.");
                return;
            }

            var discorddate = Format.Sanitize(target.CreatedAt.ToString("dd MMM yyyy HH:mm"));
            var discorddays = (int)(DateTime.Now - target.CreatedAt).TotalDays;
            var discordField = new EmbedFieldBuilder()
                .WithName("**Joined Discord on**")
                .WithValue($"{discorddate}\n({discorddays} days ago)")
                .WithIsInline(true);

            EmbedFieldBuilder serverField;
            if (target.JoinedAt != null) {
                var serverdate = Format.Sanitize(target.JoinedAt.Value.ToString("dd MMM yyyy HH:mm"));
                var serverdays = (int)(DateTime.Now - target.JoinedAt.Value).TotalDays;
                serverField = new EmbedFieldBuilder()
                    .WithName("**Joined this server on**")
                    .WithValue($"{serverdate}\n({serverdays} days ago)")
                    .WithIsInline(true);
            } else
                serverField = new EmbedFieldBuilder()
                    .WithName("**Joined this server on**")
                    .WithValue("Unknown")
                    .WithIsInline(true);

            EmbedFieldBuilder rolesField = new EmbedFieldBuilder().WithName("**Roles**");
            var roles =
                target.RoleIds
                    .Select(roleId => Context.Guild.GetRole(roleId))
                    .Where(role => role != null)
                    .Where(role => !role.IsEveryone)
                    .Select(role => Format.Sanitize(role.Name));

            if (roles.Count() == 0)
                rolesField.WithValue("None.");
            else
                rolesField.WithValue(String.Join(", ", roles));

            var footer = $"User ID: {target.Id}";

            Color c = new Color(116, 127, 141);
            string status = target.Status.ToString();

            switch (target.Status) {
                case UserStatus.Online:
                    c = new Color(67, 181, 129);
                    break;
                case UserStatus.AFK:
                case UserStatus.Idle:
                    c = new Color(250, 166, 26);
                    break;
                case UserStatus.DoNotDisturb:
                    c = new Color(240, 71, 71);
                    status = "Do not disturb";
                    break;
                case UserStatus.Offline:
                case UserStatus.Invisible:
                    c = new Color(116, 127, 141);
                    break;
            }

            var builder =
                new EmbedBuilder()
                    .WithThumbnailUrl(target.GetAvatarUrl())
                    .WithColor(c);

            builder.AddField($"**{Format.Sanitize(target.ToString())}**", status);

            if (target.Activity != null) {
                var activityType = target.Activity.Type;
                var activityName = target.Activity.Name;
                var activityDetails = target.Activity.ToString();
                string activity = null;

                switch (target.Activity) {
                    case SpotifyGame spotify:
                        var artists = Format.Sanitize(String.Join(", ", spotify.Artists));
                        var trackTitle = Format.Sanitize(spotify.TrackTitle);
                        var trackUrl = Format.Sanitize(spotify.TrackUrl);
                        var album = Format.Sanitize(spotify.AlbumTitle);
                        var track = $"[{trackTitle}]({trackUrl})";
                        activity = $"{artists} - {track} (on {album})";
                        builder.AddField($"**Listening to**", activity);
                        break;

                    case Game game:
                        activity = $"{activityType.ToString()} {target.Activity.Name}";
                        builder.AddField("**Playing**", Format.Sanitize(activity));
                        break;

                    default:
                        builder.AddField($"**Doing**", Format.Sanitize(target.Activity.ToString()));
                        break;
                }

            }

            var embed = builder
                .AddField(discordField)
                .AddField(serverField)
                .AddField(rolesField)
                .WithFooter(footer)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("serverinfo", RunMode = RunMode.Async)]
        [Summary("Display some information about this server")]
        [RequireContext(ContextType.Guild)]
        public async Task Serverinfo() {
            var user = Context.User as IGuildUser;
            var guild = Context.Guild;

            var builder =
                new EmbedBuilder()
                    .WithThumbnailUrl(guild.IconUrl);
            var created = Format.Sanitize(guild.CreatedAt.ToString("dd MM yyyy HH:mm"));
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
                .AddField("**Region**", Format.Sanitize(guild.VoiceRegionId), true)
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
        [RequireBotAdmin]
        public async Task Die() {
            await Task.Run(() => Environment.Exit(0));
        }
    }
}

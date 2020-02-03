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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using Discord;
using System.Windows.Input;

namespace Morgana {
    public class HelpModule : ModuleBase<SocketCommandContext> {
        public CommandService Commands { get; set; }
        public StorageContext DB { get; set; }
        public IServiceProvider Services { get; set; }

        [Command("help")]
        [Summary("Ask me about my commands")]
        public async Task Help([Remainder] string command = null) {
            var (text, embed) = await ShowHelp(Services, Context, DB, Commands, command);
            await ReplyAsync(message: text, embed: embed);
        }

        public static async Task<bool> UserCanRunCommand(IServiceProvider services, ICommandContext ctx, CommandInfo command) {
            var res = await command.CheckPreconditionsAsync(ctx, services);
            return res.IsSuccess;
        }

        public static async Task<bool> UserCanRunModule(IServiceProvider services, ICommandContext ctx, ModuleInfo mod) {
            foreach (var cmd in mod.Commands)
                if (await UserCanRunCommand(services, ctx, cmd))
                    return true;

            foreach (var submod in mod.Submodules)
                if (await UserCanRunModule(services, ctx, submod))
                    return true;

            return false;
        }

        public static async Task<(string, Embed)> ShowHelp(IServiceProvider services, SocketCommandContext ctx, StorageContext DB, CommandService cmds, string command) { 
            string reply;
            string prefix = "";
            GuildConfig gcfg = null;
            EmbedBuilder embed = null;

            if (ctx.Guild != null) {
                gcfg = DB.GetGuild(ctx.Guild);
                prefix = await gcfg.GetCommandPrefixAsync() ?? "~";
            }

            var muser = MentionUtils.MentionUser(ctx.User.Id);

            if (command == null) {
                reply = $"{muser}, I know about the following commands; use `{prefix}help <command>` for more details:";

                foreach (var mod in cmds.Modules) {
                    if (mod.Parent != null)
                        continue;
                    if (!await UserCanRunModule(services, ctx, mod))
                        continue;

                    if (mod.Group != null)
                        reply += $"\n`{prefix}{mod.Group}` - {mod.Summary}.";
                    else
                        foreach (var cmd in mod.Commands)
                            if (await UserCanRunCommand(services, ctx, cmd))
                                reply += $"\n`{prefix}{cmd.Name}` - {cmd.Summary}.";
                }

                return (reply, null);
            }

            if (command.StartsWith(prefix))
                command = command.Substring(prefix.Length);

            var sr = cmds.Search(command);

            if (sr.Error != null) {
                var bits = command.Split(" ");

                ModuleInfo minfo;
                try {
                    minfo = cmds.Modules.First(m => m.Name == bits[0]);
                } catch (InvalidOperationException) {
                    return ("{muser}, I don't recognise that command.", null);
                }
                var matched = minfo.Name;

                for (int i = 1; i < bits.Length; i++) {
                    try {
                        minfo = minfo.Submodules.First(m => m.Name == bits[i]);
                        matched += " " + bits[i];
                    } catch (InvalidOperationException) {
                        return ($"{muser}, I couldn't find any matching commands for \"{command}\"; matched: \"{matched}\"", null);
                    }
                }

                embed = new EmbedBuilder();
                embed.AddField("**Module**", $"{prefix}{matched} - {minfo.Summary}");

                string commands = "";
                foreach (var mod in minfo.Submodules)
                    if (await UserCanRunModule(services, ctx, mod))
                        commands += $"\n`{prefix}{matched} {mod.Name}` - {mod.Summary}.";
                foreach (var cmd in minfo.Commands)
                    if (await UserCanRunCommand(services, ctx, cmd))
                        commands += $"\n`{prefix}{matched} {cmd.Name}` - {cmd.Summary}.";
                embed.AddField("**Commands**", commands);

                return (muser, embed.Build());
            }

            var info = sr.Commands[0];
            var cmdName = info.Command.Name;

            embed = new EmbedBuilder();

            var module = info.Command.Module;
            while (module != null) {
                if (module.Group != null)
                    cmdName = module.Group + " " + cmdName;
                module = module.Parent;
            }
            cmdName = prefix + cmdName;

            embed.WithTitle($"Command help: {cmdName}");

            var parms = new List<string>();
            string parmHelp = "";
            foreach (var parm in info.Command.Parameters) {
                string astext;

                if (parm.IsOptional)
                    astext = $"[{parm.Name}]";
                else
                    astext = $"<{parm.Name}>";
                parms.Add(astext);

                var req = parm.IsOptional ? "(optional)" : "(required)";
                parmHelp += $"\n`{parm.Name}`: {req} {parm.Summary}.";
            }
            var parmstr = String.Join(" ", parms);
            string syntax = "";

            if (parmstr.Length == 0)
                syntax = $"`{cmdName}`";
            else
                syntax = $"`{cmdName} {parmstr}`";
            embed.AddField("**Syntax**", syntax);

            string summary = info.Command.Summary;
            embed.AddField("**Description**", $"{summary}.");

            if (parmHelp.Length > 0)
                embed.AddField("**Parameters**", parmHelp);

            if (info.Command.Remarks is string remarks)
                embed.AddField("**Remarks**", remarks.Replace("<cmd>", cmdName).Replace("<muser>", muser));

            return (muser, embed.Build());
        }
    }
}

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

        public async Task<bool> UserCanRunCommand(ICommandContext ctx, CommandInfo command) {
            var res = await command.CheckPreconditionsAsync(ctx, Services);
            return res.IsSuccess;
        }

        public async Task<bool> UserCanRunModule(ICommandContext ctx, ModuleInfo mod) {
            foreach (var cmd in mod.Commands)
                if (await UserCanRunCommand(ctx, cmd))
                    return true;

            foreach (var submod in mod.Submodules)
                if (await UserCanRunModule(ctx, submod))
                    return true;

            return false;
        }

        [Command("help")]
        [Summary("Ask me about my commands")]
        public async Task Help([Remainder] string command = null) {
            string reply;
            string prefix = "";
            GuildConfig gcfg = null;
            EmbedBuilder embed = null;

            if (Context.Guild != null) {
                gcfg = DB.GetGuild(Context.Guild);
                prefix = await gcfg.GetCommandPrefixAsync() ?? "~";
            }

            if (command == null) {
                reply = $"I know about the following commands; use `{prefix}help <command>` for more details:";

                foreach (var mod in Commands.Modules) {
                    if (mod.Parent != null)
                        continue;
                    if (!await UserCanRunModule(Context, mod))
                        continue;

                    if (mod.Group != null)
                        reply += $"\n`{prefix}{mod.Group}` - {mod.Summary}.";
                    else
                        foreach (var cmd in mod.Commands)
                            if (await UserCanRunCommand(Context, cmd))
                                reply += $"\n`{prefix}{cmd.Name}` - {cmd.Summary}.";
                }

                await ReplyAsync(reply);
                return;
            }

            if (command.StartsWith(prefix))
                command = command.Substring(prefix.Length);

            var sr = Commands.Search(command);

            if (sr.Error != null) {
                var bits = command.Split(" ");

                ModuleInfo minfo;
                try {
                    minfo = Commands.Modules.First(m => m.Name == bits[0]);
                } catch (InvalidOperationException) {
                    await ReplyAsync("I don't recognise that command.");
                    return;
                }
                var matched = minfo.Name;

                for (int i = 1; i < bits.Length; i++) {
                    try {
                        minfo = minfo.Submodules.First(m => m.Name == bits[i]);
                        matched += " " + bits[i];
                    } catch (InvalidOperationException) {
                        await ReplyAsync($"I couldn't find any matching commands for \"{command}\"; matched: \"{matched}\"");
                        return;
                    }
                }

                embed = new EmbedBuilder();
                embed.AddField("**Module**", $"{prefix}{matched} - {minfo.Summary}");

                string commands = "";
                foreach (var mod in minfo.Submodules)
                    if (await UserCanRunModule(Context, mod))
                        commands += $"\n`{prefix}{matched} {mod.Name}` - {mod.Summary}.";
                foreach (var cmd in minfo.Commands)
                    if (await UserCanRunCommand(Context, cmd))
                        commands += $"\n`{prefix}{matched} {cmd.Name}` - {cmd.Summary}.";
                embed.AddField("**Commands**", commands);

                await ReplyAsync(embed: embed.Build());
                return;
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

            await ReplyAsync(embed: embed.Build());
        }
    }
}

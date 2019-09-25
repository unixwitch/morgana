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

namespace Morgana {
    public class HelpModule : ModuleBase<SocketCommandContext> {
        public CommandService Commands { get; set; }
        public Storage Vars { get; set; }

        [Command("help")]
        [Summary("Ask me about my commands")]
        public async Task Help([Remainder] string command = null) {
            string reply;
            string prefix = "";
            if (Context.Guild != null) {
                var gcfg = Vars.GetGuild(Context.Guild);
                prefix = gcfg.CommandPrefix;
            }

            if (command == null) {
                reply = $"I know about the following commands; use `{prefix}help <command>` for more details:";

                foreach (var mod in Commands.Modules) {
                    if (mod.Group != null)
                        reply += $"\n`{prefix}{mod.Group}` - {mod.Summary}";
                    else
                        foreach (var cmd in mod.Commands)
                            reply += $"\n`{prefix}{cmd.Name}` - {cmd.Summary}";
                }

                await ReplyAsync(reply);
                return;
            }

            var sr = Commands.Search(command);

            if (sr.Error != null) {
                var bits = command.Split(" ");

                ModuleInfo minfo = Commands.Modules.First(m => m.Name == bits[0]);
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

                reply = $"**Module**: `{prefix}{matched}` - {minfo.Summary}";

                reply += "\n**Commands**:";
                foreach (var mod in minfo.Submodules)
                    reply += $"\n`{prefix}{matched} {mod.Name}` - {mod.Summary}";
                foreach (var cmd in minfo.Commands)
                    reply += $"\n`{prefix}{matched} {cmd.Name}` - {cmd.Summary}";

                await ReplyAsync(reply);
                return;
            }

            var info = sr.Commands[0];
            var cmdName = info.Command.Name;

            var module = info.Command.Module;
            while (module != null) {
                if (module.Group != null)
                    cmdName = module.Group + " " + cmdName;
                module = module.Parent;
            }
            cmdName = prefix + cmdName;

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
                parmHelp += $"\n`{parm.Name}`: {req} {parm.Summary}";
            }
            var parmstr = String.Join(" ", parms);

            string summary = info.Command.Summary;

            if (parmstr.Length == 0)
                reply = $"`{cmdName}`";
            else
                reply = $"`{cmdName} {parmstr}`";

            reply += $"\n**Description**: {summary}";

            if (parmHelp.Length > 0) {
                reply += "\n**Parameters**:";
                reply += parmHelp;
            }

            await ReplyAsync(reply);
        }
    }
}

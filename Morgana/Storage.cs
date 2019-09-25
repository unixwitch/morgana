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
using System.Reflection;

using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord;

namespace Morgana {
    public class GuildConfig {
        public HashSet<ulong> Admins { get; set; } = new HashSet<ulong>();
        public HashSet<ulong> ManagedRoles { get; set; } = new HashSet<ulong>();
        public string CommandPrefix { get; set; }
        public HashSet<string> Badwords { get; set; } = new HashSet<string>();
        public bool BadwordsEnabled { get; set; } = true;
        public string BadwordsMessage { get; set; }
        public bool AuditEnabled { get; set; } = false;
        public ulong AuditChannel { get; set; } = 0;

        public bool AdminAdd(IGuildUser user) {
            return Admins.Add(user.Id);
        }

        public bool AdminRemove(IGuildUser user) {
            return Admins.Remove(user.Id);
        }

        public bool IsAdmin(IGuildUser user) {
            return Admins.Contains(user.Id);
        }

        public bool ManagedRoleAdd(IRole role) {
            return ManagedRoles.Add(role.Id);
        }

        public bool ManagedRoleRemove(IRole role) {
            return ManagedRoles.Remove(role.Id);
        }

        public bool IsManagedrole(IRole role) {
            return ManagedRoles.Contains(role.Id);
        }

        public bool BadwordAdd(string word) {
            return Badwords.Add(word);
        }

        public bool BadwordRemove(string word) {
            return Badwords.Remove(word);
        }

        public bool IsBadword(string word) {
            return Badwords.Contains(word);
        }
    }

    public class DataStore {
        public Dictionary<ulong, GuildConfig> Guilds { get; set; } = new Dictionary<ulong, GuildConfig>();
    }

    public class Storage {
        DataStore store = new DataStore();

        public Storage() {
            Load();
        }

        public void Save() {
            File.WriteAllText("vars.json", JsonConvert.SerializeObject(store));
        }

        public void Load() {
            if (File.Exists("vars.json"))
                store = JsonConvert.DeserializeObject<DataStore>(File.ReadAllText("vars.json"));
        }

        /*
         * Return the config for a specific guild.
         */
        public GuildConfig GetGuild(IGuild guild) {
            if (store.Guilds.TryGetValue(guild.Id, out GuildConfig cfg))
                return cfg;

            cfg = new GuildConfig();
            store.Guilds.Add(guild.Id, cfg);
            return cfg;
        }
    }
}

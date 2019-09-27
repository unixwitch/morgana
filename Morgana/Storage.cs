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
using System.Threading;

namespace Morgana {
    public class GuildConfig {
        [JsonProperty("Admins")]
        private HashSet<ulong> _admins = new HashSet<ulong>();
        [JsonIgnore]
        public HashSet<ulong> AdminList {
            get {
                lock (_mutex) {
                    return new HashSet<ulong>(_admins);
                }
            }
        }


        [JsonProperty("ManagedRoles")]
        private HashSet<ulong> _managedRoles = new HashSet<ulong>();
        [JsonIgnore]
        public HashSet<ulong> ManagedRoleList {
            get {
                lock (_mutex) {
                    return _managedRoles;
                }
            }
        }

        [JsonProperty("CommandPrefix")]
        private string _commandPrefix;
        [JsonIgnore]
        public string CommandPrefix {
            get {
                lock (_mutex) {
                    return _commandPrefix;
                }
            }
            set {
                lock (_mutex) {
                    _commandPrefix = value;
                }
            }
        }

        [JsonProperty("Badwords")]
        private HashSet<string> _badwords = new HashSet<string>();
        [JsonIgnore]
        public HashSet<string> BadwordsList {
            get {
                lock (_mutex) {
                    return new HashSet<string>(_badwords);
                }
            }
        }

        [JsonProperty("BadwordsEnabled")]
        private bool _badwordsEnabled = true;
        [JsonIgnore]
        public bool BadwordsEnabled {
            get {
                lock (_mutex) {
                    return _badwordsEnabled;
                }
            }
            set {
                lock (_mutex) {
                    _badwordsEnabled = value;
                }
            }
        }

        [JsonProperty("BadwordsMessage")]
        private string _badwordsMessage;
        [JsonIgnore]
        public string BadwordsMessage {
            get {
                lock (_mutex) {
                    return _badwordsMessage;
                }
            }
            set {
                lock (_mutex) {
                    _badwordsMessage = value;
                }
            }
        }

        [JsonProperty("AuditEnabled")]
        private bool _auditEnabled = false;
        [JsonIgnore]
        public bool AuditEnabled {
            get {
                lock (_mutex) {
                    return _auditEnabled;
                }
            }
            set {
                lock (_mutex) {
                    _auditEnabled = value;
                }
            }
        }

        [JsonProperty("AuditChannel")]
        private ulong _auditChannel = 0;

        [JsonIgnore]
        public ulong AuditChannel {
            get {
                lock (_mutex) {
                    return _auditChannel;
                }
            }
            set {
                lock (_mutex) {
                    _auditChannel = value;
                }
            }
        }

        [JsonProperty("PinFrom")]
        private ulong _pinFrom = 0;
        [JsonIgnore]
        public ulong PinFrom {
            get {
                lock (_mutex) {
                    return _pinFrom;
                }
            }
            set {
                lock (_mutex) {
                    _pinFrom = value;
                }
            }
        }

        [JsonProperty("PinTo")]
        private ulong _pinTo = 0;
        [JsonIgnore]
        public ulong PinTo {
            get {
                lock (_mutex) {
                    return _pinTo;
                }
            }
            set {
                lock (_mutex) {
                    _pinTo = value;
                }
            }
        }

        [JsonProperty("DoPins")]
        private bool _doPins = false;
        [JsonIgnore]
        public bool DoPins {
            get {
                lock (_mutex) {
                    return _doPins;
                }
            }
            set {
                lock (_mutex) {
                    _doPins = value;
                }
            }
        }

        internal Mutex _mutex = new Mutex();

        public bool AdminAdd(ulong id) {
            lock (_mutex) {
                return _admins.Add(id);
            }
        }
        public bool AdminAdd(IGuildUser user) {
            return AdminAdd(user.Id);
        }

        public bool AdminRemove(ulong id) {
            lock (_mutex) {
                return _admins.Remove(id);
            }
        }
        public bool AdminRemove(IGuildUser user) {
            return AdminRemove(user.Id);
        }

        public bool IsAdmin(ulong id) {
            lock (_mutex) {
                return _admins.Contains(id);
            }
        }
        public bool IsAdmin(IGuildUser user) {
            return IsAdmin(user.Id);
        }

        public bool ManagedRoleAdd(IRole role) {
            lock (_mutex) {
                return _managedRoles.Add(role.Id);
            }
        }

        public bool ManagedRoleRemove(IRole role) {
            lock (_mutex) {
                return _managedRoles.Remove(role.Id);
            }
        }

        public bool IsManagedrole(IRole role) {
            lock (_mutex) {
                return _managedRoles.Contains(role.Id);
            }
        }

        public bool BadwordAdd(string word) {
            lock (_mutex) {
                return _badwords.Add(word);
            }
        }

        public bool BadwordRemove(string word) {
            lock (_mutex) {
                return _badwords.Remove(word);
            }
        }

        public bool IsBadword(string word) {
            lock (_mutex) {
                return _badwords.Contains(word);
            }
        }
    }

    public class DataStore {
        public Dictionary<ulong, GuildConfig> Guilds { get; set; } = new Dictionary<ulong, GuildConfig>();
    }

    public class Storage {
        DataStore store = new DataStore();
        private Mutex _mutex = new Mutex();

        public Storage() {
            Load();
        }

        public void Save() {
            lock (_mutex) {
                File.WriteAllText("vars.json", JsonConvert.SerializeObject(store));
            }
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

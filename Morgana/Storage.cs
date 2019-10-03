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
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Design;
using System.ComponentModel.DataAnnotations.Schema;

namespace Morgana {
    public class StorageContext : DbContext {
        public DbSet<GuildAdmin> GuildAdmins { get; set; }
        public DbSet<GuildOption> GuildOptions { get; set; }
        public DbSet<GuildManagedRole> GuildManagedRoles { get; set; }
        public DbSet<GuildBadword> GuildBadwords { get; set; }

        public StorageContext(DbContextOptions<StorageContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb) {
            mb.Entity<GuildAdmin>().HasIndex(ga => new { ga.GuildId, ga.AdminId }).IsUnique();
            mb.Entity<GuildOption>().HasIndex(go => new { go.GuildId, go.Option }).IsUnique();
            mb.Entity<GuildManagedRole>().HasIndex(gmr => new { gmr.GuildId, gmr.RoleId }).IsUnique();
            mb.Entity<GuildBadword>().HasIndex(gbw => new { gbw.GuildId, gbw.Badword }).IsUnique();
        }
    }

    public class StorageContextFactory : IDesignTimeDbContextFactory<StorageContext> {
        public StorageContext CreateDbContext(string[] args) {
            var _config = Configuration.Load(Path.Join(Directory.GetCurrentDirectory(), "config.ini"));
            var options = new DbContextOptionsBuilder<StorageContext>();
            _config.ConfigureDb(options);
            return new StorageContext(options.Options);
        }
    }

    public class GuildAdmin {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(20)]
        public string AdminId { get; set; }
    }

    public class GuildOption {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(64)]
        public string Option { get; set; }

        [Required]
        [MaxLength(256)]
        public string Value { get; set; }
    }

    public class GuildManagedRole {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(20)]
        public string RoleId { get; set; }
    }

    public class GuildBadword {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuildId { get; set; }

        [Required]
        [MaxLength(64)]
        public string Badword { get; set; }
    }

    public class GuildConfig {
        private StorageContext _db;
        private IGuild _guild;

        public GuildConfig(StorageContext db, IGuild guild) {
            _db = db;
            _guild = guild;
        }

        /*
         * Internal option handling.
         */
        protected async Task<string> GetOptionAsync(string option) {
            try {
                var opt = await _db.GuildOptions.Where(opt => opt.GuildId == _guild.Id.ToString() && opt.Option == option).SingleAsync();
                return opt.Value;
            } catch (InvalidOperationException) {
                return null;
            }
        }

        protected async Task SetOptionAsync(string option, string value) {
            GuildOption opt;

            try {
                opt = _db.GuildOptions.Where(opt => opt.GuildId == _guild.Id.ToString() && opt.Option == option).Single();
                opt.Value = value;
            } catch (InvalidOperationException) {
                opt = new GuildOption { GuildId = _guild.Id.ToString(), Option = option, Value = value };
                _db.GuildOptions.Add(opt);
            }

            await _db.SaveChangesAsync();
        }

        protected async Task<bool> GetOptionBoolOrTrue(string option) {
            var v = await GetOptionAsync(option);
            if (v == null)
                return true;

            if (v != null && v == "true")
                return true;

            return false;
        }

        protected async Task<bool> GetOptionBoolOrFalse(string option) {
            var v = await GetOptionAsync(option);
            if (v == null)
                return false;

            if (v != null && v == "true")
                return true;

            return false;
        }

        protected Task SetOptionBoolAsync(string option, bool value) {
            return SetOptionAsync(option, value ? "true" : "false");
        }

        /*
         * Admins.
         */
        public Task<List<ulong>> GetAdminsAsync() {
            return
                _db.GuildAdmins
                    .Where(ga => ga.GuildId == _guild.Id.ToString())
                    .Select(ga => ulong.Parse(ga.AdminId))
                    .ToListAsync();
        }

        public async Task<bool> AdminAddAsync(ulong id) {
            if (await IsAdminAsync(id))
                return false;

            var o = new GuildAdmin { GuildId = _guild.Id.ToString(), AdminId = id.ToString() };
            _db.GuildAdmins.Add(o);
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<bool> AdminAddAsync(IGuildUser user) => AdminAddAsync(user.Id);

        public async Task<bool> AdminRemoveAsync(ulong id) {
            GuildAdmin ga = null;

            try {
                ga = await _db.GuildAdmins.Where(a => a.GuildId == _guild.Id.ToString() && a.AdminId == id.ToString()).SingleAsync();
            } catch (InvalidOperationException) {
                return false;
            }

            _db.GuildAdmins.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<bool> AdminRemoveAsync(IUser user) => AdminRemoveAsync(user.Id);

        public async Task<bool> IsAdminAsync(ulong id) {
            try {
                await _db.GuildAdmins.Where(a => a.GuildId == _guild.Id.ToString() && a.AdminId == id.ToString()).SingleAsync();
                return true;
            } catch (InvalidOperationException) {
                return false;
            }
        }

        public Task<bool> IsAdminAsync(IUser user) => IsAdminAsync(user.Id);

        /*
         * Managed roles.
         */
        public Task<List<IRole>> GetManagedRolesAsync() {
            return
                _db.GuildManagedRoles
                    .Where(mr => mr.GuildId == _guild.Id.ToString())
                    .Select(mr => _guild.GetRole(ulong.Parse(mr.RoleId)))
                    .ToListAsync();
        }

        public async Task<bool> ManagedRoleAddAsync(IRole role) {
            if (await IsManagedRoleAsync(role))
                return false;

            var o = new GuildManagedRole { GuildId = _guild.Id.ToString(), RoleId = role.Id.ToString() };
            _db.GuildManagedRoles.Add(o);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ManagedRoleRemoveAsync(IRole role) {
            GuildManagedRole ga = null;

            try {
                ga = await _db.GuildManagedRoles.Where(a => a.GuildId == _guild.Id.ToString() && a.RoleId == role.Id.ToString()).SingleAsync();
            } catch (InvalidOperationException) {
                return false;
            }

            _db.GuildManagedRoles.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsManagedRoleAsync(IRole role) {
            try {
                await _db.GuildManagedRoles.Where(a => a.GuildId == _guild.Id.ToString() && a.RoleId == role.Id.ToString()).SingleAsync();
                return true;
            } catch (InvalidOperationException) {
                return false;
            }
        }

        /*
         * General guild options.
         */
        public Task<string> GetCommandPrefixAsync() => GetOptionAsync("commandprefix");
        public Task SetCommandPrefixAsync(string p) => SetOptionAsync("commandprefix", p);

        /*
         * Badwords filter.
         */
        public Task<List<string>> GetBadwordsAsync() {
            return _db.GuildBadwords.Where(bw => bw.GuildId == _guild.Id.ToString()).Select(bw => bw.Badword).ToListAsync();
        }

        public Task<bool> IsBadwordsEnabledAsync() => GetOptionBoolOrFalse("badwords-enabled");
        public Task SetBadwordsEnabledAsync(bool v) => SetOptionBoolAsync("badwords-enabled", v);

        public Task<string> GetBadwordsMessageAsync() => GetOptionAsync("badwords-message");
        public Task SetBadwordsMessageAsync(string m) => SetOptionAsync("badwords-message", m);

        public async Task<bool> BadwordAddAsync(string w) {
            if (await IsBadwordAsync(w))
                return false;

            var o = new GuildBadword { GuildId = _guild.Id.ToString(), Badword = w };
            _db.GuildBadwords.Add(o);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BadwordRemoveAsync(string w) {
            GuildBadword ga = null;

            try {
                ga = await _db.GuildBadwords.Where(a => a.GuildId == _guild.Id.ToString() && a.Badword == w).SingleAsync();
            } catch (InvalidOperationException) {
                return false;
            }

            _db.GuildBadwords.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsBadwordAsync(string w) {
            try {
                await _db.GuildBadwords.Where(a => a.GuildId == _guild.Id.ToString() && a.Badword == w).SingleAsync();
                return true;
            } catch (InvalidOperationException) {
                return false;
            }
        }

        public async Task<bool> IsAnyBadwordAsync(string[] w) {
            return await _db.GuildBadwords.Where(bw => w.Contains(bw.Badword)).AnyAsync();
        }

        /*
         * Audit log.
         */
        public Task<bool> IsAuditEnabledAsync() => GetOptionBoolOrFalse("audit-enabled");
        public Task SetAuditEnabledAsync(bool v) => SetOptionBoolAsync("audit-enabled", v);

        public async Task<ulong> GetAuditChannelAsync() => ulong.Parse(await GetOptionAsync("audit-channel") ?? "0");
        public Task SetAuditChannelAsync(ulong c) => SetOptionAsync("audit-channel", c.ToString());

        /*
         * Picture pinner.
         */
        public async Task<ulong> GetPinFromAsync() => ulong.Parse(await GetOptionAsync("pin-from") ?? "0");
        public Task SetPinFromAsync(ulong c) => SetOptionAsync("pin-from", c.ToString());

        public async Task<ulong> GetPinToAsync() => ulong.Parse(await GetOptionAsync("pin-to") ?? "0");
        public Task SetPinToAsync(ulong c) => SetOptionAsync("pin-to", c.ToString());

        public Task<bool> IsPinnerEnabledAsync() => GetOptionBoolOrFalse("pin-enabled");
        public Task SetPinnerEnabledAsync(bool v) => SetOptionBoolAsync("pin-enabled", v);
    }

    public class Storage {
        private StorageContext _db;

        public Storage(StorageContext db) {
            _db = db;
        }

        /*
         * Return the config for a specific guild.
         */
        public GuildConfig GetGuild(IGuild guild) {
            return new GuildConfig(_db, guild);
        }
    }
}

﻿/*
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Design;
using System.ComponentModel.DataAnnotations.Schema;
using EFSecondLevelCache.Core;
using EFSecondLevelCache.Core.Contracts;
using Microsoft.Data.SqlClient;
using Npgsql;

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

        public override int SaveChanges() {
            Console.WriteLine("invalidating");
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled = false;
            var result = base.SaveChanges();
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            this.GetService<IEFCacheServiceProvider>().InvalidateCacheDependencies(changedEntityNames);

            return result;
        }
        /*
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            Console.WriteLine("invalidating 2");
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled = false;
            var result = await base.SaveChangesAsync(cancellationToken);
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            this.GetService<IEFCacheServiceProvider>().InvalidateCacheDependencies(changedEntityNames);

            return result;
        }
        */

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
            Console.WriteLine("invalidating 1");
            var changedEntityNames = this.GetChangedEntityNames();

            this.ChangeTracker.AutoDetectChangesEnabled = false;
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            this.GetService<IEFCacheServiceProvider>().InvalidateCacheDependencies(changedEntityNames);

            return result;
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
                var opt = await _db.GuildOptions
                    .Where(opt => opt.GuildId == _guild.Id.ToString() && opt.Option == option)
                    .Cacheable()
                    .SingleAsync();
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
        public async Task<IList<IGuildUser>> GetAdminsAsync() {
            var adminWaits = await _db.GuildAdmins
                    .Where(ga => ga.GuildId == _guild.Id.ToString())
                    .Select(ga => ulong.Parse(ga.AdminId))
                    .Cacheable()
                    .Select(id => _guild.GetUserAsync(id, CacheMode.AllowDownload, null))
                    .ToListAsync();
            return (await Task.WhenAll(adminWaits)).Where(a => a != null).ToList();
        }

        public async Task<bool> AdminAddAsync(IGuildUser user) {
            try {
                var o = new GuildAdmin { GuildId = _guild.Id.ToString(), AdminId = user.Id.ToString() };
                _db.GuildAdmins.Add(o);
                await _db.SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (e.InnerException is PostgresException sqlex && sqlex.SqlState == "23505") {
                return false;
            }
        }

        public async Task<bool> AdminRemoveAsync(IGuildUser user) {
            GuildAdmin ga = null;

            try {
                ga = await _db.GuildAdmins
                    .Where(a => a.GuildId == _guild.Id.ToString() && a.AdminId == user.Id.ToString())
                    .SingleAsync();
            } catch (InvalidOperationException) {
                return false;
            }

            _db.GuildAdmins.Remove(ga);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsAdminAsync(IGuildUser user) {
            try {
                await _db.GuildAdmins
                    .Where(a => a.GuildId == _guild.Id.ToString() && a.AdminId == user.Id.ToString())
                    .Cacheable()
                    .SingleAsync();
                return true;
            } catch (InvalidOperationException) {
                return false;
            }
        }

        /*
         * Managed roles.
         */
        public Task<List<IRole>> GetManagedRolesAsync() {
            return
                _db.GuildManagedRoles
                    .Where(mr => mr.GuildId == _guild.Id.ToString())
                    .Select(mr => _guild.GetRole(ulong.Parse(mr.RoleId)))
                    .Cacheable()
                    .ToListAsync();
        }

        public async Task<bool> ManagedRoleAddAsync(IRole role) {
            try {
                var o = new GuildManagedRole { GuildId = _guild.Id.ToString(), RoleId = role.Id.ToString() };
                _db.GuildManagedRoles.Add(o);
                await _db.SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (e.InnerException is PostgresException sqlex && sqlex.SqlState == "23505") {
                return false;
            }
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
                await _db.GuildManagedRoles
                    .Where(a => a.GuildId == _guild.Id.ToString() && a.RoleId == role.Id.ToString())
                    .Cacheable()
                    .SingleAsync();
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
            return _db.GuildBadwords
                .Where(bw => bw.GuildId == _guild.Id.ToString())
                .Select(bw => bw.Badword)
                .Cacheable()
                .ToListAsync();
        }

        public Task<bool> IsBadwordsEnabledAsync() => GetOptionBoolOrFalse("badwords-enabled");
        public Task SetBadwordsEnabledAsync(bool v) => SetOptionBoolAsync("badwords-enabled", v);

        public Task<string> GetBadwordsMessageAsync() => GetOptionAsync("badwords-message");
        public Task SetBadwordsMessageAsync(string m) => SetOptionAsync("badwords-message", m);

        public async Task<bool> BadwordAddAsync(string w) {
            try {
                var o = new GuildBadword { GuildId = _guild.Id.ToString(), Badword = w };
                _db.GuildBadwords.Add(o);
                await _db.SaveChangesAsync();
                return true;
            } catch (DbUpdateException e) when (e.InnerException is PostgresException sqlex && sqlex.SqlState == "23505") {
                Console.WriteLine(e.ToString());
                return false;
            }
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

        public async Task<bool> IsAnyBadwordAsync(string[] ws) {
            var badwords = await _db.GuildBadwords
                .Where(bw => bw.GuildId == _guild.Id.ToString())
                .Select(bw => bw.Badword)
                .Cacheable()
                .ToListAsync();
            return ws.Any(w => badwords.Contains(w));
        }

        /*
         * Audit log.
         */
        public Task<bool> IsAuditEnabledAsync() => GetOptionBoolOrFalse("audit-enabled");
        public Task SetAuditEnabledAsync(bool v) => SetOptionBoolAsync("audit-enabled", v);

        public async Task<ITextChannel> GetAuditChannelAsync() {
            var chanId = await GetOptionAsync("audit-channel");
            if (chanId == null)
                return null;

            return await _guild.GetTextChannelAsync(ulong.Parse(chanId));
        }

        public Task SetAuditChannelAsync(ITextChannel c) => SetOptionAsync("audit-channel", c.Id.ToString());

        /*
         * Picture pinner.
         */
        public async Task<ITextChannel> GetPinFromAsync() {
            var chanId = await GetOptionAsync("pin-from");
            if (chanId == null)
                return null;

            return await _guild.GetTextChannelAsync(ulong.Parse(chanId));
        }

        public Task SetPinFromAsync(ITextChannel chan) => SetOptionAsync("pin-from", chan.Id.ToString());

        public async Task<ITextChannel> GetPinToAsync() {
            var chanId = await GetOptionAsync("pin-to");
            if (chanId == null)
                return null;

            return await _guild.GetTextChannelAsync(ulong.Parse(chanId));
        }
        public Task SetPinToAsync(ITextChannel chan) => SetOptionAsync("pin-to", chan.Id.ToString());

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

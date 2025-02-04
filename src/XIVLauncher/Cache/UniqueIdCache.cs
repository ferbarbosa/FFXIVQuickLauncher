﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using XIVLauncher.Common;

namespace XIVLauncher.Cache
{
    public class UniqueIdCache
    {
        private static UniqueIdCache _instance;
        public static UniqueIdCache Instance => _instance ??= new UniqueIdCache();

        private const int DAYS_TO_TIMEOUT = 1;

        private List<UniqueIdCacheEntry> _cache;

        public UniqueIdCache()
        {
            Load();
        }

        #region SaveLoad

        private static readonly string ConfigPath = Path.Combine(Paths.RoamingPath, "uidCache.json");

        public void Save()
        {
            File.WriteAllText(ConfigPath,  JsonConvert.SerializeObject(_cache, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            }));
        }

        public void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                _cache = new List<UniqueIdCacheEntry>();
                return;
            }

            _cache = JsonConvert.DeserializeObject<List<UniqueIdCacheEntry>>(File.ReadAllText(ConfigPath), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            }) ?? new List<UniqueIdCacheEntry>();
        }

        public void Reset()
        {
            _cache.Clear();
            Save();
        }

        #endregion

        private void DeleteOldCaches()
        {
            _cache.RemoveAll(entry => (DateTime.Now - entry.CreationDate).TotalDays > DAYS_TO_TIMEOUT);
        }

        public bool HasValidCache(string userName)
        {
            return _cache.Any(entry => entry.UserName == userName && (DateTime.Now - entry.CreationDate).TotalDays <= DAYS_TO_TIMEOUT);
        }

        public (string Uid, int Region, int ExpansionLevel) GetCachedUid(string userName)
        {
            DeleteOldCaches();

            var cache = _cache.FirstOrDefault(entry => entry.UserName == userName && (DateTime.Now - entry.CreationDate).TotalDays <= DAYS_TO_TIMEOUT);

            if(cache == null)
                throw new Exception("Could not find a valid cache.");

            return (cache.UniqueId, cache.Region, cache.ExpansionLevel);
        }

        public void AddCachedUid(string userName, string uid, int region, int expansionLevel)
        {
             _cache.Add(new UniqueIdCacheEntry
             {
                 CreationDate = DateTime.Now,
                 UserName = userName,
                 UniqueId = uid,
                 Region = region,
                 ExpansionLevel = expansionLevel
             });

             Save();
        }
    }
}
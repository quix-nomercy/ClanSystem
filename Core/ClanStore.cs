using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Exiled.API.Features;
using Newtonsoft.Json;

namespace ClanSystem.Core
{
    public sealed class ClanFile
    {
        public int Version { get; set; } = 1;

        public List<Clan> Clans { get; set; } = new List<Clan>();
    }

    public sealed class ClanStore
    {
        private readonly Plugin plugin;
        private readonly object sync = new object();
        private readonly Dictionary<string, Clan> byName = new Dictionary<string, Clan>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Clan>> byMember = new Dictionary<string, List<Clan>>(StringComparer.Ordinal);
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        public ClanStore(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public string FilePath { get; private set; }

        public bool Load()
        {
            FilePath = ResolvePath(plugin.Config.DataFile);
            string directory = Path.GetDirectoryName(FilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            lock (sync)
            {
                byName.Clear();
                byMember.Clear();

                if (!File.Exists(FilePath))
                    return true;

                try
                {
                    string json = File.ReadAllText(FilePath, Encoding.UTF8);
                    if (string.IsNullOrWhiteSpace(json))
                        return true;

                    ClanFile file = JsonConvert.DeserializeObject<ClanFile>(json, settings);
                    if (file == null || file.Clans == null)
                        return true;

                    foreach (Clan clan in file.Clans)
                        Index(clan);

                    Log.Info("[ClanSystem] Loaded " + byName.Count + " clan(s) from " + FilePath + ".");
                    return true;
                }
                catch (Exception exception)
                {
                    Log.Error("[ClanSystem] Could not read " + FilePath + ": " + exception.Message);
                    return false;
                }
            }
        }

        public bool Save()
        {
            if (string.IsNullOrEmpty(FilePath))
                return false;

            string failure = null;

            lock (sync)
            {
                try
                {
                    ClanFile file = new ClanFile { Clans = byName.Values.OrderBy(clan => clan.Name, StringComparer.OrdinalIgnoreCase).ToList() };
                    string temp = FilePath + ".tmp";

                    File.WriteAllText(temp, JsonConvert.SerializeObject(file, settings), Encoding.UTF8);
                    File.Copy(temp, FilePath, true);
                    File.Delete(temp);
                    return true;
                }
                catch (Exception exception)
                {
                    Log.Error("[ClanSystem] Could not write " + FilePath + ": " + exception.Message);
                    failure = exception.Message;
                }
            }

            plugin.NotifyStorageError(failure);
            return false;
        }

        public List<Clan> All()
        {
            lock (sync)
                return byName.Values.ToList();
        }

        public int Count
        {
            get
            {
                lock (sync)
                    return byName.Count;
            }
        }

        public bool Exists(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && TryGetByName(name, out Clan ignored);
        }

        public bool TryGetByName(string name, out Clan clan)
        {
            clan = null;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            lock (sync)
                return byName.TryGetValue(name.Trim(), out clan);
        }

        public Clan GetByMember(string userId)
        {
            lock (sync)
            {
                if (string.IsNullOrEmpty(userId) || !byMember.TryGetValue(userId, out List<Clan> clans) || clans.Count == 0)
                    return null;

                return clans[0];
            }
        }

        public List<Clan> GetMemberships(string userId)
        {
            lock (sync)
            {
                if (string.IsNullOrEmpty(userId) || !byMember.TryGetValue(userId, out List<Clan> clans))
                    return new List<Clan>();

                return clans.ToList();
            }
        }

        public void Add(Clan clan)
        {
            if (clan == null || string.IsNullOrWhiteSpace(clan.Name))
                return;

            lock (sync)
            {
                byName[clan.Name.Trim()] = clan;
                RebuildMemberIndex();
            }
        }

        public void Remove(Clan clan)
        {
            if (clan == null || string.IsNullOrWhiteSpace(clan.Name))
                return;

            lock (sync)
            {
                Clan stored;
                if (byName.TryGetValue(clan.Name.Trim(), out stored) && !ReferenceEquals(stored, clan))
                    return;

                byName.Remove(clan.Name.Trim());
                RebuildMemberIndex();
            }
        }

        private void Index(Clan clan)
        {
            if (clan == null || string.IsNullOrWhiteSpace(clan.Name))
                return;

            if (clan.Members == null)
                clan.Members = new List<string>();

            if (clan.Names == null)
                clan.Names = new Dictionary<string, string>(StringComparer.Ordinal);

            byName[clan.Name.Trim()] = clan;
            RebuildMemberIndex();
        }

        private void RebuildMemberIndex()
        {
            byMember.Clear();

            foreach (Clan clan in byName.Values)
            {
                if (clan.Members == null)
                    continue;

                foreach (string memberId in clan.Members)
                {
                    if (string.IsNullOrEmpty(memberId))
                        continue;

                    if (!byMember.TryGetValue(memberId, out List<Clan> clans))
                    {
                        clans = new List<Clan>();
                        byMember[memberId] = clans;
                    }

                    if (!clans.Contains(clan))
                        clans.Add(clan);
                }
            }
        }

        private static string ResolvePath(string relative)
        {
            string path = string.IsNullOrWhiteSpace(relative) ? "ClanSystem/clans.json" : relative;

            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(Paths.Configs, path.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}

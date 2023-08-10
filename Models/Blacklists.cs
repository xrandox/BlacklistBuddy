using Blish_HUD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Teh.BHUD.Blacklist_Buddy_Module.Utils;

namespace Teh.BHUD.Blacklist_Buddy_Module.Models
{

    public class Blacklists
    {
        
        private static readonly Logger Logger = Logger.GetLogger<BlacklistBuddyModule>();

        public List<BlacklistedPlayer> internalBlacklist = new List<BlacklistedPlayer>();
        private List<BlacklistedPlayer> externalBlacklist = new List<BlacklistedPlayer>();
        public List<BlacklistedPlayer> missingBlacklistedPlayers = new List<BlacklistedPlayer>();
        public int cachedListIndex { get; set; }
        public int totalScam { get; private set; }
        public int totalRMT { get; private set; }
        public int totalGW2E { get; private set; }
        public int totalOther { get; private set; }
        public int totalUnknown { get; private set; }
        public int totalAll { get; private set; }
        public int missingScam { get; private set; }
        public int missingRMT { get; private set; }
        public int missingGW2E { get; private set; }
        public int missingOther { get; private set; }
        public int missingUnknown { get; private set; }
        public int missingAll { get; private set; }
        public double estimatedTime { get; private set; }
        public int totalInternalNames { get; private set; }


        public Blacklists() { cachedListIndex = 0; }

        public async Task LoadAll() {
            LoadInternalList(); 
            await LoadExternalList();
            LoadMissingList();
        }

        private void LoadInternalList()
        {
            List<BlacklistedPlayer> blacklistedPlayers = new List<BlacklistedPlayer>();

            try
            {
                string dir = BlacklistBuddyModule.ModuleInstance.AccountUtil.Path + "\\Blacklist.json";

                if (!File.Exists(dir))
                {
                    internalBlacklist = blacklistedPlayers;
                    totalInternalNames = 0;
                    SaveInternalList();
                    return;
                }

                blacklistedPlayers = JsonSerializer.Deserialize<List<BlacklistedPlayer>>(File.ReadAllText(dir));
                internalBlacklist = blacklistedPlayers;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error reading Blacklist.json");
                internalBlacklist = blacklistedPlayers;
            }

            totalInternalNames = internalBlacklist.Count();
        }

        private async Task LoadExternalList() {
            var newUserList = await RemoteDataUtil.GetBlockedPlayers();

            // If there is an issue getting the new list,
            // we don't want to override the current list with a blank one.
            if (newUserList.Length > externalBlacklist.Count) {
                externalBlacklist = newUserList.ToList();
            }

        }

        public void LoadMissingList() 
        {
            bool includeScam = BlacklistBuddyModule._settingIncludeScam.Value;
            bool includeRMT = BlacklistBuddyModule._settingIncludeRMT.Value;
            bool includeGW2E = BlacklistBuddyModule._settingIncludeGW2E.Value;
            bool includeOther = BlacklistBuddyModule._settingIncludeOther.Value;
            bool includeUnknown = BlacklistBuddyModule._settingIncludeUnknown.Value;

            totalScam = 0;
            totalRMT = 0;
            totalGW2E = 0;
            totalOther = 0;
            totalUnknown = 0;
            totalAll = 0;
            
            missingScam = 0;
            missingRMT = 0;
            missingGW2E = 0;
            missingOther = 0;
            missingUnknown = 0;
            missingAll = 0;

            missingBlacklistedPlayers.Clear();

            List<BlacklistedPlayer> missing = new List<BlacklistedPlayer>();

            foreach (BlacklistedPlayer blp in externalBlacklist) 
            {
                if (!internalBlacklist.Any(x => x.ign == blp.ign))
                {
                    missing.Add(blp);
                }
            }

            foreach (BlacklistedPlayer blacklistedPlayer in missing)
            {
                switch (blacklistedPlayer.reason)
                {
                    case "scam": 
                        totalScam++;
                        if (includeScam) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingScam++; missingAll++; }
                        break;
                    case "rmt": 
                        totalRMT++; 
                        if (includeRMT) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingRMT++; missingAll++; }
                        break;
                    case "gw2e": 
                        totalGW2E++;
                        if (includeGW2E) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingGW2E++; missingAll++; }
                        break;
                    case "other": 
                        totalOther++; 
                        if (includeOther) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingOther++; missingAll++; }
                        break;
                    default: 
                        totalUnknown++; 
                        if (includeUnknown) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingUnknown++; missingAll++; }
                        break;
                }
                totalAll++;
            }

            EstimateTime();
        }

        public void EstimateTime()
        {
            estimatedTime = Math.Round((double)(missingAll * (BlacklistBuddyModule._settingInputBuffer.Value + 100)) / 1000);
        }

        private void SaveInternalList()
        {
            try
            {
                string dir = BlacklistBuddyModule.ModuleInstance.AccountUtil.Path + "\\Blacklist.json";
                File.WriteAllText(dir, JsonSerializer.Serialize(internalBlacklist));
            }
            catch (Exception e)
            {
                Logger.Warn("Error saving Blacklist.json: " + e.ToString());
            }
        }

        /// <summary>
        /// Loads all the lists, then checks if there are any missing from the internal list
        /// </summary>
        /// <returns>True if there are any missing</returns>
        public async Task<bool> HasUpdate()
        {
            await LoadAll();
            return missingBlacklistedPlayers.Any();
        }

        /// <summary>
        /// Adds the missingBlacklistedPlayers list to the internalBlacklist, saves the internal blacklist and reloads all lists
        /// </summary>
        public async Task SyncLists()
        {
            internalBlacklist.AddRange(missingBlacklistedPlayers);
            SaveInternalList();
            LoadMissingList();
            //await LoadAll(); ========================================================================================================================================================================
        }

        /// <summary>
        /// Clears all the lists, including the Blacklist.json, to start fresh
        /// </summary>
        public async Task ResetBlacklists()
        {
            internalBlacklist.Clear();
            missingBlacklistedPlayers.Clear();
            SaveInternalList();
            LoadMissingList();
            //await LoadAll(); ========================================================================================================================================================================
        }

        /// <summary>
        /// Syncs the internal blacklist up until the given blacklisted player, then saves it and reloads all lists
        /// </summary>
        /// <param name="blacklistedPlayer">The last player added to the block list</param>
        public async Task PartialSync(BlacklistedPlayer blacklistedPlayer)
        {
            int index = missingBlacklistedPlayers.IndexOf(blacklistedPlayer);
            internalBlacklist.AddRange(missingBlacklistedPlayers.GetRange(0, index + 1));
            SaveInternalList();
            LoadMissingList();
            //await LoadAll(); ========================================================================================================================================================================
        }

        /// <summary>
        /// Creates a string containing the numbers of new Blacklist IGNs
        /// </summary>
        public string NewNamesLabel()
        {
            string text = "Total New IGNs: " + missingAll;
            if (missingScam != 0) text += "\nNew Scammer IGNs: " + missingScam;
            if (missingRMT != 0) text += "\nNew RMT IGNs: " + missingRMT;
            if (missingGW2E != 0) text += "\nNew GW2E IGNs: " + missingGW2E;
            if (missingOther != 0) text += "\nNew Other IGNs: " + missingOther;
            if (missingUnknown != 0) text += "\nNew Unknown IGNs: " + missingUnknown;
            return text;
        }


        public bool HasMissingNames() { return missingBlacklistedPlayers.Count > 0; }
    }
}

using Blish_HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Teh.BHUD.Blacklist_Buddy_Module.Models;

namespace Teh.BHUD.Blacklist_Buddy_Module.Utils
{

    public class BlacklistService
    {
        private static readonly Logger Logger = Logger.GetLogger<BlacklistBuddyModule>();

        public List<BlacklistedPlayer> internalBlacklist = new List<BlacklistedPlayer>();
        private List<BlacklistedPlayer> externalBlacklist = new List<BlacklistedPlayer>();
        public List<BlacklistedPlayer> missingBlacklistedPlayers = new List<BlacklistedPlayer>();

        private Categories totals;
        private Categories missingTotals;

        public enum UpdateType { Full, Reset, Partial }
        
        public double EstimatedTime { get; private set; }


        public BlacklistService() { }

        /// <summary>
        /// Loads the internal and external list, and then loads the missing list
        /// </summary>
        public async Task LoadAll() {
            Task loadInternal = LoadInternalList(); 
            Task loadExternal = LoadExternalList();
            await Task.WhenAll(loadInternal, loadExternal);

            await LoadMissingList();
        }

        /// <summary>
        /// Loads the internal (the users) blacklist
        /// </summary>
        private async Task LoadInternalList()
        {
            List<BlacklistedPlayer> blacklistedPlayers = new List<BlacklistedPlayer>();

            try
            {
                string dir = BlacklistBuddyModule.ModuleInstance.AccountUtil.Path + "\\Blacklist.json";
                blacklistedPlayers = await FileUtil.LoadFromJson<List<BlacklistedPlayer>>(dir);

                if (blacklistedPlayers.Equals(default(List<BlacklistedPlayer>)))
                {
                    internalBlacklist = blacklistedPlayers;
                    await SaveInternalList();
                    return;
                }

                internalBlacklist = blacklistedPlayers;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error reading Blacklist.json");
                internalBlacklist = blacklistedPlayers;
            }
        }

        /// <summary>
        /// Loads the external (OTC's) blacklist
        /// </summary>
        private async Task LoadExternalList() {
            var newUserList = await RemoteDataUtil.GetBlockedPlayers();

            // If there is an issue getting the new list,
            // we don't want to override the current list with a blank one.
            if (newUserList.Length > externalBlacklist.Count) {
                externalBlacklist = newUserList.ToList();
            }

        }

        public Task LoadMissingList() 
        {
            bool includeScam = BlacklistBuddyModule._settingIncludeScam.Value;
            bool includeRMT = BlacklistBuddyModule._settingIncludeRMT.Value;
            bool includeGW2E = BlacklistBuddyModule._settingIncludeGW2E.Value;
            bool includeOther = BlacklistBuddyModule._settingIncludeOther.Value;
            bool includeUnknown = BlacklistBuddyModule._settingIncludeUnknown.Value;

            totals = new Categories();
            missingTotals = new Categories();

            // Clear the existing 
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
                        totals.Scam.Inc();
                        if (includeScam) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingTotals.Scam.Inc(); }
                        break;
                    case "rmt": 
                        totals.RMT.Inc();
                        if (includeRMT) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingTotals.RMT.Inc(); }
                        break;
                    case "gw2e": 
                        totals.GW2E.Inc();
                        if (includeGW2E) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingTotals.GW2E.Inc(); }
                        break;
                    case "other":
                        totals.Other.Inc(); 
                        if (includeOther) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingTotals.Other.Inc(); }
                        break;
                    default: 
                        totals.Unknown.Inc();
                        if (includeUnknown) { missingBlacklistedPlayers.Add(blacklistedPlayer); missingTotals.Unknown.Inc(); }
                        break;
                }
            }

            EstimateTime();
            return Task.CompletedTask;
        }

        public void EstimateTime()
        {
            EstimatedTime = Math.Round((double)(missingTotals.All.Count * (BlacklistBuddyModule._settingInputBuffer.Value + 100)) / 1000);
        }

        private async Task SaveInternalList()
        {
            string dir = BlacklistBuddyModule.ModuleInstance.AccountUtil.Path + "\\Blacklist.json";
            await FileUtil.SaveToJson(dir, internalBlacklist);
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

        public async Task UpdateBlacklists(UpdateType updateType, BlacklistedPlayer blacklistedPlayer = null)
        {
            switch (updateType) 
            {
                case UpdateType.Partial:
                    int index = missingBlacklistedPlayers.IndexOf(blacklistedPlayer);
                    internalBlacklist.AddRange(missingBlacklistedPlayers.GetRange(0, index + 1));
                    break;
                case UpdateType.Reset:
                    internalBlacklist.Clear();
                    missingBlacklistedPlayers.Clear();
                    break;
                default: //UpdateType.Full
                    internalBlacklist.AddRange(missingBlacklistedPlayers);
                    break;
            }

            Task t1 = SaveInternalList();
            Task t2 = LoadMissingList();
            await Task.WhenAll(t1, t2);
        }

        /// <summary>
        /// Creates a string containing the numbers of new Blacklist IGNs
        /// </summary>
        public string NewNamesLabel()
        {
            string text = "Total New IGNs: " + missingTotals.All.Count;
            if (missingTotals.Scam.Count != 0) text += "\nNew Scammer IGNs: " + missingTotals.Scam.Count;
            if (missingTotals.RMT.Count != 0) text += "\nNew RMT IGNs: " + missingTotals.RMT.Count;
            if (missingTotals.GW2E.Count != 0) text += "\nNew GW2E IGNs: " + missingTotals.GW2E.Count;
            if (missingTotals.Other.Count != 0) text += "\nNew Other IGNs: " + missingTotals.Other.Count;
            if (missingTotals.Unknown.Count != 0) text += "\nNew Unknown IGNs: " + missingTotals.Unknown.Count;
            return text;
        }

        public bool HasMissingNames() { return missingBlacklistedPlayers.Count > 0; }

        public int TotalMissing() { return missingBlacklistedPlayers.Count; }
    }
}

using System;
using System.Threading.Tasks;
using Blish_HUD;
using Flurl.Http;
using Teh.BHUD.Blacklist_Buddy_Module.Models;

namespace Teh.BHUD.Blacklist_Buddy_Module
{
    public static class RemoteDataUtil {

        private static readonly Logger Logger = Logger.GetLogger(typeof(RemoteDataUtil));

        private const string BLOCKLIST_URI = "https://bhm.blishhud.com/Teh.BHUD.Blacklist_Buddy_Module/blocklist.json";

        /// <summary>
        /// Returns a list of BlacklistedPlayer's from the hosted json
        /// </summary>
        /// <returns>A list of BlacklistedPlayers</returns>
        public static async Task<BlacklistedPlayer[]> GetBlockedPlayers()
        {
            try {
                return await BLOCKLIST_URI.GetJsonAsync<BlacklistedPlayer[]>();
            } catch (Exception e) {
                Logger.Warn(e, $"Failed to download blocklist from {BLOCKLIST_URI}.");
            }

            return Array.Empty<BlacklistedPlayer>();
        }
    }
}

using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using File = System.IO.File;
using Blish_HUD.Settings;

namespace Teh.BHUD.Blacklist_Buddy_Module.Utils
{
    internal class AccountUtil
    {
        private readonly Logger _logger;
        private readonly Gw2ApiManager _gw2ApiManager;
        private readonly DirectoriesManager _directoriesManager;
        private string path;
        private string accountName;
        public string Path { 
            get { return path ?? _directoriesManager.GetFullDirectoryPath("blacklistbuddy"); }
        }

        /// <summary>
        /// Provides basic account detection utilities
        /// </summary>
        public AccountUtil(Gw2ApiManager gw2ApiManager, DirectoriesManager directoriesManager, Logger logger) 
        {
            _gw2ApiManager = gw2ApiManager;
            _directoriesManager = directoriesManager;
            _logger = logger;
            path = null;
        }

        public SettingValidationResult ValidateSetting(bool setting)
        {
            if (setting && !_gw2ApiManager.HasPermission(TokenPermission.Account))
            {
                ScreenNotification.ShowNotification("Missing API Key For Account-Specific Lists");
                path = null;
                accountName = null;
                return new SettingValidationResult(false, null);
            }
            return new SettingValidationResult(true, null);
        }

        /// <summary>
        /// Changes the blacklist path according to the current account, if the setting is enabled, or reverts back to default if it is disabled
        /// </summary>
        public async Task AccountSettingChanged(bool newSetting)
        {
            // If they turned off account-specific lists, we switch to using the 'main' list. 
            // Right now, don't bring over the changes but maybe I should?
            if (newSetting == false)
            {
                path = null;
                accountName = null;
            }
            // Otherwise, try and get the account name from API
            else 
            {
                await NameFromApi();

                if (path != null)
                {
                    string newPath = path + "\\Blacklist.json";
                    if (!File.Exists(newPath))
                    {
                        var origPath = _directoriesManager.GetFullDirectoryPath("blacklistbuddy") + "\\Blacklist.json";
                        if (File.Exists(origPath))
                        {
                            File.Copy(origPath, newPath);
                        }
                    }
                }
            }

            // Then we need to reload the lists to whatever the new list path is
            await BlacklistBuddyModule.ModuleInstance._blacklists.LoadAll();
        }

        /// <summary>
        /// Attempts to grab the account name from the API until it runs out of remaining attempts
        /// </summary>
        private async Task NameFromApi(int remainingAttempts = 4)
        {
            Account account = null;
            
            try
            {
                // Try to grab from API
                account = await _gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
                ScreenNotification.ShowNotification("Found player account: " + account.Name);
            }
            catch (Exception ex)
            {
                // If fail, wait and try again, unless no attempts left
                if (remainingAttempts > 0) 
                {
                    _logger.Warn("Failed to get account name from API. Trying again in 30 seconds.");
                    await Task.Yield();
                    await Task.Delay(30000);
                    await NameFromApi(remainingAttempts-1);
                } 
                else
                {
                    _logger.Warn(ex, "Failed to get account name from API. No more retries left.");
                }
            }

            // If no account then got nothing to change
            if (account == null) return;

            accountName = account.Name.Replace('.', '-');
            path = _directoriesManager.GetFullDirectoryPath("blacklistbuddy") + "\\" + accountName;
            Directory.CreateDirectory(path);
        }
    }
}

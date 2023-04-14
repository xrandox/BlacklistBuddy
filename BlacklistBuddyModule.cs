using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD.Input;
using Blish_HUD.Controls.Extern;
using Teh.BHUD.Blacklist_Buddy_Module.Models;
using Teh.BHUD.Blacklist_Buddy_Module.Controls;
using Blish_HUD.Controls;

namespace Teh.BHUD.Blacklist_Buddy_Module
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class BlacklistBuddyModule : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger<BlacklistBuddyModule>();

        internal static BlacklistBuddyModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        public static SettingEntry<bool> _settingShowAlertPopup;
        public static SettingEntry<bool> _settingIncludeScam;
        public static SettingEntry<bool> _settingIncludeRMT;
        public static SettingEntry<bool> _settingIncludeGW2E;
        public static SettingEntry<bool> _settingIncludeOther;
        public static SettingEntry<bool> _settingIncludeUnknown;
        public static SettingEntry<int> _settingInputBuffer;
        private PopupWindow _popupWindow;
        private BlacklistCornerIcon _blacklistCornerIcon;
        internal Blacklists _blacklists;

        private volatile bool _doSync = false;
        private double _runningTime;

        [ImportingConstructor]
        public BlacklistBuddyModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            _settingShowAlertPopup = settings.DefineSetting("ShowAlertPopup", false, ()=>"Show alert popup", ()=>"Shows an alert popup when the module detects an update to the list, in addition to the alert on the icon");

            _settingIncludeScam        = settings.DefineSetting("IncludeScam", true, ()=>"Include scammers", ()=>"We recommend always blocking these individuals to protect yourself");
            _settingIncludeRMT         = settings.DefineSetting("IncludeRMT", true, ()=>"Include real money traders (RMT)", ()=>"RMT is against ToS and trading with an RMTer can result in an unwarranted ban by association.");
            _settingIncludeGW2E        = settings.DefineSetting("IncludeGW2E", true, ()=>"Include r/GW2Exchange blacklist", ()=>"GW2Exchange does not list specific reasoning, but they are listed for either breaking ToS, subreddit rules or scamming");
            _settingIncludeOther       = settings.DefineSetting("IncludeOther", true, ()=>"Include individuals blacklisted for other reasons", ()=>"Such as: Gross Misconduct, Horrible Trade Etiquette, other ToS Violations, etc");
            _settingIncludeUnknown     = settings.DefineSetting("IncludeUnknown", true, ()=>"Include individuals blacklist for unknown reasons", ()=>"The reason behind why these names are blacklisted have been lost with time. Still not recommended to trade with them.");

            _settingInputBuffer = settings.DefineSetting("InputBuffer", 350, () => "Input Buffer (Low - High)", () => "Increases the time between adding names. Default: Low. \nWARNING: Raising this slider too high will result in very long sync durations.");
            _settingInputBuffer.SetRange(350, 2000);
            _settingInputBuffer.SettingChanged += delegate { _blacklists.EstimateTime(); };

            //check every time settings changed
            _settingIncludeScam.SettingChanged    += async (s, e) => { await CheckForBlacklistUpdate(false, false); };
            _settingIncludeRMT.SettingChanged     += async (s, e) => { await CheckForBlacklistUpdate(false, false); };
            _settingIncludeGW2E.SettingChanged    += async (s, e) => { await CheckForBlacklistUpdate(false, false); };
            _settingIncludeOther.SettingChanged   += async (s, e) => { await CheckForBlacklistUpdate(false, false); };
            _settingIncludeUnknown.SettingChanged += async (s, e) => { await CheckForBlacklistUpdate(false, false); };
        }

        protected override void Initialize() { }

        protected override async Task LoadAsync() {
            _blacklists = new Blacklists();
            await _blacklists.LoadAll();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            _blacklistCornerIcon = new BlacklistCornerIcon();
            _blacklistCornerIcon._syncMenuItem.Click += delegate { InitiateSync(); };
            _blacklistCornerIcon._updateCheckMenuItem.Click += async (s, args) => { await ForceUpdateCheck(); };
            _blacklistCornerIcon._resetMenuItem.Click += delegate { InitiateReset(); };
            _blacklistCornerIcon._skipUpdateMenuItem.Click += async (s, args) => { await SkipUpdate(); };

            _blacklistCornerIcon.Click += delegate { };

            CheckForBlacklistUpdate(true, true);

            GameService.GameIntegration.Gw2Instance.Gw2LostFocus += delegate { 
                if (_doSync)
                {
                    _doSync = false;
                    _popupWindow.Dispose();
                    MakePauseWindow(1);
                }
            };

            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime)
        {
            _runningTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_runningTime > 1800000) //check every 30 min
            {
                _runningTime = 0;
                CheckForBlacklistUpdate(true, true);
            }
        }

        protected override void Unload()
        {
            _popupWindow?.Dispose();
            _blacklistCornerIcon?.Dispose();
        }

        /// <summary>
        /// Manually checks for any updates to the list and shows a popup according to if there are any
        /// </summary>
        private async Task ForceUpdateCheck()
        {
            await CheckForBlacklistUpdate(false, true);

            if (_blacklists.HasMissingNames())
            {
                _popupWindow = new PopupWindow("Update Available!");
                _popupWindow.ShowUpperLabel("New names found! To start sync process, click the button\n\n" + _blacklists.NewNamesLabel());
                _popupWindow.ShowMiddleButton("Begin Sync");
                _popupWindow.middleButton.Click += delegate { _popupWindow.Dispose(); InitiateSync(); };
            }
            else
            {
                _popupWindow = new PopupWindow("No Updates");
                _popupWindow.ShowLowerLabel("No new names found");
                _popupWindow.ShowMiddleButton("Close");
                _popupWindow.middleButton.Click += delegate { _popupWindow.Dispose(); };
            }
        }

        /// <summary>
        /// Confirmation popup to reset the users internal blacklist
        /// </summary>
        private void InitiateReset()
        {
            _popupWindow = new PopupWindow("Reset Blacklist");
            _popupWindow.ShowUpperLabel("You are about to reset your internal blacklist,\nmeaning you will have to re-sync all names over again.\n\nAre you sure?");
            _popupWindow.ShowLeftButton("Yes, Reset");
            _popupWindow.leftButton.Click += async (s, args) => { _popupWindow.Dispose(); await ResetBlocklist(); };
            _popupWindow.ShowRightButton("Cancel");
            _popupWindow.rightButton.Click += delegate { _popupWindow.Dispose(); };
        }

        /// <summary>
        /// Resets the users internal blacklist
        /// </summary>
        private async Task ResetBlocklist()
        {
            await _blacklists.ResetBlacklists();

            await CheckForBlacklistUpdate(false, false);

            _popupWindow = new PopupWindow("Blacklist Reset");
            _popupWindow.ShowLowerLabel("Your internal blocklist has been reset successfully.");
            _popupWindow.ShowMiddleButton("Close");
            _popupWindow.middleButton.Click += delegate { _popupWindow.Dispose(); };
        }

        /// <summary>
        /// First window to start the sync process
        /// </summary>
        private void InitiateSync()
        {
            _popupWindow = new PopupWindow("Update Blocklist");
            _popupWindow.ShowUpperLabel("Get to a safe spot and close all other windows, then press next\n\n" + _blacklists.NewNamesLabel());
            _popupWindow.ShowMiddleButton("Next");
            _popupWindow.middleButton.Click += delegate { _popupWindow.Dispose(); ConfirmSync(); };
        }

        /// <summary>
        /// Second sync confirmation window, displays estimated time to complete if there are a significant amount of names to sync
        /// </summary>
        private void ConfirmSync()
        {
            _popupWindow = new PopupWindow("Update Blocklist");
            if (_blacklists.missingAll > 30) { _popupWindow.ShowUpperLabel("You are about to sync a lot of names, this will take\nabout " + _blacklists.estimatedTime + " seconds.\n\n"); }
            _popupWindow.ShowLowerLabel("Please remain still and do not alt-tab during the sync process.");
            _popupWindow.ShowLeftButton("Start Sync");
            _popupWindow.leftButton.Click += async delegate { _popupWindow.Dispose(); await SyncNames(); };
            _popupWindow.ShowRightButton("Cancel");
            _popupWindow.rightButton.Click += delegate { _popupWindow.Dispose(); };
        }

        /// <summary>
        /// Main sync task, loops through all players in the current missing player list, pasting them into the players block list
        /// </summary>
        private async Task SyncNames()
        {
            _popupWindow = new PopupWindow("Syncing");
            _popupWindow.ShowBackgroundImage();
            _popupWindow.ShowLeftButton("Pause");
            _popupWindow.leftButton.Click += delegate { _doSync = false; _popupWindow.Dispose(); MakePauseWindow(0); };
            _popupWindow.ShowRightButton("Cancel");
            _popupWindow.rightButton.Click += delegate { _doSync = false; _popupWindow.Dispose(); };
            _popupWindow.ShowUpperLabel("Please do not alt-tab\n");

            int count = _blacklists.missingAll;

            _doSync = true;

            foreach (BlacklistedPlayer blacklistedPlayer in _blacklists.missingBlacklistedPlayers)
            {
                string ign = blacklistedPlayer.ign;
                _popupWindow.ShowName(ign);
                _popupWindow.Subtitle = count + " remaining";

                //copy the name to clipboard, then paste into the text box and press enter
                try
                {
                    bool copyToClipboard = await ClipboardUtil.WindowsClipboardService.SetTextAsync("/block " + ign);
                    if (copyToClipboard)
                    {
                        Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.RETURN);
                        await Task.Delay(50);

                        if (!Gw2MumbleService.Gw2Mumble.UI.IsTextInputFocused)
                        {
                            Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.RETURN);
                            await Task.Delay(50);
                        }
                        
                        Blish_HUD.Controls.Intern.Keyboard.Press(VirtualKeyShort.CONTROL, true);
                        Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.KEY_A, true);
                        await Task.Delay(25);
                        Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.CONTROL, true);
                        await Task.Delay(25);
                        Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.BACK, true);
                        await Task.Delay(25);

                        //paste block command
                        Blish_HUD.Controls.Intern.Keyboard.Press(VirtualKeyShort.CONTROL, true);
                        Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.KEY_V, true);
                        await Task.Delay(25);
                        Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.CONTROL, true);
                        await Task.Delay(25);
                        Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.RETURN, true);

                        //delay for input buffer
                        await Task.Delay(100);
                        count--;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error copying " + ign + "to clipboard: " + e);
                }


                if (!_doSync)
                {
                    await _blacklists.PartialSync(blacklistedPlayer);
                    return;
                }
            }

            await _blacklists.SyncLists();

            _popupWindow.Dispose();

            _popupWindow = new PopupWindow("Sync Complete");
            _popupWindow.ShowLowerLabel("Finished syncing your block list successfully");
            _popupWindow.ShowMiddleButton("Close");
            _popupWindow.middleButton.Click += delegate { _popupWindow.Dispose(); };

            await CheckForBlacklistUpdate(false, false);
        }

        /// <summary>
        /// Creates a pause window for if the user pause in the middle of syncing
        /// </summary>
        private void MakePauseWindow(int reason)
        {
            _popupWindow = new PopupWindow("Syncing Paused");

            string reasonStr = "";

            switch (reason)
            {
                case 0: reasonStr = "Syncing Paused\n\n\nMake sure to the text box is clear before resuming.";
                    break;
                case 1: reasonStr = "Syncing Paused\n\n\nPlease do not alt-tab while syncing is in progress";
                    break;
                default: reasonStr = "Syncing Paused";
                    break;
            }    

            _popupWindow.ShowUpperLabel(reasonStr);
            _popupWindow.ShowLeftButton("Resume");
            _popupWindow.leftButton.Click += async delegate { _popupWindow.Dispose(); await SyncNames(); };
            _popupWindow.ShowRightButton("Cancel");
            _popupWindow.rightButton.Click += delegate { _popupWindow.Dispose(); };
        }

        /// <summary>
        /// Acts as if the player has synced their lists, skipping the pending update
        /// </summary>
        private async Task SkipUpdate()
        {
            await _blacklists.SyncLists();
            await CheckForBlacklistUpdate(false, false);
        }

        /// <summary>
        /// Performs check for any updates, changes corner icon and creates popup if enabled in user settings
        /// </summary>
        /// <param name="showPopup">If true, will show a popup if there is an update available. If false, skips showing an update</param>
        /// <param name="checkForUpdates">If true, will query the remote list to check for updates there. If false, just checks the local lists</param>
        private async Task CheckForBlacklistUpdate(bool showPopup, bool checkForUpdates)
        {
            if (checkForUpdates) { await _blacklists.HasUpdate(); }
            else _blacklists.LoadMissingList();


            if (_blacklists.HasMissingNames())
            {
                if (showPopup && _settingShowAlertPopup.Value)
                {
                    _popupWindow = new PopupWindow("Update Available!");
                    _popupWindow.ShowUpperLabel("There has been an update to the blacklist!\nTo start sync process, click the button\n\n" + _blacklists.NewNamesLabel());
                    _popupWindow.ShowMiddleButton("Begin Sync");
                    _popupWindow.middleButton.Click += delegate { _popupWindow.Dispose(); InitiateSync(); };
                }

                _blacklistCornerIcon.ShowAlert(_blacklists.missingAll);
            }
            else
            {
                _blacklistCornerIcon.ShowNormal();
            }
        }

    }
}

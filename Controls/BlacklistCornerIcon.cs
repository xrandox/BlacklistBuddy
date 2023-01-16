using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teh.BHUD.Blacklist_Buddy_Module.Models;

namespace Teh.BHUD.Blacklist_Buddy_Module.Controls
{
    public class BlacklistCornerIcon : CornerIcon
    {

        private ContextMenuStrip _iconMenu;
        internal ContextMenuStripItem _syncMenuItem;
        internal ContextMenuStripItem _updateCheckMenuItem;
        internal ContextMenuStripItem _resetMenuItem;
        internal ContextMenuStripItem _skipUpdateMenuItem;

        #region Load Static 

        private static Texture2D _blacklistIconAlertTexture;
        private static Texture2D _blacklistIconTexture;

        static BlacklistCornerIcon()
        {
            _blacklistIconAlertTexture = BlacklistBuddyModule.ModuleInstance.ContentsManager.GetTexture("BlacklistAlert.png");
            _blacklistIconTexture = BlacklistBuddyModule.ModuleInstance.ContentsManager.GetTexture("Blacklist.png");
        }

        #endregion

        public BlacklistCornerIcon() : base()
        {
            _iconMenu = new ContextMenuStrip();

            _syncMenuItem = new ContextMenuStripItem("Sync Blocklist") { BasicTooltipText = "Opens the blacklist sync window" };

            _updateCheckMenuItem = new ContextMenuStripItem("Check for Updates") { BasicTooltipText = "Forces the module to perform a check for new blacklist updates" };

            _resetMenuItem = new ContextMenuStripItem("Reset Blacklist") { BasicTooltipText = "Resets the internal blacklist completely, allowing a full resync" };

            _skipUpdateMenuItem = new ContextMenuStripItem("Skip Sync") { BasicTooltipText = "If your blacklist is already up to date, allows you to skip syncing" };

            _iconMenu.AddMenuItem(_syncMenuItem);
            _iconMenu.AddMenuItem(_updateCheckMenuItem);
            _iconMenu.AddMenuItem(_resetMenuItem);
            _iconMenu.AddMenuItem(_skipUpdateMenuItem);

            Icon = _blacklistIconTexture;
            BasicTooltipText = "Blacklist Buddy";
            Parent = GameService.Graphics.SpriteScreen;
            Menu = _iconMenu;
        }

        /// <summary>
        /// Shows an alert on the corner icon including how many new names there are
        /// </summary>
        /// <param name="numNewNames">The number of new names</param>
        public void ShowAlert(int numNewNames)
        {
            this.Icon = _blacklistIconAlertTexture;
            this.BasicTooltipText = "Update Available - " + numNewNames + " names to add - Right-click to open menu";
        }

        /// <summary>
        /// Changes the corner icon back to its default state
        /// </summary>
        public void ShowNormal()
        {
            this.Icon = _blacklistIconTexture;
            this.BasicTooltipText = "Blacklist Buddy";
        }
    }
}

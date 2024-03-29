﻿using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Teh.BHUD.Blacklist_Buddy_Module.Controls
{
    public class PopupWindow : StandardWindow
    {
        //private const int WINDOW_WIDTH = 488;
        //private const int WINDOW_HEIGHT = 250;
        //private const int CONTENT_HEIGHT = 260;

        private Label upperLabel;
        private Label lowerLabel;
        private Label nameLabel;

        private Image backgroundImage;

        public StandardButton leftButton;
        public StandardButton rightButton;
        public StandardButton middleButton;

        #region Load Static

        private static Texture2D windowTexture;
        private static Texture2D windowEmblemTexture;
        private static Texture2D backgroundImageTexture;

        static PopupWindow()
        {
            windowTexture = BlacklistBuddyModule.ModuleInstance.ContentsManager.GetTexture("155960resize.png");
            windowEmblemTexture = BlacklistBuddyModule.ModuleInstance.ContentsManager.GetTexture("1654245.png");
            backgroundImageTexture = BlacklistBuddyModule.ModuleInstance.ContentsManager.GetTexture("156771.png");
        }

        #endregion

        public PopupWindow(string subtitle) : base(windowTexture, new Rectangle(4, 50, 488, 236), new Rectangle(0, 0, 488, 260))
        {
            this.Title = "Blacklist Buddy";
            this.Subtitle = subtitle;
            this.Emblem = windowEmblemTexture;
            this.Location = new Point(125,125);
            this.SavesPosition = true;
            this.Parent = GameService.Graphics.SpriteScreen;
            BuildContents();
            this.Show();
        }

        private void BuildContents()
        {
            upperLabel = new Label()
            {
                Height = 300,
                Width = 450,
                Text = "",
                TextColor = Color.Beige,
                Font = GameService.Content.DefaultFont16,
                ShowShadow = true,
                WrapText = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                Location = new Point(25, -120),
                Visible = false,
                Parent = this,
            };

            lowerLabel = new Label()
            {
                Height = 232,
                Width = 450,
                Text = "",
                TextColor = Color.Beige,
                Font = GameService.Content.DefaultFont16,
                ShowShadow = true,
                WrapText = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                Location = new Point(25, -25),
                Visible = false,
                Parent = this,
            };

            nameLabel = new Label()
            {
                Height = 232,
                Width = 450,
                Text = "",
                TextColor = Color.Beige,
                Font = GameService.Content.DefaultFont16,
                ShowShadow = true,
                WrapText = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                Location = new Point(175, -5),
                Visible = false,
                Parent = this,
            };
            

            backgroundImage = new Image(backgroundImageTexture)
            {
                Location = new Point(116, 52),
                ZIndex = 4,
                Size = new Point(256, 128),
                Visible = false,
                Parent = this,
            };

            leftButton = new StandardButton()
            {
                Text = "",
                Size = new Point(110, 30),
                Location = new Point(107, 180),
                Padding = new Thickness(5, 5, 5, 5),
                Visible = false,
                Parent = this,
            };

            rightButton = new StandardButton()
            {
                Text = "",
                Size = new Point(110, 30),
                Location = new Point(269, 180),
                Padding = new Thickness(5, 5, 5, 5),
                Visible = false,
                Parent = this,
            };

            middleButton = new StandardButton()
            {
                Text = "",
                Size = new Point(150, 30),
                Location = new Point(169, 190),
                Padding = new Thickness(5, 5, 5, 5),
                Visible = false,
                Parent = this,
            };
        }

        public void ShowLowerLabel(string text)
        {
            lowerLabel.Text = text;
            lowerLabel.Show();
        }

        public void ShowUpperLabel(string text)
        {
            upperLabel.Text = text;
            upperLabel.Show();
        }

        public void ShowName(string name)
        {
            nameLabel.Text = name;
            nameLabel.Show();
        }

        public void ShowLeftButton(string text)
        {
            leftButton.Text = text;
            leftButton.Show();
        }

        public void ShowRightButton(string text)
        {
            rightButton.Text = text;
            rightButton.Show();
        }

        public void ShowMiddleButton(string text)
        {
            middleButton.Text = text;
            middleButton.Show();
        }

        public void HideLowerLabel()
        {
            lowerLabel.Hide();
        }

        public void ShowBackgroundImage() { backgroundImage.Show(); }

    }
}

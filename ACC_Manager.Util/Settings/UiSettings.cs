﻿using ACCManager.Util;
using System.Windows;

namespace ACC_Manager.Util.Settings
{
    public class UiSettingsJson : IGenericSettingsJson
    {
        public int SelectedTabIndex;
        public int X;
        public int Y;
    }

    public class UiSettings : AbstractSettingsJson<UiSettingsJson>
    {
        public override string Path => FileUtil.AccManangerSettingsPath;

        public override string FileName => "UI.json";

        public override UiSettingsJson Default()
        {
            var settings = new UiSettingsJson()
            {
                SelectedTabIndex = 0,
                X = (int)SystemParameters.PrimaryScreenWidth / 2,
                Y = (int)SystemParameters.PrimaryScreenHeight / 2
            };

            return settings;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Wipe Info", "dFxPhoeniX", "1.3.4")]
    [Description("Adds the ablity to see wipe cycles")]
    public class WipeInfo : RustPlugin
    {
        private string LastWipe;
        private string NextWipe;
        private const int WipeHourUtc = 19;

        private DateTime lastWipeUtc;
        private DateTime nextWipeUtc;

        Timer announceTimer;

        ////////////////////////////////////////////////////////////
        // Oxide Hooks
        ////////////////////////////////////////////////////////////

        private void Init()
        {
            InitConfig();
        }

        void OnServerInitialized()
        {
            LoadVariables();

            timer.Every(60f, LoadVariables);

            if (AnnounceOnTimer)
            {
                announceTimer = timer.Repeat((AnnounceTimer * 60) * 60, 0, () => BroadcastWipe());
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (AnnounceOnJoin)
            {
                cmdNextWipe(player, "", new string[0]);
            }
        }

        ////////////////////////////////////////////////////////////
        // General Methods
        ////////////////////////////////////////////////////////////

        private DateTime ParseTime(string time) => DateTime.ParseExact(time, DateFormat, CultureInfo.InvariantCulture);

        private string NextWipeDays(string wipeDateStr)
        {
            if (!DateTime.TryParseExact(wipeDateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var wipeDate))
                return "NoDateFound";

            var wipeUtc = new DateTime(wipeDate.Year, wipeDate.Month, wipeDate.Day, WipeHourUtc, 0, 0, DateTimeKind.Utc);
            var nowUtc = DateTime.UtcNow;

            if (wipeUtc.Date == nowUtc.Date)
                return "Today";

            var days = (wipeUtc.Date - nowUtc.Date).Days;
            return string.Format("{0:D2}D ({1})", days, wipeDateStr);
        }

        private void LoadVariables()
        {
            var nowUtc = DateTime.UtcNow;

            var firstDayThisMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1);

            var firstThursdayThisMonthUtc = GetFirstThursday(firstDayThisMonth).AddHours(WipeHourUtc);
            var firstThursdayNextMonthUtc = GetFirstThursday(firstDayThisMonth.AddMonths(1)).AddHours(WipeHourUtc);
            var firstThursdayPrevMonthUtc = GetFirstThursday(firstDayThisMonth.AddMonths(-1)).AddHours(WipeHourUtc);

            if (nowUtc < firstThursdayThisMonthUtc)
            {
                lastWipeUtc = firstThursdayPrevMonthUtc;
                nextWipeUtc = firstThursdayThisMonthUtc;
            }
            else
            {
                lastWipeUtc = firstThursdayThisMonthUtc;
                nextWipeUtc = firstThursdayNextMonthUtc;
            }

            LastWipe = lastWipeUtc.ToString(DateFormat);
            NextWipe = nextWipeUtc.ToString(DateFormat);
        }

        private DateTime GetFirstThursday(DateTime firstDayOfMonth)
        {
            var d = firstDayOfMonth.Date;
            while (d.DayOfWeek != DayOfWeek.Thursday)
                d = d.AddDays(1);
            return d;
        }


        private void BroadcastWipe()
        {
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (NextWipeDays(NextWipe) == "Today")
                {
                    SendReply(p, string.Format(msg("MapWipeToday", p.UserIDString), LastWipe, NextWipeDays(NextWipe)));
                }
                else
                {
                    SendReply(p, string.Format(msg("MapWipe", p.UserIDString), LastWipe, NextWipeDays(NextWipe)));
                }
            }
        }

        private string msg(string key, string id = null, params object[] args)
        {
            string message = id == null ? RemoveFormatting(lang.GetMessage(key, this, id)) : lang.GetMessage(key, this, id);

            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private string RemoveFormatting(string source)
        {
            return source.Contains(">") ? Regex.Replace(source, "<.*?>", string.Empty) : source;
        }

        ////////////////////////////////////////////////////////////
        // Commands
        ////////////////////////////////////////////////////////////

        [ChatCommand("wipe")]
        private void cmdNextWipe(BasePlayer player, string command, string[] args)
        {
            if (NextWipeDays(NextWipe) == "Today")
            {
                SendReply(player, string.Format(msg("MapWipeToday", player.UserIDString), LastWipe, NextWipeDays(NextWipe)));
            }
            else
            {
                SendReply(player, string.Format(msg("MapWipe", player.UserIDString), LastWipe, NextWipeDays(NextWipe)));
            }
        }

        [ConsoleCommand("wipe")]
        private void cmdGetWipe(ConsoleSystem.Arg arg)
        {
            if (NextWipeDays(NextWipe) == "Today")
            {
                SendReply(arg, string.Format(msg("MapWipeToday"), LastWipe, NextWipeDays(NextWipe)));
            }
            else
            {
                SendReply(arg, string.Format(msg("MapWipe"), LastWipe, NextWipeDays(NextWipe)));
            }
        }

        ////////////////////////////////////////////////////////////
        // Configs
        ////////////////////////////////////////////////////////////

        private bool ConfigChanged;
        private string DateFormat;
        private bool AnnounceOnJoin;
        private bool AnnounceOnTimer;
        private int AnnounceTimer;

        protected override void LoadDefaultConfig() => PrintWarning("Generating default configuration file...");

        private void InitConfig()
        {
            DateFormat = GetConfig("MM/dd/yyyy", "Date format");
            AnnounceOnJoin = GetConfig(false, "Announce on join");
            AnnounceOnTimer = GetConfig(false, "Announce on timer");
            AnnounceTimer = GetConfig(3, "Announce timer");

            if (ConfigChanged)
            {
                PrintWarning("Updated configuration file with new/changed values.");
                SaveConfig();
            }
        }

        private T GetConfig<T>(T defaultVal, params string[] path)
        {
            var data = Config.Get(path);
            if (data != null)
            {
                return Config.ConvertValue<T>(data);
            }

            Config.Set(path.Concat(new object[] { defaultVal }).ToArray());
            ConfigChanged = true;
            return defaultVal;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"MapWipe", "Last Map Wipe: <color=#ffae1a>{0}</color>\nTime Until Next Map Wipe: <color=#ffae1a>{1}</color>" },
                {"MapWipeToday", "Last Map Wipe: <color=#ffae1a>{0}</color>\nTime Until Next Map Wipe: <color=#ffae1a>today (19:00 UTC)</color>" }
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"MapWipe", "Ultimul Wipe de Mapă: <color=#ffae1a>{0}</color>\nTimpul până la urmâtorul Wipe de Mapă: <color=#ffae1a>{1}</color>" },
                {"MapWipeToday", "Ultimul Wipe de Mapă: <color=#ffae1a>{0}</color>\nTimpul până la urmâtorul Wipe de Mapă: <color=#ffae1a>astăzi (19:00 UTC)</color>" }
            }, this, "ro");
        }

        ////////////////////////////////////////////////////////////
        // Plugin Hooks
        ////////////////////////////////////////////////////////////

        [HookMethod(nameof(API_GetLastWipe))]
        public string API_GetLastWipe()
        {
            if (string.IsNullOrEmpty(LastWipe) || string.IsNullOrEmpty(NextWipe))
                LoadVariables();

            return LastWipe;
        }

        [HookMethod(nameof(API_GetNextWipe))]
        public string API_GetNextWipe()
        {
            if (string.IsNullOrEmpty(LastWipe) || string.IsNullOrEmpty(NextWipe))
                LoadVariables();

            return NextWipe;
        }

        [HookMethod(nameof(API_GetLastWipeUtc))]
        public DateTime API_GetLastWipeUtc()
        {
            if (lastWipeUtc == default || nextWipeUtc == default)
                LoadVariables();

            return lastWipeUtc;
        }

        [HookMethod(nameof(API_GetNextWipeUtc))]
        public DateTime API_GetNextWipeUtc()
        {
            if (lastWipeUtc == default || nextWipeUtc == default)
                LoadVariables();

            return nextWipeUtc;
        }

        [HookMethod(nameof(API_GetTimeUntilNextWipeSeconds))]
        public int API_GetTimeUntilNextWipeSeconds()
        {
            if (nextWipeUtc == default)
                LoadVariables();

            var seconds = (int)Math.Floor((nextWipeUtc - DateTime.UtcNow).TotalSeconds);
            return seconds < 0 ? 0 : seconds;
        }

        [HookMethod(nameof(API_Refresh))]
        public void API_Refresh()
        {
            LoadVariables();
        }

    }
}
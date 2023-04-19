using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Wipe Info", "dFxPhoeniX", "1.2.6")]
    [Description("Adds the ablity to see wipe cycles")]
    public class WipeInfo : RustPlugin
    {
        #region Fields
        DateTime NextWipeDate;
        Timer announceTimer;
        #endregion

        #region Oxide Hooks 
        void OnServerInitialized()
        {
            LoadVariables();

            LoadWipeDates();

            if (config.AnnounceOnTimer)
            {
                announceTimer = timer.Repeat((config.AnnounceTimer * 60) * 60, 0, ()=> BroadcastWipe()); 
            }
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (config.AnnounceOnJoin)
            {
                cmdNextWipe(player, "", new string[0]);
            }
        }
        #endregion

        #region Functions        
        private string LastWipe = "";
        private string NextWipe = "";
        private DateTime ParseTime(string time) => DateTime.ParseExact(time, config.DateFormat, CultureInfo.InvariantCulture);
        private void LoadWipeDates()
        {
            NextWipeDate = ParseTime(NextWipe);
        }
        private string NextWipeDays(DateTime WipeDate)
        {            
            TimeSpan t = WipeDate.Subtract(DateTime.Now);
            return string.Format(string.Format("{0:D2}D",t.Days));
        }
        private void BroadcastWipe()
        {
            foreach (var p in BasePlayer.activePlayerList)
            {
                SendReply(p, string.Format(MSG("MapWipe", p.UserIDString), LastWipe, NextWipeDays(NextWipeDate)));
			}				
        }
        #endregion

        #region ChatCommands
        [ChatCommand("wipe")]
        private void cmdNextWipe(BasePlayer player, string command, string[] args)
        {
            SendReply(player, string.Format(MSG("MapWipe", player.UserIDString), LastWipe, NextWipeDays(NextWipeDate)));            
        }

        [ConsoleCommand("wipe")]
        private void cmdGetWipe(ConsoleSystem.Arg arg)
        {
            SendReply(arg, string.Format(MSG("MapWipe"), LastWipe, NextWipeDays(NextWipeDate)));
        }

        #endregion

        #region Config        
        private Configuration config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "Date format")]
            public string DateFormat { get; set; } = "MM/dd/yyyy";
            [JsonProperty(PropertyName = "Announce on join")]
            public bool AnnounceOnJoin { get; set; } = false;
            [JsonProperty(PropertyName = "Announce on timer")]
            public bool AnnounceOnTimer { get; set; } = false;
            [JsonProperty(PropertyName = "Announce timer")]
            public int AnnounceTimer { get; set; } = 3;
        }
        private void LoadVariables()
        {
            if (string.IsNullOrEmpty(LastWipe)) {
                DateTime dateTime = DateTime.Now;
                DateTime firstDayMonth = new DateTime(dateTime.Year, dateTime.Month, 1);
                DateTime lastDayMonth = firstDayMonth.AddMonths(1).AddDays(-1);

                List<DateTime> dates = new List<DateTime>();
                for (DateTime day =firstDayMonth.Date; day.Date <= lastDayMonth.Date; day = day.AddDays(1))
                {
                    dates.Add(day);
                }
                var query = dates.Where(d => d.DayOfWeek == DayOfWeek.Thursday).GroupBy(d => d.Month).Select(e => e.Take(3));
                foreach (var item in query)
                {
                    foreach (var date in item)
                    {
                        LastWipe = date.ToString(config.DateFormat);
                        break;
                    }
                }
            }           
            if (string.IsNullOrEmpty(NextWipe)) {
                DateTime dateTime = DateTime.Now;
                DateTime firstDayMonth = new DateTime(dateTime.Year, dateTime.Month, 1);
                DateTime firstDayNextMonth = firstDayMonth.AddMonths(1);
                DateTime lastDayNextMonth = firstDayMonth.AddMonths(2).AddDays(-1);

                List<DateTime> dates = new List<DateTime>();
                for (DateTime day =firstDayNextMonth.Date; day.Date <= lastDayNextMonth.Date; day = day.AddDays(1))
                {
                    dates.Add(day);
                }
                var query = dates.Where(d => d.DayOfWeek == DayOfWeek.Thursday).GroupBy(d => d.Month).Select(e => e.Take(3));
                foreach (var item in query)
                {
                    foreach (var date in item)
                    {
                        NextWipe = date.ToString(config.DateFormat);
                        break;
                    }
                }
            }                   
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Messaging
        private string MSG(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {            
                {"MapWipe", "Last Map Wipe: <color=#ffae1a>{0}</color>\nTime Until Next Map Wipe: <color=#ffae1a>{1}</color>" }
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {            
                {"MapWipe", "Ultimul Wipe de Mapă: <color=#ffae1a>{0}</color>\nTimpul până la urmâtorul Wipe de Mapă: <color=#ffae1a>{1}</color>" }
            }, this, "ro");
        }
        #endregion
    }
}
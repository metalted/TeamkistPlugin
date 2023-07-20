using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;

namespace TeamkistPlugin
{
    public static class TKConfig
    {
        public static ConfigFile config;
        public static int configLabelLength = 38;

        //Titles
        public static string preferencesTitle = "1. Preferences";
        public static string networkTitle = "2. Network";

        public static string preferences_showPlayers = CreateConfigLabel(1, "Show Players");
        public static string preferences_logMessages = CreateConfigLabel(2, "Log Messages");
        public static string preferences_logWarnings = CreateConfigLabel(3, "Log Warnings");
        public static string preferences_logErrors = CreateConfigLabel(4, "Log Errors");

        public static string network_serverIP = CreateConfigLabel(1, "Server IP");
        public static string network_serverPort = CreateConfigLabel(2, "Server Port");

        //Values
        public static bool showPlayers = true;
        public static bool logMessages = false;
        public static bool logWarnings = false;
        public static bool logErrors = false;

        public static string serverIP = "127.0.0.1";
        public static int serverPort = 50000;

        public static string appIdentifier = "Teamkist";

        public static void InitializeConfig(ConfigFile cfg)
        {
            config = cfg;

            ConfigEntry<bool> cfg_showPlayers = config.Bind(preferencesTitle, preferences_showPlayers, showPlayers, "");
            ConfigEntry<bool> cfg_logMessages = config.Bind(preferencesTitle, preferences_logMessages, logMessages, "");
            ConfigEntry<bool> cfg_logWarnings = config.Bind(preferencesTitle, preferences_logWarnings, logWarnings, "");
            ConfigEntry<bool> cfg_logErrors = config.Bind(preferencesTitle, preferences_logErrors, logErrors, "");

            ConfigEntry<string> cfg_serverIP = config.Bind(networkTitle, network_serverIP, serverIP, "");
            ConfigEntry<int> cfg_serverPort = config.Bind(networkTitle, network_serverPort, serverPort, "");

            cfg.SettingChanged += Cfg_SettingChanged;
        }
        public static void ForceReload()
        {
            Cfg_SettingChanged(null, null);
        }

        private static void Cfg_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            try
            {
                showPlayers = (bool)config[preferencesTitle, preferences_showPlayers].BoxedValue;
                logMessages = (bool)config[preferencesTitle, preferences_logMessages].BoxedValue;
                logWarnings = (bool)config[preferencesTitle, preferences_logWarnings].BoxedValue;
                logErrors = (bool)config[preferencesTitle, preferences_logErrors].BoxedValue;

                serverIP = (string)config[networkTitle, network_serverIP].BoxedValue;
                serverPort = (int)config[networkTitle, network_serverPort].BoxedValue;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }

            TKPlayerManager.HandleConfigUpdate();
        }

        private static string CreateConfigLabel(int index, string label)
        {
            string formattedIndex = index < 10 ? $"0{index}" : index.ToString();
            return ($"{formattedIndex}. {label}").PadRight(configLabelLength) + "|";
        }
    }
}

using Oxide.Core.Plugins;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("Claim Player Rewards", "saulteafarmer", "0.1.0")]
    [Description("Allows players to claim rewards based on a JSON configuration file and logs claims.")]

    public class ClaimPlayerRewards : RustPlugin
    {
        // Configurable parameters
        private ConfigData config;

        // Path to your JSON configuration files within a subfolder
        private const string DataFolderPath = "oxide/data/ClaimPlayerRewards/";
        private const string ConfigFilePath = DataFolderPath + "ClaimPlayerRewards.json";
        private const string ClaimedRewardsFilePath = DataFolderPath + "ClaimedRewards.json";

        // Dictionary to hold SteamIDs and corresponding item amounts
        private Dictionary<string, int> rewardData;

        // List to hold claims history
        private List<ClaimRecord> claims;

        // Permission name for using the claim command
        private const string PermissionClaim = "claimplayerrewards.use";

        // Configurable data structure
        private class ConfigData
        {
            public string RewardItem { get; set; } = "blood"; // Default reward item
            public ulong RewardSkinID { get; set; } = 0;      // Default skin ID (0 means no skin)
        }

        void Init()
        {
            // Register the permission
            permission.RegisterPermission(PermissionClaim, this);

            // Ensure the data directory exists
            if (!Directory.Exists(DataFolderPath))
            {
                Directory.CreateDirectory(DataFolderPath);
            }

            // Load the configuration
            LoadConfig();

            // Load reward data from JSON file when the plugin initializes
            LoadRewardData();

            // Load claims data from JSON file when the plugin initializes
            LoadClaimedRewards();
        }

        // Load plugin configuration and ensure defaults are saved if config is empty or missing
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new configuration file...");
            config = new ConfigData();
            SaveConfig();
        }

        private new void LoadConfig()
        {
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    PrintWarning("Config file was empty, creating new defaults...");
                    LoadDefaultConfig();
                }
            }
            catch (Exception e)
            {
                PrintWarning($"Error loading config: {e.Message}, generating default config...");
                LoadDefaultConfig();
            }
        }

        private void SaveConfig()
        {
            Config.WriteObject(config, true);  // Save with indentation for readability
            PrintWarning("Configuration saved successfully.");
        }

        [ChatCommand("claim")]
        private void ClaimCommand(BasePlayer player, string command, string[] args)
        {
            // Check if the player has permission
            if (!permission.UserHasPermission(player.UserIDString, PermissionClaim))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            string playerSteamId = player.UserIDString;

            if (rewardData.ContainsKey(playerSteamId))
            {
                int amountToGive = rewardData[playerSteamId];
                // Use the configured reward item and skin ID
                GiveItem(player, config.RewardItem, amountToGive, config.RewardSkinID);

                // Log the claim
                LogClaim(playerSteamId, amountToGive);

                // Remove the player's entry from the rewardData after claiming
                rewardData.Remove(playerSteamId);

                // Save updated reward data to JSON file
                SaveRewardData();

                SendReply(player, Lang("ClaimSuccess", player.UserIDString, amountToGive, config.RewardItem));
            }
            else
            {
                SendReply(player, Lang("NothingToClaim", player.UserIDString));
            }
        }

        private void GiveItem(BasePlayer player, string itemShortName, int amount, ulong skinId)
        {
            // This function gives the specified item to the player with the defined skin ID
            Item item = ItemManager.CreateByName(itemShortName, amount, skinId);
            if (item != null)
            {
                player.GiveItem(item);
            }
        }

        private void LogClaim(string steamId, int amountClaimed)
        {
            // Create a new claim record
            ClaimRecord newClaim = new ClaimRecord
            {
                steamid = steamId,
                timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                amount_claimed = amountClaimed
            };

            // Add the new claim to the list
            claims.Add(newClaim);

            // Save the updated claims list to the JSON file
            SaveClaimedRewards();
        }

        private void LoadRewardData()
        {
            // Check if the file exists before loading
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    rewardData = JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
                    Puts("Reward data loaded successfully.");
                }
                catch (System.Exception ex)
                {
                    Puts($"Failed to load reward data: {ex.Message}");
                    rewardData = new Dictionary<string, int>();
                }
            }
            else
            {
                Puts("No reward data found, creating a new file.");
                rewardData = new Dictionary<string, int>();
                SaveRewardData();
            }
        }

        private void SaveRewardData()
        {
            try
            {
                // Save the reward data back to the JSON file
                string json = JsonConvert.SerializeObject(rewardData, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
                Puts("Reward data saved successfully.");
            }
            catch (System.Exception ex)
            {
                Puts($"Failed to save reward data: {ex.Message}");
            }
        }

        private void LoadClaimedRewards()
        {
            // Check if the claimed rewards file exists before loading
            if (File.Exists(ClaimedRewardsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ClaimedRewardsFilePath);
                    var claimContainer = JsonConvert.DeserializeObject<ClaimContainer>(json);
                    claims = claimContainer.claims ?? new List<ClaimRecord>();
                    Puts("Claimed rewards data loaded successfully.");
                }
                catch (System.Exception ex)
                {
                    Puts($"Failed to load claimed rewards data: {ex.Message}");
                    claims = new List<ClaimRecord>();
                }
            }
            else
            {
                Puts("No claimed rewards data found, creating a new file.");
                claims = new List<ClaimRecord>();
                SaveClaimedRewards();
            }
        }

        private void SaveClaimedRewards()
        {
            try
            {
                // Save the claims data back to the JSON file
                var claimContainer = new ClaimContainer { claims = claims };
                string json = JsonConvert.SerializeObject(claimContainer, Formatting.Indented);
                File.WriteAllText(ClaimedRewardsFilePath, json);
                Puts("Claimed rewards data saved successfully.");
            }
            catch (System.Exception ex)
            {
                Puts($"Failed to save claimed rewards data: {ex.Message}");
            }
        }

        // Localization with Lang API
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ClaimSuccess"] = "You have claimed {0} {1}.",
                ["NothingToClaim"] = "Nothing to claim.",
                ["NoPermission"] = "You do not have permission to use this command."
            }, this);
        }

        private string Lang(string key, string userId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, userId), args);
        }

        private class ClaimRecord
        {
            public string steamid { get; set; }
            public string timestamp { get; set; }
            public int amount_claimed { get; set; }
        }

        private class ClaimContainer
        {
            public List<ClaimRecord> claims { get; set; }
        }
    }
}

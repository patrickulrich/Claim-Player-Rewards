using Oxide.Core.Plugins;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("Claim Player Rewards", "saulteafarmer", "0.1.1")]
    [Description("Allows players to claim rewards based on a JSON configuration file and logs claims.")]

    public class ClaimPlayerRewards : RustPlugin
    {
        // Configurable parameters
        private ConfigData config;

        // Path to your JSON configuration files within a subfolder
        private const string DataFolderPath = "oxide/data/ClaimPlayerRewards/";
        private const string ConfigFilePath = DataFolderPath + "ClaimPlayerRewards.json";
        private const string ClaimedRewardsFilePath = DataFolderPath + "ClaimedRewards.json";

        // Permission name for using the claim command
        private const string PermissionClaim = "claimplayerrewards.use";

        // Data managers
        private RewardDataManager rewardDataManager;
        private ClaimDataManager claimDataManager;

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

            // Initialize data managers
            rewardDataManager = new RewardDataManager(ConfigFilePath, this);
            claimDataManager = new ClaimDataManager(ClaimedRewardsFilePath, this);

            // Load reward data from JSON file when the plugin initializes
            rewardDataManager.LoadRewardData();

            // Load claims data from JSON file when the plugin initializes
            claimDataManager.LoadClaimedRewards();
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
            catch (IOException e)
            {
                PrintWarning($"IO error loading config: {e.Message}, generating default config...");
                LoadDefaultConfig();
            }
            catch (JsonException e)
            {
                PrintWarning($"JSON error loading config: {e.Message}, generating default config...");
                LoadDefaultConfig();
            }
            catch (Exception e)
            {
                PrintWarning($"Unexpected error loading config: {e.Message}, generating default config...");
                throw; // Rethrow the exception
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

            if (rewardDataManager.HasReward(playerSteamId))
            {
                int amountToGive = rewardDataManager.GetRewardAmount(playerSteamId);
                // Use the configured reward item and skin ID
                GiveItem(player, config.RewardItem, amountToGive, config.RewardSkinID);

                // Log the claim
                claimDataManager.LogClaim(playerSteamId, amountToGive);

                // Remove the player's entry from the rewardData after claiming
                rewardDataManager.RemoveReward(playerSteamId);

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
            return string.Format(CultureInfo.InvariantCulture, lang.GetMessage(key, this, userId), args);
        }

        // Nested class to handle reward data
        private class RewardDataManager
        {
            private string filePath;
            private Dictionary<string, int> rewardData;
            private ClaimPlayerRewards plugin;

            public RewardDataManager(string filePath, ClaimPlayerRewards plugin)
            {
                this.filePath = filePath;
                this.plugin = plugin;
                rewardData = new Dictionary<string, int>();
            }

            public void LoadRewardData()
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        rewardData = JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
                        plugin.Puts("Reward data loaded successfully.");
                    }
                    catch (IOException ex)
                    {
                        plugin.Puts($"IO error loading reward data: {ex.Message}");
                        rewardData = new Dictionary<string, int>();
                    }
                    catch (JsonException ex)
                    {
                        plugin.Puts($"JSON error loading reward data: {ex.Message}");
                        rewardData = new Dictionary<string, int>();
                    }
                    catch (Exception ex)
                    {
                        plugin.Puts($"Unexpected error loading reward data: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    plugin.Puts("No reward data found, creating a new file.");
                    rewardData = new Dictionary<string, int>();
                    SaveRewardData();
                }
            }

            public void SaveRewardData()
            {
                try
                {
                    // Save the reward data back to the JSON file
                    string json = JsonConvert.SerializeObject(rewardData, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                    plugin.Puts("Reward data saved successfully.");
                }
                catch (IOException ex)
                {
                    plugin.Puts($"IO error saving reward data: {ex.Message}");
                    throw;
                }
                catch (JsonException ex)
                {
                    plugin.Puts($"JSON error saving reward data: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    plugin.Puts($"Unexpected error saving reward data: {ex.Message}");
                    throw;
                }
            }

            public bool HasReward(string steamId)
            {
                return rewardData.ContainsKey(steamId);
            }

            public int GetRewardAmount(string steamId)
            {
                return rewardData.ContainsKey(steamId) ? rewardData[steamId] : 0;
            }

            public void RemoveReward(string steamId)
            {
                if (rewardData.Remove(steamId))
                {
                    SaveRewardData();
                }
            }
        }

        // Nested class to handle claimed rewards data
        private class ClaimDataManager
        {
            private string filePath;
            private List<ClaimRecord> claims;
            private ClaimPlayerRewards plugin;

            public ClaimDataManager(string filePath, ClaimPlayerRewards plugin)
            {
                this.filePath = filePath;
                this.plugin = plugin;
                claims = new List<ClaimRecord>();
            }

            public void LoadClaimedRewards()
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        var claimContainer = JsonConvert.DeserializeObject<ClaimContainer>(json);
                        claims = claimContainer.claims ?? new List<ClaimRecord>();
                        plugin.Puts("Claimed rewards data loaded successfully.");
                    }
                    catch (IOException ex)
                    {
                        plugin.Puts($"IO error loading claimed rewards data: {ex.Message}");
                        claims = new List<ClaimRecord>();
                    }
                    catch (JsonException ex)
                    {
                        plugin.Puts($"JSON error loading claimed rewards data: {ex.Message}");
                        claims = new List<ClaimRecord>();
                    }
                    catch (Exception ex)
                    {
                        plugin.Puts($"Unexpected error loading claimed rewards data: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    plugin.Puts("No claimed rewards data found, creating a new file.");
                    claims = new List<ClaimRecord>();
                    SaveClaimedRewards();
                }
            }

            public void SaveClaimedRewards()
            {
                try
                {
                    // Save the claims data back to the JSON file
                    var claimContainer = new ClaimContainer { claims = claims };
                    string json = JsonConvert.SerializeObject(claimContainer, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                    plugin.Puts("Claimed rewards data saved successfully.");
                }
                catch (IOException ex)
                {
                    plugin.Puts($"IO error saving claimed rewards data: {ex.Message}");
                    throw;
                }
                catch (JsonException ex)
                {
                    plugin.Puts($"JSON error saving claimed rewards data: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    plugin.Puts($"Unexpected error saving claimed rewards data: {ex.Message}");
                    throw;
                }
            }

            public void LogClaim(string steamId, int amountClaimed)
            {
                // Create a new claim record
                ClaimRecord newClaim = new ClaimRecord
                {
                    steamid = steamId,
                    timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), // ISO 8601 format
                    amount_claimed = amountClaimed
                };

                // Add the new claim to the list
                claims.Add(newClaim);

                // Save the updated claims list to the JSON file
                SaveClaimedRewards();
            }
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

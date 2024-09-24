## Overview
The **Claim Player Rewards** plugin allows players to claim rewards based on a pre-configured JSON file. Each reward claim is logged to prevent players from claiming multiple times.

This plugin is ideal for servers that want to distribute rewards to players based on specific achievements, events, or other criteria managed through a JSON configuration file.

### Features
- Allows players to claim pre-configured rewards.
- Stores reward configurations in a JSON file.
- Logs every successful reward claim to track claims and prevent duplicates.
- No external dependencies required.

## Installation
1. Download the plugin and place it in the `oxide/plugins/` directory.
2. Create the JSON configuration file `ClaimPlayerRewards.json` in the `oxide/data/ClaimPlayerRewards/` directory.
3. Configure the SteamIDs and reward amounts.
4. Restart your server or reload the plugin.

## Configuration
The plugin will automatically generate a configuration folder and file on first run. The configuration is stored at:

`oxide/data/ClaimPlayerRewards/ClaimPlayerRewards.json`

### Example Configuration:
```json
{
  "76561198000000000": 10,
  "76561198000000001": 5
}
```

In this example, the SteamID `76561198000000000` is entitled to 10 reward items, and the SteamID `76561198000000001` is entitled to 5 reward items.

### Claims Logging:
All claims are logged in a separate JSON file located at:

`oxide/data/ClaimPlayerRewards/ClaimedRewards.json`

Each log entry contains:
- **SteamID**: The player's SteamID.
- **Timestamp**: When the claim was made.
- **Amount Claimed**: The number of items claimed.

## Permissions

| Permission                | Purpose                                               |
|---------------------------|-------------------------------------------------------|
| `claimplayerrewards.use`   | Required to allow players to claim their rewards.     |

Make sure to assign the `claimplayerrewards.use` permission to players or groups you want to grant access to claim rewards.

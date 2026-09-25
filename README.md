# Your Friend's Mom — Clothing Pieces v2.0 (18+ / NSFW)

**18+ / NSFW — clothing removal and nudity controls.**

## [Download the Windows installer ZIP](https://github.com/lkiraren/steam/raw/refs/heads/main/downloads/YFM-Clothing-Pieces-v2.0-public.zip)

This unofficial mod adds a separate menu for removing or restoring individual clothing pieces, including underwear. It uses the game's existing character model and does not add new nude meshes or textures.

### Install

1. Close the game and extract the **entire ZIP linked above**.
2. Run **Install Clothing Pieces.exe**, select the game folder, and click **Install / update**.
3. Launch through Steam, then right-click the character → **Clothing** → **Clothing pieces...**

### Features

- Individual Show / Hide controls, including separate bra and stocking controls.
- Toggle top, Toggle bottom, Hide all, and Restore all.
- No hotkeys or keyboard listeners, so chat input is unaffected.
- Session-only choices; restarting restores the selected outfit.

### Compatibility

Windows Steam build **25513478** only. The installer rejects unsupported files and creates **no backup**. Game updates or Steam file verification may remove the patch. Saves and DLC ownership are unchanged. Other mods that change the same game assembly have not been tested.

The ZIP contains the custom mod, installer, source, and Mono.Cecil under its included MIT license. No game assemblies, models, or textures are distributed.

The installed mod passed 712 in-game checks. The public installer passed read-only compatibility and in-memory patch checks.

### Removal

Close the game, then use Steam → Properties → Installed Files → **Verify integrity of game files**. After verification, remove `YFMFiveToggle.dll` and `YFMClothing.install-receipt.txt` (if present) from the game's `Your Friend's Mom_Data/Managed` folder.

[Full instructions and source code](mods/your-friends-mom/README.md) · [Latest release](https://github.com/lkiraren/steam/releases/latest) · [All releases](https://github.com/lkiraren/steam/releases) · [Report a problem](https://github.com/lkiraren/steam/issues)

### Download verification

SHA-256 of `YFM-Clothing-Pieces-v2.0-public.zip`:

```text
2a9c93d319552b78013254eed889f4b9753e8b5aaedbd91c79ccc05fdcc53a76
```

This is an unofficial community project and is not affiliated with the game's developer or Valve.

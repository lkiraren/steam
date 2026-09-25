# Steam community mods

Unofficial game mods with downloadable installers, source code, and instructions.

## Your Friend's Mom — Clothing Pieces v2.0

Adds a menu for showing and hiding individual pieces of the current outfit. It uses the game's existing models and textures. There are **no hotkeys or keyboard listeners**, so the controls do not interfere with chat.

**[Download the Windows installer ZIP](https://github.com/lkiraren/steam/raw/refs/heads/main/downloads/YFM-Clothing-Pieces-v2.0-public.zip)** · [Release notes](https://github.com/lkiraren/steam/releases/tag/yfm-clothing-pieces-v2.0) · [Full instructions and source](mods/your-friends-mom/README.md)

### Install and use

1. Close the game and extract the **entire ZIP**.
2. Run **Install Clothing Pieces.exe**, check the selected game folder, and click **Install / update**.
3. Launch the game through Steam.
4. Right-click the character → **Clothing** → **Clothing pieces...**

Use Show / Hide beside each available item, or the Toggle top, Toggle bottom, Hide all, and Restore all buttons. Choices last for the current session; restarting restores the selected outfit. The single-piece swimsuit cannot be separated into a top and bottom.

### Compatibility and removal

- Supports **Windows, Steam build 25513478**. Other builds are deliberately rejected by the installer.
- The installer modifies your own installed game code. **It creates no backup.**
- Game updates or Steam file verification may remove the patch.
- It does not change saves or DLC ownership. Other modifications to the same game assembly have not been tested.
- To remove: close the game, use Steam → Properties → Installed Files → Verify integrity of game files, then remove `YFMFiveToggle.dll` and `YFMClothing.install-receipt.txt` (if present) from the game's `Your Friend's Mom_Data/Managed` folder.

### Download verification

SHA-256 of `YFM-Clothing-Pieces-v2.0-public.zip`:

```text
2a9c93d319552b78013254eed889f4b9753e8b5aaedbd91c79ccc05fdcc53a76
```

The package contains the custom mod, installer, source, and Mono.Cecil with its MIT license. It contains **no game assemblies, models, or textures**. This is an unofficial community project and is not affiliated with the game's developer or Valve.

For problems, [open an issue](https://github.com/lkiraren/steam/issues) with your game build, selected outfit, and the exact error message. Remove personal information before sharing logs.

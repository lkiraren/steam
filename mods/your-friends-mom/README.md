# Your Friend's Mom — Clothing Pieces v2.0
Unofficial menu-only mod for Windows, Steam build 25513478.

## INSTALL
1. Close the game.
2. Extract the entire ZIP into a folder.
3. Double-click "Install Clothing Pieces.exe".
4. Confirm the game folder, or use Browse to choose the folder containing
   "Your Friend's Mom.exe". Click Install / update.
5. Launch the game normally through Steam.

## USE
Right-click the character > Clothing > Clothing pieces...

The new panel has Show / Hide buttons for each piece of the current outfit.
It also has Toggle top, Toggle bottom, Hide all, and Restore all.
There are no hotkeys or keyboard listeners, so the mod does not intercept chat.

The panel supports shirts, sweaters, tank tops, T-shirts, sports bras, bras,
swimsuits, skirts, jeans, shorts, leggings, underwear, stockings, shoes, boots,
sneakers, and the necklace/choker. Only currently worn pieces are listed.
The bra and stockings have independent controls. The swimsuit is a single
combined piece and cannot be split into a separate top and bottom.

Top/bottom group buttons leave shoes, stockings, and accessories alone.
Underwear is included in the bottom group, and has its own individual controls.
Restore all clears the mod's choices. Restarting also restores the selected outfit.
The mod uses the game's existing models and textures. It does not rewrite saves
or change DLC ownership. Controls are disabled during locked outfit previews.

## WHAT THE INSTALLER DOES
This download contains custom mod code, its installer, and Mono.Cecil under its
included MIT license. It contains no game assemblies, models, or textures.
The installer patches your own installed Assembly-CSharp.dll locally and adds
YFMFiveToggle.dll plus a small installation checksum receipt to the Managed folder.
It checks compatibility and package checksums and refuses unsupported game files.
It makes no backup, does not download anything, and does not change your saves.
Windows' .NET Framework 4.x is required; no Unity editor or mod loader is needed.
The source code is in the source folder.

## UPDATE / RECOVERY / REMOVE
Close the game before installing an update.
A game update or Steam file verification may remove the patch. Do not force this
release onto a newer unsupported build.

To restore the original game: Steam > Properties > Installed Files >
Verify integrity of game files. With the game closed, you can then delete these
two extra files from Your Friend's Mom_Data\Managed:
  YFMFiveToggle.dll
  YFMClothing.install-receipt.txt (if present)

## TESTING
The installed menu mod passed 712 in-game checks covering independent clothing
pieces, restoration, top/bottom separation, animation persistence, menu open/close,
locked-preview protection, and unchanged saved customization. Its panel and real
menu clicks were also checked in the live game.
The public installer was tested without modifying the live game. Its local patch
changes only two methods; all repatched methods and exception boundaries match
the working installed version. Duplicate patching is rejected.

## KNOWN LIMITS
Tested on the supported Windows game build; other builds are deliberately rejected.
Other mods that change the same game assembly have not been tested.
This is a community mod, not an official update from the game developer.

## THIRD-PARTY COMPONENT
Mono.Cecil 0.11.6 — https://github.com/jbevain/cecil
Its complete license is included as Mono.Cecil-LICENSE.txt.

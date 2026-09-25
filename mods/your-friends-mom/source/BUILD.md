# Build notes

The `ClothingMode.cs`, `ClothingPanel.cs`, and `ClothingSelfTest.cs` files build the custom `YFMFiveToggle.dll`. The class and assembly names preserve compatibility with the existing two game hooks; this version has no keyboard handlers.

Compile them as a .NET Framework-compatible C# library against the supported game's own `mscorlib.dll`, `System.dll`, `System.Core.dll`, `netstandard.dll`, `Assembly-CSharp.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UIElementsModule.dll`, `UnityEngine.JSONSerializeModule.dll`, and `UnityEngine.TextRenderingModule.dll`.

`Installer.cs` builds a Windows executable against .NET Framework 4.x, `System.Core`, `System.Drawing`, `System.Windows.Forms`, and the bundled `Mono.Cecil.dll` (the official 0.11.6 net40 build). It does not need game binaries to compile.

The installer pins the mod DLL's SHA-256 in `HelperHash`. If you rebuild or edit the mod, update that value before compiling the installer. Timestamps may change a rebuilt binary's hash even if its source is unchanged.

The installer supports these command-line modes, each followed by a quoted game-folder path:

- `--check`: read-only compatibility and package checks.
- `--self-test`: read-only, in-memory patch validation against the supported original or recognized patched game.
- `--install`: the same install operation as the graphical Install button.

The self-test never writes a reconstructed original or backs up the game. It compares instruction sequences and exception boundaries because assembly serialization can reorder metadata without changing code.

The runtime self-test only runs if a file named `YFMFiveToggle.selftest` exists in the game's Managed directory. That marker is not included or created by the public installer. The normal mod starts with all pieces unchanged.

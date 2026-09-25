using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ClothingInstaller
{
    internal const string OriginalHash = "ECC0BD5328CDAE2555E9BB96AAD839A4EB63469A81B76BD5CB06AC314986AB65";
    internal const string PatchedHash = "F0C1B402AC8FFD54513A2426047A9412E53DB488469F597329F55A5E98E457C9";
    internal const string HelperHash = "FF7F2C92AE81C9E3BD1822797C069C9AB38E65373BE48B28A674724DC1F7CB34";
    private static readonly string[] OldHelpers = {
        "9BF7C465AF1AF72B563EE4F331B7E978F7C86FB9A48A40BE140A5B285FB8FAB5",
        "5F2FDD6261DE431DEF15FE1CFD9F711AA109051DC2B6D4A5D6FC6A877B9A9DD2"
    };
    private static string PackageRoot { get { return AppDomain.CurrentDomain.BaseDirectory; } }
    private static string HelperPath { get { return Path.Combine(PackageRoot, "YFMFiveToggle.dll"); } }

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 0)
        {
            try
            {
                if (args.Length != 2) throw new ArgumentException("Use --check, --install, or --self-test followed by the game folder.");
                string result;
                if (args[0] == "--check") result = CheckFolder(args[1]);
                else if (args[0] == "--install") result = Install(args[1]);
                else if (args[0] == "--self-test") result = SelfTest(args[1]);
                else throw new ArgumentException("Unknown command.");
                Console.WriteLine(result);
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerWindow());
        return 0;
    }

    internal static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
    }
    private static string Managed(string root)
    {
        return Path.Combine(Path.GetFullPath(root), "Your Friend's Mom_Data", "Managed");
    }
    private static string Receipt(string managed) { return Path.Combine(managed, "YFMClothing.install-receipt.txt"); }
    private static bool HasReceipt(string managed, string hash)
    {
        string path = Receipt(managed);
        return File.Exists(path) && File.ReadAllText(path).Trim() == OriginalHash + ":" + hash;
    }
    internal static string CheckFolder(string root)
    {
        if (!File.Exists(HelperPath) || Hash(File.ReadAllBytes(HelperPath)) != HelperHash)
            throw new InvalidOperationException("The mod file is missing or does not match this release. Extract the complete ZIP again.");
        if (!File.Exists(Path.Combine(root, "Your Friend's Mom.exe")))
            throw new InvalidOperationException("Select the game folder containing Your Friend's Mom.exe.");
        string assembly = Path.Combine(Managed(root), "Assembly-CSharp.dll");
        if (!File.Exists(assembly)) throw new InvalidOperationException("The game's managed assembly was not found.");
        string hash = Hash(File.ReadAllBytes(assembly));
        if (hash != OriginalHash && hash != PatchedHash && !HasReceipt(Managed(root), hash))
            throw new InvalidOperationException("This release supports Steam build 25513478 only. The game version is different or another mod changed it. No files were changed.");
        string helper = Path.Combine(Managed(root), "YFMFiveToggle.dll");
        if (File.Exists(helper))
        {
            string existing = Hash(File.ReadAllBytes(helper));
            if (existing != HelperHash && !OldHelpers.Contains(existing))
                throw new InvalidOperationException("An unrecognized mod file already exists. No files were changed.");
        }
        if (hash != OriginalHash) VerifyPatched(File.ReadAllBytes(assembly), Managed(root));
        return hash != OriginalHash ? "Supported game found; existing hooks recognized." : "Supported unmodified game found.";
    }
    internal static string Install(string root)
    {
        CheckFolder(root);
        // Do not patch an assembly that the game currently has loaded.
        foreach (Process process in Process.GetProcessesByName("Your Friend's Mom"))
        {
            process.Dispose();
            throw new InvalidOperationException("Close Your Friend's Mom, then click Install again.");
        }
        string managed = Managed(root);
        string target = Path.Combine(managed, "Assembly-CSharp.dll");
        byte[] original = File.ReadAllBytes(target);
        byte[] helper = File.ReadAllBytes(HelperPath);
        byte[] patched = Hash(original) == OriginalHash ? Transform(original, managed) : original;
        VerifyPatched(patched, managed);
        string installedHash = Hash(patched);
        // Complete all validation before any write. Temporary files contain only
        // the NEW modded data. No copy/backup of the original is written to disk.
        AtomicWrite(Receipt(managed), Encoding.UTF8.GetBytes(OriginalHash + ":" + installedHash));
        AtomicWrite(Path.Combine(managed, "YFMFiveToggle.dll"), helper);
        if (Hash(original) != installedHash) AtomicWrite(target, patched);
        if (Hash(File.ReadAllBytes(target)) != installedHash || Hash(File.ReadAllBytes(Path.Combine(managed, "YFMFiveToggle.dll"))) != HelperHash)
            throw new IOException("Installed-file verification failed. Use Steam's Verify integrity if recovery is needed.");
        return "Installed! Start the game, then right-click the character > Clothing > Clothing pieces. No hotkeys. No backup created.";
    }
    private static void AtomicWrite(string path, byte[] bytes)
    {
        if (File.Exists(path) && Hash(File.ReadAllBytes(path)) == Hash(bytes)) return;
        string temp = path + ".new-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllBytes(temp, bytes);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static ReaderParameters Reader(string managed, DefaultAssemblyResolver resolver)
    {
        resolver.AddSearchDirectory(managed);
        resolver.AddSearchDirectory(PackageRoot);
        return new ReaderParameters { AssemblyResolver = resolver, InMemory = true, ReadWrite = false };
    }
    private static MethodDefinition Advance(AssemblyDefinition game)
    {
        return game.MainModule.Types.Single(t => t.Name == "BootstrapDriver").Methods.Single(m => m.Name == "Advance");
    }
    private static MethodDefinition Apply(AssemblyDefinition game)
    {
        return game.MainModule.Types.Single(t => t.Name == "CustomizationService").Methods.Single(m => m.Name == "Apply" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Single");
    }
    internal static byte[] Transform(byte[] input, string managed)
    {
        using (DefaultAssemblyResolver resolver = new DefaultAssemblyResolver())
        using (MemoryStream source = new MemoryStream(input))
        using (AssemblyDefinition game = AssemblyDefinition.ReadAssembly(source, Reader(managed, resolver)))
        using (AssemblyDefinition helper = AssemblyDefinition.ReadAssembly(HelperPath, Reader(managed, resolver)))
        {
            if (game.MainModule.AssemblyReferences.Any(r => r.Name == "YFMFiveToggle"))
                throw new InvalidOperationException("The game is already patched.");
            Dictionary<string, string> before = Fingerprints(game);
            TypeDefinition modType = helper.MainModule.Types.Single(t => t.FullName == "YFMFiveToggle.NudityMode");
            // Import in the same order as the tested in-game patch.
            MethodReference tick = game.MainModule.ImportReference(modType.Methods.Single(m => m.Name == "Tick"));
            MethodReference after = game.MainModule.ImportReference(modType.Methods.Single(m => m.Name == "AfterApply"));
            MethodDefinition advance = Advance(game);
            ILProcessor il = advance.Body.GetILProcessor();
            Instruction first = advance.Body.Instructions[0];
            foreach (string name in new string[] { "_customizationDriver", "_playerInputDriver", "_menuDriver" })
            {
                FieldDefinition field = advance.DeclaringType.Fields.Single(f => f.Name == name);
                il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(first, il.Create(OpCodes.Ldfld, field));
            }
            il.InsertBefore(first, il.Create(OpCodes.Call, tick));
            MethodDefinition apply = Apply(game);
            il = apply.Body.GetILProcessor();
            Instruction last = apply.Body.Instructions.Last();
            if (last.OpCode.Code != Code.Ret) throw new InvalidOperationException("Unexpected method structure.");
            Instruction hook = il.Create(OpCodes.Ldarg_0);
            Retarget(apply, last, hook);
            il.InsertBefore(last, hook);
            il.InsertBefore(last, il.Create(OpCodes.Call, after));
            Dictionary<string, string> afterMethods = Fingerprints(game);
            string[] changed = before.Keys.Where(k => before[k] != afterMethods[k]).ToArray();
            if (changed.Length != 2 || !changed.Contains(advance.FullName) || !changed.Contains(apply.FullName))
                throw new InvalidOperationException("Patch changed an unexpected method.");
            using (MemoryStream output = new MemoryStream()) { game.Write(output); return output.ToArray(); }
        }
    }
    private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
    {
        foreach (TypeDefinition type in roots)
        {
            yield return type;
            foreach (TypeDefinition nested in Types(type.NestedTypes)) yield return nested;
        }
    }
    private static Dictionary<string, string> Fingerprints(AssemblyDefinition game)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        foreach (MethodDefinition method in Types(game.MainModule.Types).SelectMany(t => t.Methods))
        {
            StringBuilder text = new StringBuilder(method.Attributes.ToString());
            if (method.HasBody)
            {
                var instructions = method.Body.Instructions;
                foreach (VariableDefinition local in method.Body.Variables) text.Append("|local:").Append(local.VariableType.FullName);
                text.Append("|init:").Append(method.Body.InitLocals);
                foreach (Instruction i in instructions)
                {
                    text.Append('|').Append(i.OpCode.Code).Append(':');
                    if (i.Operand is Instruction) text.Append(instructions.IndexOf((Instruction)i.Operand));
                    else if (i.Operand is Instruction[]) foreach (Instruction target in (Instruction[])i.Operand) text.Append(instructions.IndexOf(target)).Append(',');
                    else if (i.Operand != null) text.Append(i.Operand.ToString());
                }
                foreach (ExceptionHandler h in method.Body.ExceptionHandlers)
                    text.Append("|EH:").Append(h.HandlerType).Append(':').Append(instructions.IndexOf(h.TryStart)).Append(':').Append(instructions.IndexOf(h.TryEnd))
                        .Append(':').Append(instructions.IndexOf(h.HandlerStart)).Append(':').Append(instructions.IndexOf(h.HandlerEnd)).Append(':').Append(instructions.IndexOf(h.FilterStart)).Append(':').Append(h.CatchType);
            }
            result.Add(method.FullName, text.ToString());
        }
        return result;
    }
    private static void VerifyPatched(byte[] bytes, string managed)
    {
        using (DefaultAssemblyResolver resolver = new DefaultAssemblyResolver())
        using (MemoryStream input = new MemoryStream(bytes))
        using (AssemblyDefinition game = AssemblyDefinition.ReadAssembly(input, Reader(managed, resolver)))
        {
            if (game.MainModule.AssemblyReferences.Count(r => r.Name == "YFMFiveToggle") != 1)
                throw new InvalidOperationException("Expected one mod assembly reference.");
            MethodDefinition advance = Advance(game), apply = Apply(game);
            MethodReference tick = advance.Body.Instructions[6].Operand as MethodReference;
            MethodReference after = apply.Body.Instructions[apply.Body.Instructions.Count - 2].Operand as MethodReference;
            if (tick == null || tick.Name != "Tick" || tick.DeclaringType.FullName != "YFMFiveToggle.NudityMode" ||
                after == null || after.Name != "AfterApply" || after.DeclaringType.FullName != "YFMFiveToggle.NudityMode")
                throw new InvalidOperationException("Installed hooks failed verification.");
            Instruction hook = apply.Body.Instructions[apply.Body.Instructions.Count - 3];
            foreach (ExceptionHandler h in apply.Body.ExceptionHandlers)
                if (hook.Offset >= h.HandlerStart.Offset && (h.HandlerEnd == null || hook.Offset < h.HandlerEnd.Offset))
                    throw new InvalidOperationException("Customization hook is inside an exception handler.");
        }
    }
    private static void Retarget(MethodDefinition method, Instruction from, Instruction to)
    {
        foreach (Instruction i in method.Body.Instructions) if (Object.ReferenceEquals(i.Operand, from)) i.Operand = to;
        foreach (ExceptionHandler h in method.Body.ExceptionHandlers)
        {
            if (h.TryStart == from) h.TryStart = to;
            if (h.TryEnd == from) h.TryEnd = to;
            if (h.HandlerStart == from) h.HandlerStart = to;
            if (h.HandlerEnd == from) h.HandlerEnd = to;
            if (h.FilterStart == from) h.FilterStart = to;
        }
    }
    private static string SelfTest(string root)
    {
        CheckFolder(root);
        string managed = Managed(root);
        byte[] current = File.ReadAllBytes(Path.Combine(managed, "Assembly-CSharp.dll"));
        byte[] clean;
        if (Hash(current) == OriginalHash) clean = current;
        else
        {
            // Reconstruct an unpatched fixture in memory only; never write it or
            // a backup of the game. Then prove the installer reproduces the
            // exact already-tested patched file.
            using (DefaultAssemblyResolver resolver = new DefaultAssemblyResolver())
            using (MemoryStream source = new MemoryStream(current))
            using (AssemblyDefinition game = AssemblyDefinition.ReadAssembly(source, Reader(managed, resolver)))
            {
                MethodDefinition advance = Advance(game);
                Instruction call = advance.Body.Instructions[6];
                if (call.OpCode.Code != Code.Call || ((MethodReference)call.Operand).FullName.IndexOf("NudityMode::Tick", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Unexpected startup hook.");
                for (int i = 0; i < 7; i++) advance.Body.Instructions.RemoveAt(0);
                MethodDefinition apply = Apply(game);
                int count = apply.Body.Instructions.Count;
                Instruction last = apply.Body.Instructions[count - 1];
                Instruction after = apply.Body.Instructions[count - 2];
                Instruction hook = apply.Body.Instructions[count - 3];
                if (((MethodReference)after.Operand).FullName.IndexOf("NudityMode::AfterApply", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Unexpected customization hook.");
                Retarget(apply, hook, last);
                apply.Body.Instructions.Remove(after); apply.Body.Instructions.Remove(hook);
                game.MainModule.AssemblyReferences.Remove(game.MainModule.AssemblyReferences.Single(r => r.Name == "YFMFiveToggle"));
                using (MemoryStream output = new MemoryStream()) { game.Write(output); clean = output.ToArray(); }
            }
        }
        byte[] patched = Transform(clean, managed);
        VerifyPatched(patched, managed);
        // Cecil may reorder metadata when an assembly is reserialized. Compare
        // every method and exception boundary instead of assuming identical PE bytes.
        if (Hash(current) != OriginalHash)
        {
            using (DefaultAssemblyResolver resolver = new DefaultAssemblyResolver())
            using (MemoryStream left = new MemoryStream(current))
            using (MemoryStream right = new MemoryStream(patched))
            using (AssemblyDefinition a = AssemblyDefinition.ReadAssembly(left, Reader(managed, resolver)))
            using (AssemblyDefinition b = AssemblyDefinition.ReadAssembly(right, Reader(managed, resolver)))
            {
                Dictionary<string, string> expected = Fingerprints(a), actual = Fingerprints(b);
                if (expected.Count != actual.Count || expected.Any(p => !actual.ContainsKey(p.Key) || actual[p.Key] != p.Value))
                    throw new InvalidOperationException("Repatch changed game method semantics.");
            }
        }
        bool duplicateRejected = false;
        try { Transform(patched, managed); } catch (InvalidOperationException) { duplicateRejected = true; }
        if (!duplicateRejected) throw new InvalidOperationException("Duplicate patch was not rejected.");
        return "PASS: patch changes only two intended methods; every repatched method and exception boundary matches the tested game; hooks verified; duplicate patch rejected. Game files unchanged; no backup created.";
    }
    internal static string DetectGame()
    {
        List<string> libraries = new List<string>();
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
        {
            string steam = key == null ? null : key.GetValue("SteamPath") as string;
            if (!String.IsNullOrEmpty(steam)) libraries.Add(steam);
        }
        libraries.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        foreach (string steam in libraries.ToArray())
        {
            string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;
            foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                libraries.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
        }
        foreach (string library in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string game = Path.Combine(library, "steamapps", "common", "Your Friend's Mom");
            if (File.Exists(Path.Combine(game, "Your Friend's Mom.exe"))) return game;
        }
        return String.Empty;
    }
}

internal sealed class InstallerWindow : Form
{
    private readonly TextBox folder = new TextBox();
    private readonly Label status = new Label();
    private readonly Button install = new Button();
    internal InstallerWindow()
    {
        Text = "Clothing Pieces v2.0 — Installer";
        ClientSize = new Size(640, 330);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 10);
        Label title = new Label { Text = "Your Friend's Mom — Clothing Pieces", Left = 20, Top = 18, Width = 600, Height = 35, Font = new Font("Segoe UI", 16) };
        Controls.Add(title);
        Controls.Add(new Label { Text = "Menu-only mod · Windows · Steam build 25513478", Left = 20, Top = 61, Width = 600, Height = 28 });
        Controls.Add(new Label { Text = "Game folder", Left = 20, Top = 103, Width = 600, Height = 25 });
        folder.SetBounds(20, 130, 500, 28);
        folder.Text = ClothingInstaller.DetectGame();
        Controls.Add(folder);
        Button browse = new Button { Text = "Browse...", Left = 532, Top = 127, Width = 88, Height = 32 };
        browse.Click += delegate {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog { Description = "Select the folder containing Your Friend's Mom.exe", SelectedPath = folder.Text, ShowNewFolderButton = false })
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
        };
        Controls.Add(browse);
        Controls.Add(new Label { Text = "Patches your own installed copy. No game files are bundled.\nNo backups, downloads, or save changes.", Left = 20, Top = 177, Width = 600, Height = 48 });
        install.Text = "Install / update";
        install.SetBounds(20, 241, 160, 42);
        install.Click += delegate {
            install.Enabled = false;
            try { string result = ClothingInstaller.Install(folder.Text.Trim()); status.Text = "Installed successfully."; MessageBox.Show(this, result, "Clothing Pieces", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            catch (Exception ex) { status.Text = "Installation stopped."; MessageBox.Show(this, ex.Message, "Clothing Pieces", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { install.Enabled = true; }
        };
        Controls.Add(install);
        status.SetBounds(198, 250, 422, 54);
        Controls.Add(status);
    }
}

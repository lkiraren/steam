using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace YFMFiveToggle
{
    // Temporary presentation overrides. No saved clothing or ownership changes.
    public static partial class NudityMode
    {
        private const int Top = 1, Bottom = 2, Feet = 4;
        private static readonly FieldInfo ServiceField = typeof(CustomizationDriver).GetField("_service", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo RootField = typeof(CustomizationDriver).GetField("_modelRoot", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo DocumentField = typeof(CompanionMenuDriver).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly List<Piece> pieces = new List<Piece>();
        private static readonly Dictionary<string, SkinnedMeshRenderer> meshes = new Dictionary<string, SkinnedMeshRenderer>();
        private static readonly Dictionary<string, Shape> shapes = new Dictionary<string, Shape>();
        private static CustomizationDriver driver;
        private static CustomizationService service;
        private static CompanionMenuDriver menu;
        private static CustomizationState baseline;
        private static bool failed, restoring, previewLocked, pendingSelfTest, suppress;
        private static Label status;
        private static float statusUntil;
        private sealed class Piece
        {
            internal string Id, Label, Mesh, HideShape;
            internal int Group;
            internal bool Hidden, Available;
            internal VisualElement Row;
            internal Button Show, Hide;
        }
        private sealed class Shape { internal SkinnedMeshRenderer Renderer; internal int Index; }

        public static void Tick(CustomizationDriver current, PlayerInputDriver input, CompanionMenuDriver currentMenu)
        {
            try
            {
                if (failed || current == null || !current.IsBooted) return;
                CustomizationService nextService = (CustomizationService)ServiceField.GetValue(current);
                if (!System.Object.ReferenceEquals(nextService, service))
                {
                    Initialize(current, nextService, currentMenu);
                    return;
                }
                if (pendingSelfTest) { pendingSelfTest = false; SelfTest(); }
                if (status != null && Time.unscaledTime >= statusUntil) status.style.display = DisplayStyle.None;
            }
            catch (Exception ex) { Fail(ex); }
        }
        private static void Initialize(CustomizationDriver current, CustomizationService nextService, CompanionMenuDriver currentMenu)
        {
            if (nextService == null) throw new InvalidOperationException("Customization service is unavailable.");
            if (driver != null) driver.Changed -= GameChanged;
            if (piecesPanel != null) piecesPanel.RemoveFromHierarchy();
            driver = current; service = nextService; menu = currentMenu;
            pieces.Clear(); meshes.Clear(); shapes.Clear();
            Transform root = (Transform)RootField.GetValue(current);
            CustomizationDefinition definition = current.Definition;
            for (int i = 0; i < definition.Renderers.Length; i++)
            {
                Transform child = root.Find(definition.RendererPaths[i]);
                SkinnedMeshRenderer mesh = child == null ? null : child.GetComponent<SkinnedMeshRenderer>();
                if (mesh == null) throw new InvalidOperationException("Missing clothing binding: " + definition.Renderers[i]);
                meshes.Add(definition.Renderers[i], mesh);
            }
            AddPiece("Shirt", "Shirt", "Shirt", Top, null);
            AddPiece("Sweater", "Sweater", "Sweater", Top, null);
            AddPiece("Tanktop", "Tank top", "Tanktop", Top, null);
            AddPiece("Tshirt", "T-shirt", "Tshirt", Top, null);
            AddPiece("SportsBra", "Sports bra", "SportsBra_LP", Top, null);
            AddPiece("Bra", "Bra", "Stockings_Low", Top, "HideBra");
            AddPiece("Swimsuit", "Swimsuit (one piece)", "Swimsuit", Top | Bottom, null);
            AddPiece("Skirt", "Skirt", "Skirt", Bottom, null);
            AddPiece("Jeans", "Jeans", "Jeans", Bottom, null);
            AddPiece("Shorts", "Shorts", "SportsShorts_LP", Bottom, null);
            AddPiece("Leggings", "Leggings", "SportsLeggins_LP", Bottom, null);
            AddPiece("Underwear", "Underwear", "Underwear", Bottom, null);
            AddPiece("Stockings", "Stockings", "Stockings_Low", 0, "HideLegs");
            AddPiece("Shoes", "Shoes", "Shoes", Feet, null);
            AddPiece("Boots", "Boots", "Boots", Feet, null);
            AddPiece("Sneakers", "Sneakers", "AthleticShoes_LP", Feet, null);
            AddPiece("Accessories", "Necklace / choker", "Body", 0, "HideNecklaceChoker");
            foreach (CustomizationClothingDefinition outfit in definition.Clothing)
                foreach (CustomizationPreset variant in outfit.Variants)
                    foreach (CustomizationMorph morph in variant.Morphs) BindShape(morph.Renderer, morph.Shape);
            BindShape("Body", "Nips");
            BuildPanel();
            driver.Changed += GameChanged;
            GameChanged();
            pendingSelfTest = File.Exists(Path.Combine(Application.dataPath, "Managed", "YFMFiveToggle.selftest"));
            Debug.Log("[YFM Clothing] Loaded v2.0: menu only, no hotkeys. Clothing > Clothing pieces; " + pieces.Count + " independent controls; starts with normal outfit.");
        }
        private static void AddPiece(string id, string label, string mesh, int group, string hideShape)
        {
            if (!meshes.ContainsKey(mesh)) throw new InvalidOperationException("Missing piece: " + mesh);
            pieces.Add(new Piece { Id = id, Label = label, Mesh = mesh, Group = group, HideShape = hideShape });
            if (hideShape != null) BindShape(mesh, hideShape);
        }
        private static void BindShape(string mesh, string name)
        {
            string key = mesh + "/" + name;
            if (shapes.ContainsKey(key)) return;
            SkinnedMeshRenderer renderer = meshes[mesh];
            int index = renderer.sharedMesh.GetBlendShapeIndex(name);
            if (index < 0) throw new InvalidOperationException("Missing clothing adjustment: " + key);
            shapes.Add(key, new Shape { Renderer = renderer, Index = index });
        }
        private static void SetShape(string mesh, string name, float weight)
        {
            Shape shape = shapes[mesh + "/" + name];
            shape.Renderer.SetBlendShapeWeight(shape.Index, weight);
        }
        private static Piece FindPiece(string id) { return pieces.Find(delegate(Piece p) { return p.Id == id; }); }
        private static bool Worn(string id) { Piece p = FindPiece(id); return p.Available && !p.Hidden; }
        private static bool Removed(string id) { Piece p = FindPiece(id); return p.Available && p.Hidden; }
        private static bool AnyWorn(int group)
        {
            foreach (Piece p in pieces) if ((p.Group & group) != 0 && p.Available && !p.Hidden) return true;
            return false;
        }
        private static void UpdateBaseline(CustomizationState state, bool locked)
        {
            baseline = state; previewLocked = locked;
            CustomizationPreset preset = CustomizationCore.ResolveOutfit(driver.Definition, state);
            if (preset == null) throw new InvalidOperationException("Unknown current outfit.");
            foreach (Piece p in pieces)
            {
                p.Available = p.Mesh == "Body" || Array.IndexOf(preset.Visible, p.Mesh) >= 0;
                if (p.HideShape != null)
                {
                    float weight = 0f;
                    foreach (CustomizationMorph morph in preset.Morphs)
                        if (morph.Renderer == p.Mesh && morph.Shape == p.HideShape) weight = morph.Weight;
                    p.Available &= weight < 99f;
                }
                if ((p.Group & Feet) != 0) p.Available &= state.Clothing.Current.WearShoes;
                if (p.Id == "Stockings") p.Available &= state.Clothing.Current.WearStockings;
                if (p.Id == "Accessories") p.Available &= state.Clothing.Current.WearAccessories;
            }
        }
        private static void GameChanged()
        {
            try
            {
                if (failed) return;
                CustomizationState state = driver.State;
                CustomizationClothingOptions options = driver.ClothingOptions;
                // Preserve a locked preview and disable this panel during it.
                if (options.PreviewLocked)
                {
                    state.Clothing.Outfit = options.Outfit;
                    state.Clothing.Current.Variant = options.Variant;
                    state.Clothing.Current.ShirtPosition = options.ShirtPosition;
                    state.Clothing.Current.WearShoes = options.WearShoes;
                    state.Clothing.Current.WearStockings = options.WearStockings;
                    state.Clothing.Current.WearAccessories = options.WearAccessories;
                }
                UpdateBaseline(state, options.PreviewLocked);
                service.Apply(baseline);
                RefreshPanel();
            }
            catch (Exception ex) { Fail(ex); }
        }
        private static void ToggleGroup(int group, string label)
        {
            if (previewLocked || failed) { ShowStatus("Select an available outfit first"); return; }
            bool hide = AnyWorn(group), found = false;
            foreach (Piece p in pieces)
                if (p.Available && (p.Group & group) != 0) { p.Hidden = hide; found = true; }
            if (!found) { ShowStatus("No " + label.ToLowerInvariant() + " in this outfit"); return; }
            ApplyChange(label + (hide ? " removed" : " restored"));
        }
        private static void SetPiece(Piece piece, bool hidden)
        {
            if (failed || previewLocked || !piece.Available) return;
            piece.Hidden = hidden;
            ApplyChange(piece.Label + (hidden ? " removed" : " restored"));
        }
        private static void SetAll(bool hidden)
        {
            if (failed || previewLocked) return;
            foreach (Piece p in pieces) if (!hidden || p.Available) p.Hidden = hidden;
            ApplyChange(hidden ? "All clothing removed" : "Selected outfit restored");
        }
        private static void ApplyChange(string message)
        {
            try
            {
                service.Apply(baseline); RefreshPanel(); ShowStatus(message);
                Debug.Log("[YFM Clothing] " + message + "; outfit=" + baseline.Clothing.Outfit);
            }
            catch (Exception ex) { Fail(ex); }
        }
        // Existing hook runs after every normal animation/body update.
        public static void AfterApply(CustomizationService source)
        {
            if (failed || suppress || previewLocked || baseline == null || !System.Object.ReferenceEquals(source, service)) return;
            try
            {
                bool changed = false;
                foreach (Piece p in pieces)
                {
                    if (!p.Available || !p.Hidden) continue;
                    changed = true;
                    if (p.HideShape == null) meshes[p.Mesh].enabled = false;
                    else SetShape(p.Mesh, p.HideShape, 100f);
                }
                if (!changed) return;
                if (!Worn("Bra") && !Worn("Stockings")) meshes["Stockings_Low"].enabled = false;
                if (Removed("Sweater")) SetShape("Body", "SweaterHide", 0f);
                if (Removed("Jeans")) SetShape("Body", "JeansHide", 0f);
                if (Removed("Shorts")) SetShape("Body", "SportShorts_Comp", 0f);
                if (Removed("Leggings")) SetShape("Body", "SportLeggings_Comp", 0f);
                if (Removed("Stockings"))
                {
                    SetShape("Body", "LegSquish", 0f); SetShape("Body", "ThighhighComp", 0f); SetShape("Skirt", "ThighhighComp", 0f);
                }
                if (Removed("Skirt"))
                {
                    SetShape("Body", "SkirtComp", 0f); SetShape("Underwear", "SkirtComp", 0f); SetShape("Stockings_Low", "SkirtComp", 0f);
                }
                if (!AnyWorn(Top))
                {
                    SetShape("Body", "Nips", 0f); SetShape("Body", "BreastShrink", 0f); SetShape("Body", "BreastExpand", 0f);
                    SetShape("Body", "ShirtComp", 0f); SetShape("Stockings_Low", "ShirtComp", 0f);
                }
                if (!AnyWorn(Bottom)) SetShape("Body", "LowerLips", 0f);
                if (!AnyWorn(Feet))
                {
                    SetShape("Body", "Heels", 0f); SetShape("Body", "ShoeComp", 0f); SetShape("Stockings_Low", "Heels", 0f);
                }
            }
            catch (Exception ex) { Fail(ex); }
        }
        private static UIDocument Document() { return menu == null ? null : (UIDocument)DocumentField.GetValue(menu); }
        private static void Fail(Exception ex)
        {
            failed = true;
            foreach (Piece p in pieces) p.Hidden = false;
            if (!restoring && service != null && driver != null && driver.IsBooted)
            {
                restoring = true;
                try { service.Apply(baseline ?? driver.State); } catch { }
                restoring = false;
            }
            if (piecesPanel != null) piecesPanel.SetEnabled(false);
            Debug.LogError("[YFM Clothing] Disabled after error; normal outfit restored where possible: " + ex);
        }
    }
}

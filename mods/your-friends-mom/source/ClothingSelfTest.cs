using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace YFMFiveToggle
{
    public static partial class NudityMode
    {
        private static int testChecks;
        private static void Check(bool condition, string message)
        {
            testChecks++;
            if (!condition || failed) throw new InvalidOperationException("Clothing self-test: " + message);
        }
        private static float Weight(string key)
        {
            Shape shape = shapes[key];
            return shape.Renderer.GetBlendShapeWeight(shape.Index);
        }
        private static void SelfTest()
        {
            CustomizationState original = driver.State;
            string savedState = JsonUtility.ToJson(original);
            testChecks = 0;
            Dictionary<string, bool> originalMeshes = new Dictionary<string, bool>();
            Dictionary<string, float> originalShapes = new Dictionary<string, float>();
            foreach (Piece p in pieces) p.Hidden = false;
            UpdateBaseline(original, false);
            service.Apply(original);
            foreach (KeyValuePair<string, SkinnedMeshRenderer> m in meshes) originalMeshes[m.Key] = m.Value.enabled;
            foreach (string key in shapes.Keys) originalShapes[key] = Weight(key);
            try
            {
                Check(pieces.Count == 17, "expected 17 independent controls");
                Check(Document().rootVisualElement.Q<ScrollView>("clothing-settings").contentContainer.Contains(openPiecesButton), "menu entry is not inside the clothing scroll view");
                Check(piecesPanel.parent.name == "customization-content", "pieces window is outside the native menu region");
                foreach (Piece p in pieces)
                    Check(p.Row != null && p.Show != null && p.Hide != null, "missing piece controls: " + p.Id);
                OpenPieces();
                Check(piecesPanel.style.display.value == DisplayStyle.Flex, "window failed to open");
                ClosePieces();
                Check(piecesPanel.style.display.value == DisplayStyle.None, "window failed to close");

                foreach (string outfit in new string[] { original.Clothing.Outfit, "office", "sport" })
                {
                    CustomizationState state = original.Copy();
                    state.Clothing.Outfit = outfit;
                    foreach (Piece p in pieces) p.Hidden = false;
                    UpdateBaseline(state, false);
                    service.Apply(state);
                    Dictionary<string, bool> normalMeshes = new Dictionary<string, bool>();
                    Dictionary<string, float> normalShapes = new Dictionary<string, float>();
                    foreach (KeyValuePair<string, SkinnedMeshRenderer> m in meshes) normalMeshes[m.Key] = m.Value.enabled;
                    foreach (string key in shapes.Keys) normalShapes[key] = Weight(key);
                    foreach (Piece selected in pieces)
                    {
                        if (!selected.Available) continue;
                        SetPiece(selected, true);
                        service.Apply(0.5f);
                        if (selected.HideShape == null)
                            Check(!meshes[selected.Mesh].enabled, "piece remains visible: " + selected.Id);
                        else
                            Check(Math.Abs(Weight(selected.Mesh + "/" + selected.HideShape) - 100f) < 0.001f, "part not hidden: " + selected.Id);
                        foreach (Piece other in pieces)
                        {
                            if (other == selected || !other.Available || other.Mesh == selected.Mesh) continue;
                            Check(meshes[other.Mesh].enabled == normalMeshes[other.Mesh], "unselected piece changed: " + other.Id);
                        }
                        if (selected.Id == "Bra") Check(Worn("Stockings") == FindPiece("Stockings").Available, "bra removal changed stockings selection");
                        if (selected.Id == "Stockings") Check(Worn("Bra") == FindPiece("Bra").Available, "stockings removal changed bra selection");
                        Check(meshes["Body"].enabled && meshes["HairPonytail"].enabled, "body or hair hidden");
                        SetPiece(selected, false);
                        foreach (KeyValuePair<string, bool> m in normalMeshes)
                            Check(meshes[m.Key].enabled == m.Value, "renderer not restored: " + m.Key);
                        foreach (KeyValuePair<string, float> s in normalShapes)
                            Check(Math.Abs(Weight(s.Key) - s.Value) < 0.001f, "shape not restored: " + s.Key);
                    }
                    ToggleGroup(Top, "Top");
                    foreach (Piece p in pieces)
                    {
                        if (!p.Available) continue;
                        Check(p.Hidden == ((p.Group & Top) != 0), "top toggle affected wrong piece: " + p.Id);
                    }
                    ToggleGroup(Bottom, "Bottom");
                    SetAll(false);
                    SetAll(true);
                    service.Apply(0.5f);
                    foreach (Piece p in pieces) if (p.Available) Check(p.Hidden, "hide all missed " + p.Id);
                    SetAll(false);
                }
                // A locked outfit preview ignores all removal actions.
                UpdateBaseline(original, true);
                SetAll(true);
                foreach (Piece p in pieces) Check(!p.Hidden, "locked preview accepted removal");
            }
            finally
            {
                suppress = false;
                foreach (Piece p in pieces) p.Hidden = false;
                UpdateBaseline(original, false);
                service.Apply(original);
                RefreshPanel();
                ClosePieces();
                if (status != null) status.style.display = DisplayStyle.None;
            }
            foreach (KeyValuePair<string, bool> m in originalMeshes)
                Check(meshes[m.Key].enabled == m.Value, "original outfit not restored: " + m.Key);
            foreach (KeyValuePair<string, float> s in originalShapes)
                Check(Math.Abs(Weight(s.Key) - s.Value) < 0.001f, "original shape not restored: " + s.Key);
            Check(JsonUtility.ToJson(driver.State) == savedState, "saved customization changed");
            Debug.Log("[YFM Clothing] SELF-TEST PASS: " + testChecks + " checks; independent pieces, top/bottom separation, animation persistence, restore all, menu open/close, locked preview and unchanged saved customization. No hotkeys.");
        }
    }
}

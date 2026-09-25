using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace YFMFiveToggle
{
    public static partial class NudityMode
    {
        private static VisualElement piecesPanel, pieceRows, panelActions;
        private static Label panelHint;
        private static Button openPiecesButton;

        private static Button MakeButton(string name, string text, Action action)
        {
            Button button = new Button(action) { name = name, text = text, focusable = false };
            button.style.minHeight = 32;
            button.style.flexGrow = 1;
            button.style.marginLeft = 3;
            button.style.marginRight = 3;
            button.style.color = Color.white;
            button.style.backgroundColor = new Color(0.19f, 0.22f, 0.29f, 1f);
            button.style.fontSize = 13;
            return button;
        }

        private static VisualElement ButtonRow()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexShrink = 0;
            row.style.marginBottom = 6;
            return row;
        }

        private static void BuildPanel()
        {
            UIDocument document = Document();
            if (document == null) throw new InvalidOperationException("Clothing menu is unavailable.");
            VisualElement root = document.rootVisualElement;
            ScrollView settings = root.Q<ScrollView>("clothing-settings");
            VisualElement clothingPage = root.Q("customization-content");
            if (settings == null || clothingPage == null) throw new InvalidOperationException("Clothing page is unavailable.");
            if (openPiecesButton != null) openPiecesButton.RemoveFromHierarchy();
            openPiecesButton = MakeButton("yfm-open-pieces", "Clothing pieces...", OpenPieces);
            openPiecesButton.style.minHeight = 40;
            openPiecesButton.style.marginBottom = 12;
            openPiecesButton.style.flexGrow = 0;
            settings.contentContainer.Insert(0, openPiecesButton);

            // Stay inside the existing native menu's pointer region. No new window
            // hooks or keyboard handlers are needed for this independent panel.
            piecesPanel = new VisualElement { name = "yfm-clothing-pieces-window" };
            piecesPanel.style.position = Position.Absolute;
            piecesPanel.style.left = 0; piecesPanel.style.right = 0;
            piecesPanel.style.top = 0; piecesPanel.style.bottom = 0;
            piecesPanel.style.paddingLeft = 12; piecesPanel.style.paddingRight = 12;
            piecesPanel.style.paddingTop = 10; piecesPanel.style.paddingBottom = 10;
            piecesPanel.style.backgroundColor = new Color(0.08f, 0.09f, 0.13f, 1f);
            piecesPanel.style.color = Color.white;
            piecesPanel.style.display = DisplayStyle.None;
            clothingPage.Add(piecesPanel);

            VisualElement header = ButtonRow();
            Label title = new Label("Clothing pieces");
            title.style.fontSize = 20; title.style.flexGrow = 1;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            header.Add(title);
            Button close = MakeButton("yfm-close-pieces", "Close", ClosePieces);
            close.style.flexGrow = 0; close.style.width = 70;
            header.Add(close);
            piecesPanel.Add(header);
            panelHint = new Label("Show or hide each piece of the current outfit.");
            panelHint.style.whiteSpace = WhiteSpace.Normal;
            panelHint.style.fontSize = 12;
            panelHint.style.marginBottom = 10;
            panelHint.style.flexShrink = 0;
            piecesPanel.Add(panelHint);

            panelActions = new VisualElement();
            VisualElement row = ButtonRow();
            row.Add(MakeButton("yfm-toggle-top", "Toggle top", delegate { ToggleGroup(Top, "Top"); }));
            row.Add(MakeButton("yfm-toggle-bottom", "Toggle bottom", delegate { ToggleGroup(Bottom, "Bottom"); }));
            panelActions.Add(row);
            row = ButtonRow();
            row.Add(MakeButton("yfm-hide-all", "Hide all", delegate { SetAll(true); }));
            row.Add(MakeButton("yfm-restore-all", "Restore all", delegate { SetAll(false); }));
            panelActions.Add(row);
            piecesPanel.Add(panelActions);

            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical) {
                name = "yfm-pieces-scroll", horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            scroll.style.flexGrow = 1; scroll.style.minHeight = 0;
            piecesPanel.Add(scroll);
            pieceRows = scroll.contentContainer;
            foreach (Piece p in pieces)
            {
                Piece item = p;
                p.Row = new VisualElement { name = "yfm-piece-" + p.Id };
                p.Row.style.marginBottom = 10;
                p.Row.style.flexShrink = 0;
                Label label = new Label(p.Label);
                label.style.fontSize = 14; label.style.marginBottom = 4;
                p.Row.Add(label);
                VisualElement actions = ButtonRow();
                p.Show = MakeButton("yfm-show-" + p.Id, "Show", delegate { SetPiece(item, false); });
                p.Hide = MakeButton("yfm-hide-" + p.Id, "Hide", delegate { SetPiece(item, true); });
                actions.Add(p.Show); actions.Add(p.Hide);
                p.Row.Add(actions);
                pieceRows.Add(p.Row);
            }
            root.Q<Button>("window-back").clicked += ClosePieces;
            root.Q<Button>("window-close").clicked += ClosePieces;
        }

        private static void OpenPieces()
        {
            try
            {
                if (failed) return;
                RefreshPanel();
                piecesPanel.style.display = DisplayStyle.Flex;
                piecesPanel.BringToFront();
                Debug.Log("[YFM Clothing] Pieces window opened from menu; outfit=" + baseline.Clothing.Outfit);
            }
            catch (Exception ex) { Fail(ex); }
        }

        private static void ClosePieces()
        {
            if (piecesPanel != null) piecesPanel.style.display = DisplayStyle.None;
        }

        private static void RefreshPanel()
        {
            if (piecesPanel == null) return;
            panelHint.text = previewLocked ? "Select an available outfit to use these controls." : "Show or hide each piece. The swimsuit is one combined piece.";
            panelActions.SetEnabled(!previewLocked);
            pieceRows.SetEnabled(!previewLocked);
            foreach (Piece p in pieces)
            {
                p.Row.style.display = p.Available ? DisplayStyle.Flex : DisplayStyle.None;
                p.Show.style.backgroundColor = !p.Hidden ? new Color(0.22f, 0.38f, 0.56f, 1f) : new Color(0.19f, 0.22f, 0.29f, 1f);
                p.Hide.style.backgroundColor = p.Hidden ? new Color(0.22f, 0.38f, 0.56f, 1f) : new Color(0.19f, 0.22f, 0.29f, 1f);
                p.Show.text = p.Hidden ? "Show" : "Shown";
                p.Hide.text = p.Hidden ? "Hidden" : "Hide";
            }
        }

        private static void ShowStatus(string message)
        {
            UIDocument document = Document();
            if (document == null || document.rootVisualElement == null) return;
            if (status == null || status.panel != document.rootVisualElement.panel)
            {
                if (status != null) status.RemoveFromHierarchy();
                status = new Label { name = "yfm-clothing-status", pickingMode = PickingMode.Ignore };
                status.style.position = Position.Absolute;
                status.style.right = 24; status.style.top = 24;
                status.style.paddingLeft = 12; status.style.paddingRight = 12;
                status.style.paddingTop = 8; status.style.paddingBottom = 8;
                status.style.fontSize = 15; status.style.color = Color.white;
                status.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.92f);
                document.rootVisualElement.Add(status);
            }
            status.text = message;
            status.style.display = DisplayStyle.Flex;
            status.BringToFront();
            statusUntil = Time.unscaledTime + 3f;
        }
    }
}

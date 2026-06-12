using UnityEngine;
using UnityEngine.UI;

namespace BlacktideRequiem.UI.Combat.Naval
{
    /// <summary>
    /// Static helpers to build the naval HUD's uGUI hierarchy at runtime.
    /// Follows the construction patterns of CombatHUDCanvas (land) and the
    /// uGUI rules from coplay-unity-lessons.md §2 (anchors, VLG, CSF).
    /// </summary>
    internal static class NavalUIFactory
    {
        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Creates a panel stretched between normalized anchors of its parent.</summary>
        public static GameObject CreateZone(Transform parent, string name,
            float xMin, float yMin, float xMax, float yMax, Color? bg = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (bg.HasValue)
            {
                var img = go.AddComponent<Image>();
                img.color = bg.Value;
                img.raycastTarget = false;
            }
            return go;
        }

        public static Text CreateText(Transform parent, string name, string content,
            int fontSize, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.font = DefaultFont;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>Text stretched to fill its parent rect (with optional padding).</summary>
        public static Text CreateStretchedText(Transform parent, string name, string content,
            int fontSize, Color color, TextAnchor alignment, float padLeft = 0, float padRight = 0)
        {
            var txt = CreateText(parent, name, content, fontSize, color, alignment);
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padLeft, 0);
            rt.offsetMax = new Vector2(-padRight, 0);
            return txt;
        }

        /// <summary>
        /// Horizontal fill bar: background Image + "Fill" child (Image.Type.Filled).
        /// Returns the bar root; the fill is bar.transform.Find("Fill").
        /// </summary>
        public static GameObject CreateBar(Transform parent, string name,
            Color bgColor, Color fillColor)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            bar.GetComponent<Image>().color = bgColor;
            bar.GetComponent<Image>().raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.raycastTarget = false;
            return bar;
        }

        public static Image GetBarFill(GameObject bar)
        {
            return bar.transform.Find("Fill").GetComponent<Image>();
        }

        /// <summary>
        /// Wood-styled action button (visual design §5): WoodBase bg, WoodBorder
        /// outline, optional icon (sprite fallback: colored square + letter),
        /// gold label. Returns the Button; label child is "Label", icon "Icon",
        /// sublabel "SubLabel" (small reason/cost text under the label).
        /// </summary>
        public static Button CreateActionButton(Transform parent, string name,
            string label, string iconResource, Color iconFallbackColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = NavalUIColors.WoodBase;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = NavalUIColors.WoodBorder;
            outline.effectDistance = new Vector2(2, 2);

            // Icon (left side)
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(12f, 0f);
            iconRt.sizeDelta = new Vector2(32f, 32f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.raycastTarget = false;
            var sprite = string.IsNullOrEmpty(iconResource)
                ? null : Resources.Load<Sprite>(iconResource);
            if (sprite != null)
            {
                iconImg.sprite = sprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }
            else
            {
                iconImg.color = iconFallbackColor;
            }

            var labelTxt = CreateText(go.transform, "Label", label, 18,
                NavalUIColors.Gold, TextAnchor.MiddleLeft);
            var labelRt = labelTxt.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.35f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(52f, 0f);
            labelRt.offsetMax = new Vector2(-6f, -4f);

            var subTxt = CreateText(go.transform, "SubLabel", "", 11,
                NavalUIColors.Cream, TextAnchor.UpperLeft);
            var subRt = subTxt.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.35f);
            subRt.offsetMin = new Vector2(52f, 4f);
            subRt.offsetMax = new Vector2(-6f, 0f);

            // Button transition colors (visual design: gold family on interaction)
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.05f, 1f);
            colors.pressedColor = new Color(0.85f, 0.82f, 0.78f, 1f);
            colors.disabledColor = Color.white; // disabled visual handled manually
            btn.colors = colors;
            return btn;
        }

        /// <summary>Applies the disabled/enabled visual to an action button.</summary>
        public static void SetActionButtonEnabled(Button btn, bool enabled, string reason = null)
        {
            btn.interactable = enabled;
            var img = btn.GetComponent<Image>();
            img.color = enabled ? NavalUIColors.WoodBase : NavalUIColors.BtnDisabledBg;

            var label = btn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
                label.color = enabled ? NavalUIColors.Gold : NavalUIColors.DisabledLabel;

            var sub = btn.transform.Find("SubLabel")?.GetComponent<Text>();
            if (sub != null && reason != null)
            {
                sub.text = reason;
                sub.color = enabled ? NavalUIColors.Cream : NavalUIColors.DisabledLabel;
            }
        }

        /// <summary>
        /// Configures a ScrollRect with Viewport/Content per lessons §2:
        /// VLG childControl on, CSF PreferredSize, pivot (0.5, 1), viewport
        /// alpha 0.004. Returns the Content transform.
        /// </summary>
        public static Transform CreateScrollList(GameObject host, float itemSpacing)
        {
            var scroll = host.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(host.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.004f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = itemSpacing;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            return content.transform;
        }

        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}

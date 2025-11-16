using System;
using DuckovCustomModel.Localizations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DuckovCustomModel.UI.Base
{
    public static class UIFactory
    {
        public static GameObject CreateImage(string name, Transform parent, Color? color = null)
        {
            var obj = new GameObject(name, typeof(Image));
            obj.transform.SetParent(parent, false);

            if (!color.HasValue) return obj;
            var image = obj.GetComponent<Image>();
            image.color = color.Value;

            return obj;
        }

        public static GameObject CreateText(string name, Transform parent, string text, int fontSize = 14,
            Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle fontStyle = FontStyle.Normal)
        {
            var obj = new GameObject(name, typeof(Text));
            obj.transform.SetParent(parent, false);
            var textComponent = obj.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = color ?? Color.white;
            textComponent.alignment = alignment;
            textComponent.fontStyle = fontStyle;
            return obj;
        }

        public static GameObject CreateLocalizedText(string name, Transform parent, Func<string> textGetter,
            int fontSize = 14, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft,
            FontStyle fontStyle = FontStyle.Normal)
        {
            var obj = CreateText(name, parent, textGetter(), fontSize, color, alignment, fontStyle);
            SetLocalizedText(obj, textGetter);
            return obj;
        }

        public static void SetLocalizedText(GameObject textObj, Func<string>? textGetter)
        {
            if (textObj == null || textGetter == null) return;

            var localizedText = textObj.GetComponent<LocalizedText>();
            if (localizedText == null)
                localizedText = textObj.AddComponent<LocalizedText>();

            localizedText.SetTextGetter(textGetter);
        }

        public static void SetupButtonText(GameObject textObj, int minFontSize = 12, int maxFontSize = 18,
            float padding = 8f)
        {
            var textComponent = textObj.GetComponent<Text>();
            if (textComponent == null) return;

            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = minFontSize;
            textComponent.resizeTextMaxSize = maxFontSize;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.offsetMin = new(padding, 0);
            textRect.offsetMax = new(-padding, 0);
            textRect.anchorMin = new(0, 0);
            textRect.anchorMax = new(1, 1);
        }

        public static GameObject CreateButton(string name, Transform parent, UnityAction? onClick = null,
            Color? backgroundColor = null)
        {
            var obj = new GameObject(name, typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            if (backgroundColor.HasValue)
            {
                var image = obj.GetComponent<Image>();
                image.color = backgroundColor.Value;
            }

            var button = obj.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(onClick);

            return obj;
        }

        public static InputField CreateInputField(string name, Transform parent, string placeholder = "")
        {
            var inputObj = new GameObject(name, typeof(Image));
            var inputImage = inputObj.GetComponent<Image>();
            inputImage.color = new(0.1f, 0.12f, 0.15f, 0.9f);

            var outline = inputObj.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var textObj = new GameObject("Text", typeof(Text));
            textObj.transform.SetParent(inputObj.transform, false);
            var textComponent = textObj.GetComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new(0, 0);
            textRect.anchorMax = new(1, 1);
            textRect.offsetMin = new(8, 0);
            textRect.offsetMax = new(-8, 0);

            var placeholderObj = new GameObject("Placeholder", typeof(Text));
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderComponent = placeholderObj.GetComponent<Text>();
            placeholderComponent.text = placeholder;
            placeholderComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderComponent.color = new(1, 1, 1, 0.4f);
            placeholderComponent.alignment = TextAnchor.MiddleLeft;
            var placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new(0, 0);
            placeholderRect.anchorMax = new(1, 1);
            placeholderRect.offsetMin = new(8, 0);
            placeholderRect.offsetMax = new(-8, 0);

            var inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;

            inputObj.transform.SetParent(parent, false);
            return inputField;
        }

        public static Toggle CreateToggle(string name, Transform parent, bool isOn = false,
            UnityAction<bool>? onValueChanged = null)
        {
            var toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(parent, false);
            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = isOn;
            if (onValueChanged != null) toggle.onValueChanged.AddListener(onValueChanged);

            var toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = Vector2.zero;
            toggleRect.anchorMax = Vector2.zero;
            toggleRect.pivot = new(0.5f, 0.5f);
            toggleRect.sizeDelta = new(20, 20);
            toggleRect.anchoredPosition = Vector2.zero;

            var toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = new(0.2f, 0.2f, 0.2f, 1);

            var checkmark = CreateImage("Checkmark", toggleObj.transform, new(0.2f, 0.8f, 0.2f, 1));
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new(0.2f, 0.2f);
            checkmarkRect.anchorMax = new(0.8f, 0.8f);
            checkmarkRect.sizeDelta = Vector2.zero;
            toggle.graphic = checkmark.GetComponent<Image>();

            return toggle;
        }

        public static ScrollRect CreateScrollView(string name, Transform parent, out GameObject content,
            bool vertical = true, bool horizontal = false)
        {
            var scrollView = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollView.transform.SetParent(parent, false);

            var scrollImage = scrollView.GetComponent<Image>();
            scrollImage.color = new(0.05f, 0.08f, 0.12f, 0.8f);

            var mask = scrollView.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = horizontal;
            scrollRect.vertical = vertical;
            scrollRect.scrollSensitivity = 1;

            content = new("Content", typeof(RectTransform));
            content.transform.SetParent(scrollView.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            SetupRectTransform(content, new(0, 1), new(1, 1), Vector2.zero, pivot: new(0, 1),
                anchoredPosition: Vector2.zero);

            scrollRect.content = contentRect;

            return scrollRect;
        }

        public static void SetupRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax,
            Vector2? sizeDelta = null, Vector2? offsetMin = null, Vector2? offsetMax = null,
            Vector2? pivot = null, Vector2? anchoredPosition = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (offsetMin.HasValue || offsetMax.HasValue)
            {
                if (offsetMin.HasValue) rect.offsetMin = offsetMin.Value;
                if (offsetMax.HasValue) rect.offsetMax = offsetMax.Value;
            }
            else if (sizeDelta.HasValue)
            {
                rect.sizeDelta = sizeDelta.Value;
            }

            if (pivot.HasValue) rect.pivot = pivot.Value;
            if (anchoredPosition.HasValue) rect.anchoredPosition = anchoredPosition.Value;
        }

        public static void SetupAnchor(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2? pivot = null,
            Vector2? sizeDelta = null, Vector2? anchoredPosition = null)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (pivot.HasValue) rect.pivot = pivot.Value;
            if (sizeDelta.HasValue) rect.sizeDelta = sizeDelta.Value;
            if (anchoredPosition.HasValue) rect.anchoredPosition = anchoredPosition.Value;
        }

        public static void SetupLeftLabel(GameObject obj, float height = 30f, float leftOffset = 20f)
        {
            SetupAnchor(obj, new(0, 0.5f), new(0, 0.5f), new(0, 0.5f), new(0, height), new(leftOffset, 0));
        }

        public static void SetupRightControl(GameObject obj, Vector2 sizeDelta, float rightOffset = -20f)
        {
            SetupAnchor(obj, new(1, 0.5f), new(1, 0.5f), new(1, 0.5f), sizeDelta, new(rightOffset, 0));
        }

        public static void SetupRightLabel(GameObject obj, float height = 25f, float rightOffset = -120f)
        {
            SetupAnchor(obj, new(1, 0.5f), new(1, 0.5f), new(1, 0.5f), new(0, height), new(rightOffset, 0));
        }

        public static void SetupContentSizeFitter(GameObject obj,
            ContentSizeFitter.FitMode horizontalFit = ContentSizeFitter.FitMode.PreferredSize,
            ContentSizeFitter.FitMode verticalFit = ContentSizeFitter.FitMode.PreferredSize)
        {
            var sizeFitter = obj.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null) sizeFitter = obj.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = horizontalFit;
            sizeFitter.verticalFit = verticalFit;
        }

        public static void SetupVerticalLayoutGroup(GameObject obj, float spacing = 10f,
            RectOffset? padding = null, TextAnchor childAlignment = TextAnchor.UpperCenter,
            bool childControlWidth = true, bool childControlHeight = false,
            bool childForceExpandWidth = false, bool childForceExpandHeight = false)
        {
            var layoutGroup = obj.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null) layoutGroup = obj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = spacing;
            layoutGroup.padding = padding ?? new(10, 10, 10, 10);
            layoutGroup.childAlignment = childAlignment;
            layoutGroup.childControlWidth = childControlWidth;
            layoutGroup.childControlHeight = childControlHeight;
            layoutGroup.childForceExpandWidth = childForceExpandWidth;
            layoutGroup.childForceExpandHeight = childForceExpandHeight;
        }

        public static void SetupHorizontalLayoutGroup(GameObject obj, float spacing = 8f,
            RectOffset? padding = null, TextAnchor childAlignment = TextAnchor.MiddleLeft,
            bool childControlWidth = false, bool childControlHeight = true,
            bool childForceExpandWidth = false, bool childForceExpandHeight = true)
        {
            var layoutGroup = obj.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null) layoutGroup = obj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = spacing;
            layoutGroup.padding = padding ?? new(10, 8, 10, 8);
            layoutGroup.childAlignment = childAlignment;
            layoutGroup.childControlWidth = childControlWidth;
            layoutGroup.childControlHeight = childControlHeight;
            layoutGroup.childForceExpandWidth = childForceExpandWidth;
            layoutGroup.childForceExpandHeight = childForceExpandHeight;
        }

        public static void SetupButtonColors(Button button, Color? normalColor = null, Color? highlightedColor = null,
            Color? pressedColor = null, Color? selectedColor = null)
        {
            var colors = button.colors;
            colors.normalColor = normalColor ?? new(1, 1, 1, 1);
            colors.highlightedColor = highlightedColor ?? new(0.5f, 0.7f, 0.9f, 1);
            colors.pressedColor = pressedColor ?? new(0.4f, 0.6f, 0.8f, 1);
            colors.selectedColor = selectedColor ?? new(0.5f, 0.7f, 0.9f, 1);
            button.colors = colors;
        }

        public static Dropdown CreateDropdown(string name, Transform parent, UnityAction<int>? onValueChanged = null)
        {
            var dropdownObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            dropdownObj.transform.SetParent(parent, false);

            var dropdownImage = dropdownObj.GetComponent<Image>();
            dropdownImage.color = new(0.1f, 0.12f, 0.15f, 0.9f);

            var outline = dropdownObj.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(1, -1);

            var dropdown = dropdownObj.GetComponent<Dropdown>();
            if (onValueChanged != null) dropdown.onValueChanged.AddListener(onValueChanged);

            var labelObj = new GameObject("Label", typeof(Text));
            labelObj.transform.SetParent(dropdownObj.transform, false);
            var labelText = labelObj.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 16;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0);
            labelRect.anchorMax = new(1, 1);
            labelRect.offsetMin = new(10, 2);
            labelRect.offsetMax = new(-25, -2);
            dropdown.captionText = labelText;

            var arrowObj = new GameObject("Arrow", typeof(Image));
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            var arrowImage = arrowObj.GetComponent<Image>();
            arrowImage.color = Color.white;
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new(1, 0.5f);
            arrowRect.anchorMax = new(1, 0.5f);
            arrowRect.pivot = new(1, 0.5f);
            arrowRect.sizeDelta = new(20, 20);
            arrowRect.anchoredPosition = new(-5, 0);
            dropdown.captionImage = arrowImage;

            var templateObj = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateObj.transform.SetParent(dropdownObj.transform, false);
            templateObj.SetActive(false);
            var templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.anchorMin = new(0, 0);
            templateRect.anchorMax = new(1, 0);
            templateRect.pivot = new(0.5f, 1);
            templateRect.sizeDelta = new(0, 150);
            templateRect.anchoredPosition = new(0, 2);

            var templateImage = templateObj.GetComponent<Image>();
            templateImage.color = new(0.1f, 0.12f, 0.15f, 0.95f);

            var templateScrollRect = templateObj.GetComponent<ScrollRect>();
            templateScrollRect.horizontal = false;
            templateScrollRect.vertical = true;

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(templateObj.transform, false);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            templateScrollRect.viewport = viewportRect;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(ToggleGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 1);
            contentRect.anchorMax = new(1, 1);
            contentRect.pivot = new(0.5f, 1);
            contentRect.sizeDelta = new(0, 36);
            contentRect.anchoredPosition = Vector2.zero;
            templateScrollRect.content = contentRect;

            var itemObj = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new(0, 1);
            itemRect.anchorMax = new(1, 1);
            itemRect.pivot = new(0.5f, 1);
            itemRect.sizeDelta = new(0, 28);
            itemRect.anchoredPosition = Vector2.zero;

            var itemBackgroundObj = new GameObject("Item Background", typeof(Image));
            itemBackgroundObj.transform.SetParent(itemObj.transform, false);
            var itemBackgroundRect = itemBackgroundObj.GetComponent<RectTransform>();
            itemBackgroundRect.anchorMin = new(0, 0.1f);
            itemBackgroundRect.anchorMax = new(1, 0.9f);
            itemBackgroundRect.sizeDelta = Vector2.zero;
            itemBackgroundRect.anchoredPosition = Vector2.zero;
            var itemBackgroundImage = itemBackgroundObj.GetComponent<Image>();
            itemBackgroundImage.color = new(0.05f, 0.06f, 0.08f, 0.95f);

            var itemToggle = itemObj.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBackgroundImage;

            var colors = itemToggle.colors;
            colors.normalColor = new(0.05f, 0.06f, 0.08f, 0.95f);
            colors.highlightedColor = new(0.15f, 0.18f, 0.22f, 0.95f);
            colors.pressedColor = new(0.1f, 0.12f, 0.15f, 0.95f);
            colors.selectedColor = new(0.15f, 0.18f, 0.22f, 0.95f);
            colors.disabledColor = new(0.05f, 0.06f, 0.08f, 0.5f);
            itemToggle.colors = colors;

            var itemLabelObj = new GameObject("Item Label", typeof(Text));
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelText = itemLabelObj.GetComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            itemLabelText.fontSize = 16;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            var itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = new(0, 0);
            itemLabelRect.anchorMax = new(1, 1);
            itemLabelRect.offsetMin = new(10, 2);
            itemLabelRect.offsetMax = new(-10, -2);

            dropdown.template = templateRect;
            dropdown.itemText = itemLabelText;

            return dropdown;
        }
    }
}

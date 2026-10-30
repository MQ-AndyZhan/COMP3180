using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VNStudy
{
    /// <summary>
    /// A lightweight click filter for every button in the study UI.
    ///
    /// It only forwards the first left-button pointer click of a sequence:
    /// keyboard submit is never received because this component does not use
    /// UnityEngine.UI.Button, and clickCount 2/3/... (the rest of a double or
    /// triple click) is ignored. The controller still performs state, token and
    /// one-input-per-frame checks on top of this filter.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SingleClickButton : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [Header("Targets")]
        public Image targetImage;
        public TMP_Text label;

        [Header("Background Colours")]
        public Color normalColor = FromHex("#273142");
        public Color highlightedColor = FromHex("#3B4A63");
        public Color pressedColor = FromHex("#1D2634");
        public Color disabledColor = FromHex("#20252D");

        [Header("Label Colours")]
        public Color normalLabelColor = Color.white;
        public Color disabledLabelColor = FromHex("#9BA4B2");

        [Header("Label Outline")]
        public Color normalOutlineColor = FromHex("#778399");
        public Color highlightedOutlineColor = FromHex("#B5C6E2");
        public Color pressedOutlineColor = FromHex("#D1D9E6");
        public Color disabledOutlineColor = FromHex("#20252D");

        [Range(0f, 0.5f)]
        public float outlineWidth = 0.1f;

        [SerializeField]
        private bool interactable = true;

        /// <summary>Raised for one accepted left-button single click while interactable.</summary>
        public event Action Clicked;

        private bool pointerInside;
        private bool pointerDown;

        public bool Interactable
        {
            get { return interactable; }
        }

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            ShaderUtilities.GetShaderPropertyIDs();
            ApplyState();
        }

        private void OnEnable()
        {
            pointerInside = false;
            pointerDown = false;
            ApplyState();
        }

        private void OnDisable()
        {
            pointerInside = false;
            pointerDown = false;
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (!value)
            {
                pointerDown = false;
            }

            ApplyState();
        }

        /// <summary>Re-applies the visual state, e.g. after the study font changes.</summary>
        public void RefreshVisual()
        {
            ApplyState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            ApplyState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pointerDown = false;
            ApplyState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            pointerDown = true;
            ApplyState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerDown = false;
            ApplyState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable || eventData == null)
            {
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // clickCount 2, 3, ... belongs to a double/triple click sequence.
            if (eventData.clickCount != 1)
            {
                return;
            }

            Clicked?.Invoke();
        }

        private void ApplyState()
        {
            Color backgroundColor;
            Color labelColor;
            Color outlineColor;

            if (!interactable)
            {
                backgroundColor = disabledColor;
                labelColor = disabledLabelColor;
                outlineColor = disabledOutlineColor;
            }
            else if (pointerDown && pointerInside)
            {
                backgroundColor = pressedColor;
                labelColor = normalLabelColor;
                outlineColor = pressedOutlineColor;
            }
            else if (pointerInside)
            {
                backgroundColor = highlightedColor;
                labelColor = normalLabelColor;
                outlineColor = highlightedOutlineColor;
            }
            else
            {
                backgroundColor = normalColor;
                labelColor = normalLabelColor;
                outlineColor = normalOutlineColor;
            }

            if (targetImage != null)
            {
                targetImage.color = backgroundColor;
            }

            if (label == null || label.font == null)
            {
                return;
            }

            label.color = labelColor;

            Material material = label.fontMaterial;
            if (material == null)
            {
                return;
            }

            if (ShaderUtilities.ID_OutlineWidth != 0 && material.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, interactable ? outlineWidth : 0f);
            }

            if (ShaderUtilities.ID_OutlineColor != 0 && material.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                material.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            }
        }

        private static Color FromHex(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.white;
        }
    }
}

using UnityEngine;
using TMPro;

public class TextCanvas : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    TMP_Text Text => text;

    void Awake()
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
            if (text == null)
            {
                text = GetComponentInChildren<TMP_Text>();
            }
        }
    }

    public string GetText()
    {
        return text != null ? text.text : string.Empty;
    }

    public void SetText(string newText)
    {
        if (text != null)
        {
            text.text = newText;
        }
    }
    public void SetTextAlignment(TextAlignmentOptions alignment)
    {
        if (text != null)
        {
            text.alignment = alignment;
        }
    }

    public void SetTextFont(TMP_FontAsset newFont)
    {
        if (text != null)
        {
            text.font = newFont;
        }
    }

    public void SetTextStyle(FontStyles newStyle)
    {
        if (text != null)
        {
            text.fontStyle = newStyle;
        }
    }

    public void SetTextSize(float newSize)
    {
        if (text != null)
        {
            text.fontSize = newSize;
        }
    }

    public void SetTextColor(Color newColor)
    {
        if (text != null)
        {
            text.color = newColor;
        }
    }

    public void SetTextVisibility(bool isVisible)
    {
        if (text != null)
        {
            text.enabled = isVisible;
        }
    }

    public void ShowTextForDuration(string newText, float duration)
    {
        if (text != null)
        {
            SetText(newText);
            // cancel any invokes
            CancelInvoke();
            Invoke(nameof(ClearAndHideText), duration);
        }
    }

    public void ShowTextForDuration(float duration)
    {
        if (text != null)
        {
            ShowText();
            // cancel any invokes
            CancelInvoke();
            Invoke(nameof(HideText), duration);
        }
    }

    public void ShowText()
    {
        if (text != null)
        {
            text.enabled = true;
        }
    }
    public void HideText()
    {
        if (text != null)
        {
            text.enabled = false;
        }
    }

    public void ClearAndHideText()
    {
        if (text != null)
        {
            text.text = string.Empty;
            text.enabled = false;
        }
    }
}

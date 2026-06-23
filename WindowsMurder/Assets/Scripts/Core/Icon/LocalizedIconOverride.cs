using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalizedIconOverride : MonoBehaviour
{
    [Header("Localized character id")]
    [SerializeField] private string characterId = "";

    [Header("Optional targets")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    private InteractableIcon interactableIcon;
    private Sprite defaultIcon;
    private string defaultName;

    private void Awake()
    {
        interactableIcon = GetComponent<InteractableIcon>();

        if (iconImage == null && interactableIcon != null)
            iconImage = interactableIcon.iconImage;

        if (nameText == null && interactableIcon != null)
            nameText = interactableIcon.nameText;

        if (iconImage != null)
            defaultIcon = iconImage.sprite;

        if (nameText != null)
            defaultName = nameText.text;
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
        ApplyLocalization();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;
    }

    public void SetCharacterId(string id)
    {
        characterId = id;
        ApplyLocalization();
    }

    private void OnLanguageChanged(SupportedLanguage language)
    {
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        if (string.IsNullOrEmpty(characterId))
            return;

        if (nameText != null)
        {
            string overrideName = LocalizedCharacterVisuals.GetDisplayNameOverride(characterId);
            nameText.text = string.IsNullOrEmpty(overrideName) ? defaultName : overrideName;
        }

        if (iconImage != null)
        {
            Sprite localizedIcon = LocalizedCharacterVisuals.LoadLocalizedIcon(characterId);
            iconImage.sprite = localizedIcon != null ? localizedIcon : defaultIcon;
        }
    }
}

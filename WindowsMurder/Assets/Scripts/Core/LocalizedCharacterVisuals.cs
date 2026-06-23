using UnityEngine;

public static class LocalizedCharacterVisuals
{
    public static string GetVisualId(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return characterId;

        SupportedLanguage language = GetCurrentLanguage();

        if (characterId == "xunlei" &&
            (language == SupportedLanguage.English || language == SupportedLanguage.Japanese))
        {
            return "idm";
        }

        if (characterId == "qq" && language == SupportedLanguage.Japanese)
            return "yahoo";

        return characterId;
    }

    public static string GetDisplayNameOverride(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return null;

        SupportedLanguage language = GetCurrentLanguage();

        if (characterId == "xunlei" &&
            (language == SupportedLanguage.English || language == SupportedLanguage.Japanese))
        {
            return "IDM";
        }

        if (characterId == "qq" && language == SupportedLanguage.Japanese)
            return "Yahoo!";

        return null;
    }

    public static Sprite LoadLocalizedPortrait(string characterId)
    {
        string visualId = GetVisualId(characterId);
        return LoadSpriteWithFallback($"Art/Characters/{visualId}", $"Art/Characters/{characterId}");
    }

    public static Sprite LoadLocalizedIcon(string characterId)
    {
        string visualId = GetVisualId(characterId);
        Sprite sprite = Resources.Load<Sprite>($"Art/UI/{visualId}");
        if (sprite != null)
            return sprite;

        sprite = Resources.Load<Sprite>($"Art/Characters/{visualId}");
        if (sprite != null)
            return sprite;

        return LoadSpriteWithFallback($"Art/UI/{characterId}", $"Art/Characters/{characterId}");
    }

    private static Sprite LoadSpriteWithFallback(string localizedPath, string fallbackPath)
    {
        Sprite sprite = Resources.Load<Sprite>(localizedPath);
        if (sprite != null)
            return sprite;

        if (localizedPath == fallbackPath)
            return null;

        return Resources.Load<Sprite>(fallbackPath);
    }

    private static SupportedLanguage GetCurrentLanguage()
    {
        return LanguageManager.Instance != null
            ? LanguageManager.Instance.currentLanguage
            : SupportedLanguage.Chinese;
    }
}

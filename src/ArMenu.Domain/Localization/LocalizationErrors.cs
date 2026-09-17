using ArMenu.Domain.Common;

namespace ArMenu.Domain.Localization;

public static class LocalizationErrors
{
    public static readonly Error CultureCodeRequired = Error.Validation(
        "culture_code.required", "A culture code is required.");

    public static readonly Error CultureCodeInvalid = Error.Validation(
        "culture_code.invalid", "Culture codes must look like 'tr', 'en' or 'pt-BR' (language[-script][-region]).");

    public static readonly Error TextRequired = Error.Validation(
        "localized_text.required", "At least one translation is required.");

    public static readonly Error TranslationEmpty = Error.Validation(
        "localized_text.translation_empty", "Translations cannot be empty.");

    public static readonly Error TranslationDuplicated = Error.Validation(
        "localized_text.translation_duplicated", "Each culture can be translated only once.");
}

using System.Text.Json.Serialization;

namespace ArMenu.Application.Menus.Statistics;

/// <summary>What a guest did. Counted per day, never stored per guest.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuEventType>))]
public enum MenuEventType
{
    /// <summary>The menu was opened (once per page load).</summary>
    [JsonStringEnumMemberName("menu_viewed")]
    MenuViewed = 0,

    /// <summary>A dish's details were opened.</summary>
    [JsonStringEnumMemberName("dish_opened")]
    DishOpened = 1,

    /// <summary>A dish's 3D model finished loading.</summary>
    [JsonStringEnumMemberName("model_viewed")]
    ModelViewed = 2,

    /// <summary>The guest pressed "see it on your table".</summary>
    [JsonStringEnumMemberName("ar_started")]
    ArStarted = 3,
}

public static class MenuEventTypes
{
    /// <summary>The stable name stored in the database and used in metrics.</summary>
    public static string Name(this MenuEventType type) => type switch
    {
        MenuEventType.MenuViewed => "menu_viewed",
        MenuEventType.DishOpened => "dish_opened",
        MenuEventType.ModelViewed => "model_viewed",
        MenuEventType.ArStarted => "ar_started",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static bool IsAboutADish(this MenuEventType type) => type != MenuEventType.MenuViewed;
}

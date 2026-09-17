using ArMenu.Application.Abstractions.Email;
using ArMenu.Domain.Memberships;

namespace ArMenu.Application.Emails;

/// <summary>An invitation to join a business's team.</summary>
internal static class InvitationEmail
{
    public const string Template = "team-invitation";

    public static EmailMessage Compose(string to, string businessName, string inviterName, TenantRole role, Uri acceptUrl, string language)
    {
        var days = (int)TenantInvitation.Lifetime.TotalDays;

        return language == EmailLanguage.English
            ? EmailLayout.Compose(
                to,
                language,
                subject: $"{inviterName} invited you to the {businessName} team",
                greeting: "Hello,",
                body: $"{inviterName} invited you to join the {businessName} team on ArMenu as " +
                    (role == TenantRole.Manager ? "a manager (can edit the menu and 3D models)." : "staff (can mark dishes as sold out)."),
                action: "Accept the invitation",
                acceptUrl,
                [$"This link works for {days} days and can be used once.", "If you were not expecting this invitation, you can ignore this e-mail."])
            : EmailLayout.Compose(
                to,
                language,
                subject: $"{inviterName} sizi {businessName} ekibine davet etti",
                greeting: "Merhaba,",
                body: $"{inviterName}, sizi ArMenu'de {businessName} ekibine " +
                    (role == TenantRole.Manager ? "yönetici (menüyü ve 3D modelleri düzenleyebilir)" : "personel (ürünleri tükendi olarak işaretleyebilir)") +
                    " olarak davet etti.",
                action: "Daveti kabul et",
                acceptUrl,
                [$"Bu bağlantı {days} gün geçerlidir ve yalnızca bir kez kullanılabilir.", "Bu daveti beklemiyorsanız bu e-postayı görmezden gerebilirsiniz."]);
    }
}

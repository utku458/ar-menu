using ArMenu.Application.Abstractions.Email;
using ArMenu.Domain.Users;

namespace ArMenu.Application.Emails;

/// <summary>Messages about a person's own account: resetting the password, verifying the address, deleting it.</summary>
internal static class AccountEmails
{
    public const string PasswordResetTemplate = "password-reset";
    public const string EmailVerificationTemplate = "email-verification";
    public const string AccountDeletedTemplate = "account-deleted";

    public static EmailMessage PasswordReset(string to, Uri resetUrl, string language)
    {
        var minutes = (int)UserToken.PasswordResetLifetime.TotalMinutes;

        return language == EmailLanguage.English
            ? EmailLayout.Compose(
                to,
                language,
                subject: "Reset your ArMenu password",
                greeting: "Hello,",
                body: "Someone asked to reset the password of the ArMenu account with this address. If it was you, choose a new password.",
                action: "Choose a new password",
                resetUrl,
                [$"This link works for {minutes} minutes and can be used once. Your other devices will need to sign in again.", "If you did not ask for this, ignore this e-mail: your password stays the same."])
            : EmailLayout.Compose(
                to,
                language,
                subject: "ArMenu şifrenizi sıfırlayın",
                greeting: "Merhaba,",
                body: "Bu adrese ait ArMenu hesabının şifresini sıfırlama isteği aldık. İstek sizden geldiyse yeni bir şifre belirleyin.",
                action: "Yeni şifre belirle",
                resetUrl,
                [$"Bu bağlantı {minutes} dakika geçerlidir ve yalnızca bir kez kullanılabilir. Diğer cihazlarınızda yeniden giriş yapmanız gerekir.", "Bu isteği siz yapmadıysanız e-postayı görmezden gerebilirsiniz; şifreniz değişmez."]);
    }

    public static EmailMessage EmailVerification(string to, string fullName, Uri verifyUrl, string language)
    {
        var days = (int)UserToken.EmailVerificationLifetime.TotalDays;

        return language == EmailLanguage.English
            ? EmailLayout.Compose(
                to,
                language,
                subject: "Confirm your e-mail address for ArMenu",
                greeting: $"Hello {fullName},",
                body: "Confirm that this is your address, so you can invite your team and get back into your account if you forget the password.",
                action: "Confirm my address",
                verifyUrl,
                [$"This link works for {days} days.", "If you did not create an ArMenu account, ignore this e-mail."])
            : EmailLayout.Compose(
                to,
                language,
                subject: "ArMenu için e-posta adresinizi doğrulayın",
                greeting: $"Merhaba {fullName},",
                body: "Bu adresin size ait olduğunu doğrulayın; böylece ekibinizi davet edebilir, şifrenizi unutursanız hesabınıza yeniden erişebilirsiniz.",
                action: "Adresimi doğrula",
                verifyUrl,
                [$"Bu bağlantı {days} gün geçerlidir.", "ArMenu hesabı oluşturmadıysanız bu e-postayı görmezden gerebilirsiniz."]);
    }

    /// <summary>Sent to the address the account had, after it was erased. It names no one: there is no one left to name.</summary>
    public static EmailMessage AccountDeleted(string to, IReadOnlyList<string> closedBusinesses, DateTimeOffset deletedAt, string language)
    {
        var date = deletedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);

        return language == EmailLanguage.English
            ? EmailLayout.Compose(
                to,
                language,
                subject: "Your ArMenu account was deleted",
                greeting: "Hello,",
                body: $"The ArMenu account with this address was deleted on {date}. Your name, address and password are no longer stored, and you left every team you were in.",
                action: null,
                actionUrl: null,
                [.. closedBusinesses.Select(name => $"{name} was closed with the account: its menu is no longer shown."), "If it was not you, someone knew your password: ask the businesses you worked with to invite you again, and choose a password you use nowhere else."])
            : EmailLayout.Compose(
                to,
                language,
                subject: "ArMenu hesabınız silindi",
                greeting: "Merhaba,",
                body: $"Bu adrese ait ArMenu hesabı {date} tarihinde silindi. Adınız, e-posta adresiniz ve şifreniz artık saklanmıyor; üyesi olduğunuz tüm ekiplerden çıkarıldınız.",
                action: null,
                actionUrl: null,
                [.. closedBusinesses.Select(name => $"{name} hesapla birlikte kapatıldı; menüsü artık gösterilmiyor."), "Bu işlemi siz yapmadıysanız şifreniz başkası tarafından biliniyordu: çalıştığınız işletmelerden sizi yeniden davet etmelerini isteyin ve başka hiçbir yerde kullanmadığınız bir şifre seçin."]);
    }
}

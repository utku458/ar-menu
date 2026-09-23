import type { InterfaceLanguage } from './messages.ts';

type ErrorMessages = Readonly<Record<string, readonly [tr: string, en: string]>>;

/**
 * The API's stable error codes, in the staff member's language. Codes are part of the contract; the English text the
 * API sends along is meant for developers and is only used for codes this table does not know yet.
 */
const errors: ErrorMessages = {
  'auth.invalid_credentials': ['E-posta veya şifre hatalı.', 'The email or password is incorrect.'],
  'auth.invalid_refresh_token': [
    'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.',
    'Your session has ended. Please sign in again.',
  ],
  'tenant.not_found': ['Bu adreste bir işletme bulunamadı.', 'No business was found at this address.'],
  rate_limited: [
    'Çok fazla deneme yapıldı. Biraz bekleyip tekrar deneyin.',
    'Too many attempts. Wait a moment and try again.',
  ],
  concurrency_conflict: [
    'Bu içerik başka biri tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.',
    'Someone else changed this. Reload and try again.',
  ],
  conflict: ['İstek mevcut verilerle çakışıyor.', 'The request conflicts with existing data.'],
  internal_error: [
    'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.',
    'Something went wrong. Please try again.',
  ],

  'validation.not_empty': ['Bu alan zorunlu.', 'This field is required.'],
  'validation.not_null': ['Bu alan zorunlu.', 'This field is required.'],
  'validation.maximum_length': ['Bu alan çok uzun.', 'This field is too long.'],
  'validation.length': ['Bu alanın uzunluğu uygun değil.', 'This field has the wrong length.'],

  'localized_text.required': ['En az bir dilde metin girin.', 'Enter the text in at least one language.'],
  'localized_text.translation_empty': ['Çeviriler boş olamaz.', 'Translations cannot be empty.'],
  'menu.default_culture_translation_missing': [
    'Varsayılan dildeki metin zorunlu.',
    'The text in the default language is required.',
  ],
  'menu.culture_not_supported': [
    'Menünüzde sunulmayan bir dilde metin gönderildi.',
    'A text was sent in a language your menu does not offer.',
  ],
  'menu.reorder_mismatch': [
    'Menü başka bir yerde değişti. Sayfayı yenileyip tekrar sıralayın.',
    'The menu changed elsewhere. Reload and reorder again.',
  ],
  'menu_category.name_too_long': ['Kategori adı çok uzun.', 'The category name is too long.'],
  'menu_category.description_too_long': ['Açıklama çok uzun.', 'The description is too long.'],
  'menu_category.not_empty': [
    'Önce bu kategorideki ürünleri silin veya başka kategoriye taşıyın.',
    'Delete or move the items in this category first.',
  ],
  'menu_category.not_found': [
    'Kategori bulunamadı; silinmiş olabilir.',
    'The category was not found; it may have been deleted.',
  ],
  'menu_item.name_too_long': ['Ürün adı çok uzun.', 'The item name is too long.'],
  'menu_item.description_too_long': ['Açıklama çok uzun.', 'The description is too long.'],
  'menu_item.not_found': [
    'Ürün bulunamadı; silinmiş olabilir.',
    'The item was not found; it may have been deleted.',
  ],
  'money.amount_negative': ['Fiyat negatif olamaz.', 'The price cannot be negative.'],
  'money.amount_precision_exceeded': [
    'Fiyatta en fazla iki ondalık basamak olabilir.',
    'Prices can have at most two decimals.',
  ],
  'money.amount_too_large': ['Fiyat çok yüksek.', 'The price is too high.'],

  'tenant.name_required': ['İşletme adı zorunlu.', 'The business name is required.'],
  'tenant.name_too_long': ['İşletme adı çok uzun.', 'The business name is too long.'],
  'tenant.slug_required': ['Menü adresi zorunlu.', 'The menu address is required.'],
  'tenant.slug_invalid': [
    'Yalnızca küçük harf, rakam ve tire kullanın (3–63 karakter).',
    'Use lowercase letters, digits and hyphens only (3–63 characters).',
  ],
  'tenant.slug_reserved': [
    'Bu adres ayrılmış; başka bir adres seçin.',
    'This address is reserved; choose another one.',
  ],
  'tenant.slug_taken': [
    'Bu adres kullanımda; başka bir adres seçin.',
    'This address is taken; choose another one.',
  ],
  'tenant.culture_not_supported': [
    'Varsayılan dil, sunulan diller arasında olmalı.',
    'The default language must be one of the offered languages.',
  ],
  'tenant.supported_culture_limit_reached': [
    'En fazla 10 dil sunabilirsiniz.',
    'You can offer up to 10 languages.',
  ],
  'culture_code.invalid': ['Geçersiz dil kodu.', 'Invalid language code.'],
  'currency.invalid': ['Geçersiz para birimi.', 'Invalid currency.'],
  'user.email_invalid': ['Geçerli bir e-posta adresi girin.', 'Enter a valid email address.'],
  'user.email_required': ['E-posta zorunlu.', 'The email is required.'],
  'user.email_taken': ['Bu e-posta ile zaten bir hesap var.', 'An account with this email already exists.'],
  'user.full_name_required': ['Ad soyad zorunlu.', 'Your name is required.'],
  'user.user_name_taken': [
    'Bu kullanıcı adı zaten kullanılıyor.',
    'An account with this user name already exists.',
  ],
  'user.user_name_required': ['Kullanıcı adı zorunlu.', 'A user name is required.'],
  'user.user_name_invalid': [
    'Kullanıcı adı 3-32 karakter olmalı: küçük harf, rakam, nokta, tire veya alt çizgi; harf ya da rakamla başlayıp bitmeli.',
    'User names are 3-32 characters: lower-case letters, digits, dots, hyphens or underscores, starting and ending with a letter or digit.',
  ],
  'auth.password_too_short': [
    'Şifre en az 12 karakter olmalı.',
    'The password must be at least 12 characters.',
  ],

  'team.email_not_verified': [
    'Ekibe davet göndermeden önce e-posta adresinizi doğrulayın.',
    'Confirm your e-mail address before inviting people to the team.',
  ],
  'user_token.invalid_link': [
    'Bu bağlantı geçerli değil. Yeni bir bağlantı isteyin.',
    'This link is not valid. Request a new one.',
  ],
  'user_token.expired': [
    'Bu bağlantının süresi dolmuş. Yeni bir bağlantı isteyin.',
    'This link has expired. Request a new one.',
  ],
  'user_token.already_used': [
    'Bu bağlantı zaten kullanılmış ya da yerine yenisi gönderilmiş.',
    'This link was already used or replaced by a newer one.',
  ],
  'invitation.invalid_link': [
    'Bu davet bağlantısı geçerli değil. İşletmeden yeni bir davet isteyin.',
    'This invitation link is not valid. Ask the business for a new invitation.',
  ],
  'invitation.expired': [
    'Bu davetin süresi dolmuş. İşletmeden daveti tekrar göndermesini isteyin.',
    'This invitation has expired. Ask the business to send it again.',
  ],
  'invitation.no_longer_pending': [
    'Bu davet zaten kullanılmış ya da geri alınmış.',
    'This invitation was already used or withdrawn.',
  ],
  'invitation.already_pending': [
    'Bu adrese zaten bekleyen bir davet var. Listeden tekrar gönderebilirsiniz.',
    'This address already has a pending invitation. You can send it again from the list.',
  ],
  'invitation.owner_not_invitable': [
    'Yalnızca yönetici veya personel davet edilebilir.',
    'Only managers and staff can be invited.',
  ],
  'invitation.not_found': [
    'Davet bulunamadı; geri alınmış olabilir.',
    'The invitation was not found; it may have been withdrawn.',
  ],
  'membership.already_member': ['Bu kişi zaten ekipte.', 'This person is already on the team.'],
  'team.password_managed_by_mailbox': [
    'Bu kişi e-posta adresiyle giriş yapıyor ve şifresini kendisi sıfırlar; başka işletmelerde de olabileceği için buradan değiştirilemez.',
    'This person signs in with an e-mail address and resets their own password; it can also open other businesses, so it is not reset from here.',
  ],
  'platform.not_administrator': [
    'Bu işlemi yalnızca platform yöneticisi yapabilir.',
    'Only the platform administrator can do this.',
  ],
  'platform.business_not_found': [
    'Bu kimliğe sahip etkin bir işletme yok.',
    'No active business has this id.',
  ],
  'membership.not_found': [
    'Üye bulunamadı; ekipten çıkarılmış olabilir.',
    'The member was not found; they may have been removed.',
  ],
  'membership.owner_unchangeable': [
    'İşletme sahibinin rolü değiştirilemez ve ekipten çıkarılamaz.',
    'The owner’s role cannot be changed and the owner cannot be removed.',
  ],
  'membership.not_owner': [
    'İşletmeyi yalnızca sahibi devredebilir.',
    'Only the owner can hand the business over.',
  ],
  'membership.already_owner': ['Bu kişi zaten işletmenin sahibi.', 'This person already owns the business.'],
  'account.password_incorrect': ['Şifre hatalı.', 'The password is incorrect.'],
  'account.too_many_attempts': [
    'Çok fazla hatalı şifre girildi. Birkaç dakika sonra tekrar deneyin.',
    'Too many incorrect passwords. Try again in a few minutes.',
  ],
  'account.ownership_transfer_required': [
    'Ekibi olan bir işletmenin sahibisiniz. Hesabınızı silmeden önce sahipliği devredin.',
    'You own a business that has a team. Hand it over before deleting your account.',
  ],
  'menu_import.empty': ['Dosyada ürün yok.', 'The file has no dishes.'],
  'menu_import.malformed': [
    'Dosya okunamadı: kapanmayan bir tırnak işareti var.',
    'The file could not be read: a quote is never closed.',
  ],
  'menu_import.too_many_rows': [
    'Bir dosyada en fazla 2.000 ürün olabilir.',
    'A file can hold at most 2,000 dishes.',
  ],
  'menu_import.too_large': ['Dosya en fazla 1 MB olabilir.', 'The file can be at most 1 MB.'],
  'menu_import.unsupported_media_type': ['Bir CSV dosyası seçin.', 'Choose a CSV file.'],
  'menu_import.missing_column': ['Bu sütun dosyada olmalı.', 'The file needs this column.'],
  'menu_import.unknown_column': [
    'Menüde böyle bir sütun yok ya da iki kez geçiyor. Yeni bir dil için önce dili ayarlardan ekleyin.',
    'The menu has no such column, or it appears twice. For a new language, add it in settings first.',
  ],
  'menu_import.item_not_found': [
    'Bu kimlikte bir ürün yok. Yeni ürün için kimliği boş bırakın.',
    'No dish has this id. Leave it empty to add a dish.',
  ],
  'menu_import.duplicate_item': [
    'Bu ürün dosyada birden fazla kez geçiyor.',
    'This dish appears more than once.',
  ],
  'menu_import.category_required': [
    'Yeni ürün için kategori adı yazın.',
    'A new dish needs a category name.',
  ],
  'menu_import.category_not_found': [
    'Bu adda bir kategori yok. Önce menüde oluşturun.',
    'No category has this name. Create it in the menu first.',
  ],
  'menu_import.category_ambiguous': [
    'Bu adda birden fazla kategori var; birinin adını değiştirin.',
    'Several categories have this name; rename one of them.',
  ],
  'menu_import.price_required': ['Yeni ürün için fiyat yazın.', 'A new dish needs a price.'],
  'menu_import.price_invalid': [
    'Fiyatı 185 veya 185,50 gibi, binlik ayırıcı olmadan yazın.',
    'Write the price like 185 or 185.50, without thousands separators.',
  ],
  'menu_import.flag_invalid': ['evet veya hayır yazın.', 'Write yes or no.'],
  'tenant.time_zone_invalid': ['Listeden bir saat dilimi seçin.', 'Choose a time zone from the list.'],
  'tenant.brand_color_required': ['Bir renk seçin.', 'Choose a colour.'],
  'tenant.brand_color_invalid': [
    'Renk #b8442f gibi altı onaltılık basamakla yazılmalı.',
    'The colour must be written as six hexadecimal digits, such as #b8442f.',
  ],
  'tenant.time_zone_required': ['Bir saat dilimi seçin.', 'Choose a time zone.'],
  'tenant.closed': ['Bu işletme kapatıldı.', 'This business was closed.'],
  'membership.role_not_assignable': [
    'Üyeler yalnızca yönetici veya personel olabilir.',
    'Members can only be managers or staff.',
  ],

  'asset.content_type_not_allowed': [
    'Bu alan bu dosya türünü kabul etmiyor.',
    'This field does not accept this type of file.',
  ],
  'asset.too_large': ['Dosya çok büyük.', 'The file is too large.'],
  'asset.size_invalid': ['Dosya boş.', 'The file is empty.'],
  'asset.upload_not_found': [
    'Yükleme süresi doldu. Dosyayı yeniden seçin.',
    'The upload expired. Choose the file again.',
  ],
  'asset.model_invalid': [
    'Dosya geçerli bir GLB (glTF 2.0) modeli değil.',
    'The file is not a valid GLB (glTF 2.0) model.',
  ],
  'asset.model_extension_unsupported': [
    'Model işlenemeyen bir glTF uzantısı kullanıyor (ör. KTX2 dokular). PNG, JPEG veya WebP dokularla dışa aktarın.',
    'The model uses a glTF extension that cannot be processed, such as KTX2 textures. Export it with PNG, JPEG or WebP textures.',
  ],
  'asset.model_requires_processing': [
    'Modeller işlenerek yayımlanır.',
    'Models are published through processing.',
  ],
  'asset.processing_unavailable': [
    'Model şu anda işlenemedi. Biraz sonra yeniden yükleyin.',
    'The model could not be processed right now. Upload it again in a while.',
  ],
  'asset.processing_output_invalid': [
    'İşleme sırasında bir sorun oluştu. Modeli yeniden yükleyin.',
    'Something went wrong during processing. Upload the model again.',
  ],
  'asset.processing_failed': ['Model işlenemedi.', 'The model could not be processed.'],
  'model.too_large': ['Model dosyası işlenemeyecek kadar büyük.', 'The model file is too large to process.'],
  'model.unreadable': [
    'Dosya okunabilir bir GLB (glTF 2.0) modeli değil.',
    'The file is not a readable GLB (glTF 2.0) model.',
  ],
  'model.empty': ['Modelde görünecek bir yüzey yok.', 'The model has no surfaces to show.'],
  'model.too_complex': [
    'Model bir tabak için fazla karmaşık (4 milyondan fazla üçgen). Dışa aktarmadan önce sadeleştirin.',
    'The model is too complex for a dish (over 4 million triangles). Simplify it before exporting.',
  ],
  'model.texture_unsupported': [
    'Modelin bir dokusu okunamadı ya da KTX2 biçiminde. PNG, JPEG veya WebP kullanın.',
    'A texture of the model is unreadable or in KTX2 format. Use PNG, JPEG or WebP.',
  ],
  'model.render_failed': [
    'Model misafirlerin görüntüleyicisinde açılamadı. Dosyayı 3D programınızda kontrol edin.',
    'The model did not open in the viewer guests use. Check the file in your 3D tool.',
  ],
  'asset.apple_model_invalid': [
    'Dosya geçerli bir USDZ paketi değil.',
    'The file is not a valid USDZ package.',
  ],
  'asset.poster_invalid': [
    'Poster WebP, AVIF, PNG veya JPEG olmalı.',
    'The poster must be WebP, AVIF, PNG or JPEG.',
  ],
  'menu_item.allergen_unknown': ['Bilinmeyen bir alerjen seçildi.', 'An unknown allergen was chosen.'],
  'menu_item.dietary_label_unknown': [
    'Bilinmeyen bir diyet etiketi seçildi.',
    'An unknown dietary label was chosen.',
  ],
  'menu_item.dietary_label_contradicts_allergen': [
    'Seçilen diyet, işaretlenen alerjenlerle çelişiyor (örneğin süt içeren vegan yemek).',
    'The chosen diet contradicts the ticked allergens (for example a vegan dish with milk).',
  ],
  'asset.logo_invalid': [
    'Logo WebP, AVIF, PNG veya JPEG olmalı.',
    'The logo must be WebP, AVIF, PNG or JPEG.',
  ],
  'asset.not_owned': ['Bu dosya işletmenize ait değil.', 'This file does not belong to your business.'],
  'asset.not_found': [
    'Dosya depolamada bulunamadı. Yeniden yükleyin.',
    'The file is missing from storage. Upload it again.',
  ],
};

export function errorMessage(code: string | undefined, language: InterfaceLanguage): string | undefined {
  const messages = code === undefined ? undefined : errors[code];
  return messages === undefined ? undefined : messages[language === 'tr' ? 0 : 1];
}

/** Sentences of the history page; the page puts the person who acted in front of each. */
export interface HistoryMessages {
  readonly itemCreated: (name: string) => string;
  readonly itemUpdated: (name: string) => string;
  readonly itemDeleted: (name: string) => string;
  readonly itemSoldOut: (name: string) => string;
  readonly itemBackOnSale: (name: string) => string;
  readonly categoryCreated: (name: string) => string;
  readonly categoryUpdated: (name: string) => string;
  readonly categoryDeleted: (name: string) => string;
  readonly itemsReordered: (category: string) => string;
  readonly categoriesReordered: string;
  readonly businessUpdated: string;
  readonly memberJoined: string;
  readonly memberLeft: string;
  readonly memberRoleChanged: (person: string) => string;
  readonly memberRemoved: (person: string) => string;
  readonly fields: Readonly<Record<string, string>>;
  readonly present: string;
  readonly absent: string;
  readonly replaced: string;
  readonly visible: string;
  readonly hidden: string;
  readonly onSale: string;
  readonly soldOut: string;
  readonly notDeclared: string;
  readonly noAllergens: string;
}

/** Interface text of the dashboard, for restaurant staff in Turkey (Turkish) and elsewhere (English). */
export interface Messages {
  readonly save: string;
  readonly cancel: string;
  readonly delete: string;
  readonly edit: string;
  readonly actions: string;
  readonly loading: string;
  readonly saved: string;
  readonly networkError: string;
  readonly unexpectedError: string;
  readonly tryAgain: string;
  readonly optional: string;

  readonly mainNavigation: string;
  readonly navMenu: string;
  readonly navQrCodes: string;
  readonly navLanguages: string;
  readonly navTeam: string;
  readonly navStatistics: string;
  readonly otherWorkspaces: string;
  readonly allWorkspaces: string;
  readonly verifyEmailBanner: (email: string) => string;
  readonly resendVerification: string;
  readonly verificationSent: string;
  readonly viewGuestMenu: string;
  readonly signOut: string;
  readonly roles: Readonly<Record<string, string>>;
  readonly interfaceLanguage: string;

  readonly workspaceTitle: string;
  readonly workspaceIntro: string;
  readonly workspaceLabel: string;
  readonly workspaceDescription: string;
  readonly continue: string;
  readonly newToArMenu: string;
  readonly createBusiness: string;

  readonly signInTitle: (business: string) => string;
  readonly email: string;
  readonly password: string;
  readonly signIn: string;
  readonly otherBusiness: string;
  readonly sessionEnded: string;
  readonly forgotPassword: string;
  readonly forgotPasswordTitle: string;
  readonly forgotPasswordIntro: string;
  readonly sendResetLink: string;
  readonly resetLinkSent: (email: string) => string;
  readonly resetPasswordTitle: string;
  readonly newPassword: string;
  readonly setNewPassword: string;
  readonly passwordChanged: string;
  readonly linkMissing: string;
  readonly linkUnavailable: string;
  readonly verifyEmailTitle: string;
  readonly emailVerified: string;
  readonly continueToDashboard: string;
  readonly backToSignIn: string;

  readonly signUpTitle: string;
  readonly businessName: string;
  readonly menuAddress: string;
  readonly menuAddressDescription: (url: string) => string;
  readonly menuLanguage: string;
  readonly currency: string;
  readonly fullName: string;
  readonly passwordDescription: (minimum: number) => string;
  readonly createBusinessAction: string;
  readonly haveAccount: string;

  readonly categories: string;
  readonly addCategory: string;
  readonly newCategory: string;
  readonly editCategory: string;
  readonly deleteCategory: string;
  readonly deleteCategoryConfirm: (name: string) => string;
  readonly items: string;
  readonly addItem: string;
  readonly newItem: string;
  readonly editItem: string;
  readonly deleteItem: string;
  readonly deleteItemConfirm: (name: string) => string;
  readonly deleteCannotBeUndone: string;
  readonly itemCount: (count: number) => string;
  readonly emptyMenuTitle: string;
  readonly emptyMenuBody: string;
  readonly emptyCategory: string;
  readonly hidden: string;
  readonly soldOut: string;
  readonly availableLabel: (name: string) => string;
  readonly reorder: (name: string) => string;
  readonly orderSaved: string;
  readonly name: string;
  readonly description: string;
  readonly price: string;
  readonly category: string;
  readonly visibleToGuests: string;
  readonly dietarySection: string;
  readonly allergensDeclared: string;
  readonly allergensDeclaredDescription: string;
  readonly allergensLabel: string;
  readonly noAllergensChecked: string;
  readonly dietaryLabelsLabel: string;
  readonly allergenNames: Readonly<Record<string, string>>;
  readonly dietaryLabelNames: Readonly<Record<string, string>>;
  readonly defaultLanguageBadge: string;
  readonly translationsHint: string;
  readonly detailsTab: string;
  readonly modelTab: string;
  readonly saveItemFirst: string;

  readonly modelFile: string;
  readonly modelFileDescription: string;
  readonly appleModelFile: string;
  readonly appleModelFileDescription: string;
  readonly posterFile: string;
  readonly posterFileDescription: string;
  readonly chooseFile: string;
  readonly replaceFile: string;
  readonly removeFile: string;
  readonly dropFileHere: string;
  readonly uploading: string;
  readonly noModelYet: string;
  readonly preview: string;
  readonly previewAlt: (name: string) => string;
  readonly removeModel: string;
  readonly modelRemoved: string;
  readonly processingQueued: string;
  readonly processingRunning: string;
  readonly processingKeepsCurrentModel: string;
  readonly processingFailed: string;
  readonly modelPublished: string;
  readonly latestProcessing: string;
  readonly reportDownload: (before: string, after: string, percent: number) => string;
  readonly reportTriangles: (before: string, after: string) => string;
  readonly reportSizeOnTable: (dimensions: string) => string;
  readonly processingWarning: (code: string) => string;
  readonly replaceGeneratedFiles: string;
  readonly fileReplaced: string;

  readonly qrIntro: string;
  readonly qrLanguageAuto: string;
  readonly menuLink: string;
  readonly copyLink: string;
  readonly linkCopied: string;
  readonly downloadSvg: string;
  readonly downloadPng: string;
  readonly qrAlt: (url: string) => string;
  readonly tableCards: string;
  readonly tableCardsIntro: string;
  readonly tableCount: string;
  readonly printCards: string;
  readonly tableLabel: (label: string) => string;
  readonly scanForMenu: string;

  readonly languagesIntro: string;
  readonly defaultLanguage: string;
  readonly makeDefault: (language: string) => string;
  readonly addLanguage: string;
  readonly removeLanguage: (language: string) => string;
  readonly languagesSaved: string;
  readonly translationsKept: string;

  readonly teamIntro: string;
  readonly members: string;
  readonly you: string;
  readonly joinedOn: (date: string) => string;
  readonly pendingInvitations: string;
  readonly noPendingInvitations: string;
  readonly inviteMember: string;
  readonly inviteTitle: string;
  readonly inviteEmailDescription: string;
  readonly role: string;
  readonly roleDescriptions: Readonly<Record<'Manager' | 'Staff', string>>;
  readonly sendInvitation: string;
  readonly invitationSent: (email: string) => string;
  readonly invitationNotEmailed: string;
  readonly invitedBy: (name: string, date: string) => string;
  readonly expiresOn: (date: string) => string;
  readonly invitationExpired: string;
  readonly resendInvitation: string;
  readonly revokeInvitation: string;
  readonly revokeInvitationConfirm: (email: string) => string;
  readonly revokeInvitationBody: string;
  readonly invitationRevoked: string;
  readonly makeRole: (role: 'Manager' | 'Staff') => string;
  readonly roleChanged: (name: string, role: string) => string;
  readonly removeMember: string;
  readonly removeMemberConfirm: (name: string) => string;
  readonly removeMemberBody: string;
  readonly memberRemoved: (name: string) => string;

  readonly statisticsIntro: string;
  readonly statisticsRange: string;
  readonly lastDays: (days: number) => string;
  readonly menuViews: string;
  readonly dishOpens: string;
  readonly modelViews: string;
  readonly arStarts: string;
  readonly dailyMenuViews: string;
  readonly dayValue: (day: string, value: string) => string;
  readonly showAsTable: string;
  readonly day: string;
  readonly topDishes: string;
  readonly dish: string;
  readonly arRate: string;
  readonly noStatisticsYet: string;
  readonly statisticsPrivacy: string;

  readonly navSettings: string;
  readonly navHistory: string;
  readonly myAccount: string;

  readonly settingsIntro: string;
  readonly branding: string;
  readonly brandingIntro: string;
  readonly brandingSaved: string;
  readonly logo: string;
  readonly logoIntro: string;
  readonly removeLogo: string;
  readonly brandColor: string;
  readonly brandColorIntro: string;
  readonly useDefaultColor: string;
  readonly timeZone: string;
  readonly timeZoneIntro: string;
  readonly timeZoneSaved: string;
  readonly deviceTimeZone: (zone: string) => string;
  readonly useDeviceTimeZone: string;

  readonly transferOwnership: string;
  readonly transferOwnershipTitle: (name: string) => string;
  readonly transferOwnershipBody: (name: string) => string;
  readonly confirmWithPassword: string;
  readonly ownershipTransferred: (name: string) => string;

  readonly accountIntro: string;
  readonly deleteAccount: string;
  readonly deleteAccountIntro: string;
  readonly closingWorkspaces: string;
  readonly closingWorkspacesBody: string;
  readonly workspacesToHandOver: string;
  readonly workspacesToHandOverBody: string;
  readonly workspacesToLeave: string;
  readonly teamSize: (others: number) => string;
  readonly handOverIn: (business: string) => string;
  readonly deleteAccountConfirm: string;
  readonly deleteAccountConfirmBody: string;
  readonly accountDeleted: string;

  readonly exportMenu: string;
  readonly importMenu: string;
  readonly menuImport: {
    readonly title: string;
    readonly rules: readonly string[];
    readonly summary: (created: number, updated: number, unchanged: number) => string;
    readonly created: string;
    readonly hasErrors: (count: number) => string;
    readonly errorsCaption: string;
    readonly line: string;
    readonly column: string;
    readonly problem: string;
    readonly apply: string;
    readonly applied: (created: number, updated: number) => string;
  };

  readonly historyIntro: string;
  readonly noHistoryYet: string;
  readonly loadMore: string;
  readonly system: string;
  readonly deletedAccount: string;
  readonly changes: string;
  readonly history: HistoryMessages;

  readonly joinTitle: (business: string) => string;
  readonly joinDetails: (role: string, email: string) => string;
  readonly joinWithAccount: string;
  readonly joinCreateAccount: string;
  readonly joinAction: string;
  readonly joinLinkMissing: string;
  readonly joinUnavailable: string;

  readonly pageNotFound: string;
  readonly backToStart: string;
}

const tr: Messages = {
  save: 'Kaydet',
  cancel: 'Vazgeç',
  delete: 'Sil',
  edit: 'Düzenle',
  actions: 'İşlemler',
  loading: 'Yükleniyor',
  saved: 'Değişiklikler kaydedildi.',
  networkError: 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.',
  unexpectedError: 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.',
  tryAgain: 'Tekrar dene',
  optional: 'isteğe bağlı',

  mainNavigation: 'Ana gezinme',
  navMenu: 'Menü',
  navQrCodes: 'QR kodları',
  navLanguages: 'Diller',
  navTeam: 'Ekip',
  navStatistics: 'İstatistikler',
  otherWorkspaces: 'Diğer işletmeleriniz',
  allWorkspaces: 'Tüm işletmeler',
  verifyEmailBanner: (email) =>
    `${email} adresinizi doğrulayın: ekibinizi davet edebilmek ve şifrenizi unutursanız hesabınıza dönebilmek için gerekli.`,
  resendVerification: 'Doğrulama e-postasını tekrar gönder',
  verificationSent: 'Doğrulama e-postası gönderildi.',
  viewGuestMenu: 'Misafir menüsünü aç',
  signOut: 'Çıkış yap',
  roles: { Owner: 'İşletme sahibi', Manager: 'Yönetici', Staff: 'Personel' },
  interfaceLanguage: 'Arayüz dili',

  workspaceTitle: 'İşletmenize giriş yapın',
  workspaceIntro: 'Menü bağlantınızdaki işletme adresini girin.',
  workspaceLabel: 'İşletme adresi',
  workspaceDescription: 'Örneğin …/m/kadikoy-burger-lab bağlantısı için “kadikoy-burger-lab”.',
  continue: 'Devam',
  newToArMenu: 'ArMenu’yü ilk kez mi kullanıyorsunuz?',
  createBusiness: 'İşletmenizi oluşturun',

  signInTitle: (business) => `${business} için giriş yapın`,
  email: 'E-posta',
  password: 'Şifre',
  signIn: 'Giriş yap',
  otherBusiness: 'Başka bir işletme',
  sessionEnded: 'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.',
  forgotPassword: 'Şifremi unuttum',
  forgotPasswordTitle: 'Şifrenizi sıfırlayın',
  forgotPasswordIntro:
    'Hesabınızın e-posta adresini girin; şifrenizi sıfırlamanız için bir bağlantı gönderelim.',
  sendResetLink: 'Bağlantıyı gönder',
  resetLinkSent: (email) =>
    `${email} adresine ait bir hesap varsa, birkaç dakika içinde bir sıfırlama bağlantısı gelecek. Bağlantı 1 saat geçerli.`,
  resetPasswordTitle: 'Yeni şifre belirleyin',
  newPassword: 'Yeni şifre',
  setNewPassword: 'Şifreyi değiştir',
  passwordChanged: 'Şifreniz değişti. Tüm cihazlarınızda yeni şifrenizle giriş yapın.',
  linkMissing: 'Bu sayfayı e-postanızdaki bağlantıyla açın.',
  linkUnavailable: 'Bu bağlantı kullanılamıyor',
  verifyEmailTitle: 'E-posta doğrulama',
  emailVerified: 'E-posta adresiniz doğrulandı.',
  continueToDashboard: 'Panele devam et',
  backToSignIn: 'Girişe dön',

  signUpTitle: 'İşletmenizi oluşturun',
  businessName: 'İşletme adı',
  menuAddress: 'Menü adresi',
  menuAddressDescription: (url) => `Misafirleriniz menünüzü ${url} adresinde açar. Sonradan değiştirilemez.`,
  menuLanguage: 'Menü dili',
  currency: 'Para birimi',
  fullName: 'Adınız soyadınız',
  passwordDescription: (minimum) => `En az ${minimum} karakter.`,
  createBusinessAction: 'İşletmeyi oluştur',
  haveAccount: 'Zaten hesabınız var mı?',

  categories: 'Kategoriler',
  addCategory: 'Kategori ekle',
  newCategory: 'Yeni kategori',
  editCategory: 'Kategoriyi düzenle',
  deleteCategory: 'Kategoriyi sil',
  deleteCategoryConfirm: (name) => `“${name}” kategorisi silinsin mi?`,
  items: 'Ürünler',
  addItem: 'Ürün ekle',
  newItem: 'Yeni ürün',
  editItem: 'Ürünü düzenle',
  deleteItem: 'Ürünü sil',
  deleteItemConfirm: (name) => `“${name}” menüden silinsin mi?`,
  deleteCannotBeUndone: 'Misafirler bu içeriği artık görmez.',
  itemCount: (count) => `${count} ürün`,
  emptyMenuTitle: 'Menünüz henüz boş',
  emptyMenuBody: 'Bir kategoriyle başlayın, örneğin “Ana yemekler”.',
  emptyCategory: 'Bu kategoride henüz ürün yok.',
  hidden: 'Gizli',
  soldOut: 'Tükendi',
  availableLabel: (name) => `${name} satışta`,
  reorder: (name) => `${name} sırasını değiştir`,
  orderSaved: 'Sıralama kaydedildi.',
  name: 'Ad',
  description: 'Açıklama',
  price: 'Fiyat',
  category: 'Kategori',
  visibleToGuests: 'Misafirlere göster',
  dietarySection: 'Alerjenler ve diyet',
  allergensDeclared: 'Alerjen bilgisini girdim',
  allergensDeclaredDescription:
    'Kapalıyken misafirler bu yemeğin alerjen bilgisinin verilmediğini görür ve alerjen filtresinde yemek gösterilmez.',
  allergensLabel: 'İçerdiği alerjenler',
  noAllergensChecked:
    'Hiçbiri işaretli değil: misafirler bu yemeğin 14 alerjenin hiçbirini içermediğini görür.',
  dietaryLabelsLabel: 'Uygun olduğu diyetler',
  allergenNames: {
    gluten: 'Gluten',
    crustaceans: 'Kabuklular',
    eggs: 'Yumurta',
    fish: 'Balık',
    peanuts: 'Yer fıstığı',
    soybeans: 'Soya',
    milk: 'Süt',
    nuts: 'Sert kabuklu yemişler',
    celery: 'Kereviz',
    mustard: 'Hardal',
    sesame: 'Susam',
    sulphites: 'Sülfitler',
    lupin: 'Acı bakla',
    molluscs: 'Yumuşakçalar',
  },
  dietaryLabelNames: { vegetarian: 'Vejetaryen', vegan: 'Vegan', glutenFree: 'Glutensiz' },
  defaultLanguageBadge: 'varsayılan',
  translationsHint: 'Boş bıraktığınız dillerde misafirler varsayılan dildeki metni görür.',
  detailsTab: 'Bilgiler',
  modelTab: '3D model',
  saveItemFirst: '3D model eklemek için önce ürünü kaydedin.',

  modelFile: '3D model (GLB)',
  modelFileDescription:
    '3D programınızdan ya da taramadan çıkan GLB dosyası, en fazla 64 MB. Optimize edilmiş model, iPhone modeli ve poster otomatik oluşturulur.',
  appleModelFile: 'iOS modeli (USDZ)',
  appleModelFileDescription:
    'Otomatik oluşturulur. Yalnızca kendi USDZ dosyanızı kullanmak istiyorsanız değiştirin.',
  posterFile: 'Poster',
  posterFileDescription:
    'Model yüklenirken ve menü listesinde görünür; otomatik oluşturulur. WebP, AVIF, PNG veya JPEG; en fazla 2 MB.',
  chooseFile: 'Dosya seç',
  replaceFile: 'Değiştir',
  removeFile: 'Kaldır',
  dropFileHere: 'veya dosyayı buraya bırakın',
  uploading: 'Yükleniyor',
  noModelYet: 'Henüz 3D model yok',
  preview: 'Misafirlerin gördüğü model',
  previewAlt: (name) => `${name} 3D önizlemesi`,
  removeModel: '3D modeli kaldır',
  modelRemoved: '3D model kaldırıldı.',
  processingQueued: 'Model işlenmek için sırada…',
  processingRunning: 'Model optimize ediliyor…',
  processingKeepsCurrentModel:
    'Yeni model hazır olana kadar misafirler mevcut modeli görür. Bu pencereyi kapatabilirsiniz.',
  processingFailed: 'Model yayımlanamadı',
  modelPublished: 'Yeni 3D model yayımlandı. Misafirler hemen görebilir.',
  latestProcessing: 'Son işleme',
  reportDownload: (before, after, percent) => `İndirme: ${before} → ${after} (%${percent} daha küçük)`,
  reportTriangles: (before, after) => `Üçgen: ${before} → ${after}`,
  reportSizeOnTable: (dimensions) => `Masadaki boyut: ${dimensions}`,
  processingWarning: (code) =>
    ({
      'model.simplified': 'Model, telefonlarda akıcı dönmesi için sadeleştirildi.',
      'model.textures_downscaled': 'Dokular telefonların ihtiyaç duyduğu boyuta küçültüldü.',
      'model.unusual_size':
        'Model 1,5 metreden büyük ya da 2 santimetreden küçük. AR yemeği gerçek boyutunda gösterir: dışa aktarırken birimin metre olduğunu kontrol edin.',
      'model.many_materials':
        'Modelde 10’dan fazla malzeme var; birleştirmek telefonlarda daha hızlı çizilmesini sağlar.',
      'model.large_download':
        'Model optimize edildikten sonra da 5 MB’tan büyük; mobil ağda yavaş açılabilir.',
      'model.scene_viewer_too_large': 'Android AR dosyası Google’ın önerdiği 10 MB sınırını aşıyor.',
    })[code] ?? code,
  replaceGeneratedFiles: 'Oluşturulan dosyaları değiştir',
  fileReplaced: 'Dosya değiştirildi.',

  qrIntro: 'Masalarınızdaki QR kod, menünüzü misafirin telefonunda doğrudan açar.',
  qrLanguageAuto: 'Misafirin telefon dili',
  menuLink: 'Menü bağlantısı',
  copyLink: 'Bağlantıyı kopyala',
  linkCopied: 'Bağlantı kopyalandı.',
  downloadSvg: 'SVG indir',
  downloadPng: 'PNG indir',
  qrAlt: (url) => `${url} adresini açan QR kod`,
  tableCards: 'Masa kartları',
  tableCardsIntro: 'Her masa için numaralı bir kart yazdırın; misafir menüde masa numarasını görür.',
  tableCount: 'Masa sayısı',
  printCards: 'Kartları yazdır',
  tableLabel: (label) => `Masa ${label}`,
  scanForMenu: 'Menü için okutun',

  languagesIntro:
    'Menünüzün sunulduğu diller. Misafirler telefonlarının dilini, yoksa varsayılan dili görür.',
  defaultLanguage: 'Varsayılan dil',
  makeDefault: (language) => `${language} varsayılan olsun`,
  addLanguage: 'Dil ekle',
  removeLanguage: (language) => `${language} dilini kaldır`,
  languagesSaved: 'Diller kaydedildi.',
  translationsKept: 'Kaldırdığınız bir dildeki çeviriler silinmez; dili yeniden eklerseniz geri gelir.',

  teamIntro:
    'Menünüzü birlikte yönettiğiniz kişiler. Yöneticiler menüyü ve 3D modelleri düzenler, personel ürünleri tükendi olarak işaretler.',
  members: 'Ekip üyeleri',
  you: 'siz',
  joinedOn: (date) => `${date} tarihinde katıldı`,
  pendingInvitations: 'Bekleyen davetler',
  noPendingInvitations: 'Bekleyen davet yok.',
  inviteMember: 'Davet et',
  inviteTitle: 'Ekibe davet et',
  inviteEmailDescription: 'Davet bağlantısı bu adrese e-postayla gönderilir ve 7 gün geçerlidir.',
  role: 'Rol',
  roleDescriptions: {
    Manager: 'Menüyü, fiyatları ve 3D modelleri düzenler.',
    Staff: 'Menüyü görür, ürünleri tükendi olarak işaretler.',
  },
  sendInvitation: 'Daveti gönder',
  invitationSent: (email) => `${email} adresine davet gönderildi.`,
  invitationNotEmailed: 'Davet kaydedildi ancak e-posta gönderilemedi. Birazdan tekrar göndermeyi deneyin.',
  invitedBy: (name, date) => `${name} davet etti · ${date}`,
  expiresOn: (date) => `${date} tarihine kadar geçerli`,
  invitationExpired: 'Süresi doldu',
  resendInvitation: 'Tekrar gönder',
  revokeInvitation: 'Daveti geri al',
  revokeInvitationConfirm: (email) => `${email} davetini geri al`,
  revokeInvitationBody:
    'E-postadaki bağlantı çalışmayı bırakır. İsterseniz daha sonra yeniden davet edebilirsiniz.',
  invitationRevoked: 'Davet geri alındı.',
  makeRole: (role) => (role === 'Manager' ? 'Yönetici yap' : 'Personel yap'),
  roleChanged: (name, role) =>
    `${name} artık ${role.toLocaleLowerCase('tr')}. Değişiklik birkaç dakika içinde geçerli olur.`,
  removeMember: 'Ekipten çıkar',
  removeMemberConfirm: (name) => `${name} ekipten çıkarılsın mı?`,
  removeMemberBody:
    'Oturumları sona erer ve bu işletmeye artık giriş yapamaz. Hesabı ve başka işletmelerdeki üyelikleri silinmez.',
  memberRemoved: (name) => `${name} ekipten çıkarıldı.`,

  statisticsIntro:
    'Misafirlerin menünüzde ne yaptığı, gün gün. Misafirleri tanımlayan hiçbir bilgi toplanmaz.',
  statisticsRange: 'Dönem',
  lastDays: (days) => `Son ${days} gün`,
  menuViews: 'Menü görüntüleme',
  dishOpens: 'Açılan yemek',
  modelViews: '3D görüntüleme',
  arStarts: 'AR başlatma',
  dailyMenuViews: 'Günlük menü görüntüleme',
  dayValue: (day, value) => `${day}: ${value} görüntüleme`,
  showAsTable: 'Tablo olarak göster',
  day: 'Gün',
  topDishes: 'En çok açılan yemekler',
  dish: 'Yemek',
  arRate: 'AR oranı',
  noStatisticsYet: 'Bu dönemde henüz misafir hareketi yok. QR kodunuz okutuldukça burada görünecek.',
  statisticsPrivacy:
    'Sayılar sayfa açılışlarını gösterir, kişileri değil: aynı misafir menüyü iki kez açarsa iki kez sayılır. Günler işletmenizin saat dilimine göredir.',

  navSettings: 'Ayarlar',
  navHistory: 'Geçmiş',
  myAccount: 'Hesabım',

  settingsIntro: 'İşletmenizin görünümü, menünüzün dilleri ve saat dilimi.',
  branding: 'Görünüm',
  brandingIntro: 'Misafirlerinizin menünüzün başında gördüğü ad, logo ve renk.',
  brandingSaved: 'Görünüm kaydedildi.',
  logo: 'Logo',
  logoIntro: 'WebP, AVIF, PNG veya JPEG; en çok 512 KB. Kare bir görsel en iyi görünür.',
  removeLogo: 'Logoyu kaldır',
  brandColor: 'Renk',
  brandColorIntro:
    'Menüdeki şerit ve düğmeler bu renkle boyanır. Yazılar okunaklı kalsın diye menünün kendi renkleriyle yazılır.',
  useDefaultColor: 'Varsayılan renge dön',
  timeZone: 'Saat dilimi',
  timeZoneIntro:
    'Günlük istatistikler bu saat dilimine göre gece yarısı başlar. Değiştirdiğinizde geçmiş günler olduğu gibi kalır.',
  timeZoneSaved: 'Saat dilimi kaydedildi.',
  deviceTimeZone: (zone) => `Bu cihazın saat dilimi: ${zone}`,
  useDeviceTimeZone: 'Bunu kullan',

  transferOwnership: 'Sahipliği devret',
  transferOwnershipTitle: (name) => `İşletme ${name} kişisine devredilsin mi?`,
  transferOwnershipBody: (name) =>
    `${name} işletmenin sahibi olur; ekibi o yönetir. Siz yönetici olarak kalırsınız ve sahipliği yalnızca yeni sahip size geri devredebilir.`,
  confirmWithPassword: 'Onaylamak için şifrenizi girin',
  ownershipTransferred: (name) => `İşletmenin sahibi artık ${name}.`,

  accountIntro: 'Tüm işletmelerde kullandığınız hesabınız.',
  deleteAccount: 'Hesabımı sil',
  deleteAccountIntro:
    'Adınız, e-posta adresiniz ve şifreniz kalıcı olarak silinir; tüm ekiplerden çıkarılırsınız. Yaptığınız değişiklikler geçmişte “silinmiş hesap” olarak görünür.',
  closingWorkspaces: 'Hesabınızla birlikte kapanacak işletmeler',
  closingWorkspacesBody:
    'Bu işletmelerde sizden başka kimse yok. Kapanınca menüleri gösterilmez ve QR kodları çalışmaz.',
  workspacesToHandOver: 'Önce devretmeniz gereken işletmeler',
  workspacesToHandOverBody:
    'Bu işletmelerin ekibi var. Hesabınızı silmeden önce sahipliği bir ekip üyesine devredin.',
  workspacesToLeave: 'Ekibinden ayrılacağınız işletmeler',
  teamSize: (others) => `${others} ekip üyesi daha`,
  handOverIn: (business) => `${business} ekibine git`,
  deleteAccountConfirm: 'Hesabınız kalıcı olarak silinsin mi?',
  deleteAccountConfirmBody: 'Bu işlem geri alınamaz.',
  accountDeleted: 'Hesabınız silindi. Adresinize bir bilgilendirme e-postası gönderdik.',

  exportMenu: 'Dışa aktar',
  importMenu: 'İçe aktar',
  menuImport: {
    title: 'Menüyü tablodan içe aktar',
    rules: [
      'Önce menüyü dışa aktarın, tabloda düzenleyin ve aynı dosyayı buraya yükleyin.',
      'Kimliği (id) olan satırlar o ürünü günceller; kimliği boş satırlar yeni ürün ekler. Dosyada olmayan ürünlere dokunulmaz.',
      'Excel veya Google E-Tablolar’dan CSV olarak kaydedin; noktalı virgül ve 185,50 gibi fiyatlar da olur.',
      'Tek bir satırda sorun varsa hiçbir değişiklik yapılmaz; önce ne olacağını görürsünüz.',
    ],
    summary: (created, updated, unchanged) =>
      `${created} ürün eklenecek, ${updated} ürün güncellenecek, ${unchanged} ürün aynı kalacak.`,
    created: 'Yeni ürün',
    hasErrors: (count) => `Dosyada ${count} sorun var. Düzeltip yeniden yükleyin; hiçbir şey değiştirilmedi.`,
    errorsCaption: 'Dosyadaki sorunlar',
    line: 'Satır',
    column: 'Sütun',
    problem: 'Sorun',
    apply: 'Değişiklikleri uygula',
    applied: (created, updated) => `${created} ürün eklendi, ${updated} ürün güncellendi.`,
  },

  historyIntro: 'Menüde, ekipte ve işletme ayarlarında kimin neyi ne zaman değiştirdiği.',
  noHistoryYet: 'Henüz kayıtlı bir değişiklik yok.',
  loadMore: 'Daha eskileri göster',
  system: 'Sistem',
  deletedAccount: 'Silinmiş hesap',
  changes: 'Değişiklikler',
  history: {
    itemCreated: (name) => `“${name}” ürününü ekledi`,
    itemUpdated: (name) => `“${name}” ürününü düzenledi`,
    itemDeleted: (name) => `“${name}” ürününü sildi`,
    itemSoldOut: (name) => `“${name}” ürününü tükendi olarak işaretledi`,
    itemBackOnSale: (name) => `“${name}” ürününü yeniden satışa açtı`,
    categoryCreated: (name) => `“${name}” kategorisini ekledi`,
    categoryUpdated: (name) => `“${name}” kategorisini düzenledi`,
    categoryDeleted: (name) => `“${name}” kategorisini sildi`,
    itemsReordered: (category) => `“${category}” kategorisindeki ürünleri yeniden sıraladı`,
    categoriesReordered: 'kategorileri yeniden sıraladı',
    businessUpdated: 'işletme ayarlarını değiştirdi',
    memberJoined: 'ekibe katıldı',
    memberLeft: 'hesabını sildi ve ekipten ayrıldı',
    memberRoleChanged: (person) => `${person} kişisinin rolünü değiştirdi`,
    memberRemoved: (person) => `${person} kişisini ekipten çıkardı`,
    fields: {
      name: 'Ad',
      description: 'Açıklama',
      price: 'Fiyat',
      category: 'Kategori',
      photo: 'Fotoğraf',
      model: '3D model',
      visible: 'Görünürlük',
      available: 'Durum',
      defaultLanguage: 'Varsayılan dil',
      languages: 'Diller',
      timeZone: 'Saat dilimi',
      logo: 'Logo',
      accentColor: 'Renk',
      allergens: 'Alerjenler',
      dietaryLabels: 'Diyetler',
      role: 'Rol',
    },
    notDeclared: 'Girilmemiş',
    noAllergens: 'Yok',
    present: 'Var',
    absent: 'Yok',
    replaced: 'Değiştirildi',
    visible: 'Görünür',
    hidden: 'Gizli',
    onSale: 'Satışta',
    soldOut: 'Tükendi',
  },

  joinTitle: (business) => `${business} ekibine katılın`,
  joinDetails: (role, email) => `${email} adresine ${role.toLocaleLowerCase('tr')} olarak davet edildiniz.`,
  joinWithAccount: 'Bu adresle zaten bir ArMenu hesabınız var. Katılmak için şifrenizi girin.',
  joinCreateAccount: 'Katılmak için hesabınızı oluşturun.',
  joinAction: 'Ekibe katıl',
  joinLinkMissing: 'Bu sayfayı davet e-postanızdaki bağlantıyla açın.',
  joinUnavailable: 'Bu davet kullanılamıyor',

  pageNotFound: 'Sayfa bulunamadı',
  backToStart: 'Başlangıca dön',
};

const en: Messages = {
  save: 'Save',
  cancel: 'Cancel',
  delete: 'Delete',
  edit: 'Edit',
  actions: 'Actions',
  loading: 'Loading',
  saved: 'Changes saved.',
  networkError: 'The server could not be reached. Check your connection and try again.',
  unexpectedError: 'Something went wrong. Please try again.',
  tryAgain: 'Try again',
  optional: 'optional',

  mainNavigation: 'Main navigation',
  navMenu: 'Menu',
  navQrCodes: 'QR codes',
  navLanguages: 'Languages',
  navTeam: 'Team',
  navStatistics: 'Statistics',
  otherWorkspaces: 'Your other businesses',
  allWorkspaces: 'All businesses',
  verifyEmailBanner: (email) =>
    `Confirm ${email}: it lets you invite your team and get back into your account if you forget the password.`,
  resendVerification: 'Send the confirmation e-mail again',
  verificationSent: 'Confirmation e-mail sent.',
  viewGuestMenu: 'Open guest menu',
  signOut: 'Sign out',
  roles: { Owner: 'Owner', Manager: 'Manager', Staff: 'Staff' },
  interfaceLanguage: 'Interface language',

  workspaceTitle: 'Sign in to your business',
  workspaceIntro: 'Enter the business address from your menu link.',
  workspaceLabel: 'Business address',
  workspaceDescription: 'For example “kadikoy-burger-lab” for the link …/m/kadikoy-burger-lab.',
  continue: 'Continue',
  newToArMenu: 'New to ArMenu?',
  createBusiness: 'Create your business',

  signInTitle: (business) => `Sign in to ${business}`,
  email: 'Email',
  password: 'Password',
  signIn: 'Sign in',
  otherBusiness: 'A different business',
  sessionEnded: 'Your session has ended. Please sign in again.',
  forgotPassword: 'Forgot password?',
  forgotPasswordTitle: 'Reset your password',
  forgotPasswordIntro:
    'Enter the e-mail address of your account and we will send a link to reset your password.',
  sendResetLink: 'Send the link',
  resetLinkSent: (email) =>
    `If an account uses ${email}, a reset link arrives within a few minutes. It works for 1 hour.`,
  resetPasswordTitle: 'Choose a new password',
  newPassword: 'New password',
  setNewPassword: 'Change password',
  passwordChanged: 'Your password was changed. Sign in with it on all your devices.',
  linkMissing: 'Open this page with the link in your e-mail.',
  linkUnavailable: 'This link cannot be used',
  verifyEmailTitle: 'E-mail confirmation',
  emailVerified: 'Your e-mail address is confirmed.',
  continueToDashboard: 'Continue to the dashboard',
  backToSignIn: 'Back to sign in',

  signUpTitle: 'Create your business',
  businessName: 'Business name',
  menuAddress: 'Menu address',
  menuAddressDescription: (url) => `Guests open your menu at ${url}. It cannot be changed later.`,
  menuLanguage: 'Menu language',
  currency: 'Currency',
  fullName: 'Your full name',
  passwordDescription: (minimum) => `At least ${minimum} characters.`,
  createBusinessAction: 'Create business',
  haveAccount: 'Already have an account?',

  categories: 'Categories',
  addCategory: 'Add category',
  newCategory: 'New category',
  editCategory: 'Edit category',
  deleteCategory: 'Delete category',
  deleteCategoryConfirm: (name) => `Delete the “${name}” category?`,
  items: 'Items',
  addItem: 'Add item',
  newItem: 'New item',
  editItem: 'Edit item',
  deleteItem: 'Delete item',
  deleteItemConfirm: (name) => `Remove “${name}” from the menu?`,
  deleteCannotBeUndone: 'Guests will no longer see it.',
  itemCount: (count) => `${count} ${count === 1 ? 'item' : 'items'}`,
  emptyMenuTitle: 'Your menu is still empty',
  emptyMenuBody: 'Start with a category, such as “Main courses”.',
  emptyCategory: 'No items in this category yet.',
  hidden: 'Hidden',
  soldOut: 'Sold out',
  availableLabel: (name) => `${name} available`,
  reorder: (name) => `Reorder ${name}`,
  orderSaved: 'Order saved.',
  name: 'Name',
  description: 'Description',
  price: 'Price',
  category: 'Category',
  visibleToGuests: 'Show to guests',
  dietarySection: 'Allergens and diets',
  allergensDeclared: 'I have entered the allergens',
  allergensDeclaredDescription:
    'While this is off, guests see that allergen information was not given, and allergen filters leave the dish out.',
  allergensLabel: 'Allergens it contains',
  noAllergensChecked: 'None ticked: guests see that the dish contains none of the 14 allergens.',
  dietaryLabelsLabel: 'Diets it suits',
  allergenNames: {
    gluten: 'Gluten',
    crustaceans: 'Crustaceans',
    eggs: 'Eggs',
    fish: 'Fish',
    peanuts: 'Peanuts',
    soybeans: 'Soybeans',
    milk: 'Milk',
    nuts: 'Tree nuts',
    celery: 'Celery',
    mustard: 'Mustard',
    sesame: 'Sesame',
    sulphites: 'Sulphites',
    lupin: 'Lupin',
    molluscs: 'Molluscs',
  },
  dietaryLabelNames: { vegetarian: 'Vegetarian', vegan: 'Vegan', glutenFree: 'Gluten-free' },
  defaultLanguageBadge: 'default',
  translationsHint: 'Wherever a translation is left empty, guests see the default language.',
  detailsTab: 'Details',
  modelTab: '3D model',
  saveItemFirst: 'Save the item before adding a 3D model.',

  modelFile: '3D model (GLB)',
  modelFileDescription:
    'The GLB from your 3D tool or scan, up to 64 MB. The optimized model, the iPhone model and the poster are created automatically.',
  appleModelFile: 'iOS model (USDZ)',
  appleModelFileDescription: 'Created automatically. Replace it only to use your own USDZ.',
  posterFile: 'Poster',
  posterFileDescription:
    'Shown while the model loads and in the menu list; created automatically. WebP, AVIF, PNG or JPEG, up to 2 MB.',
  chooseFile: 'Choose file',
  replaceFile: 'Replace',
  removeFile: 'Remove',
  dropFileHere: 'or drop the file here',
  uploading: 'Uploading',
  noModelYet: 'No 3D model yet',
  preview: 'The model guests see',
  previewAlt: (name) => `3D preview of ${name}`,
  removeModel: 'Remove 3D model',
  modelRemoved: '3D model removed.',
  processingQueued: 'The model is waiting to be processed…',
  processingRunning: 'Optimizing the model…',
  processingKeepsCurrentModel:
    'Guests keep seeing the current model until the new one is ready. You can close this window.',
  processingFailed: 'The model could not be published',
  modelPublished: 'The new 3D model is live. Guests can see it right away.',
  latestProcessing: 'Latest processing',
  reportDownload: (before, after, percent) => `Download: ${before} → ${after} (${percent}% smaller)`,
  reportTriangles: (before, after) => `Triangles: ${before} → ${after}`,
  reportSizeOnTable: (dimensions) => `Size on the table: ${dimensions}`,
  processingWarning: (code) =>
    ({
      'model.simplified': 'The model was simplified so it turns smoothly on phones.',
      'model.textures_downscaled': 'Textures were reduced to the size phones need.',
      'model.unusual_size':
        'The model is over 1.5 meters or under 2 centimeters. AR shows dishes at their real size: check that it was exported in meters.',
      'model.many_materials':
        'The model has more than 10 materials; merging them makes it draw faster on phones.',
      'model.large_download':
        'Even optimized, the model is over 5 MB and may open slowly on mobile networks.',
      'model.scene_viewer_too_large': 'The Android AR file is over Google’s recommended 10 MB.',
    })[code] ?? code,
  replaceGeneratedFiles: 'Replace generated files',
  fileReplaced: 'File replaced.',

  qrIntro: 'The QR code on your tables opens your menu straight on the guest’s phone.',
  qrLanguageAuto: 'Guest’s phone language',
  menuLink: 'Menu link',
  copyLink: 'Copy link',
  linkCopied: 'Link copied.',
  downloadSvg: 'Download SVG',
  downloadPng: 'Download PNG',
  qrAlt: (url) => `QR code opening ${url}`,
  tableCards: 'Table cards',
  tableCardsIntro: 'Print a numbered card for each table; guests see the table number in the menu.',
  tableCount: 'Number of tables',
  printCards: 'Print cards',
  tableLabel: (label) => `Table ${label}`,
  scanForMenu: 'Scan for the menu',

  languagesIntro:
    'The languages your menu is offered in. Guests get their phone’s language, otherwise the default.',
  defaultLanguage: 'Default language',
  makeDefault: (language) => `Make ${language} the default`,
  addLanguage: 'Add language',
  removeLanguage: (language) => `Remove ${language}`,
  languagesSaved: 'Languages saved.',
  translationsKept: 'Translations in a removed language are kept and come back if you add it again.',

  teamIntro:
    'The people you run your menu with. Managers edit the menu and 3D models; staff mark dishes as sold out.',
  members: 'Team members',
  you: 'you',
  joinedOn: (date) => `Joined ${date}`,
  pendingInvitations: 'Pending invitations',
  noPendingInvitations: 'No pending invitations.',
  inviteMember: 'Invite',
  inviteTitle: 'Invite to the team',
  inviteEmailDescription: 'The invitation link is emailed to this address and works for 7 days.',
  role: 'Role',
  roleDescriptions: {
    Manager: 'Edits the menu, prices and 3D models.',
    Staff: 'Sees the menu and marks dishes as sold out.',
  },
  sendInvitation: 'Send invitation',
  invitationSent: (email) => `Invitation sent to ${email}.`,
  invitationNotEmailed:
    'The invitation was saved, but the email could not be sent. Try sending it again shortly.',
  invitedBy: (name, date) => `Invited by ${name} · ${date}`,
  expiresOn: (date) => `Valid until ${date}`,
  invitationExpired: 'Expired',
  resendInvitation: 'Send again',
  revokeInvitation: 'Withdraw invitation',
  revokeInvitationConfirm: (email) => `Withdraw the invitation to ${email}`,
  revokeInvitationBody: 'The link in the email stops working. You can invite them again later.',
  invitationRevoked: 'Invitation withdrawn.',
  makeRole: (role) => (role === 'Manager' ? 'Make manager' : 'Make staff'),
  roleChanged: (name, role) => `${name} is now ${role.toLowerCase()}. It takes effect within a few minutes.`,
  removeMember: 'Remove from team',
  removeMemberConfirm: (name) => `Remove ${name} from the team?`,
  removeMemberBody:
    'Their sessions end and they can no longer sign in to this business. Their account and other memberships are kept.',
  memberRemoved: (name) => `${name} was removed from the team.`,

  statisticsIntro: 'What guests do on your menu, day by day. Nothing that identifies a guest is collected.',
  statisticsRange: 'Period',
  lastDays: (days) => `Last ${days} days`,
  menuViews: 'Menu views',
  dishOpens: 'Dishes opened',
  modelViews: '3D views',
  arStarts: 'AR starts',
  dailyMenuViews: 'Daily menu views',
  dayValue: (day, value) => `${day}: ${value} views`,
  showAsTable: 'Show as a table',
  day: 'Day',
  topDishes: 'Most opened dishes',
  dish: 'Dish',
  arRate: 'AR rate',
  noStatisticsYet: 'No guest activity in this period yet. It shows up here as your QR code gets scanned.',
  statisticsPrivacy:
    'Counts are page loads, not people: a guest opening the menu twice counts twice. Days follow your business’s time zone.',

  navSettings: 'Settings',
  navHistory: 'History',
  myAccount: 'My account',

  settingsIntro: 'How your business looks, the languages of your menu and its time zone.',
  branding: 'Appearance',
  brandingIntro: 'The name, logo and colour guests see at the top of your menu.',
  brandingSaved: 'Appearance saved.',
  logo: 'Logo',
  logoIntro: 'WebP, AVIF, PNG or JPEG, up to 512 KB. A square image looks best.',
  removeLogo: 'Remove logo',
  brandColor: 'Colour',
  brandColorIntro:
    'The band and the buttons on your menu are filled with this colour. Text stays in the menu’s own colours, so it is always legible.',
  useDefaultColor: 'Back to the default colour',
  timeZone: 'Time zone',
  timeZoneIntro:
    'Daily statistics start at midnight in this time zone. Changing it leaves past days as they were counted.',
  timeZoneSaved: 'Time zone saved.',
  deviceTimeZone: (zone) => `This device is in ${zone}`,
  useDeviceTimeZone: 'Use it',

  transferOwnership: 'Hand over ownership',
  transferOwnershipTitle: (name) => `Hand the business over to ${name}?`,
  transferOwnershipBody: (name) =>
    `${name} becomes the owner and runs the team. You stay on as a manager, and only the new owner can hand it back to you.`,
  confirmWithPassword: 'Enter your password to confirm',
  ownershipTransferred: (name) => `${name} now owns the business.`,

  accountIntro: 'The account you use in every business.',
  deleteAccount: 'Delete my account',
  deleteAccountIntro:
    'Your name, email address and password are deleted for good, and you leave every team. Changes you made show as “deleted account” in history.',
  closingWorkspaces: 'Businesses that close with your account',
  closingWorkspacesBody:
    'Nobody else is in these businesses. Once closed, their menus are no longer shown and their QR codes stop working.',
  workspacesToHandOver: 'Businesses to hand over first',
  workspacesToHandOverBody:
    'These businesses have a team. Hand ownership over to a team member before deleting your account.',
  workspacesToLeave: 'Teams you will leave',
  teamSize: (others) => (others === 1 ? '1 other member' : `${others} other members`),
  handOverIn: (business) => `Go to the ${business} team`,
  deleteAccountConfirm: 'Delete your account for good?',
  deleteAccountConfirmBody: 'This cannot be undone.',
  accountDeleted: 'Your account was deleted. We sent a notice to your address.',

  exportMenu: 'Export',
  importMenu: 'Import',
  menuImport: {
    title: 'Import the menu from a spreadsheet',
    rules: [
      'Export the menu first, edit it in a spreadsheet and upload the same file here.',
      'Rows with an id update that dish; rows with an empty id add a dish. Dishes not in the file are left alone.',
      'Save it as CSV from Excel or Google Sheets; semicolons and prices such as 185,50 work too.',
      'If any row has a problem nothing changes, and you see what would happen before anything does.',
    ],
    summary: (created, updated, unchanged) =>
      `${created} to add, ${updated} to update, ${unchanged} unchanged.`,
    created: 'New dish',
    hasErrors: (count) =>
      count === 1
        ? 'The file has 1 problem. Fix it and upload again; nothing was changed.'
        : `The file has ${count} problems. Fix them and upload again; nothing was changed.`,
    errorsCaption: 'Problems in the file',
    line: 'Line',
    column: 'Column',
    problem: 'Problem',
    apply: 'Apply changes',
    applied: (created, updated) => `${created} added, ${updated} updated.`,
  },

  historyIntro: 'Who changed what, and when, in the menu, the team and the business settings.',
  noHistoryYet: 'No changes recorded yet.',
  loadMore: 'Show older changes',
  system: 'System',
  deletedAccount: 'Deleted account',
  changes: 'Changes',
  history: {
    itemCreated: (name) => `added “${name}”`,
    itemUpdated: (name) => `edited “${name}”`,
    itemDeleted: (name) => `deleted “${name}”`,
    itemSoldOut: (name) => `marked “${name}” as sold out`,
    itemBackOnSale: (name) => `put “${name}” back on sale`,
    categoryCreated: (name) => `added the “${name}” category`,
    categoryUpdated: (name) => `edited the “${name}” category`,
    categoryDeleted: (name) => `deleted the “${name}” category`,
    itemsReordered: (category) => `reordered the dishes in “${category}”`,
    categoriesReordered: 'reordered the categories',
    businessUpdated: 'changed the business settings',
    memberJoined: 'joined the team',
    memberLeft: 'deleted their account and left the team',
    memberRoleChanged: (person) => `changed the role of ${person}`,
    memberRemoved: (person) => `removed ${person} from the team`,
    fields: {
      name: 'Name',
      description: 'Description',
      price: 'Price',
      category: 'Category',
      photo: 'Photo',
      model: '3D model',
      visible: 'Visibility',
      available: 'Availability',
      defaultLanguage: 'Default language',
      languages: 'Languages',
      timeZone: 'Time zone',
      logo: 'Logo',
      accentColor: 'Colour',
      allergens: 'Allergens',
      dietaryLabels: 'Diets',
      role: 'Role',
    },
    notDeclared: 'Not entered',
    noAllergens: 'None',
    present: 'Yes',
    absent: 'None',
    replaced: 'Replaced',
    visible: 'Visible',
    hidden: 'Hidden',
    onSale: 'On sale',
    soldOut: 'Sold out',
  },

  joinTitle: (business) => `Join the ${business} team`,
  joinDetails: (role, email) => `${email} was invited as ${role.toLowerCase()}.`,
  joinWithAccount: 'You already have an ArMenu account with this address. Enter its password to join.',
  joinCreateAccount: 'Create your account to join.',
  joinAction: 'Join the team',
  joinLinkMissing: 'Open this page with the link in your invitation email.',
  joinUnavailable: 'This invitation cannot be used',

  pageNotFound: 'Page not found',
  backToStart: 'Back to start',
};

export const interfaceLanguages = ['tr', 'en'] as const;
export type InterfaceLanguage = (typeof interfaceLanguages)[number];

export function messagesFor(language: InterfaceLanguage): Messages {
  return language === 'tr' ? tr : en;
}

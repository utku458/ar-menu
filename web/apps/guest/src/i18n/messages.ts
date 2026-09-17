/**
 * Interface text of the guest app. Menu content arrives translated from the API; these strings follow the language
 * the menu is served in, so a page never mixes languages. Missing languages fall back to English.
 */
export interface Messages {
  readonly menu: string;
  readonly table: (label: string) => string;
  readonly pageTitle: (restaurant: string) => string;
  readonly language: string;
  readonly sections: string;
  readonly soldOut: string;
  readonly viewableIn3d: string;
  readonly close: string;
  readonly seeOnYourTable: string;
  readonly loadingModel: string;
  readonly modelAlt: (dish: string) => string;
  readonly showIn3d: string;
  readonly modelFailed: string;
  readonly arOnPhone: string;
  readonly arFailed: string;
  readonly dragToTurn: string;
  readonly loadingMenu: string;
  readonly emptyMenu: string;
  readonly menuNotFoundTitle: string;
  readonly menuNotFoundBody: string;
  readonly loadFailedTitle: string;
  readonly loadFailedBody: string;
  readonly rateLimitedBody: string;
  readonly tryAgain: string;
  readonly pageNotFoundTitle: string;
  readonly homeTitle: string;
  readonly homeBody: string;
  readonly demoMenus: string;
  readonly poweredBy: string;
  readonly searchMenu: string;
  readonly filters: string;
  readonly suitableFor: string;
  readonly without: string;
  readonly clearFilters: string;
  readonly resultCount: (count: number) => string;
  readonly noMatchingDishes: string;
  readonly undeclaredHidden: (count: number) => string;
  readonly allergens: string;
  readonly allergensDeclaredBy: string;
  readonly containsNoAllergens: string;
  readonly allergensNotDeclared: string;
  readonly allergenNames: Readonly<Record<string, string>>;
  readonly dietaryLabelNames: Readonly<Record<string, string>>;
}

const en: Messages = {
  menu: 'Menu',
  table: (label) => `Table ${label}`,
  pageTitle: (restaurant) => `${restaurant} · Menu`,
  language: 'Language',
  sections: 'Menu sections',
  soldOut: 'Sold out',
  viewableIn3d: 'Viewable in 3D',
  close: 'Close',
  seeOnYourTable: 'See it on your table',
  loadingModel: 'Loading 3D model',
  modelAlt: (dish) => `3D model of ${dish}`,
  showIn3d: 'Show in 3D',
  modelFailed: 'The 3D model could not be loaded.',
  arOnPhone: 'Open this menu on your phone to place the dish on your table.',
  arFailed: 'Augmented reality could not start on this device.',
  dragToTurn: 'Drag to turn the dish',
  loadingMenu: 'Loading the menu',
  emptyMenu: 'No dishes have been added to this menu yet.',
  menuNotFoundTitle: 'Menu not found',
  menuNotFoundBody: 'Check the link, or scan the QR code on your table again.',
  loadFailedTitle: 'The menu could not be loaded',
  loadFailedBody: 'Check your connection and try again.',
  rateLimitedBody: 'Lots of guests are opening this menu right now. Try again in a moment.',
  tryAgain: 'Try again',
  pageNotFoundTitle: 'Page not found',
  homeTitle: 'Scan the QR code on your table',
  homeBody: 'The menu opens right in your browser, with dishes you can see on your table.',
  demoMenus: 'Demo menus',
  poweredBy: 'Menu by ArMenu',
  searchMenu: 'Search the menu',
  filters: 'Filters',
  suitableFor: 'Suitable for',
  without: 'Without',
  clearFilters: 'Clear',
  resultCount: (count) => `${count} ${count === 1 ? 'dish' : 'dishes'}`,
  noMatchingDishes: 'No dishes match your search.',
  undeclaredHidden: (count) =>
    `${count} ${count === 1 ? 'dish is' : 'dishes are'} not shown because the restaurant has not given allergen information for ${count === 1 ? 'it' : 'them'}. Ask the staff.`,
  allergens: 'Allergens',
  allergensDeclaredBy: 'As declared by the restaurant.',
  containsNoAllergens: 'Contains none of the 14 major allergens.',
  allergensNotDeclared: 'The restaurant has not given allergen information for this dish. Ask the staff.',
  allergenNames: {
    gluten: 'Gluten',
    crustaceans: 'Crustaceans',
    eggs: 'Eggs',
    fish: 'Fish',
    peanuts: 'Peanuts',
    soybeans: 'Soy',
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
};

const tr: Messages = {
  menu: 'Menü',
  table: (label) => `Masa ${label}`,
  pageTitle: (restaurant) => `${restaurant} · Menü`,
  language: 'Dil',
  sections: 'Menü bölümleri',
  soldOut: 'Tükendi',
  viewableIn3d: '3D olarak incelenebilir',
  close: 'Kapat',
  seeOnYourTable: 'Masanızda görün',
  loadingModel: '3D model yükleniyor',
  modelAlt: (dish) => `${dish} 3D modeli`,
  showIn3d: '3D olarak göster',
  modelFailed: '3D model yüklenemedi.',
  arOnPhone: 'Yemeği masanızda görmek için bu menüyü telefonunuzda açın.',
  arFailed: 'Artırılmış gerçeklik bu cihazda başlatılamadı.',
  dragToTurn: 'Çevirmek için sürükleyin',
  loadingMenu: 'Menü yükleniyor',
  emptyMenu: 'Bu menüye henüz yemek eklenmemiş.',
  menuNotFoundTitle: 'Menü bulunamadı',
  menuNotFoundBody: 'Bağlantıyı kontrol edin ya da masanızdaki QR kodu yeniden okutun.',
  loadFailedTitle: 'Menü yüklenemedi',
  loadFailedBody: 'İnternet bağlantınızı kontrol edip tekrar deneyin.',
  rateLimitedBody: 'Şu anda bu menüyü çok sayıda misafir açıyor. Birazdan tekrar deneyin.',
  tryAgain: 'Tekrar deneyin',
  pageNotFoundTitle: 'Sayfa bulunamadı',
  homeTitle: 'Masanızdaki QR kodu okutun',
  homeBody: 'Menü doğrudan tarayıcınızda açılır; yemekleri masanızda bile görebilirsiniz.',
  demoMenus: 'Demo menüler',
  poweredBy: 'ArMenu ile hazırlandı',
  searchMenu: 'Menüde ara',
  filters: 'Filtreler',
  suitableFor: 'Uygun olduğu diyet',
  without: 'İçermesin',
  clearFilters: 'Temizle',
  resultCount: (count) => `${count} yemek`,
  noMatchingDishes: 'Aramanıza uyan yemek yok.',
  undeclaredHidden: (count) =>
    `Restoran alerjen bilgisi vermediği için ${count} yemek gösterilmiyor. Personele sorabilirsiniz.`,
  allergens: 'Alerjenler',
  allergensDeclaredBy: 'Restoranın beyanına göre.',
  containsNoAllergens: '14 temel alerjenin hiçbirini içermez.',
  allergensNotDeclared: 'Restoran bu yemek için alerjen bilgisi vermemiş. Personele sorabilirsiniz.',
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
};

const de: Messages = {
  menu: 'Speisekarte',
  table: (label) => `Tisch ${label}`,
  pageTitle: (restaurant) => `${restaurant} · Speisekarte`,
  language: 'Sprache',
  sections: 'Bereiche der Speisekarte',
  soldOut: 'Ausverkauft',
  viewableIn3d: 'In 3D ansehen',
  close: 'Schließen',
  seeOnYourTable: 'Auf Ihrem Tisch ansehen',
  loadingModel: '3D-Modell wird geladen',
  modelAlt: (dish) => `3D-Modell von ${dish}`,
  showIn3d: 'In 3D anzeigen',
  modelFailed: 'Das 3D-Modell konnte nicht geladen werden.',
  arOnPhone: 'Öffnen Sie diese Speisekarte auf Ihrem Smartphone, um das Gericht auf Ihrem Tisch zu sehen.',
  arFailed: 'Augmented Reality konnte auf diesem Gerät nicht gestartet werden.',
  dragToTurn: 'Zum Drehen ziehen',
  loadingMenu: 'Speisekarte wird geladen',
  emptyMenu: 'Diese Speisekarte enthält noch keine Gerichte.',
  menuNotFoundTitle: 'Speisekarte nicht gefunden',
  menuNotFoundBody: 'Prüfen Sie den Link oder scannen Sie den QR-Code auf Ihrem Tisch erneut.',
  loadFailedTitle: 'Die Speisekarte konnte nicht geladen werden',
  loadFailedBody: 'Prüfen Sie Ihre Verbindung und versuchen Sie es erneut.',
  rateLimitedBody: 'Gerade öffnen sehr viele Gäste diese Speisekarte. Versuchen Sie es gleich noch einmal.',
  tryAgain: 'Erneut versuchen',
  pageNotFoundTitle: 'Seite nicht gefunden',
  homeTitle: 'Scannen Sie den QR-Code auf Ihrem Tisch',
  homeBody:
    'Die Speisekarte öffnet sich direkt im Browser, mit Gerichten, die Sie auf Ihrem Tisch sehen können.',
  demoMenus: 'Demo-Speisekarten',
  poweredBy: 'Speisekarte von ArMenu',
  searchMenu: 'Speisekarte durchsuchen',
  filters: 'Filter',
  suitableFor: 'Geeignet für',
  without: 'Ohne',
  clearFilters: 'Zurücksetzen',
  resultCount: (count) => `${count} ${count === 1 ? 'Gericht' : 'Gerichte'}`,
  noMatchingDishes: 'Keine Gerichte passen zu Ihrer Suche.',
  undeclaredHidden: (count) =>
    `Nicht angezeigt, weil das Restaurant keine Allergenangaben gemacht hat: ${count}. Fragen Sie das Personal.`,
  allergens: 'Allergene',
  allergensDeclaredBy: 'Nach Angaben des Restaurants.',
  containsNoAllergens: 'Enthält keines der 14 Hauptallergene.',
  allergensNotDeclared:
    'Das Restaurant hat für dieses Gericht keine Allergenangaben gemacht. Fragen Sie das Personal.',
  allergenNames: {
    gluten: 'Gluten',
    crustaceans: 'Krebstiere',
    eggs: 'Eier',
    fish: 'Fisch',
    peanuts: 'Erdnüsse',
    soybeans: 'Soja',
    milk: 'Milch',
    nuts: 'Schalenfrüchte',
    celery: 'Sellerie',
    mustard: 'Senf',
    sesame: 'Sesam',
    sulphites: 'Sulfite',
    lupin: 'Lupinen',
    molluscs: 'Weichtiere',
  },
  dietaryLabelNames: { vegetarian: 'Vegetarisch', vegan: 'Vegan', glutenFree: 'Glutenfrei' },
};

const ru: Messages = {
  menu: 'Меню',
  table: (label) => `Стол ${label}`,
  pageTitle: (restaurant) => `${restaurant} · Меню`,
  language: 'Язык',
  sections: 'Разделы меню',
  soldOut: 'Нет в наличии',
  viewableIn3d: 'Можно посмотреть в 3D',
  close: 'Закрыть',
  seeOnYourTable: 'Посмотреть на столе',
  loadingModel: 'Загрузка 3D-модели',
  modelAlt: (dish) => `3D-модель: ${dish}`,
  showIn3d: 'Показать в 3D',
  modelFailed: 'Не удалось загрузить 3D-модель.',
  arOnPhone: 'Откройте это меню на телефоне, чтобы увидеть блюдо у себя на столе.',
  arFailed: 'Не удалось запустить дополненную реальность на этом устройстве.',
  dragToTurn: 'Проведите, чтобы повернуть блюдо',
  loadingMenu: 'Загрузка меню',
  emptyMenu: 'В это меню ещё не добавлены блюда.',
  menuNotFoundTitle: 'Меню не найдено',
  menuNotFoundBody: 'Проверьте ссылку или снова отсканируйте QR-код на столе.',
  loadFailedTitle: 'Не удалось загрузить меню',
  loadFailedBody: 'Проверьте подключение и попробуйте ещё раз.',
  rateLimitedBody: 'Сейчас это меню открывает много гостей. Попробуйте ещё раз чуть позже.',
  tryAgain: 'Попробовать ещё раз',
  pageNotFoundTitle: 'Страница не найдена',
  homeTitle: 'Отсканируйте QR-код на столе',
  homeBody: 'Меню откроется прямо в браузере, а блюда можно увидеть у себя на столе.',
  demoMenus: 'Демо-меню',
  poweredBy: 'Меню от ArMenu',
  searchMenu: 'Поиск по меню',
  filters: 'Фильтры',
  suitableFor: 'Подходит для',
  without: 'Без',
  clearFilters: 'Сбросить',
  resultCount: (count) => `Блюд: ${count}`,
  noMatchingDishes: 'Нет блюд по вашему запросу.',
  undeclaredHidden: (count) =>
    `Не показаны, так как ресторан не указал аллергены: ${count}. Уточните у персонала.`,
  allergens: 'Аллергены',
  allergensDeclaredBy: 'По данным ресторана.',
  containsNoAllergens: 'Не содержит ни одного из 14 основных аллергенов.',
  allergensNotDeclared: 'Ресторан не указал аллергены для этого блюда. Уточните у персонала.',
  allergenNames: {
    gluten: 'Глютен',
    crustaceans: 'Ракообразные',
    eggs: 'Яйца',
    fish: 'Рыба',
    peanuts: 'Арахис',
    soybeans: 'Соя',
    milk: 'Молоко',
    nuts: 'Орехи',
    celery: 'Сельдерей',
    mustard: 'Горчица',
    sesame: 'Кунжут',
    sulphites: 'Сульфиты',
    lupin: 'Люпин',
    molluscs: 'Моллюски',
  },
  dietaryLabelNames: { vegetarian: 'Вегетарианское', vegan: 'Веганское', glutenFree: 'Без глютена' },
};

const ar: Messages = {
  menu: 'قائمة الطعام',
  table: (label) => `طاولة ${label}`,
  pageTitle: (restaurant) => `${restaurant} · قائمة الطعام`,
  language: 'اللغة',
  sections: 'أقسام القائمة',
  soldOut: 'نفدت الكمية',
  viewableIn3d: 'يمكن عرضه بتقنية ثلاثية الأبعاد',
  close: 'إغلاق',
  seeOnYourTable: 'شاهده على طاولتك',
  loadingModel: 'جارٍ تحميل النموذج ثلاثي الأبعاد',
  modelAlt: (dish) => `نموذج ثلاثي الأبعاد لطبق ${dish}`,
  showIn3d: 'عرض ثلاثي الأبعاد',
  modelFailed: 'تعذّر تحميل النموذج ثلاثي الأبعاد.',
  arOnPhone: 'افتح هذه القائمة على هاتفك لترى الطبق على طاولتك.',
  arFailed: 'تعذّر تشغيل الواقع المعزز على هذا الجهاز.',
  dragToTurn: 'اسحب لتدوير الطبق',
  loadingMenu: 'جارٍ تحميل القائمة',
  emptyMenu: 'لم تتم إضافة أطباق إلى هذه القائمة بعد.',
  menuNotFoundTitle: 'لم يتم العثور على القائمة',
  menuNotFoundBody: 'تحقق من الرابط أو امسح رمز QR الموجود على طاولتك مرة أخرى.',
  loadFailedTitle: 'تعذّر تحميل القائمة',
  loadFailedBody: 'تحقق من اتصالك بالإنترنت ثم حاول مرة أخرى.',
  rateLimitedBody: 'يفتح الكثير من الضيوف هذه القائمة الآن. حاول مرة أخرى بعد قليل.',
  tryAgain: 'حاول مرة أخرى',
  pageNotFoundTitle: 'الصفحة غير موجودة',
  homeTitle: 'امسح رمز QR الموجود على طاولتك',
  homeBody: 'تُفتح القائمة مباشرة في المتصفح، ويمكنك رؤية الأطباق على طاولتك.',
  demoMenus: 'قوائم تجريبية',
  poweredBy: 'قائمة من ArMenu',
  searchMenu: 'ابحث في القائمة',
  filters: 'عوامل التصفية',
  suitableFor: 'مناسب لـ',
  without: 'بدون',
  clearFilters: 'مسح',
  resultCount: (count) => `عدد الأطباق: ${count}`,
  noMatchingDishes: 'لا توجد أطباق تطابق بحثك.',
  undeclaredHidden: (count) =>
    `أطباق غير معروضة لأن المطعم لم يذكر مسببات الحساسية فيها: ${count}. اسأل فريق العمل.`,
  allergens: 'مسببات الحساسية',
  allergensDeclaredBy: 'وفق ما صرّح به المطعم.',
  containsNoAllergens: 'لا يحتوي على أيٍّ من مسببات الحساسية الأربعة عشر الرئيسية.',
  allergensNotDeclared: 'لم يذكر المطعم مسببات الحساسية في هذا الطبق. اسأل فريق العمل.',
  allergenNames: {
    gluten: 'الغلوتين',
    crustaceans: 'القشريات',
    eggs: 'البيض',
    fish: 'السمك',
    peanuts: 'الفول السوداني',
    soybeans: 'الصويا',
    milk: 'الحليب',
    nuts: 'المكسرات',
    celery: 'الكرفس',
    mustard: 'الخردل',
    sesame: 'السمسم',
    sulphites: 'الكبريتيت',
    lupin: 'الترمس',
    molluscs: 'الرخويات',
  },
  dietaryLabelNames: { vegetarian: 'نباتي', vegan: 'نباتي صرف', glutenFree: 'خالٍ من الغلوتين' },
};

const catalog: Readonly<Record<string, Messages>> = { en, tr, de, ru, ar };

/** Interface text for a culture: exact match, then its neutral language (`de-at` → `de`), then English. */
export function messagesFor(culture: string): Messages {
  const normalized = culture.toLowerCase();
  return catalog[normalized] ?? catalog[normalized.split('-')[0] ?? ''] ?? en;
}

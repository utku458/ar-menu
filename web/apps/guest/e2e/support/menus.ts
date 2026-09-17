import type { PublicMenuItemResponse, PublicMenuResponse } from '@armenu/api-client';

/** Origins from .env.e2e. The browser never reaches them: tests answer every request. */
export const apiOrigin = 'http://api.armenu.test';
export const assetOrigin = 'http://cdn.armenu.test';

export const ids = {
  smashBurger: '0198a1f2-0000-7000-8000-000000000001',
  truffleBurger: '0198a1f2-0000-7000-8000-000000000002',
  seaBass: '0198a1f2-0000-7000-8000-000000000003',
} as const;

type Translated = Readonly<Record<string, string>>;

function item(
  id: string,
  name: Translated,
  price: number,
  culture: string,
  extra: Partial<PublicMenuItemResponse> = {},
): PublicMenuItemResponse {
  return {
    id,
    name: name[culture] ?? name.en ?? id,
    description: null,
    price,
    isAvailable: true,
    arModel: null,
    allergens: null,
    dietaryLabels: [],
    ...extra,
  };
}

/** Kadıköy Burger Lab in Turkish or English: a 3D dish, a sold-out dish and enough sections to scroll. */
export function burgerLab(culture: 'tr' | 'en'): PublicMenuResponse {
  const t = (tr: string, en: string) => (culture === 'tr' ? tr : en);

  return {
    tenant: {
      name: 'Kadıköy Burger Lab',
      slug: 'kadikoy-burger-lab',
      currency: 'TRY',
      defaultCulture: 'tr',
      supportedCultures: ['tr', 'en'],
      logoUrl: `${assetOrigin}/demo/posters/smash-burger.webp`,
      // A business that painted its menu: the band, the table chip and the AR button carry this colour.
      accentColor: '#1f6f5c',
      onAccentColor: '#ffffff',
    },
    culture,
    categories: [
      {
        id: '0198a1f2-0000-7000-8000-0000000000c1',
        name: t('Burgerler', 'Burgers'),
        description: null,
        items: [
          item(ids.smashBurger, { tr: 'Klasik Smash Burger', en: 'Classic Smash Burger' }, 385, culture, {
            description: t('Çift smash köfte, cheddar', 'Double smashed patty, cheddar'),
            allergens: ['gluten', 'eggs', 'milk', 'mustard', 'sesame'],
            arModel: {
              glbUrl: `${assetOrigin}/demo/models/smash-burger.glb`,
              sceneViewerGlbUrl: `${assetOrigin}/demo/models/smash-burger.scene-viewer.glb`,
              usdzUrl: `${assetOrigin}/demo/models/smash-burger.usdz`,
              posterUrl: `${assetOrigin}/demo/posters/smash-burger.webp`,
            },
          }),
          item(
            ids.truffleBurger,
            { tr: 'Trüflü Mantar Burger', en: 'Truffle Mushroom Burger' },
            445,
            culture,
            {
              isAvailable: false,
            },
          ),
        ],
      },
      {
        id: '0198a1f2-0000-7000-8000-0000000000c2',
        name: t('İçecekler', 'Drinks'),
        description: null,
        // Drinks declare their allergens: none, except ayran (milk).
        items: ['Limonata', 'Ayran', 'Soda', 'Çay', 'Türk Kahvesi'].map((name, index) =>
          item(
            `0198a1f2-0000-7000-8000-0000000001${index}0`,
            { tr: name, en: name },
            90 + index * 10,
            culture,
            name === 'Ayran'
              ? { allergens: ['milk'], dietaryLabels: ['vegetarian', 'glutenFree'] }
              : { allergens: [], dietaryLabels: ['vegetarian', 'vegan', 'glutenFree'] },
          ),
        ),
      },
      {
        id: '0198a1f2-0000-7000-8000-0000000000c3',
        name: t('Tatlılar', 'Desserts'),
        description: null,
        // Only baklava declares its allergens; the other desserts say nothing about them.
        items: ['Baklava', 'Künefe', 'Sütlaç', 'Kazandibi', 'Dondurma'].map((name, index) =>
          item(
            `0198a1f2-0000-7000-8000-0000000002${index}0`,
            { tr: name, en: name },
            150 + index * 15,
            culture,
            name === 'Baklava'
              ? { allergens: ['gluten', 'milk', 'nuts'], dietaryLabels: ['vegetarian'] }
              : {},
          ),
        ),
      },
    ],
  };
}

/** A restaurant served in Arabic: right-to-left layout, and a model without a pre-built USDZ. */
export function seasideGrill(): PublicMenuResponse {
  return {
    tenant: {
      name: 'مشويات الساحل',
      slug: 'seaside-grill',
      currency: 'TRY',
      defaultCulture: 'ar',
      supportedCultures: ['ar', 'en'],
      logoUrl: null,
      accentColor: null,
      onAccentColor: null,
    },
    culture: 'ar',
    categories: [
      {
        id: '0198a1f2-0000-7000-8000-0000000000c4',
        name: 'الأطباق الرئيسية',
        description: null,
        items: [
          item(ids.seaBass, { ar: 'سمك القاروص المشوي' }, 750, 'ar', {
            arModel: {
              glbUrl: `${assetOrigin}/demo/models/sea-bass.glb`,
              sceneViewerGlbUrl: null,
              usdzUrl: null,
              posterUrl: `${assetOrigin}/demo/posters/sea-bass.webp`,
            },
          }),
        ],
      },
    ],
  };
}

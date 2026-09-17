import { ids } from './support/fake-api.ts';
import { expect, signIn, test, toggle } from './support/test.ts';

test('staff mark dishes sold out, but cannot change the menu', async ({ page, api }) => {
  await signIn(page, 'staff');

  await expect(page.getByRole('button', { name: 'Kategori ekle' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: /İşlemler:/ })).toHaveCount(0);

  await toggle(page.getByRole('switch', { name: 'Trüflü Mantar Burger satışta' }));

  await expect(page.getByRole('switch', { name: 'Trüflü Mantar Burger satışta' })).not.toBeChecked();
  await expect
    .poll(() => api.requestsTo('PUT', `/api/v1/manage/menu/items/${ids.truffleBurger}/availability`))
    .toHaveLength(1);
  expect(
    api.requestsTo('PUT', `/api/v1/manage/menu/items/${ids.truffleBurger}/availability`)[0]?.body,
  ).toEqual({
    isAvailable: false,
  });
});

test('items are reordered with the keyboard alone', async ({ page, api }) => {
  await signIn(page);
  const rows = page.getByRole('grid', { name: 'Ürünler: Burgerler' }).getByRole('row');

  await rows.filter({ hasText: 'Trüflü Mantar Burger' }).focus();
  await page.keyboard.press('ArrowRight');
  await expect(page.getByRole('button', { name: 'Trüflü Mantar Burger sırasını değiştir' })).toBeFocused();
  await page.keyboard.press('Enter');
  // The drag session starts asynchronously: arrows pressed before its drop targets exist would be lost on a busy machine.
  await expect(page.locator('[role=button][aria-label$=" gir"]:focus')).toHaveCount(1);
  await page.keyboard.press('ArrowUp');
  await page.keyboard.press('ArrowUp');
  await expect(page.getByRole('button', { name: 'Klasik Smash Burger’den önce gir' })).toBeFocused();
  await page.keyboard.press('Enter');

  await expect(page.getByText('Sıralama kaydedildi.')).toBeVisible();
  await expect(rows).toHaveText([/Trüflü Mantar Burger/, /Klasik Smash Burger/]);
  // Focus stays on a row of the list, where the keyboard user left off (React Aria picks the row around the drop).
  await expect(
    page.getByRole('grid', { name: 'Ürünler: Burgerler' }).locator('[role=row]:focus'),
  ).toHaveCount(1);
  expect(
    api
      .requestsTo('PUT', `/api/v1/manage/menu/categories/${ids.burgers}/items/order`)
      .map((request) => request.body),
  ).toEqual([{ ids: [ids.truffleBurger, ids.smashBurger] }]);
});

test('a rule only the API knows is shown next to the field it concerns', async ({ page }) => {
  await signIn(page);
  await page.getByRole('button', { name: 'İşlemler: Klasik Smash Burger' }).click();
  await page.getByRole('menuitem', { name: 'Düzenle' }).click();
  const dialog = page.getByRole('dialog');

  await dialog.getByRole('textbox', { name: 'Fiyat' }).fill('250000');
  await dialog.getByRole('button', { name: 'Kaydet' }).click();

  await expect(dialog.getByText('Fiyat çok yüksek.')).toBeVisible();
  await expect(dialog.getByRole('textbox', { name: 'Fiyat' })).toHaveAttribute('aria-invalid', 'true');
});

test('editing an item sends every offered language, the price and the visibility', async ({ page, api }) => {
  await signIn(page);
  await page.getByRole('button', { name: 'İşlemler: Klasik Smash Burger' }).click();
  await page.getByRole('menuitem', { name: 'Düzenle' }).click();
  const dialog = page.getByRole('dialog');

  await dialog.getByRole('textbox', { name: 'Ad · English' }).fill('Smash Burger Deluxe');
  await dialog.getByRole('textbox', { name: 'Fiyat' }).fill('399,50');
  await toggle(dialog.getByRole('switch', { name: 'Misafirlere göster' }));
  await dialog.getByRole('button', { name: 'Kaydet' }).click();

  await expect(dialog).toBeHidden();
  expect(api.requestsTo('PUT', `/api/v1/manage/menu/items/${ids.smashBurger}`)[0]?.body).toEqual({
    categoryId: ids.burgers,
    name: { tr: 'Klasik Smash Burger', en: 'Smash Burger Deluxe' },
    description: { tr: 'Çift smash köfte, cheddar' },
    price: 399.5,
    isVisible: false,
    // Nothing was entered about allergens: that stays "not declared", never "none".
    allergens: null,
    dietaryLabels: [],
  });
});

test('allergens are declared separately from ticking none, and a contradicting diet is refused', async ({
  page,
  api,
}) => {
  await signIn(page);
  await page.getByRole('button', { name: 'İşlemler: Klasik Smash Burger' }).click();
  await page.getByRole('menuitem', { name: 'Düzenle' }).click();
  const dialog = page.getByRole('dialog');
  const allergens = dialog.getByRole('group', { name: 'İçerdiği alerjenler' });

  await expect(allergens.getByRole('checkbox', { name: 'Süt' })).toBeDisabled();
  await toggle(dialog.getByRole('switch', { name: 'Alerjen bilgisini girdim' }));
  await expect(dialog.getByText('Hiçbiri işaretli değil')).toBeVisible();
  await toggle(allergens.getByRole('checkbox', { name: 'Gluten' }));
  await toggle(allergens.getByRole('checkbox', { name: 'Süt' }));
  await toggle(
    dialog.getByRole('group', { name: 'Uygun olduğu diyetler' }).getByRole('checkbox', { name: 'Vegan' }),
  );
  await dialog.getByRole('button', { name: 'Kaydet' }).click();

  await expect(dialog.getByText('Seçilen diyet, işaretlenen alerjenlerle çelişiyor')).toBeVisible();

  await toggle(
    dialog.getByRole('group', { name: 'Uygun olduğu diyetler' }).getByRole('checkbox', { name: 'Vegan' }),
  );
  await toggle(
    dialog
      .getByRole('group', { name: 'Uygun olduğu diyetler' })
      .getByRole('checkbox', { name: 'Vejetaryen' }),
  );
  await dialog.getByRole('button', { name: 'Kaydet' }).click();

  await expect(dialog).toBeHidden();
  const saved = api.requestsTo('PUT', `/api/v1/manage/menu/items/${ids.smashBurger}`).at(-1)?.body as {
    allergens: string[] | null;
    dietaryLabels: string[];
  };
  expect(saved.allergens).toEqual(['gluten', 'milk']);
  expect(saved.dietaryLabels).toEqual(['vegetarian']);
});

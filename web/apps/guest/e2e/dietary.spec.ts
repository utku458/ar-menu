import { expect, test } from './support/test.ts';

test('a guest avoiding gluten is never shown a dish whose allergens nobody declared', async ({ page }) => {
  await page.goto('kadikoy-burger-lab');
  await page.getByRole('button', { name: 'Filtreler' }).click();
  await page.getByRole('group', { name: 'İçermesin' }).getByRole('button', { name: 'Gluten' }).click();

  const status = page.getByRole('status');
  // Five drinks declare none; baklava and the burger contain gluten; four desserts and one burger never said.
  await expect(status).toContainText('5 yemek');
  await expect(status).toContainText('Restoran alerjen bilgisi vermediği için 5 yemek gösterilmiyor');
  await expect(page.getByRole('heading', { level: 2 })).toHaveText(['İçecekler']);
  await expect(page.getByRole('navigation', { name: 'Menü bölümleri' })).toBeHidden();

  await status.getByRole('button', { name: 'Temizle' }).click();
  await expect(page.getByRole('heading', { level: 2 })).toHaveCount(3);
});

test('searching ignores accents and case, and diets narrow the menu further', async ({ page }) => {
  await page.goto('kadikoy-burger-lab');
  const search = page.getByRole('searchbox', { name: 'Menüde ara' });

  await search.fill('turk kahvesi');
  await expect(page.getByRole('listitem')).toHaveText([/Türk Kahvesi/]);

  await search.fill('');
  await page.getByRole('button', { name: 'Filtreler' }).click();
  await page
    .getByRole('group', { name: 'Uygun olduğu diyet' })
    .getByRole('button', { name: 'Vegan' })
    .click();
  await expect(page.getByRole('status')).toContainText('4 yemek');
  await expect(page.getByRole('listitem').filter({ hasText: 'Ayran' })).toHaveCount(0);
});

test('the dish sheet says what the allergens are, that there are none, or that nobody said', async ({
  page,
}) => {
  await page.goto('kadikoy-burger-lab');

  await page.getByRole('button', { name: 'Klasik Smash Burger' }).click();
  const allergens = page.getByRole('dialog').getByRole('region', { name: 'Alerjenler' });
  await expect(allergens).toContainText('Gluten, Yumurta, Süt, Hardal, Susam');
  await expect(allergens).toContainText('Restoranın beyanına göre.');
  await page.getByRole('button', { name: 'Kapat' }).click();

  await page.getByRole('button', { name: 'Limonata' }).click();
  await expect(page.getByRole('dialog').getByRole('region', { name: 'Alerjenler' })).toContainText(
    '14 temel alerjenin hiçbirini içermez.',
  );
  await page.getByRole('button', { name: 'Kapat' }).click();

  await page.getByRole('button', { name: 'Künefe' }).click();
  await expect(page.getByRole('dialog').getByRole('region', { name: 'Alerjenler' })).toContainText(
    'Restoran bu yemek için alerjen bilgisi vermemiş.',
  );
});

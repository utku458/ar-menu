import { eventually, expect, test } from './support/test.ts';

test('opens in the restaurant language with prices and sold-out dishes', async ({ page }) => {
  await page.goto('kadikoy-burger-lab');

  await expect(page.getByRole('heading', { level: 1, name: 'Kadıköy Burger Lab' })).toBeVisible();
  await expect(page).toHaveTitle('Kadıköy Burger Lab · Menü');
  await expect(page.locator('html')).toHaveAttribute('lang', 'tr');

  const truffleBurger = page.getByRole('listitem').filter({ hasText: 'Trüflü Mantar Burger' });
  await expect(truffleBurger).toContainText('₺445');
  await expect(truffleBurger).toContainText('Tükendi');
});

test('requests the menu while the application code is still downloading, and only once', async ({
  page,
  api,
}) => {
  // Hold the application chunk back; the menu request must still go out, started by the tiny entry chunk.
  let menuRequestedDuringAppDownload = false;
  await page.route('**/assets/start-*.js', async (route) => {
    menuRequestedDuringAppDownload = await eventually(() => api.menuRequests().length > 0);
    await route.continue();
  });

  await page.goto('kadikoy-burger-lab');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

  expect(menuRequestedDuringAppDownload).toBe(true);
  // The application took over the response the entry had requested instead of asking again.
  expect(api.menuRequests()).toHaveLength(1);
  // A plain GET without custom headers: no CORS preflight round trip before the menu.
  expect(api.requests.filter((request) => request.method === 'OPTIONS')).toEqual([]);
});

test('switching the language reloads the menu and is remembered for the next visit', async ({
  page,
  api,
}) => {
  await page.goto('kadikoy-burger-lab');

  await page.getByRole('combobox', { name: 'Dil' }).selectOption('en');

  await expect(page.getByRole('heading', { level: 2, name: 'Burgers' })).toBeVisible();
  await expect(page).toHaveURL(/lang=en/);
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');

  await page.goto('kadikoy-burger-lab');

  await expect(page.getByRole('heading', { level: 2, name: 'Burgers' })).toBeVisible();
  expect(api.menuRequests().at(-1)?.searchParams.get('lang')).toBe('en');
});

test('section links jump to their section and follow the reader', async ({ page }) => {
  await page.goto('kadikoy-burger-lab');
  const sections = page.getByRole('navigation', { name: 'Menü bölümleri' });

  await sections.getByRole('link', { name: 'Tatlılar' }).click();

  await expect(page.getByRole('heading', { level: 2, name: 'Tatlılar' })).toBeInViewport();
  await expect(sections.getByRole('link', { name: 'Tatlılar' })).toHaveAttribute('aria-current', 'location');
  await expect(sections.getByRole('link', { name: 'Burgerler' })).not.toHaveAttribute('aria-current');
});

test('a table QR code shows the table and keeps it while the guest browses', async ({ page }) => {
  await page.goto('kadikoy-burger-lab?table=12');

  await expect(page.getByText('Masa 12')).toBeVisible();

  await page.getByRole('combobox', { name: 'Dil' }).selectOption('en');

  await expect(page.getByText('Table 12')).toBeVisible();
  await expect(page).toHaveURL(/table=12/);
});

test('an unknown restaurant gets a helpful not-found page', async ({ page }) => {
  await page.goto('no-such-restaurant');

  await expect(page.getByRole('heading', { level: 1, name: 'Menu not found' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Try again' })).toHaveCount(0);
});

test('a menu that failed to load can be retried', async ({ page, api }) => {
  // The first attempt and both automatic retries fail.
  api.failNextRequests(503, 503, 503);
  await page.goto('kadikoy-burger-lab');

  await page.getByRole('button', { name: 'Try again' }).click();

  await expect(page.getByRole('heading', { level: 1, name: 'Kadıköy Burger Lab' })).toBeVisible();
});

test('right-to-left languages lay the page out right to left', async ({ page }) => {
  await page.goto('seaside-grill');

  await expect(page.getByRole('heading', { level: 1, name: 'مشويات الساحل' })).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar');
});

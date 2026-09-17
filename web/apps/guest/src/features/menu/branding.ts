import type { PublicTenantResponse } from '@armenu/api-client';

/** What the menu paints on: the document element, or anything else with those two style methods. */
export interface BrandTarget {
  readonly style: Pick<CSSStyleDeclaration, 'setProperty' | 'removeProperty'>;
}

/** The tokens a business's colour replaces. Defaults live in styles.css, per colour scheme. */
const brandProperty = '--color-brand';
const brandInkProperty = '--color-brand-ink';

/**
 * Paints the menu in the business's colour, or leaves the platform's own when it has none. The properties are set on
 * the document element rather than written into a stylesheet, so no inline `<style>` has to be allowed by the content
 * security policy.
 *
 * The colour and the text drawn on it come from the API as a checked pair, so a business can never end up with a menu
 * whose own name is unreadable.
 */
export function applyBranding(
  root: BrandTarget,
  tenant: Pick<PublicTenantResponse, 'accentColor' | 'onAccentColor'>,
) {
  if (tenant.accentColor !== null && tenant.onAccentColor !== null) {
    root.style.setProperty(brandProperty, tenant.accentColor);
    root.style.setProperty(brandInkProperty, tenant.onAccentColor);
  } else {
    clearBranding(root);
  }
}

/** Puts the platform's own colour back, for a menu that is left or replaced by one without a colour. */
export function clearBranding(root: BrandTarget) {
  root.style.removeProperty(brandProperty);
  root.style.removeProperty(brandInkProperty);
}

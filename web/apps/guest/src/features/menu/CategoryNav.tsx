import type { PublicMenuCategoryResponse } from '@armenu/api-client';
import { useEffect, useRef } from 'react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { sectionId, useActiveSection } from './use-active-section.ts';

/** Sticky, horizontally scrolling section links that follow the reader down the menu. */
export function CategoryNav({ categories }: { categories: readonly PublicMenuCategoryResponse[] }) {
  const { messages } = useI18n();
  const activeId = useActiveSection(categories);
  const listRef = useRef<HTMLUListElement>(null);

  useEffect(() => {
    const list = listRef.current;
    if (list === null) {
      return;
    }

    const active = list.querySelector('[aria-current="location"]');
    if (active === null) {
      return;
    }

    // Scroll the bar itself (never the page) so the active link sits in its middle; works the same in RTL.
    const listBox = list.getBoundingClientRect();
    const activeBox = active.getBoundingClientRect();
    list.scrollBy({
      left: activeBox.left + activeBox.width / 2 - (listBox.left + listBox.width / 2),
      behavior: prefersReducedMotion() ? 'auto' : 'smooth',
    });
  }, [activeId]);

  if (categories.length < 2) {
    return null;
  }

  return (
    <nav
      aria-label={messages.sections}
      className="sticky top-0 z-10 border-b border-line bg-canvas/85 backdrop-blur-md"
    >
      <ul ref={listRef} className="mx-auto scrollbar-none flex max-w-2xl gap-2 overflow-x-auto px-4 py-3">
        {categories.map((category) => (
          <li key={category.id} className="shrink-0">
            <a
              href={`#${sectionId(category.id)}`}
              aria-current={category.id === activeId ? 'location' : undefined}
              onClick={(event) => {
                event.preventDefault();
                goToSection(category.id);
              }}
              className="block rounded-full px-4 py-2 text-sm font-medium whitespace-nowrap text-ink-muted transition-colors hover:text-ink aria-[current=location]:bg-ink aria-[current=location]:text-canvas"
            >
              {category.name}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}

function goToSection(categoryId: string): void {
  const section = document.getElementById(sectionId(categoryId));
  section?.scrollIntoView({ behavior: prefersReducedMotion() ? 'auto' : 'smooth', block: 'start' });
  // Keyboard and screen reader users continue reading from the section they jumped to.
  section?.querySelector<HTMLElement>('h2')?.focus({ preventScroll: true });
}

function prefersReducedMotion(): boolean {
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

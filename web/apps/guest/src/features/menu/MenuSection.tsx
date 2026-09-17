import type { PublicMenuCategoryResponse } from '@armenu/api-client';

import { MenuItemCard } from './MenuItemCard.tsx';
import { sectionId } from './use-active-section.ts';

interface MenuSectionProps {
  readonly category: PublicMenuCategoryResponse;
  readonly currency: string;
  readonly onOpenItem: (itemId: string) => void;
  readonly isFirst: boolean;
}

const aboveTheFoldItems = 4;

export function MenuSection({ category, currency, onOpenItem, isFirst }: MenuSectionProps) {
  const id = sectionId(category.id);

  return (
    <section id={id} aria-labelledby={`${id}-title`} className="scroll-mt-20 pt-8">
      <h2 id={`${id}-title`} tabIndex={-1} className="px-1 font-serif text-2xl outline-none">
        {category.name}
      </h2>
      {category.description !== null && <p className="mt-1 px-1 text-ink-muted">{category.description}</p>}
      <ul className="mt-4 grid gap-3">
        {category.items.map((item, index) => (
          <MenuItemCard
            key={item.id}
            item={item}
            currency={currency}
            onOpen={onOpenItem}
            isAboveTheFold={isFirst && index < aboveTheFoldItems}
          />
        ))}
      </ul>
    </section>
  );
}

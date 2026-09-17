import { useEffect, useState } from 'react';

export function sectionId(categoryId: string): string {
  return `category-${categoryId}`;
}

/** Sections become active once their top passes this share of the viewport height. */
const readingLine = 0.3;

/**
 * The menu section being read: the last one whose top has passed the reading line, or the last section once the
 * page is scrolled to the end (short final sections never reach the line).
 */
export function useActiveSection(categories: readonly { readonly id: string }[]): string | undefined {
  const [activeId, setActiveId] = useState(categories[0]?.id);

  useEffect(() => {
    let frame = 0;

    const update = () => {
      frame = 0;
      const line = window.innerHeight * readingLine;
      const atEnd = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2;

      let current = categories[0]?.id;
      for (const category of categories) {
        const top = document.getElementById(sectionId(category.id))?.getBoundingClientRect().top;
        if (top !== undefined && (atEnd || top <= line)) {
          current = category.id;
        }
      }

      setActiveId(current);
    };

    // At most one layout read per frame, however fast the page scrolls.
    const schedule = () => {
      if (frame === 0) {
        frame = requestAnimationFrame(update);
      }
    };

    schedule();
    window.addEventListener('scroll', schedule, { passive: true });
    window.addEventListener('resize', schedule, { passive: true });

    return () => {
      cancelAnimationFrame(frame);
      window.removeEventListener('scroll', schedule);
      window.removeEventListener('resize', schedule);
    };
  }, [categories]);

  return activeId;
}

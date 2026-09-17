import type { HistoryEntryResponse } from '@armenu/api-client';
import { describe, expect, it } from 'vitest';

import { messagesFor } from '../../i18n/messages.ts';
import { describeEntry, type HistoryContext } from './describe-entry.ts';

const context: HistoryContext = {
  messages: messagesFor('tr'),
  defaultCulture: 'tr',
  currency: 'TRY',
  locale: 'tr-TR',
  categoryName: (id) => (id === 'c1' ? 'Burgerler' : undefined),
};

const deniz = { id: 'u1', fullName: 'Deniz Yılmaz' };

function entry(overrides: Partial<HistoryEntryResponse>): HistoryEntryResponse {
  return {
    id: 'e1',
    occurredAt: '2026-09-15T10:00:00Z',
    actor: deniz,
    action: 'updated',
    subject: { type: 'menu_item', id: 'i1', name: { tr: 'Limonata', en: 'Lemonade' }, person: null },
    changes: [],
    ...overrides,
  };
}

describe('describeEntry', () => {
  it('turns an edit into a sentence and readable before and after values', () => {
    const described = describeEntry(
      entry({
        changes: [
          { field: 'price', before: 120, after: 135.5 },
          { field: 'category', before: 'c1', after: 'c9' },
          { field: 'model', before: false, after: true },
          { field: 'photo', before: true, after: true },
        ],
      }),
      context,
    );

    expect(described.actor).toBe('Deniz Yılmaz');
    expect(described.summary).toBe('“Limonata” ürününü düzenledi');
    expect(described.changes.map(({ label, before, after }) => [label, before, after])).toEqual([
      ['Fiyat', '₺120,00', '₺135,50'],
      ['Kategori', 'Burgerler', '—'],
      ['3D model', 'Yok', 'Var'],
      ['Fotoğraf', 'Var', 'Değiştirildi'],
    ]);
  });

  it('says sold out in the sentence alone', () => {
    const described = describeEntry(
      entry({ changes: [{ field: 'available', before: true, after: false }] }),
      context,
    );

    expect(described.summary).toBe('“Limonata” ürününü tükendi olarak işaretledi');
    expect(described.changes).toEqual([]);
  });

  it('names the system and deleted accounts without inventing people', () => {
    expect(describeEntry(entry({ actor: null }), context).actor).toBe('Sistem');

    const left = describeEntry(
      entry({
        actor: { id: 'u2', fullName: null },
        action: 'deleted',
        subject: { type: 'member', id: 'u2', name: null, person: { id: 'u2', fullName: null } },
      }),
      context,
    );
    expect([left.actor, left.summary]).toEqual(['Silinmiş hesap', 'hesabını sildi ve ekipten ayrıldı']);

    const removed = describeEntry(
      entry({
        action: 'deleted',
        subject: { type: 'member', id: 'u3', name: null, person: { id: 'u3', fullName: 'Ece Kaya' } },
      }),
      context,
    );
    expect(removed.summary).toBe('Ece Kaya kişisini ekipten çıkardı');
  });

  it('tells allergens nobody entered apart from a dish that contains none', () => {
    const changed = describeEntry(
      entry({
        changes: [
          { field: 'allergens', before: null, after: [] },
          { field: 'allergens', before: [], after: ['gluten', 'milk'] },
          { field: 'dietaryLabels', before: [], after: ['vegetarian'] },
          { field: 'logo', before: true, after: true },
        ],
      }),
      context,
    );

    expect(changed.changes.map(({ before, after }) => [before, after])).toEqual([
      ['Girilmemiş', 'Yok'],
      ['Yok', 'Gluten, Süt'],
      ['—', 'Vejetaryen'],
      ['Var', 'Değiştirildi'],
    ]);
  });

  it('describes business settings and reordering', () => {
    const settings = describeEntry(
      entry({
        subject: { type: 'business', id: 't1', name: null, person: null },
        changes: [
          { field: 'languages', before: ['tr'], after: ['tr', 'en'] },
          { field: 'timeZone', before: 'UTC', after: 'America/New_York' },
        ],
      }),
      context,
    );
    expect(settings.summary).toBe('işletme ayarlarını değiştirdi');
    expect(settings.changes.map(({ before, after }) => [before, after])).toEqual([
      ['Türkçe', 'Türkçe, English'],
      ['UTC', 'America/New York'],
    ]);

    const reordered = describeEntry(
      entry({ action: 'reordered', subject: { type: 'menu_category', id: 'c1', name: null, person: null } }),
      context,
    );
    expect(reordered.summary).toBe('“Burgerler” kategorisindeki ürünleri yeniden sıraladı');
  });
});

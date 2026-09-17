import { describe, expect, test } from 'vitest';

import { messagesFor } from './messages.ts';

describe('messagesFor', () => {
  test.each([
    ['tr', 'Tükendi'],
    ['de-AT', 'Ausverkauft'],
    ['pt-br', 'Sold out'],
  ])('picks interface text for %s', (culture, soldOut) => {
    expect(messagesFor(culture).soldOut).toBe(soldOut);
  });
});

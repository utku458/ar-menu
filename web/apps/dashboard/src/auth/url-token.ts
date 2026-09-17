/**
 * The secret of a link from an e-mail arrives in the URL fragment, which browsers never send to a server. It is read
 * once and taken out of the address bar, so it does not linger in history or on a shared screen.
 */
export function takeTokenFromUrl(): string | undefined {
  const token = decodeURIComponent(window.location.hash.slice(1));
  if (token !== '') {
    window.history.replaceState(window.history.state, '', window.location.pathname + window.location.search);
  }

  return token === '' ? undefined : token;
}

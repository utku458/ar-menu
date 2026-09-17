/** A text field's value from form data; file inputs and missing fields read as empty text. */
export function formText(data: FormData, name: string): string {
  const value = data.get(name);
  return typeof value === 'string' ? value : '';
}

import {
  Button as AriaButton,
  type ButtonProps as AriaButtonProps,
  composeRenderProps,
  Link as AriaLink,
  type LinkProps as AriaLinkProps,
} from 'react-aria-components';

import { cx } from './cx.ts';

const variants = {
  primary: 'bg-accent text-accent-ink data-[hovered]:bg-accent-hover',
  secondary: 'border border-line bg-surface text-ink data-[hovered]:bg-sunken',
  ghost: 'text-ink data-[hovered]:bg-sunken',
  danger: 'bg-danger text-white data-[hovered]:opacity-90',
} as const;

const sizes = {
  sm: 'h-8 gap-1.5 rounded-lg px-3 text-sm',
  md: 'h-10 gap-2 rounded-lg px-4 text-sm',
  icon: 'size-9 rounded-lg',
} as const;

export interface ButtonStyle {
  readonly variant?: keyof typeof variants | undefined;
  readonly size?: keyof typeof sizes | undefined;
}

function buttonClass({ variant = 'secondary', size = 'md' }: ButtonStyle, className?: string): string {
  return cx(
    'inline-flex shrink-0 cursor-default items-center justify-center font-medium whitespace-nowrap transition-colors outline-none',
    'data-[focus-visible]:outline-2 data-[focus-visible]:outline-offset-2 data-[focus-visible]:outline-accent',
    'data-[disabled]:opacity-50 data-[pending]:opacity-70',
    variants[variant],
    sizes[size],
    className,
  );
}

export function Button({ variant, size, className, ...props }: AriaButtonProps & ButtonStyle) {
  return (
    <AriaButton
      {...props}
      className={composeRenderProps(className, (custom) => buttonClass({ variant, size }, custom))}
    />
  );
}

export function LinkButton({ variant, size, className, ...props }: AriaLinkProps & ButtonStyle) {
  return (
    <AriaLink
      {...props}
      className={composeRenderProps(className, (custom) => buttonClass({ variant, size }, custom))}
    />
  );
}

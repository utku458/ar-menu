import { Menu, MenuItem, MenuTrigger, Popover } from 'react-aria-components';

import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { MoreIcon } from '../../ui/icons.tsx';

const menuItem = 'cursor-default rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken';

export function RowActions({
  name,
  onEdit,
  onDelete,
}: {
  name: string;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const { messages } = useI18n();

  return (
    <MenuTrigger>
      <Button variant="ghost" size="icon" aria-label={`${messages.actions}: ${name}`}>
        <MoreIcon />
      </Button>
      <Popover
        placement="bottom end"
        className="min-w-40 rounded-lg border border-line bg-surface p-1 shadow-lg"
      >
        <Menu
          className="outline-none"
          onAction={(action) => {
            if (action === 'edit') {
              onEdit();
            } else {
              onDelete();
            }
          }}
        >
          <MenuItem id="edit" className={menuItem}>
            {messages.edit}
          </MenuItem>
          <MenuItem id="delete" className={`${menuItem} text-danger`}>
            {messages.delete}
          </MenuItem>
        </Menu>
      </Popover>
    </MenuTrigger>
  );
}

import { useI18n } from '../../i18n/i18n-context.ts';
import { PageHeader } from '../../ui/layout.tsx';
import { BrandingCard } from './BrandingCard.tsx';
import { LanguagesCard } from './LanguagesCard.tsx';
import { TimeZoneCard } from './TimeZoneCard.tsx';

export function SettingsPage() {
  const { messages } = useI18n();

  return (
    <>
      <PageHeader title={messages.navSettings} description={messages.settingsIntro} />
      <div className="grid max-w-2xl grid-cols-1 gap-6">
        <BrandingCard />
        <LanguagesCard />
        <TimeZoneCard />
      </div>
    </>
  );
}

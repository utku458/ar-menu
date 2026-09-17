import { languageName } from '@armenu/locale';
import { useState } from 'react';

import { useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { env } from '../../env.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { NumberField, SelectField, TextField } from '../../ui/fields.tsx';
import { CopyIcon, DownloadIcon, PrinterIcon } from '../../ui/icons.tsx';
import { Card, PageHeader } from '../../ui/layout.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { menuUrl, type QrCode, qrCode, qrPngFile, qrSvgFile } from './qr-code.ts';

const automaticLanguage = 'auto';

export function QrCodesPage() {
  const { messages } = useI18n();
  const workspace = useWorkspace();
  const { tenant } = useCurrentUser();
  const notify = useNotify();
  const [language, setLanguage] = useState(automaticLanguage);
  const [tableCount, setTableCount] = useState(8);

  const lang = language === automaticLanguage ? undefined : language;
  const url = menuUrl(env.guestMenuBaseUrl, workspace.slug, { lang });
  const code = qrCode(url);

  const download = (blob: Blob, extension: string) => {
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `${workspace.slug}-menu-qr.${extension}`;
    link.click();
    URL.revokeObjectURL(link.href);
  };

  return (
    <>
      <div data-print-hidden>
        <PageHeader title={messages.navQrCodes} description={messages.qrIntro} />

        <div className="grid gap-6 md:grid-cols-[minmax(0,18rem)_minmax(0,1fr)]">
          <Card className="p-4">
            <QrImage code={code} title={messages.qrAlt(url)} />
          </Card>

          <Card className="flex flex-col gap-4 p-5">
            <SelectField
              label={messages.menuLanguage}
              options={[
                { id: automaticLanguage, label: messages.qrLanguageAuto },
                ...tenant.supportedCultures.map((culture) => ({ id: culture, label: languageName(culture) })),
              ]}
              value={language}
              onChange={(key) => {
                setLanguage(String(key));
              }}
            />
            <TextField label={messages.menuLink} value={url} isReadOnly />
            <div className="flex flex-wrap gap-2">
              <Button
                onPress={() => {
                  void navigator.clipboard.writeText(url).then(() => {
                    notify(messages.linkCopied);
                  });
                }}
              >
                <CopyIcon />
                {messages.copyLink}
              </Button>
              <Button
                onPress={() => {
                  download(new Blob([qrSvgFile(code, tenant.name)], { type: 'image/svg+xml' }), 'svg');
                }}
              >
                <DownloadIcon />
                {messages.downloadSvg}
              </Button>
              <Button
                onPress={() => {
                  void qrPngFile(code).then((png) => {
                    download(png, 'png');
                  });
                }}
              >
                <DownloadIcon />
                {messages.downloadPng}
              </Button>
            </div>
          </Card>
        </div>

        <section aria-labelledby="table-cards" className="mt-10">
          <h2 id="table-cards" className="text-lg font-semibold">
            {messages.tableCards}
          </h2>
          <p className="mt-1 max-w-2xl text-sm text-ink-muted">{messages.tableCardsIntro}</p>
          <div className="mt-4 flex flex-wrap items-end gap-3">
            <NumberField
              label={messages.tableCount}
              value={tableCount}
              minValue={1}
              maxValue={60}
              onChange={(value) => {
                setTableCount(Number.isNaN(value) ? 1 : value);
              }}
              className="w-40"
            />
            <Button
              variant="primary"
              onPress={() => {
                window.print();
              }}
            >
              <PrinterIcon />
              {messages.printCards}
            </Button>
          </div>
        </section>
      </div>

      <ul
        aria-label={messages.tableCards}
        className="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 print:mt-0"
      >
        {Array.from({ length: tableCount }, (_, index) => String(index + 1)).map((table) => {
          const tableUrl = menuUrl(env.guestMenuBaseUrl, workspace.slug, { lang, table });
          return (
            <li
              key={table}
              className="flex break-inside-avoid flex-col items-center gap-2 rounded-xl border border-line bg-white p-4 text-center text-black"
            >
              <p className="text-xs tracking-wide uppercase">{tenant.name}</p>
              <p className="text-lg font-semibold">{messages.tableLabel(table)}</p>
              <QrImage code={qrCode(tableUrl)} title={messages.qrAlt(tableUrl)} />
              <p className="text-xs">{messages.scanForMenu}</p>
            </li>
          );
        })}
      </ul>
    </>
  );
}

function QrImage({ code, title }: { code: QrCode; title: string }) {
  return (
    <svg
      role="img"
      aria-label={title}
      viewBox={`0 0 ${code.size} ${code.size}`}
      shapeRendering="crispEdges"
      className="block aspect-square w-full"
    >
      <rect width={code.size} height={code.size} fill="#fff" />
      <path d={code.path} fill="#000" />
    </svg>
  );
}

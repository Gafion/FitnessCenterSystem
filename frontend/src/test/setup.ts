import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, beforeEach } from 'vitest';
import i18n from '@/i18n';

// The app defaults to Danish (D-065). Component tests were written against the
// English texts and formats, so each test starts in English; tests of the Danish
// texts switch language themselves.
beforeEach(async () => {
  await i18n.changeLanguage('en');
});

afterEach(() => {
  cleanup();
});

// jsdom has no ResizeObserver; Radix components (checkbox) need one to exist.
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
};

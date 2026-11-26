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

// jsdom has no Web Animations API; UI SDK popups (select) wait for exit animations
// through getAnimations(). With none running, they close at once, as in a browser
// with animations off.
Element.prototype.getAnimations ??= () => [];

// jsdom hides [popover] elements (display: none) but has no Popover API to show them.
// A small stand-in: showPopover() marks the element open and a rule displays it, as
// :popover-open does in a browser. UI SDK popups (select) use it when it exists.
const openPopover = 'data-test-popover-open';
const popoverStyle = document.createElement('style');
popoverStyle.textContent = `[popover][${openPopover}] { display: block; }`;
document.head.append(popoverStyle);
HTMLElement.prototype.showPopover ??= function showPopover(this: HTMLElement) {
  this.setAttribute(openPopover, '');
};
HTMLElement.prototype.hidePopover ??= function hidePopover(this: HTMLElement) {
  this.removeAttribute(openPopover);
};
HTMLElement.prototype.togglePopover ??= function togglePopover(
  this: HTMLElement,
  force?: boolean | { force?: boolean },
) {
  const open =
    typeof force === 'boolean'
      ? force
      : (force?.force ?? !this.hasAttribute(openPopover));
  this.toggleAttribute(openPopover, open);
  return open;
};

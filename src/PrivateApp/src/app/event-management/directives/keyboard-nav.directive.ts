import { Directive, ElementRef, HostListener, Input } from '@angular/core';

@Directive({
  selector: '[appKeyboardNav]',
  standalone: true
})
export class KeyboardNavDirective {
  @Input() appKeyboardNav: string = ''; // Selector for navigable items

  constructor(private el: ElementRef) {}

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent) {
    const items = Array.from(this.el.nativeElement.querySelectorAll(this.appKeyboardNav)) as HTMLElement[];
    const currentIndex = items.indexOf(document.activeElement as HTMLElement);

    if (currentIndex === -1) return;

    let nextIndex = -1;

    switch (event.key) {
      case 'ArrowRight':
        nextIndex = currentIndex + 1;
        break;
      case 'ArrowLeft':
        nextIndex = currentIndex - 1;
        break;
      case 'ArrowDown':
        // Simple linear navigation for now
        nextIndex = currentIndex + 1;
        break;
      case 'ArrowUp':
        nextIndex = currentIndex - 1;
        break;
    }

    if (nextIndex >= 0 && nextIndex < items.length) {
      items[nextIndex].focus();
      event.preventDefault();
    }
  }
}

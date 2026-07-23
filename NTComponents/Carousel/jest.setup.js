// Mock PointerEvent if not available in test environment
if (!global.PointerEvent) {
  global.PointerEvent = class PointerEvent extends Event {
    constructor(type, options = {}) {
      super(type, options);
      this.pointerId = options.pointerId || 1;
      this.clientX = options.clientX || 0;
      this.clientY = options.clientY || 0;
      this.button = options.button || 0;
      this.pointerType = options.pointerType || 'mouse';
    }
  };
}

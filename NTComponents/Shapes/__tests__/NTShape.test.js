/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals'
import { __testHooks, onDispose, onLoad, onUpdate } from '../NTShape.razor.js'

describe('NTShape runtime', () => {
  let currentTimestamp = 0

  function createShapeElement(shape = '0', animate = 'false') {
    const root = document.createElement('nt-shape')
    root.dataset.shape = shape
    root.dataset.animateShapeChanges = animate
    root.dataset.transitionDurationMs = '64'
    root.dataset.transitionEasing = '0'
    root.dataset.clipId = 'nt-shape-clip-test'

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg')
    svg.classList.add('nt-shape-defs')

    const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs')
    const clipPath = document.createElementNS('http://www.w3.org/2000/svg', 'clipPath')
    clipPath.setAttribute('id', 'nt-shape-clip-test')
    clipPath.setAttribute('clipPathUnits', 'objectBoundingBox')

    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path')
    path.classList.add('nt-shape-path')
    clipPath.appendChild(path)
    defs.appendChild(clipPath)
    svg.appendChild(defs)
    root.appendChild(svg)

    const content = document.createElement('div')
    content.classList.add('nt-shape-content')
    root.appendChild(content)

    document.body.appendChild(root)
    return { root, path }
  }

  beforeEach(() => {
    currentTimestamp = 0
    document.body.innerHTML = ''
    jest.clearAllMocks()
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: jest.fn().mockImplementation((query) => ({
        matches: false,
        media: query,
        addListener() { },
        removeListener() { },
        addEventListener() { },
        removeEventListener() { },
        dispatchEvent() { return false },
        onchange: null
      }))
    })

    jest.spyOn(global, 'requestAnimationFrame').mockImplementation((callback) => {
      currentTimestamp += 16
      callback(currentTimestamp)
      return currentTimestamp
    })
    jest.spyOn(global, 'cancelAnimationFrame').mockImplementation(() => { })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  test('catalog definitions use a shared topology and valid initial paths', () => {
    const circle = __testHooks.getShapeDefinition(0)
    const heart = __testHooks.getShapeDefinition(24)

    expect(circle.points.length).toBe(heart.points.length)
    expect(circle.pathData.startsWith('M')).toBe(true)
    expect(heart.pathData.endsWith('Z')).toBe(true)
  })

  test('onLoad snaps the initial shape path', () => {
    const { root, path } = createShapeElement('24')

    onLoad(root)

    expect(path.getAttribute('d')).toBe(__testHooks.getShapeDefinition(24).pathData)
  })

  test('onUpdate snaps when animation is disabled', () => {
    const { root, path } = createShapeElement('0', 'false')

    onLoad(root)
    root.dataset.shape = '17'
    onUpdate(root)

    expect(path.getAttribute('d')).toBe(__testHooks.getShapeDefinition(17).pathData)
  })

  test('onUpdate animates to the target shape when animation is enabled', () => {
    const { root, path } = createShapeElement('0', 'true')

    onLoad(root)
    root.dataset.shape = '24'
    onUpdate(root)

    expect(global.requestAnimationFrame).toHaveBeenCalled()
    expect(path.getAttribute('d')).toBe(__testHooks.getShapeDefinition(24).pathData)
  })

  test('reduced motion snaps even when animation is enabled', () => {
    window.matchMedia.mockImplementation((query) => ({
      matches: query === '(prefers-reduced-motion: reduce)',
      media: query,
      addListener() { },
      removeListener() { },
      addEventListener() { },
      removeEventListener() { },
      dispatchEvent() { return false },
      onchange: null
    }))

    const { root, path } = createShapeElement('0', 'true')

    onLoad(root)
    root.dataset.shape = '29'
    onUpdate(root)

    expect(path.getAttribute('d')).toBe(__testHooks.getShapeDefinition(29).pathData)
  })

  test('lifecycle calls are null-safe and dispose clears state', () => {
    const { root } = createShapeElement('0', 'true')

    expect(() => onLoad(null)).not.toThrow()
    expect(() => onUpdate(null)).not.toThrow()
    expect(() => onDispose(null)).not.toThrow()

    onLoad(root)
    expect(__testHooks.states.has(root)).toBe(true)

    onDispose(root)
    expect(__testHooks.states.has(root)).toBe(false)
  })
})

/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals'
import { onDispose, onLoad, onUpdate } from '../NTLoader.razor.js'

describe('NTLoader runtime', () => {
  let currentTimestamp = 0

  function createLoaderElement({
    animate = 'true',
    sequence = '1 2 3',
    interval = '1000',
    transitionDuration = '700'
  } = {}) {
    const loader = document.createElement('div')
    loader.classList.add('nt-loader')
    loader.dataset.animate = animate
    loader.dataset.shapeSequence = sequence
    loader.dataset.shapeIntervalMs = interval
    loader.dataset.transitionDurationMs = transitionDuration

    const shape = document.createElement('nt-shape')
    shape.classList.add('nt-loader-shape')
    shape.dataset.shape = sequence.split(' ')[0]
    shape.dataset.animateShapeChanges = 'false'
    shape.dataset.transitionDurationMs = '64'
    shape.dataset.transitionEasing = '0'
    shape.dataset.clipId = 'nt-loader-shape-clip-test'

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg')
    svg.classList.add('nt-shape-defs')

    const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs')
    const clipPath = document.createElementNS('http://www.w3.org/2000/svg', 'clipPath')
    clipPath.setAttribute('id', 'nt-loader-shape-clip-test')
    clipPath.setAttribute('clipPathUnits', 'objectBoundingBox')

    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path')
    path.classList.add('nt-shape-path')
    clipPath.appendChild(path)
    defs.appendChild(clipPath)
    svg.appendChild(defs)
    shape.appendChild(svg)

    const content = document.createElement('div')
    content.classList.add('nt-shape-content')
    shape.appendChild(content)

    const animation = {
      cancel: jest.fn(),
      addEventListener: jest.fn()
    }
    shape.animate = jest.fn(() => animation)

    loader.appendChild(shape)
    document.body.appendChild(loader)

    return { animation, loader, shape }
  }

  beforeEach(() => {
    currentTimestamp = 0
    document.body.innerHTML = ''
    jest.useFakeTimers()
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
    jest.useRealTimers()
    jest.restoreAllMocks()
  })

  test('onLoad is intentionally inert because per-render onUpdate starts the indicator', () => {
    const { loader, shape } = createLoaderElement()

    onLoad(loader)

    expect(shape.dataset.shape).toBe('1')
    expect(shape.animate).not.toHaveBeenCalled()
  })

  test('onUpdate starts immediately and unchanged updates do not restart from the first shape', () => {
    const { loader, shape } = createLoaderElement()

    onUpdate(loader)
    expect(shape.dataset.shape).toBe('2')
    expect(shape.animate).toHaveBeenCalledTimes(1)

    onUpdate(loader)
    expect(shape.dataset.shape).toBe('2')
    expect(shape.animate).toHaveBeenCalledTimes(1)
  })

  test('scheduled cycles advance shapes until dispose clears the loader state', () => {
    const { animation, loader, shape } = createLoaderElement()

    onUpdate(loader)
    jest.advanceTimersByTime(1000)

    expect(shape.dataset.shape).toBe('3')
    expect(shape.animate).toHaveBeenCalledTimes(2)

    onDispose(loader)
    jest.advanceTimersByTime(3000)

    expect(shape.animate).toHaveBeenCalledTimes(2)
    expect(animation.cancel).toHaveBeenCalled()
  })

  test('global updates coalesce into one document sync', () => {
    const first = createLoaderElement()
    const second = createLoaderElement()
    const querySelectorAll = jest.spyOn(document, 'querySelectorAll')

    onUpdate()
    onUpdate()

    expect(first.shape.animate).not.toHaveBeenCalled()
    expect(second.shape.animate).not.toHaveBeenCalled()

    jest.runAllTicks()

    expect(querySelectorAll).toHaveBeenCalledTimes(1)
    expect(first.shape.animate).toHaveBeenCalledTimes(1)
    expect(second.shape.animate).toHaveBeenCalledTimes(1)
  })
})

/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals'
import { onDispose, onLoad, onUpdate } from '../NTInputSelect.razor.js'

describe('NTInputSelect runtime', () => {
  function createSelect() {
    const root = document.createElement('div')
    root.classList.add('nt-input-select')

    const input = document.createElement('input')
    input.classList.add('tnt-input-select-search-input')
    input.dataset.requireSelection = 'false'
    root.appendChild(input)

    const hidden = document.createElement('input')
    hidden.type = 'hidden'
    root.appendChild(hidden)

    document.body.appendChild(root)
    return { input, root }
  }

  beforeEach(() => {
    document.body.innerHTML = ''
    jest.spyOn(global, 'requestAnimationFrame').mockImplementation((callback) => {
      callback(0)
      return 1
    })
    jest.spyOn(global, 'cancelAnimationFrame').mockImplementation(() => { })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  test('outside click close ignores disposed dispatcher failures', async () => {
    const { root } = createSelect()
    const dotNetRef = {
      invokeMethodAsync: jest.fn(() => Promise.reject(new Error('The renderer associated with this dispatcher is no longer available.')))
    }

    onLoad(root, dotNetRef)
    window.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }))
    window.dispatchEvent(new MouseEvent('click', { bubbles: true }))

    await Promise.resolve()

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CloseDropdownFromJs')
    onDispose(root)
  })

  test('dispose removes outside click handler and drops dotnet reference', () => {
    const { root } = createSelect()
    const dotNetRef = {
      invokeMethodAsync: jest.fn(() => Promise.resolve())
    }

    onLoad(root, dotNetRef)
    onUpdate(root, dotNetRef)
    onDispose(root)

    window.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }))
    window.dispatchEvent(new MouseEvent('click', { bubbles: true }))

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled()
  })
})

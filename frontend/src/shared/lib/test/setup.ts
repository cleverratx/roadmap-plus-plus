import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach } from 'vitest'

class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

globalThis.ResizeObserver ??= ResizeObserverStub

configure({ asyncUtilTimeout: 3000 })

afterEach(() => {
  cleanup()
})

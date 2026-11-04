import { useLayoutEffect, useState, type RefObject } from 'react'

/** Ширина элемента с подпиской на её изменения. Пока элемент не измерен (или в тестах), отдаёт fallback. */
export function useElementWidth(ref: RefObject<HTMLElement | null>, fallback: number): number {
  const [width, setWidth] = useState(fallback)

  useLayoutEffect(() => {
    const element = ref.current
    if (!element) {
      return
    }

    const update = () => {
      if (element.clientWidth > 0) {
        setWidth(element.clientWidth)
      }
    }

    update()
    const observer = new ResizeObserver(update)
    observer.observe(element)
    return () => observer.disconnect()
  }, [ref])

  return width
}

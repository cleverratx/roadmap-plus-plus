import { select } from 'd3-selection'
import { zoom, zoomIdentity, type D3ZoomEvent, type ZoomTransform } from 'd3-zoom'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useElementWidth } from '@/shared/lib'
import { Button } from '@/shared/ui'
import { axisTicks, chooseLevels, formatDate, pixelsPerDay, type Tick } from '../model/time-units'
import { baseScale, presets, scaleExtent, transformFor, visibleSpanDays } from '../model/zoom'
import styles from './Timeline.module.css'

const ROW_HEIGHT = 26
const BODY_HEIGHT = 240
const HEIGHT = ROW_HEIGHT * 2 + BODY_HEIGHT
const FALLBACK_WIDTH = 960

type TimelineProps = {
  now?: Date
}

export function Timeline({ now: nowProp }: TimelineProps) {
  const [now] = useState(() => nowProp ?? new Date())
  const viewportRef = useRef<HTMLDivElement>(null)
  const svgRef = useRef<SVGSVGElement>(null)
  const width = useElementWidth(viewportRef, FALLBACK_WIDTH)
  const [transform, setTransform] = useState<ZoomTransform>(zoomIdentity)

  const base = useMemo(() => baseScale(now, width), [now, width])
  const zoomBehavior = useMemo(
    () =>
      zoom<SVGSVGElement, unknown>()
        .scaleExtent(scaleExtent)
        .on('zoom', (event: D3ZoomEvent<SVGSVGElement, unknown>) => setTransform(event.transform)),
    [],
  )

  useEffect(() => {
    zoomBehavior.extent([
      [0, 0],
      [width, HEIGHT],
    ])
  }, [zoomBehavior, width])

  useEffect(() => {
    const svg = select(svgRef.current!)
    svg.call(zoomBehavior)
    return () => {
      svg.on('.zoom', null)
    }
  }, [zoomBehavior])

  const scale = transform.rescaleX(base)
  const levels = chooseLevels(pixelsPerDay(scale))
  const minor = axisTicks(scale, levels.minor, 'minor')
  const major = levels.major ? axisTicks(scale, levels.major, 'major') : []
  const [visibleStart, visibleEnd] = scale.domain()
  const rangeLabel = `${formatDate(visibleStart)} - ${formatDate(visibleEnd)}`
  const todayX = scale(now)

  function apply(next: ZoomTransform) {
    select(svgRef.current!).call(zoomBehavior.transform, next)
  }

  function showSpan(days: number) {
    apply(transformFor(base, scale.invert(width / 2), days, width))
  }

  function zoomBy(factor: number) {
    select(svgRef.current!).call(zoomBehavior.scaleBy, factor)
  }

  return (
    <div className={styles.timeline}>
      <div className={styles.toolbar} role="toolbar" aria-label="Масштаб таймлайна">
        <div className={styles.group}>
          {presets.map((preset) => (
            <Button key={preset.label} variant="ghost" onClick={() => showSpan(preset.days)}>
              {preset.label}
            </Button>
          ))}
        </div>
        <div className={styles.group}>
          <Button variant="ghost" aria-label="Отдалить" onClick={() => zoomBy(1 / 1.5)}>
            -
          </Button>
          <Button variant="ghost" aria-label="Приблизить" onClick={() => zoomBy(1.5)}>
            +
          </Button>
          <Button variant="ghost" onClick={() => apply(transformFor(base, now, visibleSpanDays(scale), width))}>
            Сегодня
          </Button>
        </div>
        <span className={styles.range} aria-live="polite">
          {rangeLabel}
        </span>
      </div>

      <div ref={viewportRef} className={styles.viewport}>
        <svg
          ref={svgRef}
          className={styles.svg}
          width={width}
          height={HEIGHT}
          role="img"
          aria-label={`Таймлайн, видно ${rangeLabel}. Колёсико мыши меняет масштаб, перетаскивание двигает время.`}
        >
          {levels.minor === 'day' && <Weekends ticks={minor} />}
          <Grid ticks={minor} />
          {major.length > 0 && <MajorRow ticks={major} width={width} />}
          <MinorRow ticks={minor} y={major.length > 0 ? ROW_HEIGHT : 0} />
          {todayX >= 0 && todayX <= width && (
            <g className={styles.today}>
              <line x1={todayX} x2={todayX} y1={ROW_HEIGHT * 2} y2={HEIGHT} />
              <text x={todayX + 4} y={ROW_HEIGHT * 2 + 14}>
                сегодня
              </text>
            </g>
          )}
        </svg>
      </div>
    </div>
  )
}

function MajorRow({ ticks, width }: { ticks: Tick[]; width: number }) {
  return (
    <g className={styles.major}>
      <rect x={0} y={0} width={width} height={ROW_HEIGHT} />
      {ticks.map((tick) => {
        const left = Math.max(tick.x, 0)
        const visible = Math.min(tick.x + tick.width, width) - left

        return (
          <g key={tick.key}>
            <line x1={tick.x} x2={tick.x} y1={0} y2={ROW_HEIGHT} />
            {visible > 60 && (
              <text x={left + 8} y={ROW_HEIGHT / 2}>
                {tick.label}
              </text>
            )}
          </g>
        )
      })}
    </g>
  )
}

function MinorRow({ ticks, y }: { ticks: Tick[]; y: number }) {
  return (
    <g className={styles.minor} transform={`translate(0, ${y})`}>
      {ticks.map((tick) => (
        <text key={tick.key} x={tick.x + tick.width / 2} y={ROW_HEIGHT / 2}>
          {tick.label}
        </text>
      ))}
    </g>
  )
}

function Grid({ ticks }: { ticks: Tick[] }) {
  return (
    <g className={styles.grid}>
      {ticks.map((tick) => (
        <line key={tick.key} x1={tick.x} x2={tick.x} y1={ROW_HEIGHT} y2={HEIGHT} />
      ))}
    </g>
  )
}

function Weekends({ ticks }: { ticks: Tick[] }) {
  return (
    <g className={styles.weekend}>
      {ticks
        .filter((tick) => tick.date.getDay() === 0 || tick.date.getDay() === 6)
        .map((tick) => (
          <rect key={tick.key} x={tick.x} y={ROW_HEIGHT * 2} width={tick.width} height={BODY_HEIGHT} />
        ))}
    </g>
  )
}

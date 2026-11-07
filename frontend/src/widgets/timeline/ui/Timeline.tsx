import { select } from 'd3-selection'
import { zoom, zoomIdentity, type D3ZoomEvent, type ZoomTransform } from 'd3-zoom'
import { useEffect, useMemo, useRef, useState } from 'react'
import { itemKindLabels, useItems } from '@/entities/item'
import { toFormErrors } from '@/shared/api'
import { useElementWidth } from '@/shared/lib'
import { Button } from '@/shared/ui'
import { fetchWindow } from '../model/fetch-window'
import { layoutItems, type Block } from '../model/layout'
import { axisTicks, chooseLevels, formatDate, pixelsPerDay, type Tick } from '../model/time-units'
import { baseScale, presets, scaleExtent, transformFor, visibleSpanDays } from '../model/zoom'
import styles from './Timeline.module.css'

const ROW_HEIGHT = 26
const AXIS_HEIGHT = ROW_HEIGHT * 2
const BODY_PADDING = 12
const MIN_BODY_HEIGHT = 240
const FALLBACK_WIDTH = 960
const CHAR_WIDTH = 7

export type TimelineWorkspace = {
  id: string
  color: string
}

type TimelineProps = {
  workspaces: readonly TimelineWorkspace[]
  hiddenWorkspaceIds?: ReadonlySet<string>
  now?: Date
}

export function Timeline({ workspaces, hiddenWorkspaceIds, now: nowProp }: TimelineProps) {
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

  const scale = transform.rescaleX(base)
  const [visibleStart, visibleEnd] = scale.domain()
  const range = fetchWindow(visibleStart, visibleEnd)
  const items = useItems(range)
  const colors = useMemo(() => new Map(workspaces.map((w) => [w.id, w.color])), [workspaces])
  const layout = layoutItems(items.data ?? [], scale, (id) => colors.get(id) ?? 'var(--color-text-muted)', hiddenWorkspaceIds)

  const bodyHeight = Math.max(MIN_BODY_HEIGHT, layout.height + BODY_PADDING * 2)
  const height = AXIS_HEIGHT + bodyHeight

  useEffect(() => {
    zoomBehavior.extent([
      [0, 0],
      [width, height],
    ])
  }, [zoomBehavior, width, height])

  useEffect(() => {
    const svg = select(svgRef.current!)
    svg.call(zoomBehavior)
    return () => {
      svg.on('.zoom', null)
    }
  }, [zoomBehavior])

  const levels = chooseLevels(pixelsPerDay(scale))
  const minor = axisTicks(scale, levels.minor, 'minor')
  const major = levels.major ? axisTicks(scale, levels.major, 'major') : []
  const rangeLabel = `${formatDate(visibleStart)} - ${formatDate(visibleEnd)}`
  const todayX = scale(now)
  const status = items.isError
    ? toFormErrors(items.error).message
    : items.isPending
      ? 'Загрузка...'
      : layout.blocks.length === 0
        ? 'Здесь пока нет событий с датами'
        : null

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
          height={height}
          role="img"
          aria-label={`Таймлайн, видно ${rangeLabel}. Колёсико мыши меняет масштаб, перетаскивание двигает время.`}
        >
          {levels.minor === 'day' && <Weekends ticks={minor} height={height} />}
          <Grid ticks={minor} height={height} />
          {major.length > 0 && <MajorRow ticks={major} width={width} />}
          <MinorRow ticks={minor} y={major.length > 0 ? ROW_HEIGHT : 0} />
          <g transform={`translate(0, ${AXIS_HEIGHT + BODY_PADDING})`}>
            {layout.blocks.map((block) => (
              <ItemBlock key={block.item.id} block={block} width={width} />
            ))}
          </g>
          {todayX >= 0 && todayX <= width && (
            <g className={styles.today}>
              <line x1={todayX} x2={todayX} y1={AXIS_HEIGHT} y2={height} />
              <text x={todayX + 4} y={height - 8}>
                сегодня
              </text>
            </g>
          )}
          {status && (
            <text className={styles.status} x={width / 2} y={AXIS_HEIGHT + bodyHeight / 2}>
              {status}
            </text>
          )}
        </svg>
      </div>
    </div>
  )
}

function ItemBlock({ block, width }: { block: Block; width: number }) {
  const { item } = block
  const labelX = Math.max(block.x, 0) + 6
  const room = Math.min(block.x + block.width, width) - labelX - 6
  const label = fit(item.title, room)
  const dates = item.startDate === item.endDate ? item.startDate : `${item.startDate} - ${item.endDate}`

  return (
    <g className={styles.block} data-kind={item.kind} style={{ color: block.color }}>
      <title>{`${itemKindLabels[item.kind]} "${item.title}", ${dates}`}</title>
      <rect x={block.x} y={block.y} width={Math.max(block.width, 3)} height={block.height} rx={4} />
      {label && (
        <text x={labelX} y={block.y + block.height / 2}>
          {label}
        </text>
      )}
    </g>
  )
}

function fit(title: string, room: number): string | null {
  const chars = Math.floor(room / CHAR_WIDTH)
  if (chars < 3) {
    return null
  }

  return title.length <= chars ? title : `${title.slice(0, chars - 3)}...`
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

function Grid({ ticks, height }: { ticks: Tick[]; height: number }) {
  return (
    <g className={styles.grid}>
      {ticks.map((tick) => (
        <line key={tick.key} x1={tick.x} x2={tick.x} y1={ROW_HEIGHT} y2={height} />
      ))}
    </g>
  )
}

function Weekends({ ticks, height }: { ticks: Tick[]; height: number }) {
  return (
    <g className={styles.weekend}>
      {ticks
        .filter((tick) => tick.date.getDay() === 0 || tick.date.getDay() === 6)
        .map((tick) => (
          <rect key={tick.key} x={tick.x} y={AXIS_HEIGHT} width={tick.width} height={height - AXIS_HEIGHT} />
        ))}
    </g>
  )
}

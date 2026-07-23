type Maybe<T> = T | null | undefined
type LayoutName = 'multi-browse' | 'uncontained' | 'uncontained-multi-aspect-ratio' | 'hero' | 'center-aligned-hero' | 'full-screen'

interface DotNetCarouselRef {
    invokeMethodAsync(methodName: 'NotifyIndexChangedAsync', index: number): Promise<unknown> | void
    invokeMethodAsync(methodName: 'NotifyAutoPlayPausedChangedAsync', paused: boolean): Promise<unknown> | void
}

interface ItemGeometry {
    readonly size: number
    readonly start: number
}

interface MultiBrowseArrangement {
    readonly large: number
    readonly largeCount: number
    readonly medium: number
    readonly small: number
}

interface ContainedLayout {
    readonly containerSize: number
    readonly focalCount: number
    readonly keylines: ReadonlyMap<number, ItemGeometry>
    readonly largestSize: number
    readonly maximumKeyline: number
    readonly minimumKeyline: number
    readonly smallSize: number
    readonly states: readonly (readonly ItemGeometry[])[]
    readonly step: number
}

interface DragState {
    lastAxis: number
    lastTime: number
    moved: boolean
    pointerId: number
    startAxis: number
    startScroll: number
    velocity: number
}

interface TransferredInteractionState {
    readonly activeIndex: number
    readonly dragState: DragState | null
    readonly expiresAt: number
    readonly itemCount: number
    readonly logicalScroll: number
}

declare global {
    interface HTMLElementTagNameMap {
        'nt-carousel': NTCarouselElement
    }
}

const gap = 8
const horizontalPadding = 16
const verticalPadding = 8
const pointerMoveThreshold = 6
const parallaxDistance = 24
const anchorSize = 1
const preferredItemWidthDefault = 186
const interactionTransferLifetime = 1500
const transferredInteractions = new Map<string, TransferredInteractionState>()

function clamp(value: number, minimum: number, maximum: number): number {
    return Math.min(maximum, Math.max(minimum, value))
}

function isDotNetReference(value: unknown): value is DotNetCarouselRef {
    return typeof (value as DotNetCarouselRef | null)?.invokeMethodAsync === 'function'
}

function isCarouselElement(value: unknown): value is NTCarouselElement {
    return value instanceof NTCarouselElement
}

function lerp(start: number, end: number, progress: number): number {
    return start + ((end - start) * progress)
}

function parseBoolean(value: string | null, fallback: boolean): boolean {
    if (value == null) {
        return fallback
    }

    return !['false', '0', 'no', 'off'].includes(value.trim().toLowerCase())
}

function parsePositiveNumber(value: string | null): number | null {
    if (value == null || value.trim() === '') {
        return null
    }

    const parsed = Number.parseFloat(value)
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

export class NTCarouselElement extends HTMLElement {
    private activeIndex = 0
    private allowDragging = true
    private animationFrame: number | null = null
    private animationGeneration = 0
    private autoPlayIntervalMs: number | null = null
    private autoPlayTimer: number | null = null
    private autoPlayUserPaused = false
    private containedLayout: ContainedLayout | null = null
    private dotNetRef: DotNetCarouselRef | null = null
    private dragState: DragState | null = null
    private hovering = false
    private initialized = false
    private isRtl = false
    private itemPositions: number[] = []
    private itemSizes: number[] = []
    private items: HTMLElement[] = []
    private lastNotifiedIndex = 0
    private lastLogicalScroll = 0
    private layout: LayoutName = 'multi-browse'
    private maxLargeItemWidth: number | null = null
    private preferredItemWidth = preferredItemWidthDefault
    private maxScroll = 0
    private mutationObserver: MutationObserver | null = null
    private reducedMotion = false
    private readonly reducedMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)')
    private resizeObserver: ResizeObserver | null = null
    private restoreInteractionFrame: number | null = null
    private scrollEndTimer: number | null = null
    private scrollFrame: number | null = null
    private showAutoPlayControl: HTMLButtonElement | null = null
    private snapEnabled = true
    private snapMarkers: HTMLElement[] = []
    private snapPositions: number[] = []
    private suppressNextClick = false
    private track: HTMLElement | null = null
    private viewport: HTMLElement | null = null

    private readonly onAutoPlayControlClick = (): void => {
        this.setAutoPlayPaused(!this.autoPlayUserPaused)
    }

    private readonly onClickCapture = (event: MouseEvent): void => {
        if (this.suppressNextClick) {
            event.preventDefault()
            event.stopImmediatePropagation()
            this.suppressNextClick = false
            return
        }

    }

    private readonly onFocusIn = (event: FocusEvent): void => {
        const item = (event.target as Element | null)?.closest<HTMLElement>('[data-carousel-item]')
        if (item) {
            const index = this.items.indexOf(item)
            if (index >= 0) {
                this.activeIndex = index
                this.updateTabStops(index)
            }
        }

        this.setAutoPlayPaused(true)
    }

    private readonly onKeyDown = (event: KeyboardEvent): void => {
        const item = (event.target as Element | null)?.closest<HTMLElement>('[data-carousel-item]')
        if (!item) {
            return
        }

        const currentIndex = this.items.indexOf(item)
        if (currentIndex < 0) {
            return
        }

        if ((event.key === 'Enter' || event.key === ' ') && item.dataset.clickable === 'true' && item.dataset.disabled !== 'true') {
            event.preventDefault()
            item.querySelector<HTMLElement>('.nt-carousel-item-content')?.click()
            return
        }

        let targetIndex: number | null = null
        if (event.key === 'Home') {
            targetIndex = 0
        }
        else if (event.key === 'End') {
            targetIndex = this.items.length - 1
        }
        else if (this.layout === 'full-screen') {
            if (event.key === 'ArrowUp') {
                targetIndex = currentIndex - 1
            }
            else if (event.key === 'ArrowDown') {
                targetIndex = currentIndex + 1
            }
        }
        else if (event.key === 'ArrowLeft') {
            targetIndex = currentIndex + (this.isRtl ? 1 : -1)
        }
        else if (event.key === 'ArrowRight') {
            targetIndex = currentIndex + (this.isRtl ? -1 : 1)
        }

        if (targetIndex == null) {
            return
        }

        event.preventDefault()
        this.goToIndex(clamp(targetIndex, 0, this.items.length - 1), true)
    }

    private readonly onPointerDown = (event: PointerEvent): void => {
        this.cancelAnimation()
        if (!this.viewport || (event.pointerType === 'mouse' && event.button !== 0)) {
            return
        }
        if (event.pointerType === 'touch') {
            this.clearAutoPlayTimer()
            return
        }
        if (!this.allowDragging) {
            return
        }

        const axis = this.layout === 'full-screen' ? event.clientY : event.clientX
        this.dragState = {
            lastAxis: axis,
            lastTime: event.timeStamp,
            moved: false,
            pointerId: event.pointerId,
            startAxis: axis,
            startScroll: this.getLogicalScroll(),
            velocity: 0
        }
        this.viewport.classList.add('nt-carousel-dragging')
        this.clearAutoPlayTimer()
        this.viewport.setPointerCapture(event.pointerId)
    }

    private readonly onPointerMove = (event: PointerEvent): void => {
        if (!this.viewport || !this.dragState || this.dragState.pointerId !== event.pointerId) {
            return
        }

        const axis = this.layout === 'full-screen' ? event.clientY : event.clientX
        const delta = axis - this.dragState.startAxis
        const direction = this.layout !== 'full-screen' && this.isRtl ? 1 : -1
        const target = this.dragState.startScroll + (delta * direction)
        const elapsed = Math.max(1, event.timeStamp - this.dragState.lastTime)
        const logicalDelta = (axis - this.dragState.lastAxis) * direction

        this.dragState.velocity = (this.dragState.velocity * 0.72) + ((logicalDelta / elapsed) * 0.28)
        this.dragState.lastAxis = axis
        this.dragState.lastTime = event.timeStamp
        this.dragState.moved ||= Math.abs(delta) >= pointerMoveThreshold
        this.setLogicalScroll(target)
        this.scheduleRender()

        if (this.dragState.moved) {
            event.preventDefault()
        }
    }

    private readonly onPointerUp = (event: PointerEvent): void => {
        if (!this.viewport) {
            return
        }
        if (!this.dragState) {
            if (event.pointerType === 'touch') {
                this.scheduleAutoPlay()
            }
            return
        }
        if (this.dragState.pointerId !== event.pointerId) {
            return
        }

        const releaseVelocity = this.dragState.velocity
        const projectedScroll = this.getLogicalScroll() + (releaseVelocity * 220)
        let targetIndex = this.findNearestIndex(projectedScroll)
        if (this.layout === 'hero' || this.layout === 'center-aligned-hero' || this.layout === 'full-screen') {
            const startingIndex = this.findNearestIndex(this.dragState.startScroll)
            targetIndex = clamp(targetIndex, startingIndex - 1, startingIndex + 1)
        }
        this.suppressNextClick = this.dragState.moved
        window.setTimeout(() => {
            this.suppressNextClick = false
        }, 0)
        this.dragState = null
        this.viewport.classList.remove('nt-carousel-dragging')
        if (this.viewport.hasPointerCapture(event.pointerId)) {
            this.viewport.releasePointerCapture(event.pointerId)
        }

        if (this.snapEnabled) {
            this.animateToIndex(targetIndex, false, releaseVelocity)
        }
        else {
            this.finishSettlement(this.findNearestIndex(this.getLogicalScroll()))
        }
    }

    private readonly onReducedMotionChange = (): void => {
        this.cancelAnimation()
        this.reducedMotion = this.reducedMotionQuery.matches
        if (this.reducedMotion) {
            this.setAutoPlayPaused(true)
        }
        this.layoutCarousel(true)
    }

    private readonly onScroll = (): void => {
        this.getLogicalScroll()
        this.clearAutoPlayTimer()
        this.scheduleRender()
        if (this.scrollEndTimer != null) {
            window.clearTimeout(this.scrollEndTimer)
        }
        this.scrollEndTimer = window.setTimeout(() => this.handleScrollEnd(), 140)
    }

    private readonly onScrollEnd = (): void => {
        this.handleScrollEnd()
    }

    private readonly onWheel = (): void => {
        this.cancelAnimation()
        this.scheduleAutoPlay()
    }

    private readonly onVisibilityChange = (): void => {
        if (document.hidden) {
            this.cancelAnimation()
            this.clearAutoPlayTimer()
        }
        else {
            this.scheduleAutoPlay()
        }
    }

    public connectedCallback(): void {
        this.update()
        this.observeMutations()
        this.restoreInteractionFrame = requestAnimationFrame(() => {
            this.restoreInteractionFrame = null
            this.restoreTransferredInteraction()
        })
    }

    public disconnectedCallback(): void {
        this.preserveInteractionForReplacement()
        this.removeElementListeners()
    }

    public removeElementListeners(): void {
        this.cancelAnimation()
        this.clearAutoPlayTimer()
        if (this.scrollEndTimer != null) {
            window.clearTimeout(this.scrollEndTimer)
            this.scrollEndTimer = null
        }
        if (this.scrollFrame != null) {
            cancelAnimationFrame(this.scrollFrame)
            this.scrollFrame = null
        }
        if (this.restoreInteractionFrame != null) {
            cancelAnimationFrame(this.restoreInteractionFrame)
            this.restoreInteractionFrame = null
        }

        this.resizeObserver?.disconnect()
        this.resizeObserver = null
        this.mutationObserver?.disconnect()
        this.mutationObserver = null
        this.reducedMotionQuery.removeEventListener('change', this.onReducedMotionChange)
        document.removeEventListener('visibilitychange', this.onVisibilityChange)
        this.removeEventListener('click', this.onClickCapture, true)
        this.removeEventListener('focusin', this.onFocusIn)
        this.removeEventListener('keydown', this.onKeyDown)
        this.removeEventListener('pointerenter', this.onPointerEnter)
        this.removeEventListener('pointerleave', this.onPointerLeave)
        this.showAutoPlayControl?.removeEventListener('click', this.onAutoPlayControlClick)
        this.viewport?.removeEventListener('pointerdown', this.onPointerDown)
        this.viewport?.removeEventListener('pointermove', this.onPointerMove)
        this.viewport?.removeEventListener('pointerup', this.onPointerUp)
        this.viewport?.removeEventListener('pointercancel', this.onPointerUp)
        this.viewport?.removeEventListener('scroll', this.onScroll)
        this.viewport?.removeEventListener('scrollend', this.onScrollEnd)
        this.viewport?.removeEventListener('wheel', this.onWheel)
        this.initialized = false
    }

    public update(dotNetRef?: Maybe<DotNetCarouselRef>): void {
        if (isDotNetReference(dotNetRef)) {
            this.dotNetRef = dotNetRef
        }

        const viewport = this.querySelector<HTMLElement>(':scope > [data-carousel-viewport]')
        const track = viewport?.querySelector<HTMLElement>(':scope > [data-carousel-track]') ?? null
        if (!viewport || !track) {
            return
        }

        const elementsChanged = viewport !== this.viewport || track !== this.track
        const replacedInteraction = elementsChanged && this.initialized ? this.captureInteractionState() : null
        if (elementsChanged && this.initialized) {
            this.removeElementListeners()
        }

        this.viewport = viewport
        this.track = track
        this.items = Array.from(track.querySelectorAll<HTMLElement>(':scope > [data-carousel-item]'))
        this.updateItemLabels()
        this.layout = this.readLayout()
        this.allowDragging = parseBoolean(this.dataset.allowDragging ?? null, true)
        this.snapEnabled = parseBoolean(this.dataset.snap ?? null, true)
        this.autoPlayIntervalMs = parsePositiveNumber(this.dataset.autoPlayInterval ?? null)
        this.autoPlayIntervalMs = this.autoPlayIntervalMs == null ? null : this.autoPlayIntervalMs * 1000
        this.maxLargeItemWidth = parsePositiveNumber(this.dataset.maxLargeWidth ?? null)
        this.preferredItemWidth = parsePositiveNumber(this.dataset.preferredItemWidth ?? null) ?? preferredItemWidthDefault
        this.isRtl = getComputedStyle(this).direction === 'rtl'
        this.reducedMotion = this.reducedMotionQuery.matches
        this.viewport.style.direction = 'ltr'
        for (const item of this.items) {
            item.style.direction = this.isRtl ? 'rtl' : 'ltr'
        }

        this.updateAutoPlayControl()
        if (!this.initialized) {
            this.addElementListeners()
        }

        this.activeIndex = clamp(this.activeIndex, 0, Math.max(0, this.items.length - 1))
        this.lastNotifiedIndex = clamp(this.lastNotifiedIndex, 0, Math.max(0, this.items.length - 1))
        this.layoutCarousel(true)
        if (replacedInteraction) {
            this.applyTransferredInteraction(replacedInteraction)
        }
        else {
            this.restoreTransferredInteraction()
        }
        this.observeMutations()
        this.scheduleAutoPlay()
    }

    private readonly onPointerEnter = (): void => {
        this.hovering = true
        this.clearAutoPlayTimer()
    }

    private readonly onPointerLeave = (): void => {
        this.hovering = false
        this.scheduleAutoPlay()
    }

    private addElementListeners(): void {
        if (!this.viewport) {
            return
        }

        this.initialized = true
        this.addEventListener('click', this.onClickCapture, true)
        this.addEventListener('focusin', this.onFocusIn)
        this.addEventListener('keydown', this.onKeyDown)
        this.addEventListener('pointerenter', this.onPointerEnter)
        this.addEventListener('pointerleave', this.onPointerLeave)
        this.viewport.addEventListener('pointerdown', this.onPointerDown)
        this.viewport.addEventListener('pointermove', this.onPointerMove, { passive: false })
        this.viewport.addEventListener('pointerup', this.onPointerUp)
        this.viewport.addEventListener('pointercancel', this.onPointerUp)
        this.viewport.addEventListener('scroll', this.onScroll, { passive: true })
        this.viewport.addEventListener('scrollend', this.onScrollEnd)
        this.viewport.addEventListener('wheel', this.onWheel, { passive: true })
        this.showAutoPlayControl?.addEventListener('click', this.onAutoPlayControlClick)
        this.reducedMotionQuery.addEventListener('change', this.onReducedMotionChange)
        document.addEventListener('visibilitychange', this.onVisibilityChange)
        this.resizeObserver = new ResizeObserver(() => this.layoutCarousel(true))
        this.resizeObserver.observe(this.viewport)
    }

    private animateToIndex(index: number, focus = false, initialVelocity = 0): void {
        if (!this.viewport || this.snapPositions.length === 0) {
            return
        }

        const targetIndex = clamp(index, 0, this.snapPositions.length - 1)
        const start = this.getLogicalScroll()
        const target = this.snapPositions[targetIndex]
        this.activeIndex = targetIndex
        this.updateTabStops(targetIndex)
        const focusTarget = this.items[targetIndex]
        focusTarget?.removeAttribute('aria-hidden')
        if (focus) {
            if (focusTarget) {
                focusTarget.style.visibility = 'visible'
            }
            focusTarget?.focus({ preventScroll: true })
        }

        if (this.reducedMotion || Math.abs(target - start) < 1) {
            this.setLogicalScroll(target)
            this.renderItems()
            if (focus) {
                focusTarget?.focus({ preventScroll: true })
            }
            this.finishSettlement(targetIndex)
            return
        }

        this.cancelAnimation()
        const generation = this.animationGeneration
        const startedAt = performance.now()
        const distance = Math.abs(target - start)
        const directedVelocity = Math.sign(target - start) * initialVelocity
        const duration = clamp(180 + (distance * 0.45) - (Math.max(0, directedVelocity) * 60), 180, 500)
        const initialSlope = clamp(Math.max(0, directedVelocity) * duration / distance, 0, 3)
        const tick = (now: number): void => {
            if (generation !== this.animationGeneration) {
                return
            }

            const progress = clamp((now - startedAt) / duration, 0, 1)
            const progressSquared = progress * progress
            const progressCubed = progressSquared * progress
            const eased = ((progressCubed - (2 * progressSquared) + progress) * initialSlope) + (-2 * progressCubed) + (3 * progressSquared)
            this.setLogicalScroll(lerp(start, target, eased))
            this.renderItems()
            if (progress < 1) {
                this.animationFrame = requestAnimationFrame(tick)
            }
            else {
                this.animationFrame = null
                if (focus) {
                    focusTarget?.focus({ preventScroll: true })
                }
                this.finishSettlement(targetIndex)
            }
        }
        this.animationFrame = requestAnimationFrame(tick)
    }

    private cancelAnimation(): void {
        this.animationGeneration++
        if (this.animationFrame != null) {
            cancelAnimationFrame(this.animationFrame)
            this.animationFrame = null
        }
    }

    private clearAutoPlayTimer(): void {
        if (this.autoPlayTimer != null) {
            window.clearTimeout(this.autoPlayTimer)
            this.autoPlayTimer = null
        }
    }

    private createContainedLayout(width: number): ContainedLayout {
        if (this.reducedMotion) {
            const hero = this.layout === 'hero' || this.layout === 'center-aligned-hero'
            const size = hero ? Math.max(120, width - 48) : Math.max(96, Math.min(this.maxLargeItemWidth ?? 240, (width - gap) / 2.2))
            const visibleCount = Math.max(2, Math.ceil(width / (size + gap)) + 1)
            const keylines = new Map<number, ItemGeometry>()
            for (let index = 0; index < visibleCount; index++) {
                keylines.set(index, { start: index * (size + gap), size })
            }

            return { containerSize: width, focalCount: 1, keylines, largestSize: size, maximumKeyline: visibleCount - 1, minimumKeyline: 0, smallSize: size, states: [[...keylines.values()]], step: size + gap }
        }

        const available = Math.max(120, width - (horizontalPadding * 2))
        const targetLarge = Math.min(this.preferredItemWidth, this.maxLargeItemWidth ?? Number.POSITIVE_INFINITY, available)
        const small = clamp(targetLarge / 3, 40, 56)
        const keylines = new Map<number, ItemGeometry>()
        if (this.layout === 'hero') {
            const large = Math.max(80, Math.min(this.maxLargeItemWidth ?? Number.POSITIVE_INFINITY, available - small - gap))
            const groupWidth = large + gap + small
            const start = horizontalPadding + Math.max(0, (available - groupWidth) / 2)
            keylines.set(0, { start, size: large })
            keylines.set(1, { start: start + large + gap, size: small })
            return { containerSize: width, focalCount: 1, keylines, largestSize: large, maximumKeyline: 1, minimumKeyline: 0, smallSize: small, states: [[...keylines.values()]], step: large + gap }
        }

        if (this.layout === 'center-aligned-hero') {
            const large = Math.max(80, Math.min(this.maxLargeItemWidth ?? Number.POSITIVE_INFINITY, available - (small * 2) - (gap * 2)))
            const groupWidth = large + (small * 2) + (gap * 2)
            const start = horizontalPadding + Math.max(0, (available - groupWidth) / 2)
            keylines.set(-1, { start, size: small })
            keylines.set(0, { start: start + small + gap, size: large })
            keylines.set(1, { start: start + small + gap + large + gap, size: small })
            return { containerSize: width, focalCount: 1, keylines, largestSize: large, maximumKeyline: 1, minimumKeyline: -1, smallSize: small, states: [[...keylines.values()]], step: large + gap }
        }

        const arrangement = this.createMultiBrowseArrangement(available, targetLarge)
        const sizes = [...Array<number>(arrangement.largeCount).fill(arrangement.large), arrangement.medium, arrangement.small]
        const states: ItemGeometry[][] = []
        for (let stateIndex = 0; stateIndex <= sizes.length - arrangement.largeCount; stateIndex++) {
            if (stateIndex > 0) {
                sizes.splice(stateIndex - 1, 0, sizes.pop()!)
            }
            let start = horizontalPadding
            const state = sizes.map(size => {
                const geometry = { start, size }
                start += size + gap
                return geometry
            })
            states.push(state)
        }
        states[0].forEach((geometry, index) => keylines.set(index, geometry))
        return {
            containerSize: width,
            focalCount: arrangement.largeCount,
            keylines,
            largestSize: arrangement.large,
            maximumKeyline: states[0].length - 1,
            minimumKeyline: 0,
            smallSize: arrangement.small,
            states,
            step: arrangement.large + gap
        }
    }

    private createMultiBrowseArrangement(available: number, targetLarge: number): MultiBrowseArrangement {
        const targetSmall = clamp(targetLarge / 3, 40, 56)
        const targetMedium = (targetLarge + targetSmall) / 2
        const maximumLargeCount = Math.max(1, Math.min(this.items.length - 2, Math.floor((available - targetMedium - targetSmall - (gap * 2)) / (targetLarge + gap))))
        let best: (MultiBrowseArrangement & { cost: number }) | null = null

        for (let largeCount = maximumLargeCount; largeCount >= 1; largeCount--) {
            const spacing = gap * (largeCount + 1)
            let large = targetLarge
            let medium = targetMedium
            let small = targetSmall
            let delta = available - spacing - (large * largeCount) - medium - small

            const adjustedSmall = clamp(small + delta, 40, 56)
            delta -= adjustedSmall - small
            small = adjustedSmall
            const adjustedMedium = clamp(medium + delta, small, large)
            delta -= adjustedMedium - medium
            medium = adjustedMedium
            large += delta / largeCount

            if (large < medium || large <= 0) {
                continue
            }

            const cost = (Math.abs(large - targetLarge) * largeCount) + Math.abs(medium - targetMedium) + Math.abs(small - targetSmall)
            if (!best || cost < best.cost || (Math.abs(cost - best.cost) < 0.01 && largeCount > best.largeCount)) {
                best = { cost, large, largeCount, medium, small }
            }
        }

        return best ?? { large: available - 96, largeCount: 1, medium: 48, small: 40 }
    }

    private createSnapMarkers(vertical: boolean): void {
        if (!this.track) {
            return
        }

        for (const marker of this.snapMarkers) {
            marker.remove()
        }
        this.snapMarkers = []
        if (!this.snapEnabled) {
            return
        }

        for (const logicalPosition of this.snapPositions) {
            const marker = document.createElement('span')
            marker.className = 'nt-carousel-snap-point'
            marker.ariaHidden = 'true'
            marker.style.position = 'absolute'
            marker.style.pointerEvents = 'none'
            marker.style.scrollSnapAlign = 'start'
            marker.style.inlineSize = '1px'
            marker.style.blockSize = '1px'
            if (vertical) {
                marker.style.insetBlockStart = `${logicalPosition}px`
                marker.style.insetInlineStart = '0'
            }
            else {
                const physicalPosition = this.isRtl ? this.maxScroll - logicalPosition : logicalPosition
                marker.style.insetInlineStart = `${physicalPosition}px`
                marker.style.insetBlockStart = '0'
            }
            this.track.appendChild(marker)
            this.snapMarkers.push(marker)
        }
    }

    private findNearestIndex(scroll: number): number {
        if (this.snapPositions.length === 0) {
            return 0
        }

        let nearestIndex = 0
        let nearestDistance = Number.POSITIVE_INFINITY
        for (let index = 0; index < this.snapPositions.length; index++) {
            const distance = Math.abs(this.snapPositions[index] - scroll)
            if (distance < nearestDistance) {
                nearestDistance = distance
                nearestIndex = index
            }
        }

        const activeDistance = Math.abs(this.snapPositions[this.activeIndex] - scroll)
        if (Math.abs(activeDistance - nearestDistance) < 0.5) {
            return this.activeIndex
        }

        return nearestIndex
    }

    private finishSettlement(index: number): void {
        this.activeIndex = clamp(index, 0, Math.max(0, this.items.length - 1))
        this.updateTabStops(this.activeIndex)
        if (this.activeIndex !== this.lastNotifiedIndex) {
            this.lastNotifiedIndex = this.activeIndex
            if (isDotNetReference(this.dotNetRef)) {
                Promise.resolve(this.dotNetRef.invokeMethodAsync('NotifyIndexChangedAsync', this.activeIndex)).catch(() => {
                    // Ignore late interop failures after a circuit or page disconnects.
                })
            }
        }
        this.scheduleAutoPlay()
    }

    private getContainedGeometry(position: number): ItemGeometry {
        const layout = this.containedLayout
        if (!layout) {
            return { start: 0, size: 0 }
        }

        const lowerIndex = Math.floor(position)
        const progress = position - lowerIndex
        const lower = this.getIntegerGeometry(lowerIndex, layout)
        const upper = this.getIntegerGeometry(lowerIndex + 1, layout)
        return {
            start: lerp(lower.start, upper.start, progress),
            size: lerp(lower.size, upper.size, progress)
        }
    }

    private getIntegerGeometry(index: number, layout: ContainedLayout): ItemGeometry {
        const exact = layout.keylines.get(index)
        if (exact) {
            return exact
        }

        if (index < layout.minimumKeyline) {
            const distance = layout.minimumKeyline - index
            return { start: -anchorSize - ((distance - 1) * (anchorSize + gap)), size: anchorSize }
        }

        const distance = index - layout.maximumKeyline
        return { start: layout.containerSize + ((distance - 1) * (anchorSize + gap)), size: anchorSize }
    }

    private getLogicalScroll(): number {
        if (!this.viewport || !this.viewport.isConnected) {
            return this.lastLogicalScroll
        }
        let logical: number
        if (this.layout === 'full-screen') {
            logical = clamp(this.viewport.scrollTop, 0, this.maxScroll)
        }
        else {
            logical = clamp(this.isRtl ? this.maxScroll - this.viewport.scrollLeft : this.viewport.scrollLeft, 0, this.maxScroll)
        }
        this.lastLogicalScroll = logical
        return logical
    }

    private goToIndex(index: number, focus: boolean): void {
        this.clearAutoPlayTimer()
        this.animateToIndex(index, focus)
    }

    private handleScrollEnd(): void {
        if (this.scrollEndTimer != null) {
            window.clearTimeout(this.scrollEndTimer)
            this.scrollEndTimer = null
        }
        if (this.dragState || this.animationFrame != null) {
            return
        }

        const nearest = this.findNearestIndex(this.getLogicalScroll())
        if (this.snapEnabled && Math.abs(this.getLogicalScroll() - this.snapPositions[nearest]) > 1) {
            this.animateToIndex(nearest)
        }
        else {
            this.finishSettlement(nearest)
        }
    }

    private layoutCarousel(preserveActive: boolean): void {
        if (!this.viewport || !this.track || this.items.length === 0) {
            return
        }

        const active = preserveActive ? this.activeIndex : 0
        const dragScroll = this.dragState ? this.lastLogicalScroll : null
        this.isRtl = getComputedStyle(this).direction === 'rtl'
        this.setAttribute('data-enhanced', 'true')
        this.setAttribute('data-reduced-motion', this.reducedMotion ? 'true' : 'false')
        if (this.layout === 'full-screen') {
            this.layoutFullScreen()
        }
        else if (this.layout === 'uncontained' || this.layout === 'uncontained-multi-aspect-ratio') {
            this.layoutUncontained()
        }
        else {
            this.layoutContained()
        }

        this.createSnapMarkers(this.layout === 'full-screen')
        this.setLogicalScroll(dragScroll ?? this.snapPositions[clamp(active, 0, this.snapPositions.length - 1)] ?? 0)
        this.updateTabStops(active)
        this.renderItems()
    }

    private layoutContained(): void {
        if (!this.viewport || !this.track) {
            return
        }

        const width = this.viewport.clientWidth
        this.containedLayout = this.createContainedLayout(width)
        const visibleKeylines = this.containedLayout.maximumKeyline - this.containedLayout.minimumKeyline + 1
        if (!this.reducedMotion && this.layout === 'multi-browse') {
            const baseShift = Math.max(0, this.items.length - visibleKeylines)
            const lastDefaultIndex = baseShift + this.containedLayout.focalCount - 1
            const trailingSizes = this.containedLayout.states[0].slice(this.containedLayout.focalCount).map(keyline => keyline.size).reverse()
            this.snapPositions = this.items.map((_, index) => {
                if (index <= lastDefaultIndex) {
                    return Math.max(0, index - this.containedLayout!.focalCount + 1) * this.containedLayout!.step
                }

                const endStep = Math.min(index - lastDefaultIndex, trailingSizes.length)
                return (baseShift * this.containedLayout!.step) + trailingSizes.slice(0, endStep).reduce((total, size) => total + size + gap, 0)
            })
        }
        else {
            const maximumState = this.reducedMotion ? Math.max(0, this.items.length - visibleKeylines) : Math.max(0, this.items.length - 1)
            this.snapPositions = this.items.map((_, index) => Math.min(index, maximumState) * this.containedLayout!.step)
        }
        this.maxScroll = this.snapPositions.at(-1) ?? 0
        this.track.style.inlineSize = `${this.maxScroll + width}px`
        this.track.style.blockSize = '100%'
        this.track.style.minBlockSize = '0'
        this.itemPositions = []
        this.itemSizes = []
    }

    private layoutFullScreen(): void {
        if (!this.viewport || !this.track) {
            return
        }

        this.containedLayout = null
        const width = this.viewport.clientWidth
        const height = this.viewport.clientHeight
        this.itemPositions = this.items.map((_, index) => index * (height + 16))
        this.itemSizes = this.items.map(() => height)
        this.snapPositions = [...this.itemPositions]
        this.maxScroll = this.snapPositions.at(-1) ?? 0
        this.track.style.inlineSize = `${width}px`
        this.track.style.blockSize = `${this.maxScroll + height}px`
        this.track.style.minBlockSize = '0'
    }

    private layoutUncontained(): void {
        if (!this.viewport || !this.track) {
            return
        }

        this.containedLayout = null
        const width = this.viewport.clientWidth
        const height = Math.max(40, this.viewport.clientHeight - (verticalPadding * 2))
        let start = horizontalPadding
        this.itemPositions = []
        this.itemSizes = []
        for (const item of this.items) {
            const ratio = this.layout === 'uncontained-multi-aspect-ratio'
                ? clamp(parsePositiveNumber(item.dataset.aspectRatio ?? null) ?? 1, 9 / 16, 16 / 9)
                : null
            const size = ratio == null
                ? Math.max(120, Math.min(this.maxLargeItemWidth ?? 320, Math.min(width * 0.72, height * 1.25)))
                : height * ratio
            this.itemPositions.push(start)
            this.itemSizes.push(size)
            start += size + gap
        }

        const trackWidth = Math.max(width, start - gap + horizontalPadding)
        this.maxScroll = Math.max(0, trackWidth - width)
        this.snapPositions = this.itemPositions.map(position => clamp(position - horizontalPadding, 0, this.maxScroll))
        this.track.style.inlineSize = `${trackWidth}px`
        this.track.style.blockSize = '100%'
        this.track.style.minBlockSize = '0'
    }

    private readLayout(): LayoutName {
        const value = this.dataset.layout
        switch (value) {
            case 'uncontained':
            case 'uncontained-multi-aspect-ratio':
            case 'hero':
            case 'center-aligned-hero':
            case 'full-screen':
                return value
            default:
                return 'multi-browse'
        }
    }

    private getInteractionTransferKey(): string | null {
        if (this.id) {
            return `id:${this.id}`
        }

        const label = this.getAttribute('aria-label')
        return label ? `label:${this.dataset.layout ?? 'multi-browse'}:${label}` : null
    }

    private observeMutations(): void {
        if (this.mutationObserver) {
            return
        }

        this.mutationObserver = new MutationObserver(() => this.update())
        this.mutationObserver.observe(this, { childList: true })
    }

    private preserveInteractionForReplacement(): void {
        const key = this.getInteractionTransferKey()
        const state = this.captureInteractionState()
        if (!key || !state) {
            return
        }

        transferredInteractions.set(key, state)
        window.setTimeout(() => {
            if (transferredInteractions.get(key) === state) {
                transferredInteractions.delete(key)
            }
        }, interactionTransferLifetime)
    }

    private restoreTransferredInteraction(): void {
        const key = this.getInteractionTransferKey()
        const state = key ? transferredInteractions.get(key) : null
        if (!key || !state) {
            return
        }
        if (state.expiresAt < performance.now() || (this.items.length > 0 && state.itemCount !== this.items.length)) {
            transferredInteractions.delete(key)
            return
        }
        if (this.items.length === 0 || !this.viewport) {
            return
        }

        transferredInteractions.delete(key)
        this.applyTransferredInteraction(state)
    }

    private captureInteractionState(): TransferredInteractionState | null {
        if (this.dragState == null) {
            return null
        }

        return {
            activeIndex: this.activeIndex,
            dragState: this.dragState ? { ...this.dragState } : null,
            expiresAt: performance.now() + interactionTransferLifetime,
            itemCount: this.items.length,
            logicalScroll: this.lastLogicalScroll
        }
    }

    private applyTransferredInteraction(state: TransferredInteractionState): void {
        if (!this.viewport) {
            return
        }

        this.activeIndex = clamp(state.activeIndex, 0, this.items.length - 1)
        this.lastNotifiedIndex = this.activeIndex
        this.setLogicalScroll(state.logicalScroll)
        this.dragState = state.dragState
        this.updateTabStops(this.activeIndex)
        if (this.dragState) {
            this.viewport.classList.add('nt-carousel-dragging')
            this.clearAutoPlayTimer()
            try {
                this.viewport.setPointerCapture(this.dragState.pointerId)
            }
            catch {
                // The next pointer move can continue without capture while the pointer remains over the replacement viewport.
            }
        }
        this.renderItems()
    }

    private renderContainedItems(): void {
        if (!this.viewport || !this.containedLayout) {
            return
        }

        const width = this.viewport.clientWidth
        const height = Math.max(40, this.viewport.clientHeight - (verticalPadding * 2))
        const scroll = this.getLogicalScroll()
        let lowerState = this.reducedMotion ? this.findNearestIndex(scroll) : 0
        if (!this.reducedMotion) {
            for (let index = 1; index < this.snapPositions.length && this.snapPositions[index] <= scroll; index++) {
                lowerState = index
            }
        }
        let upperState = lowerState
        if (!this.reducedMotion) {
            while (upperState + 1 < this.snapPositions.length && this.snapPositions[upperState + 1] <= this.snapPositions[lowerState]) {
                upperState++
            }
            upperState = Math.min(upperState + 1, this.items.length - 1)
        }
        const stateDistance = this.snapPositions[upperState] - this.snapPositions[lowerState]
        const stateProgress = !this.reducedMotion && stateDistance > 0 ? clamp((scroll - this.snapPositions[lowerState]) / stateDistance, 0, 1) : 0
        for (let index = 0; index < this.items.length; index++) {
            const item = this.items[index]
            const lower = this.getContainedGeometryForState(index, lowerState, width)
            const upper = this.getContainedGeometryForState(index, upperState, width)
            const geometry = this.interpolateContainedGeometry(lower, upper, stateProgress)
            const physicalStart = this.isRtl ? width - geometry.start - geometry.size : geometry.start
            const maskOffset = (this.containedLayout.largestSize - geometry.size) / 2
            const physicalHostStart = this.isRtl ? physicalStart - maskOffset : geometry.start - maskOffset
            const visible = geometry.start + geometry.size > 0 && geometry.start < width
            item.style.visibility = visible ? 'visible' : 'hidden'
            if (visible) {
                item.removeAttribute('aria-hidden')
            }
            else {
                item.setAttribute('aria-hidden', 'true')
            }
            item.style.inlineSize = `${this.containedLayout.largestSize}px`
            item.style.blockSize = `${height}px`
            item.style.transform = visible ? `translate3d(${this.getPhysicalScroll() + physicalHostStart}px, 0, 0)` : 'translate3d(0, 0, 0)'
            item.style.setProperty('--nt-carousel-item-width', `${this.containedLayout.largestSize}px`)
            item.style.setProperty('--nt-carousel-mask-offset', `${maskOffset}px`)
            item.style.setProperty('--nt-carousel-mask-width', `${geometry.size}px`)
            item.style.setProperty('--nt-carousel-media-width', `${this.containedLayout.largestSize + (parallaxDistance * 2)}px`)
            this.updateVisualState(item, physicalStart, geometry.size, width, this.containedLayout)
        }
    }

    private getContainedGeometryForState(itemIndex: number, state: number, width: number): ItemGeometry {
        const layout = this.containedLayout
        if (!layout) {
            return { start: 0, size: 0 }
        }

        const relativeIndex = itemIndex - state
        if (!this.reducedMotion && this.layout === 'multi-browse') {
            return this.getMultiBrowseGeometry(itemIndex, state, layout)
        }

        if (!this.reducedMotion && this.layout === 'center-aligned-hero' && state === 0) {
            const largeStart = horizontalPadding
            if (relativeIndex === 0) {
                return { start: largeStart, size: layout.largestSize }
            }

            return {
                start: largeStart + layout.largestSize + gap + ((relativeIndex - 1) * (layout.smallSize + gap)),
                size: layout.smallSize
            }
        }

        if (!this.reducedMotion && (this.layout === 'hero' || this.layout === 'center-aligned-hero') && state === this.items.length - 1) {
            const largeStart = width - horizontalPadding - layout.largestSize
            if (relativeIndex === 0) {
                return { start: largeStart, size: layout.largestSize }
            }

            return {
                start: largeStart + (relativeIndex * (layout.smallSize + gap)),
                size: layout.smallSize
            }
        }

        return this.getContainedGeometry(relativeIndex)
    }

    private interpolateContainedGeometry(lower: ItemGeometry, upper: ItemGeometry, progress: number): ItemGeometry {
        const start = lerp(lower.start, upper.start, progress)
        if (this.layout === 'multi-browse' && start < 0 && lower.start > 0 && upper.start + upper.size <= 0 && lower.size > upper.size) {
            const end = lerp(lower.start + lower.size, upper.start + upper.size, progress)
            const size = Math.max(upper.size, end)
            return {
                start: end - size,
                size
            }
        }

        return {
            start,
            size: lerp(lower.size, upper.size, progress)
        }
    }

    private getMultiBrowseGeometry(itemIndex: number, state: number, layout: ContainedLayout): ItemGeometry {
        const visibleCount = layout.states[0].length
        const baseShift = Math.max(0, this.items.length - visibleCount)
        const lastDefaultIndex = baseShift + layout.focalCount - 1
        if (state <= lastDefaultIndex) {
            const shift = clamp(state - layout.focalCount + 1, 0, baseShift)
            return this.getStateGeometry(itemIndex - shift, layout.states[0], layout.containerSize, layout.smallSize)
        }

        const endStep = Math.min(state - lastDefaultIndex, layout.states.length - 1)
        return this.getStateGeometry(itemIndex - baseShift, layout.states[endStep], layout.containerSize, layout.smallSize)
    }

    private getStateGeometry(slot: number, state: readonly ItemGeometry[], containerSize: number, minimumSize: number): ItemGeometry {
        if (slot < 0) {
            return { start: -minimumSize - ((Math.abs(slot) - 1) * (minimumSize + gap)), size: minimumSize }
        }
        if (slot >= state.length) {
            return { start: containerSize + ((slot - state.length) * (minimumSize + gap)), size: minimumSize }
        }
        return state[slot]
    }

    private renderFullScreenItems(): void {
        if (!this.viewport) {
            return
        }

        const width = this.viewport.clientWidth
        const height = this.viewport.clientHeight
        const scroll = this.getLogicalScroll()
        for (let index = 0; index < this.items.length; index++) {
            const item = this.items[index]
            const start = this.itemPositions[index]
            const screenStart = start - scroll
            const visible = screenStart + height > -64 && screenStart < height + 64
            item.style.visibility = visible ? 'visible' : 'hidden'
            if (visible) {
                item.removeAttribute('aria-hidden')
            }
            else {
                item.setAttribute('aria-hidden', 'true')
            }
            item.style.inlineSize = `${width}px`
            item.style.blockSize = `${height}px`
            item.style.transform = visible ? `translate3d(0, ${start}px, 0)` : 'translate3d(0, 0, 0)'
            item.style.removeProperty('--nt-carousel-item-width')
            item.style.removeProperty('--nt-carousel-mask-offset')
            item.style.removeProperty('--nt-carousel-mask-width')
            item.style.removeProperty('--nt-carousel-media-width')
            item.dataset.visualSize = 'large'
            const normalized = clamp(((screenStart + (height / 2)) / height - 0.5) * 2, -1, 1)
            item.style.setProperty('--nt-carousel-parallax-x', '0px')
            item.style.setProperty('--nt-carousel-parallax-y', this.reducedMotion ? '0px' : `${-normalized * parallaxDistance}px`)
        }
    }

    private renderItems(): void {
        if (this.layout === 'full-screen') {
            this.renderFullScreenItems()
        }
        else if (this.containedLayout) {
            this.renderContainedItems()
        }
        else {
            this.renderUncontainedItems()
        }
    }

    private renderUncontainedItems(): void {
        if (!this.viewport || !this.track) {
            return
        }

        const width = this.viewport.clientWidth
        const height = Math.max(40, this.viewport.clientHeight - (verticalPadding * 2))
        const trackWidth = Number.parseFloat(this.track.style.inlineSize) || width
        const scroll = this.getLogicalScroll()
        for (let index = 0; index < this.items.length; index++) {
            const item = this.items[index]
            const logicalStart = this.itemPositions[index]
            const size = this.itemSizes[index]
            const physicalStart = this.isRtl ? trackWidth - logicalStart - size : logicalStart
            const screenStart = logicalStart - scroll
            const physicalScreenStart = this.isRtl ? width - screenStart - size : screenStart
            item.style.visibility = 'visible'
            item.removeAttribute('aria-hidden')
            item.style.inlineSize = `${size}px`
            item.style.blockSize = `${height}px`
            item.style.transform = `translate3d(${physicalStart}px, 0, 0)`
            item.style.removeProperty('--nt-carousel-item-width')
            item.style.removeProperty('--nt-carousel-mask-offset')
            item.style.removeProperty('--nt-carousel-mask-width')
            item.style.setProperty('--nt-carousel-media-width', `${size + (parallaxDistance * 2)}px`)
            item.dataset.visualSize = size <= 56 ? 'small' : 'large'
            const normalized = clamp(((physicalScreenStart + (size / 2)) / width - 0.5) * 2, -1, 1)
            item.style.setProperty('--nt-carousel-parallax-x', this.reducedMotion ? '0px' : `${-normalized * parallaxDistance}px`)
            item.style.setProperty('--nt-carousel-parallax-y', '0px')
        }
    }

    private scheduleAutoPlay(): void {
        this.clearAutoPlayTimer()
        if (this.autoPlayIntervalMs == null || this.autoPlayUserPaused || this.hovering || this.dragState || document.hidden || this.items.length < 2) {
            return
        }

        this.autoPlayTimer = window.setTimeout(() => {
            this.autoPlayTimer = null
            const currentPosition = this.snapPositions[this.activeIndex] ?? this.getLogicalScroll()
            let nextIndex = this.activeIndex + 1
            while (nextIndex < this.snapPositions.length && this.snapPositions[nextIndex] <= currentPosition + 0.5) {
                nextIndex++
            }

            if (nextIndex >= this.snapPositions.length) {
                this.animateToIndex(0)
                return
            }

            this.animateToIndex(nextIndex)
        }, this.autoPlayIntervalMs)
    }

    private scheduleRender(): void {
        if (this.scrollFrame != null) {
            return
        }
        this.scrollFrame = requestAnimationFrame(() => {
            this.scrollFrame = null
            this.renderItems()
        })
    }

    private setAutoPlayPaused(paused: boolean): void {
        if (this.autoPlayUserPaused === paused) {
            if (!paused) {
                this.scheduleAutoPlay()
            }
            return
        }

        this.autoPlayUserPaused = paused
        this.updateAutoPlayControlText()
        if (paused) {
            this.clearAutoPlayTimer()
        }
        else {
            this.scheduleAutoPlay()
        }

        if (isDotNetReference(this.dotNetRef)) {
            Promise.resolve(this.dotNetRef.invokeMethodAsync('NotifyAutoPlayPausedChangedAsync', paused)).catch(() => {
                // Ignore late interop failures after a circuit or page disconnects.
            })
        }
    }

    private setLogicalScroll(value: number): void {
        if (!this.viewport) {
            return
        }

        const logical = clamp(value, 0, this.maxScroll)
        this.lastLogicalScroll = logical
        if (this.layout === 'full-screen') {
            this.viewport.scrollTop = logical
        }
        else {
            this.viewport.scrollLeft = this.isRtl ? this.maxScroll - logical : logical
        }
    }

    private getPhysicalScroll(): number {
        if (!this.viewport || this.layout === 'full-screen') {
            return 0
        }
        return this.viewport.scrollLeft
    }

    private updateAutoPlayControl(): void {
        const control = this.querySelector<HTMLButtonElement>(':scope > [data-autoplay-control]')
        if (control !== this.showAutoPlayControl) {
            this.showAutoPlayControl?.removeEventListener('click', this.onAutoPlayControlClick)
            this.showAutoPlayControl = control
            if (this.initialized) {
                this.showAutoPlayControl?.addEventListener('click', this.onAutoPlayControlClick)
            }
        }
        if (this.reducedMotion && !this.autoPlayUserPaused) {
            this.autoPlayUserPaused = true
        }
        this.updateAutoPlayControlText()
    }

    private updateAutoPlayControlText(): void {
        if (!this.showAutoPlayControl) {
            return
        }
        const text = this.autoPlayUserPaused ? 'Start rotation' : 'Pause rotation'
        this.showAutoPlayControl.textContent = text
        this.showAutoPlayControl.setAttribute('aria-label', text)
    }

    private updateTabStops(activeIndex: number): void {
        for (let index = 0; index < this.items.length; index++) {
            this.items[index].tabIndex = index === activeIndex ? 0 : -1
        }
    }

    private updateItemLabels(): void {
        const count = this.items.length
        for (let index = 0; index < count; index++) {
            const item = this.items[index]
            const baseLabel = item.dataset.carouselAriaLabel ?? item.getAttribute('aria-label') ?? 'Item'
            item.dataset.carouselAriaLabel = baseLabel
            item.dataset.index = String(index)
            item.setAttribute('aria-label', `${baseLabel}, ${index + 1} of ${count}`)
        }
    }

    private updateVisualState(item: HTMLElement, physicalStart: number, size: number, width: number, layout: ContainedLayout): void {
        item.dataset.visualSize = size <= layout.smallSize + 2 ? 'small' : size >= layout.largestSize - 2 ? 'large' : 'medium'
        const normalized = clamp(((physicalStart + (size / 2)) / width - 0.5) * 2, -1, 1)
        item.style.setProperty('--nt-carousel-parallax-x', this.reducedMotion ? '0px' : `${-normalized * parallaxDistance}px`)
        item.style.setProperty('--nt-carousel-parallax-y', '0px')
    }
}

export function onLoad(element: Maybe<HTMLElement>, dotNetRef: Maybe<DotNetCarouselRef>): void {
    if (!customElements.get('nt-carousel')) {
        customElements.define('nt-carousel', NTCarouselElement)
    }
    if (isCarouselElement(element)) {
        element.update(dotNetRef)
    }
}

export function onUpdate(element: Maybe<HTMLElement>, dotNetRef: Maybe<DotNetCarouselRef>): void {
    if (isCarouselElement(element)) {
        element.update(dotNetRef)
    }
}

export function onDispose(element: Maybe<HTMLElement>): void {
    if (isCarouselElement(element)) {
        element.removeElementListeners()
    }
}

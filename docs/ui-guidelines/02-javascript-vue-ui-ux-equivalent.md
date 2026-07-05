# Achieving the Same UI/UX Practices in JavaScript / Vue

This document maps every UI/UX practice documented in
`01-csharp-xamarin-maui-ui-ux-practices.md` to an equivalent implementation
in a modern JavaScript/Vue ecosystem. The target stack is:

- **Vue 3** (Composition API, `<script setup>`)
- **Vite** as the build tool
- **Pinia** for state management
- **UnoCSS** (or Tailwind CSS) for token-driven styling
- **Vue Router** for navigation
- **shadcn-vue** or a custom component library for card/button primitives
- **FFImageLoading equivalent** = `vite-plugin-image-optimizer` + Vue lazy
  loading (`vue-lazy-hydration` or `unplugin-vue-components` with lazy)

---

## 1. Design system tokens → CSS custom properties

### 1.1 Colour palette (CSS variables)

The C# app uses a `ResourceDictionary` with `AppThemeBinding`. In Vue, the
equivalent is **CSS custom properties on `:root` and `[data-theme="dark"]`**:

```css
/* src/styles/tokens.css */
:root {
  /* Colours — all values mirrored from the C# app */
  --color-primary: #3B4F74;
  --color-primary-dim: #627290;
  --color-secondary: #DFD8F7;
  --color-tertiary: #65def1;
  --color-positive: #357461;
  --color-negative: #dc7da6;
  --color-danger: #dc7da6;

  --color-bg-light: #FFFFFF;
  --color-bg-dark: #0a0a0a;
  --color-bg-dark-alt: #000000;

  --color-text-primary: #212121;
  --color-text-secondary: #6E6E6E;
  --color-text-placeholder: #ACACAC;
  --color-text-disabled: #919191;
  --color-border: #E1E1E1;
  --color-border-dark: #C8C8C8;

  --color-gray-100: #E1E1E1;
  --color-gray-200: #C8C8C8;
  --color-gray-300: #ACACAC;
  --color-gray-400: #919191;
  --color-gray-500: #6E6E6E;
  --color-gray-600: #404040;
  --color-gray-900: #212121;
  --color-gray-950: #141414;

  --color-white: #FFFFFF;
  --color-black: #000000;

  /* Accent palette (light mode only — dark uses neutral) */
  --color-accent-yellow-100: #F7B548;
  --color-accent-cyan-100: #28C2D1;
  --color-accent-blue-100: #3E8EED;
}

[data-theme="dark"] {
  --color-bg: var(--color-bg-dark);
  --color-text: var(--color-white);
  --color-border: var(--color-gray-600);
  --color-text-placeholder: var(--color-gray-500);
  --color-text-disabled: var(--color-gray-300);
  --color-card-bg: var(--color-bg-dark);
  --color-card-bg-light: var(--color-white);

  /* Invert subtle surfaces for dark */
  --color-status-bg: rgba(136, 136, 136, 0.13); /* #22888888 */
}

:root:not([data-theme="dark"]) {
  --color-bg: var(--color-bg-light);
  --color-text: var(--color-text-primary);
  --color-border: var(--color-border);
  --color-card-bg: var(--color-card-bg-light);
  --color-status-bg: rgba(136, 136, 136, 0.13);
}
```

### 1.2 Typography

The C# app uses a custom font (`XcavateFont`, 14px base, bold for values).
In Vue:

```css
@font-face {
  font-family: 'XcavateFont';
  src: url('/fonts/xcavatefont.ttf') format('truetype');
  font-display: swap;
}

:root {
  --font-family: 'XcavateFont', system-ui, sans-serif;
  --font-size-base: 14px;
  --font-size-header-small: 20px;
  --font-size-header-large: 25px;
  --font-size-value: 30px;
  --font-weight-bold: 700;

  /* Match C#: no platform font scaling */
  text-size-adjust: none;
  -webkit-text-size-adjust: none;
}
```

```vue
<!-- Use consistently via CSS class -->
<span class="text-label">Label</span>       <!-- 14px, normal -->
<span class="text-value">Value</span>       <!-- 30px, bold -->
<span class="text-header">Section</span>     <!-- 20px, bold -->
<span class="text-title">Page Title</span>   <!-- 25px, bold -->
```

### 1.3 Spacing and sizing tokens

```css
:root {
  --space-gap: 8px;
  --space-card-radius: 10px;
  --space-card-radius-lg: 15px;
  --space-button-radius: 24px;    /* full pill on 48px height */
  --space-button-height: 48px;
  --space-page-padding-x: 20px;
  --space-page-padding-y: 15px;
  --space-item-spacing: 15px;

  /* Shadow definitions */
  --shadow-card: 0 2px 4px rgba(0, 0, 0, 0.13);
  --shadow-card-dark: 0 0 4px rgba(0, 0, 0, 0.25);
  --shadow-bottom-bar: 0 2px 0 rgba(0, 0, 0, 0.16);
}
```

### 1.4 How tokens are consumed in Vue

```vue
<template>
  <div class="card">
    <h3 class="text-label">{{ title }}</h3>
    <span class="text-value">{{ value }}</span>
  </div>
</template>

<style scoped>
.card {
  background: var(--color-card-bg);
  border-radius: var(--space-card-radius);
  box-shadow: var(--shadow-card);
  padding: 10px 0;
}
[data-theme="dark"] .card {
  box-shadow: var(--shadow-card-dark);
}
</style>
```

Or with UnoCSS/Tailwind config:

```ts
// uno.config.ts
export default defineConfig({
  theme: {
    colors: {
      primary: '#3B4F74',
      'primary-dim': '#627290',
      positive: '#357461',
      negative: '#dc7da6',
    },
    borderRadius: {
      'card': '10px',
      'card-lg': '15px',
      'button': '24px',
    },
    spacing: {
      gap: '8px',
      'page-x': '20px',
    },
  },
})
```

---

## 2. Architecture & navigation → Vue Router + Pinia

### 2.1 Three-shell navigation → three route groups

The C# app's three Shell instances map to **three Vue Router route groups**:

```ts
// router/index.ts
import { createRouter, createWebHistory } from 'vue-router'

// Onboarding (unauthenticated)
const onboardingRoutes = [
  {
    path: '/',
    component: () => import('@/layouts/OnboardingLayout.vue'),
    children: [
      { path: '', name: 'Welcome', component: () => import('@/views/WelcomePage.vue') }
    ]
  }
]

// No-account (has account, not KYC'd)
const noAccountRoutes = [
  {
    path: '/no-account',
    component: () => import('@/layouts/NoAccountLayout.vue'),
    children: [
      { path: 'marketplace', name: 'Marketplace', component: () => import('@/views/MarketplacePage.vue') },
      { path: 'account', name: 'Account', component: () => import('@/views/NoAccountPage.vue') },
      { path: 'help', name: 'Help', component: () => import('@/views/HelpPage.vue') },
      { path: 'noticeboard', name: 'Noticeboard', component: () => import('@/views/NoticeboardPage.vue') }
    ]
  }
]

// Fully authenticated
const authedRoutes = [
  {
    path: '/app',
    component: () => import('@/layouts/AppLayout.vue'),
    children: [
      { path: 'account', name: 'InvestorDashboard', component: () => import('@/views/InvestorMainPage.vue') },
      { path: 'marketplace', name: 'Marketplace', component: () => import('@/views/MarketplacePage.vue') },
      { path: 'noticeboard', name: 'Noticeboard', component: () => import('@/views/NoticeboardPage.vue') },
      { path: 'help', name: 'Help', component: () => import('@/views/HelpPage.vue') },
      { path: 'logged-out', name: 'LoggedOut', component: () => import('@/views/LoggedOutPage.vue') }
    ]
  }
]

// Navigation between shells = route change (or SPA full-reload if needed)
function navigateToAuthShell() {
  window.location.href = '/app'  // or router.replace('/app')
}
```

### 2.2 Navigation guards replace `OnboardingModel`

```ts
// composables/useOnboarding.ts
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useOnboardingStore = defineStore('onboarding', () => {
  const stage = ref<'onboarding' | 'no-account' | 'authenticated'>('onboarding')
  const kycVerified = ref(false)

  const currentShell = computed(() => {
    if (stage.value === 'onboarding') return 'onboarding'
    if (!kycVerified.value) return 'no-account'
    return 'authenticated'
  })

  function setStage(s: typeof stage.value) {
    stage.value = s
    // Auto-route
    if (s === 'authenticated') window.location.href = '/app'
  }

  return { stage, kycVerified, currentShell, setStage }
})
```

### 2.3 Nested page navigation

C# uses `Shell.Current.Navigation.PushAsync()`. Vue equivalent:

```ts
// Router-based push
router.push({ name: 'PropertyDetail', params: { id: propertyId } })

// Modal overlay instead of push (for property marketplace filter)
// See Section 9 (Popups and modals) below.
```

---

## 3. UI component patterns → Vue components

### 3.1 Page layout template → `<PageTemplate>` component

The C# app uses a `ControlTemplate` with named slots. Vue equivalent:

```vue
<!-- components/PageTemplate.vue -->
<template>
  <div class="page-wrapper">
    <!-- Top navigation (optional) -->
    <TopNavigationBar
      v-if="navigationBarVisible"
      :title="title"
      :extra-text="extra1Text"
      :extra-command="extra1Command"
      :has-shadow="navigationBarHasShadow"
    />

    <!-- Main scrollable content -->
    <slot name="main-content" />

    <!-- Popup layers (Z-ordered siblings) -->
    <slot name="popup-content" />

    <!-- Global overlays -->
    <BottomPillBackground />
    <TopPillBackground />
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title?: string
  navigationBarVisible?: boolean
  navigationBarHasShadow?: boolean
  extra1Text?: string
  extra1Command?: () => void
}>()
</script>
```

Usage:

```vue
<!-- views/PropertyDetailPage.vue -->
<script setup lang="ts">
import PageTemplate from '@/components/PageTemplate.vue'
import BottomPopupCard from '@/components/BottomPopupCard.vue'
import ExtrinsicStatusToast from '@/components/ExtrinsicStatusToast.vue'
</script>

<template>
  <PageTemplate :title="property?.name" :navigation-bar-visible="false">
    <template #main-content>
      <ScrollView>
        <div class="page-content">
          <RiskWarningBanner />
          <NftMultiImageView :images="propertyImages" />
          <h1 class="text-title">{{ property?.name }}</h1>
          <!-- ... -->
        </div>
      </ScrollView>
    </template>

    <template #popup-content>
      <BottomPopupCard v-model="showBuyPanel">
        <BuyPropertyTokensPanel />
      </BottomPopupCard>
      <ExtrinsicStatusToast />
    </template>
  </PageTemplate>
</template>
```

### 3.2 Cards → `<Card>` and `<ClickableCard>`

```vue
<!-- components/ClickableCard.vue -->
<template>
  <div
    class="clickable-card"
    :class="{ thin: isThin }"
    @click="handleClick"
    role="button"
    tabindex="0"
    @keydown.enter="handleClick"
  >
    <div class="card-inner">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  isThin?: boolean
}>()
const emit = defineEmits<{ click: [] }>()

function handleClick() {
  emit('click')
}
</script>

<style scoped>
.clickable-card {
  background: var(--color-card-bg);
  border-radius: var(--space-card-radius);
  box-shadow: var(--shadow-card);
  padding: 10px 0;
  cursor: pointer;
  transition: box-shadow 0.2s ease;
}
.clickable-card:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}
.clickable-card:active {
  transform: scale(0.98);
}
.clickable-card.thin {
  border-radius: var(--space-card-radius);
  height: 40px;
  display: flex;
  align-items: center;
}
[data-theme="dark"] .clickable-card {
  box-shadow: var(--shadow-card-dark);
}
</style>
```

### 3.3 Buttons

```vue
<!-- components/ElevatedButton.vue -->
<template>
  <button
    class="btn btn-primary"
    :disabled="disabled"
    :class="{ 'btn-disabled': disabled }"
  >
    <slot />
  </button>
</template>

<script setup lang="ts">
defineProps<{ disabled?: boolean }>()
</script>

<style scoped>
.btn {
  height: var(--space-button-height);
  border-radius: var(--space-button-radius);
  font-family: var(--font-family);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-bold);
  text-size-adjust: none;
  -webkit-text-size-adjust: none;
  cursor: pointer;
  border: none;
  transition: opacity 0.2s ease;
}
.btn-primary {
  background: var(--color-primary);
  color: var(--color-white);
}
.btn-secondary {
  background: var(--color-white);
  color: var(--color-primary);
  border: 2px solid var(--color-primary);
}
.btn-disabled {
  background: var(--color-primary-dim);
  color: var(--color-primary-dim);
  cursor: not-allowed;
  opacity: 0.6;
}
</style>
```

Usage in templates:

```vue
<ElevatedButton>Buy Tokens</ElevatedButton>
<ElevatedButton class="btn-secondary">Browse</ElevatedButton>
<ElevatedButton disabled>Coming Soon</ElevatedButton>
```

### 3.4 XcavateCell (2-column key-value)

C# structure: 80 px card, title left, value right in bold Primary colour,
arrow on right, tap navigation.

Vue equivalent:

```vue
<!-- components/XcavateCell.vue -->
<template>
  <ClickableCard class="xcavate-cell" @click="handleClick">
    <div class="cell-content">
      <div class="cell-title-wrapper">
        <span class="cell-title">{{ title }}</span>
        <InfoIcon
          v-if="infoCommand"
          class="info-icon"
          @click.stop="infoCommand()"
        />
      </div>
      <span class="cell-value" :style="{ color: 'var(--color-primary)' }">
        {{ value }}
      </span>
      <ChevronRightIcon v-if="!infoCommand" class="cell-arrow" />
    </div>
  </ClickableCard>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  value: string
  infoCommand?: () => void
  clickCommand?: () => void
}>()
const emit = defineEmits<{ click: [] }>()
function handleClick() {
  emit('click')
}
</script>

<style scoped>
.xcavate-cell {
  height: 80px;
}
.cell-content {
  display: flex;
  align-items: center;
  height: 100%;
  padding: 0 10px;
}
.cell-title-wrapper {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 4px;
}
.cell-title {
  font-size: 12px;
  color: var(--color-text-secondary);
}
.cell-value {
  font-size: 20px;
  font-weight: 700;
  font-family: var(--font-family);
}
.cell-arrow {
  margin-left: auto;
  width: 20px;
  height: 20px;
  opacity: 0.5;
}
.info-icon {
  width: 14px;
  height: 14px;
  cursor: pointer;
  opacity: 0.5;
}
</style>
```

Grid layout (same as C# — 2-column for paired cells):

```vue
<GridCols2>
  <XcavateCell title="Property shares" :value="totalShares" />
  <XcavateCell title="Total invested" :value="totalInvested" />
  <XcavateCell title="ROI" :value="roiPercent" />
  <BalanceCellView />
</GridCols2>
```

```vue
<!-- components/GridCols2.vue -->
<template>
  <div class="grid-cols-2">
    <slot />
  </div>
</template>
<style scoped>
.grid-cols-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
</style>
```

### 3.5 Form inputs

```vue
<!-- components/FormInput.vue — text input with optional Max pill -->
<template>
  <ClickableCard class="form-input thin">
    <div class="input-row">
      <input
        ref="inputRef"
        v-model="localValue"
        class="form-entry"
        :placeholder="placeholder"
        :type="inputType"
        :keyboard="keyboard"
        :spellcheck="spellCheck"
        @focus="onFocus"
        @blur="onBlur"
      />
      <button
        v-if="showMax"
        class="max-pill"
        @click="onMaxClick"
      >
        Max
      </button>
    </div>
  </ClickableCard>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  modelValue?: string
  placeholder?: string
  showMax?: boolean
  keyboard?: string
  spellCheck?: boolean
}>()
const emit = defineEmits<{
  'update:modelValue': [v: string]
  'max': []
}>()

const localValue = ref(props.modelValue ?? '')
watch(() => props.modelValue, v => { localValue.value = v ?? '' })

function onMaxClick() {
  emit('max')
}
</script>

<style scoped>
.form-input.thin {
  height: 40px;
}
.input-row {
  display: grid;
  grid-template-columns: 1fr auto;
  align-items: center;
  height: 40px;
  padding: 0 10px;
  gap: 10px;
}
.form-entry {
  font-family: var(--font-family);
  font-size: var(--font-size-base);
  font-weight: 700;
  border: none;
  outline: none;
  background: transparent;
  width: 100%;
}
.max-pill {
  background: var(--color-gray-100);
  border-radius: var(--space-card-radius);
  padding: 4px 12px;
  font-family: var(--font-family);
  font-size: 12px;
  font-weight: 700;
  color: var(--color-black);
  cursor: pointer;
}
</style>
```

```vue
<!-- components/FormValueView.vue — read-only label + value -->
<template>
  <ClickableCard class="form-value thin">
    <div class="value-row">
      <span class="value-label">{{ title }}</span>
      <span class="value-text font-mono">{{ value }}</span>
    </div>
  </ClickableCard>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  value: string
}>()
</script>

<style scoped>
.form-value.thin {
  height: 40px;
}
.value-row {
  display: grid;
  grid-template-columns: 120px 1fr;
  align-items: center;
  height: 40px;
  padding: 0 10px;
  gap: 10px;
}
.value-label {
  font-weight: 700;
  font-family: var(--font-family);
  font-size: 14px;
  white-space: nowrap;
}
.value-text {
  font-family: 'SourceCodePro', 'Fira Code', monospace;
  color: var(--color-text);
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
```

### 3.6 Lists — CollectionView → `<VirtualList>` or `<InfiniteLoading>`

```vue
<!-- views/MarketplacePage.vue -->
<template>
  <PageTemplate>
    <template #main-content>
      <!-- Pull-to-refresh + infinite scroll -->
      <SwipeRefresh @refresh="onRefresh" :refreshing="isRefreshing">
        <div class="scroll-container">
          <RiskWarningBanner />

          <VirtualList
            :items="properties"
            :item-height="280"
            :buffer="5"
          >
            <template #default="{ item }">
              <PropertyThumbnail
                :property="item"
                @tap="goToDetail(item)"
              />
            </template>
            <template #footer>
              <div class="list-footer">
                <LoadingItemView v-if="loading" />
                <EmptyStateView
                  v-else-if="noItems"
                  text="No Properties"
                />
              </div>
            </template>
          </VirtualList>
        </div>
      </SwipeRefresh>
    </template>

    <template #popup-content>
      <PropertyMarketplaceFilter
        v-model="showFilter"
      />
    </template>
  </PageTemplate>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import SwipeRefresh from '@/components/SwipeRefresh.vue'
import VirtualList from '@/components/VirtualList.vue'

const isRefreshing = ref(false)
const loading = ref(false)
const noItems = ref(false)
const properties = ref([])
const showFilter = ref(false)

async function onRefresh() {
  isRefreshing.value = true
  // fetch properties...
  isRefreshing.value = false
}

// Infinite scroll: VirtualList triggers 'load-more' when near bottom
function onScrollNearBottom() {
  if (!loading.value) loadMore()
}
</script>
```

For a simpler approach without a virtual list package, use the C# equivalent
scroll handler pattern:

```vue
<template>
  <div
    class="scroll-container"
    @scroll="onScrolled"
    ref="scrollRef"
  >
    <div class="content">
      <PropertyThumbnail
        v-for="prop in properties"
        :key="prop.id"
        :property="prop"
      />
      <LoadingItemView v-if="loading" />
    </div>
  </div>
</template>

<script setup lang="ts">
function onScrolled(e: Event) {
  const el = e.currentTarget as HTMLElement
  const remainingHeight = el.scrollHeight - (el.scrollTop + el.clientHeight)
  if (remainingHeight > 280) return  // Same threshold as C# app
  loadMore()
}
</script>

<style scoped>
.scroll-container {
  height: 100%;
  overflow-y: auto;
}
</style>
```

### 3.7 Property thumbnail card

C# `PropertyThumbnailView` has: cached image (200px), location, property name,
APY, shares, price, favourite toggle, status badge.

```vue
<!-- components/PropertyThumbnail.vue -->
<template>
  <ClickableCard @click="handleMore">
    <div class="thumbnail-card">
      <!-- Image area -->
      <div class="image-container">
        <LazyImage
          :src="property.imageUrl"
          fallback="/images/xcataveloading.gif"
          class="property-image"
        />
        <div class="image-overlay">
          <!-- Filled heart (favoured) -->
          <HeartFilledIcon
            v-if="property.favourite"
            class="fav-icon fav-filled"
          />
          <!-- Empty heart (not favoured) -->
          <HeartIcon
            v-else
            class="fav-icon"
            @click.stop="toggleFavourite"
          />
        </div>
        <!-- Status badge -->
        <div v-if="property.status" class="status-badge">
          {{ property.status }}
        </div>
      </div>

      <!-- Location + status row -->
      <div class="meta-row">
        <span class="location">{{ property.location }}</span>
      </div>

      <!-- Property name + APY row -->
      <div class="meta-row">
        <span class="property-name">{{ property.name }}</span>
        <span class="apy-label">{{ property.apy }}</span>
      </div>

      <!-- Shares + price row -->
      <div class="meta-row">
        <div>
          <span class="tokens-label">{{ property.tokensTitle }}</span>
          <span class="tokens-value">{{ property.tokens }}</span>
        </div>
        <div>
          <span class="price-label">Price</span>
          <span class="price-value">{{ property.price }}</span>
        </div>
      </div>
    </div>
  </ClickableCard>
</template>

<script setup lang="ts">
defineProps<{
  property: {
    id: string
    imageUrl: string
    name: string
    location: string
    apy: string
    tokensTitle: string
    tokens: string
    price: string
    status?: string
    favourite: boolean
  }
}>()

const emit = defineEmits<{
  tap: []
  favourite: []
}>()

function handleMore() { emit('tap') }
function toggleFavourite() {
  emit('favourite')
}
</script>

<style scoped>
.thumbnail-card {
  display: flex;
  flex-direction: column;
  gap: 5px;
}
.image-container {
  position: relative;
  height: 200px;
}
.property-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.image-overlay {
  position: absolute;
  top: 0;
  right: 0;
  padding: 10px;
}
.fav-icon {
  width: 50px;
  height: 50px;
  cursor: pointer;
}
.fav-filled {
  color: var(--color-black);
}
[data-theme="dark"] .fav-filled {
  color: var(--color-white);
}
.status-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  background: var(--color-status-bg);
  border-radius: 5px;
  padding: 4px 8px;
  font-size: 12px;
  color: var(--color-text);
}
.meta-row {
  display: grid;
  grid-template-columns: 1fr auto;
  padding: 0 10px;
  gap: 5px;
  align-items: center;
}
.meta-row:nth-child(4) {
  grid-template-columns: 1fr 1fr;
}
.property-name {
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.tokens-label {
  display: block;
}
.tokens-value {
  display: block;
  font-weight: 700;
}
.price-label {
  display: block;
  text-align: right;
}
.price-value {
  display: block;
  font-weight: 700;
  text-align: right;
}
</style>
```

### 3.8 Top navigation bar

C# `TopNavigationBar`: 45px, semi-transparent, back arrow, bold centred
title, optional right text with command.

```vue
<!-- components/TopNavigationBar.vue -->
<template>
  <div class="top-nav">
    <button class="nav-back" @click="onBack">
      <BackArrowIcon />
    </button>
    <span class="nav-title">{{ title }}</span>
    <span v-if="extraTitle" class="nav-extra" @click="onExtra">
      {{ extraTitle }}
    </span>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  extraTitle?: string
}>()
const emit = defineEmits<{
  back: []
  extra: []
}>()
function onBack() { emit('back') }
function onExtra() { emit('extra') }
</script>

<style scoped>
.top-nav {
  height: 45px;
  background: rgba(136, 136, 136, 0.53); /* #88888888 */
  display: flex;
  align-items: center;
  position: relative;
  width: 100%;
}
.nav-back {
  position: absolute;
  left: 0;
  width: 45px;
  height: 45px;
  background: none;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
.nav-back svg {
  width: 25px;
  height: 25px;
  color: white;
}
.nav-title {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  color: white;
  font-weight: 700;
  font-family: var(--font-family);
  text-align: center;
}
.nav-extra {
  position: absolute;
  right: 10px;
  color: white;
  font-family: var(--font-family);
  font-size: 14px;
  cursor: pointer;
}
</style>
```

### 3.9 Bottom navigation bar

C# `XcavateNavigationBarView`: 3 tabs (Account, Help, Marketplace).

```vue
<!-- components/BottomNavigationBar.vue -->
<template>
  <nav class="bottom-nav">
    <div class="bottom-nav-content">
      <NavigationTab
        v-for="tab in tabs"
        :key="tab.key"
        :title="tab.title"
        :icon-unselected="tab.iconUnselected"
        :icon-selected="tab.iconSelected"
        :is-selected="modelValue === tab.key"
        @click="select(tab.key)"
      />
    </div>
  </nav>
</template>

<script setup lang="ts">
const props = defineProps<{
  modelValue: string
}>()
const emit = defineEmits<{
  'update:modelValue': [v: string]
}>()

const tabs = [
  { key: 'account', title: 'My account', iconUnselected: 'xcavateuser.png', iconSelected: 'xcavateuserselected.png' },
  { key: 'help', title: 'Help', iconUnselected: 'xcavatehelp.png', iconSelected: 'xcavatehelpselected.png' },
  { key: 'marketplace', title: 'Marketplace', iconUnselected: 'xcavatemarketplace.png', iconSelected: 'xcavatemarketplaceselected.png' }
]

function select(key: string) {
  emit('update:modelValue', key)
  // Navigate via router
  // router.push({ name: key })
}
</script>

<style scoped>
.bottom-nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 50;
}
.bottom-nav-content {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  padding: 10px;
}
</style>
```

### 3.10 Stepper / progress indicator

C# `TopNavigationStepperBar` with `ProgressStepperView`.

```vue
<!-- components/ProgressStepperBar.vue -->
<template>
  <div class="stepper-bar">
    <button class="stepper-back" @click="onBack">
      <BackArrowIcon />
    </button>
    <ProgressStepper
      :step="step"
      :total="steps"
    />
  </div>
</template>

<script setup lang="ts">
defineProps<{
  step: number
  steps: number
}>()
const emit = defineEmits<{ back: [] }>()
function onBack() { emit('back') }
</script>
```

```vue
<!-- components/ProgressStepper.vue -->
<template>
  <div class="progress-dots">
    <span
      v-for="i in total"
      :key="i"
      class="dot"
      :class="{ filled: i <= step, skipped: i > step + 1 }"
    />
  </div>
</template>

<style scoped>
.progress-dots {
  display: flex;
  gap: 6px;
  align-items: center;
}
.dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--color-gray-300);
}
.dot.filled {
  background: var(--color-primary);
}
.dot.skipped {
  opacity: 0.3;
}
</style>
```

### 3.11 Popups and modals

Three popup types from the C# app:

**A. Bottom sheet (BottomPopupCard)**

```vue
<!-- components/BottomPopupCard.vue -->
<template>
  <Teleport to="body">
    <!-- Dark backdrop -->
    <Transition name="fade">
      <div v-if="open" class="backdrop" @click="close">
        <!-- Bottom sheet -->
        <div
          ref="sheetRef"
          class="sheet"
          :class="{ dragging: isDragging }"
          @touchstart="onTouchStart"
          @touchmove="onTouchMove"
          @touchend="onTouchEnd"
          @mousedown="onMouseDown"
        >
          <!-- Grabber handle -->
          <div class="sheet-handlebar" @click="close" />
          <!-- Title -->
          <div class="sheet-title">{{ title }}</div>
          <!-- Content -->
          <div class="sheet-content">
            <slot />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'

const props = defineProps<{
  open: boolean
  title?: string
}>()
const emit = defineEmits<{
  'update:open': [v: boolean]
}>()

const sheetRef = ref<HTMLElement | null>(null)
const isDragging = ref(false)
let startY = 0
let currentY = 0
let translateY = 500 // Start off-screen (same as C# TranslationY="500")

function close() {
  emit('update:open', false)
}

// Pan gesture — same as C# PanGestureRecognizer.PanUpdated
function onTouchStart(e: TouchEvent) {
  startY = e.touches[0].clientY
  isDragging.value = true
}
function onTouchMove(e: TouchEvent) {
  if (!isDragging.value) return
  currentY = e.touches[0].clientY - startY
  if (currentY > 0) {
    // Pulling down
    ;(sheetRef.value as HTMLElement).style.transform = `translateY(${currentY}px)`
  }
}
function onTouchEnd() {
  isDragging.value = false
  if (currentY > 150) close()  // Threshold to dismiss
  else {
    // Snap back
    ;(sheetRef.value as HTMLElement).style.transition = 'transform 0.3s ease'
    ;(sheetRef.value as HTMLElement).style.transform = 'translateY(0)'
    setTimeout(() => {
      ;(sheetRef.value as HTMLElement).style.transition = ''
    }, 300)
  }
  currentY = 0
}
// Mouse equivalent for desktop...
</script>

<style scoped>
.backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  background: rgba(0, 0, 0, 0.4);  /* #66000000 */
  display: flex;
  align-items: flex-end;
  justify-content: center;
}
.sheet {
  width: 100%;
  height: 60%;  /* Same as C# .6 proportion */
  background: var(--color-card-bg);
  border-radius: 20px 20px 0 0;
  padding: 0;
  transform: translateY(500px);
  transition: transform 0.5s ease;  /* Same as C# BottomCardPopupAnimationDuration=500 */
  touch-action: none;
}
.sheet.dragging {
  transition: none;
}
.open .sheet {
  transform: translateY(0);
}
.sheet-handlebar {
  width: 100%;
  height: 65px;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding-top: 10px;
}
.sheet-handlebar::after {
  content: '';
  width: 100px;
  height: 5px;
  border-radius: 2.5px;
  background: rgba(136, 136, 136, 0.53);  /* #888888 */
}
.sheet-title {
  text-align: center;
  padding: 10px 20px;
  font-weight: 700;
  font-family: var(--font-family);
}
.sheet-content {
  padding: 0 20px 20px;
  overflow-y: auto;
}
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s ease;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>
```

**B. Toast notifications (ExtrinsicStatusStackLayout)**

```vue
<!-- components/ExtrinsicStatusToast.vue -->
<template>
  <div class="toast-stack">
    <div
      v-for="toast in toasts"
      :key="toast.id"
      class="toast-item"
      :class="toast.status"
    >
      <span class="toast-text">{{ toast.message }}</span>
      <button v-if="toast.status === 'failed'" class="toast-dismiss" @click="dismiss(toast.id)">
        ✕
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useToastStore } from '@/stores/toast'
const toastStore = useToastStore()
const toasts = computed(() => toastStore.active)
function dismiss(id: string) { toastStore.remove(id) }
</script>

<style scoped>
.toast-stack {
  position: fixed;
  bottom: 90px;  /* Above bottom nav */
  left: 0;
  right: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  z-index: 60;
  padding: 0 20px;
}
.toast-item {
  background: rgba(136, 136, 136, 0.53);
  border-radius: 8px;
  padding: 10px 20px;
  color: white;
  font-family: var(--font-family);
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  max-width: 400px;
  justify-content: space-between;
}
.toast-item.success {
  background: var(--color-positive);
}
.toast-item.error {
  background: var(--color-negative);
}
.toast-dismiss {
  background: none;
  border: none;
  color: white;
  cursor: pointer;
  font-size: 16px;
}
</style>
```

**C. Full-page loader**

```vue
<!-- components/FullPageLoader.vue -->
<template>
  <div v-if="show" class="full-page-loader">
    <img src="/images/xcavateloading.gif" alt="Loading..." class="loader-gif" />
  </div>
</template>

<style scoped>
.full-page-loader {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;  /* Same as C# ZIndex=20 but scaled for web */
}
.loader-gif {
  width: 60px;
  height: 60px;
}
</style>
```

### 3.12 Risk warning banner

```vue
<!-- components/RiskWarningBanner.vue -->
<template>
  <div class="risk-warning">
    <span>Don't invest unless you're prepared to lose all the money you invest.
      This is a high-risk investment and you should not expect to be protected
      if something goes wrong.</span>
    <span class="risk-link" @click="learnMore">
      Take 2 mins to learn more.
    </span>
  </div>
</template>

<script setup lang="ts">
function learnMore() {
  // router.push({ name: 'RiskEducation' })
}
</script>

<style scoped>
.risk-warning {
  display: block;
  font-weight: 700;
  font-family: var(--font-family);
  font-size: 14px;
  line-height: 1.4;
}
.risk-link {
  color: var(--color-primary);
  cursor: pointer;
  font-weight: 700;
  text-decoration: underline;
}
</style>
```

### 3.13 User profile header

C# `UserProfileHeaderView`: 200px background image, circular avatar (80px),
name + role badge, account creation date.

```vue
<!-- components/UserProfileHeaderView.vue -->
<template>
  <div class="profile-header">
    <!-- Background -->
    <div class="profile-bg" :style="{ backgroundImage: `url(${profileBackground})` }" />
    <!-- Avatar -->
    <div class="avatar" :style="{ backgroundImage: `url(${profilePicture})` }" />
    <!-- Name + badge -->
    <div class="profile-info">
      <h2 class="profile-name">{{ fullName }}</h2>
      <UserTypeBadge :role="role" />
    </div>
    <!-- Created date -->
    <span class="profile-created">{{ createdText }}</span>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  fullName: string
  profilePicture: string
  profileBackground: string
  role: string
  createdText: string
}>()
</script>

<style scoped>
.profile-header {
  position: relative;
  height: 200px;
  background: var(--color-white);
  padding-bottom: 20px;
}
.profile-bg {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 160px;
  background-size: cover;
  background-position: center;
}
.avatar {
  position: absolute;
  bottom: 0;
  right: 10px;
  width: 80px;
  height: 80px;
  border-radius: 50%;
  border: 5px solid var(--color-white);
  background-size: cover;
  background-position: center;
}
.profile-info {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 20px;
  padding: 0 15px;
}
.profile-name {
  font-size: 18px;
  font-weight: 700;
  font-family: var(--font-family);
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  max-width: 230px;
}
.profile-created {
  display: block;
  padding: 5px 15px;
  color: gray;
  font-size: 14px;
}
</style>
```

### 3.14 NftAttributeView (property details)

C# shows property details as a series of `NftAttributeView` cards:

```vue
<!-- views/PropertyDetailPage.vue -->
<template>
  <PageTemplate>
    <template #main-content>
      <div class="detail-scroll">
        <RiskWarningBanner />
        <NftMultiImageView :images="propertyImages" />
        <h1 class="text-title">{{ property.name }}</h1>
        <LocationBadge :name="property.address" />

        <div class="price-row">
          <span class="price-label">Price per share</span>
          <span class="price-value">{{ pricePerShare }}</span>
          <StatusBadge v-if="status" :text="status" />
        </div>

        <span class="detail-label">Developer</span>
        <CompanyThumbnail :company="property.company" />

        <!-- 2x2 stats grid -->
        <GridCols2>
          <XcavateCell title="Listing price" :value="listingPrice" />
          <XcavateCell title="Gross yield" :value="apy" />
          <XcavateCell title="Shares" :value="tokensAvailable" />
          <XcavateCell title="Property" :value="propertyType" />
        </GridCols2>

        <SliderBar
          title="Similar property prices in area"
          :percentage="areaPricesPercentage"
          :min-label="'£200,000'"
          :max-label="'£270,000'"
        />

        <hr class="divider" />

        <PropertyTitleInfo title="Rental income pcm" />
        <span class="detail-value">{{ rentalIncome }}</span>

        <hr class="divider" />

        <SliderBar
          title="Rental demand in the area"
          :percentage="rentalDemandPercentage"
          :min-label="'Low'"
          :max-label="'High'"
        />

        <!-- Details section with attribute cards -->
        <div class="details-section">
          <h3>Property description</h3>
          <p>{{ property.description }}</h3>
          <h3>Details</h3>
          <AttributeRow label="Post code" :value="property.postCode" :thin="true" />
          <AttributeRow label="Flat / unit" :value="property.flatOrUnit" :thin="true" />
          <AttributeRow label="Local authority" :value="property.localAuthority" :thin="true" />
          <AttributeRow label="Town / city" :value="property.townCity" :thin="true" />
          <AttributeRow label="Location" :value="property.locationShort" :thin="true" />
          <AttributeRow label="Area" :value="property.area" :thin="true" />
          <AttributeRow label="Off street parking" :value="property.offStreetParking" :thin="true" />
          <AttributeRow label="Outdoor space" :value="property.outdoorSpace" :thin="true" />
          <AttributeRow label="Bedrooms" :value="property.bedrooms" :thin="true" />
          <AttributeRow label="Construction date" :value="property.constructionDate" :thin="true" />
          <AttributeRow label="Bathrooms" :value="property.bathrooms" :thin="true" />
          <AttributeRow label="Quality" :value="property.quality" :thin="true" />
        </div>

        <PropertyMap
          :url="property.mapUrl"
          :address="property.address"
          height="350px"
        />
      </div>
    </template>

    <template #popup-content>
      <BottomPopupCard v-model="showBuySheet">
        <BuyPropertyTokensPanel />
      </BottomPopupCard>
      <ExtrinsicStatusToast />
    </template>
  </PageTemplate>
</template>
```

### 3.15 Slider bar

C# `SliderView` with gradient bar, labels, thumb:

```vue
<!-- components/SliderBar.vue -->
<template>
  <div class="slider-bar">
    <PropertyTitleInfo :title="title" />
    <div class="slider-track">
      <!-- Background track -->
      <div class="track-bg" />
      <!-- Coloured portion -->
      <div
        class="track-fill"
        :style="{ width: percentage + '%' }"
      />
    </div>
    <div class="slider-labels">
      <span>{{ minLabel }}</span>
      <span>{{ maxLabel }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  percentage: number
  minLabel: string
  maxLabel: string
}>()
</script>

<style scoped>
.slider-track {
  position: relative;
  height: 5px;
  border-radius: 2.5px;
  margin: 10px 0;
}
.track-bg {
  position: absolute;
  inset: 0;
  background: rgba(136, 136, 136, 0.27);
  border-radius: 2.5px;
}
.track-fill {
  position: absolute;
  top: 0;
  left: 0;
  height: 100%;
  border-radius: 2.5px;
  background: linear-gradient(to right,
    #ecb278 10%,
    #dc7da6 40%,
    #3b4f74 70%,
    #57a0c5 100%
  );  /* Exact gradient from C# SliderView.xaml */
}
.slider-labels {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0;
  margin-top: -5px;
}
.slider-labels span {
  color: #888;
  font-size: 12px;
}
</style>
```

### 3.16 Page bottom bar (main page top bar)

The C# `XcavateMainPageTopNavigationBarView` is the main page header with
logo + notification bell + QR scanner + menu:

```vue
<!-- components/PageTopBar.vue -->
<template>
  <div class="page-top-bar">
    <img src="/images/realxmarket.png" alt="realXmarket" class="logo" />
    <div class="spacer" />
    <IconCircle icon="bell" @click="openMessaging" />
    <IconCircle icon="qr-scanner" @click="openQrScanner" />
    <IconCircle icon="menu" @click="openMenu" />
  </div>
</template>

<script setup lang="ts">
import IconCircle from './IconCircle.vue'
</script>

<style scoped>
.page-top-bar {
  display: grid;
  grid-template-columns: auto 1fr 35px 35px 35px;
  align-items: center;
  padding: 10px;
  gap: 10px;
  height: 65px;
}
.logo {
  height: 45px;
}
.spacer { }
</style>
```

```vue
<!-- components/IconCircle.vue -->
<template>
  <div class="icon-circle" @click="$emit('click')">
    <slot />
  </div>
</template>

<script setup lang="ts">
defineEmits<{ click: [] }>()
</script>

<style scoped>
.icon-circle {
  width: 35px;
  height: 35px;
  background: rgba(78, 78, 78, 0.1);  /* #1A4E4E4E */
  border-radius: 17.5px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}
</style>
```

---

## 4. Layout system → CSS Grid + Flexbox

### 4.1 AbsoluteLayout → CSS position: fixed / absolute

The C# app uses `AbsoluteLayout` heavily for overlay positioning. In Vue:

| C# pattern | Vue equivalent |
|---|---|
| `AbsoluteLayout LayoutBounds="0.5, 0.5, 1, 1" LayoutFlags="All"` | `position: absolute; inset: 0;` |
| `AbsoluteLayout LayoutBounds="0.5, 0, 1, 45" LayoutFlags="PositionProportional, WidthProportional"` | `position: absolute; top: 0; left: 0; right: 0; height: 45px;` |
| `ZIndex` | CSS `z-index` |

### 4.2 Grid → CSS Grid

The C# app uses `Grid` for structured layouts. CSS Grid is a direct equivalent:

```vue
<!-- 2-column grid (same as C# ColumnDefinitions="*,*") -->
<div class="grid-cols-2">
  <div>Left</div>
  <div>Right</div>
</div>

<!-- 3-column grid (same as C# ColumnDefinitions="120,*") -->
<div class="grid-value">
  <label>Title</label>
  <span>Value</span>
</div>
```

```css
.grid-cols-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;  /* C# ColumnSpacing */
}
.grid-value {
  display: grid;
  grid-template-columns: 120px 1fr;
  align-items: center;
  height: 40px;
  gap: 10px;  /* C# ColumnSpacing */
}
```

### 4.3 VerticalStackLayout → flex-direction: column

```css
.stack {
  display: flex;
  flex-direction: column;
  gap: 15px;  /* C# Spacing="15" */
  padding: 0 20px 20px 20px;  /* C# Padding="20, 80, 20, 110" */
}
```

### 4.4 ScrollView → overflow-y: auto

```css
.scroll-container {
  overflow-y: auto;
  height: 100%;
}
```

---

## 5. UX interaction patterns

### 5.1 Pull-to-refresh

```vue
<!-- components/SwipeRefresh.vue -->
<template>
  <div ref="container" class="swipe-refresh">
    <div v-if="refreshing" class="spinner" />
    <div :class="{ 'pulling': isPulling }">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
// Use native CSS overscroll-behavior or a library
// like @vueuse/gesture's useGesture
</script>
```

Or simply use a library like `@stefanobartoletti/vue3-swipe-cards` or
`@vueuse/gesture`.

### 5.2 Infinite scroll (lazy load)

```ts
// composables/useInfiniteScroll.ts
import { ref, onMounted } from 'vue'

export function useInfiniteScroll(loader: () => Promise<void>) {
  const loading = ref(false)
  const threshold = 280  // Same px threshold as C# app

  function checkNearBottom() {
    const el = document.documentElement
    const remaining = el.scrollHeight - (window.scrollY + window.innerHeight)
    if (remaining > threshold) return
    if (loading.value) return
    loadMore()
  }

  async function loadMore() {
    loading.value = true
    try { await loader() }
    finally { loading.value = false }
  }

  onMounted(() => window.addEventListener('scroll', checkNearBottom))
  return { loading, loadMore }
}
```

### 5.3 Loading states

C# has three loading patterns. Vue equivalents:

1. **Full-page loader**: `<FullPageLoader v-if="pageLoading" />` — an overlay
   component (shown above).
2. **Item-level loader**: A skeleton card in list footer.
3. **Empty state**: Conditional `EmptyStateView` component.

```vue
<!-- components/LoadingItemView.vue — skeleton placeholder -->
<template>
  <div class="loading-skeleton thin">
    <div class="skeleton-bar" />
    <div class="skeleton-bar short" />
  </div>
</template>

<style scoped>
.loading-skeleton.thin {
  height: 80px;
  border-radius: 10px;
  background: var(--color-gray-100);
}
.skeleton-bar {
  height: 12px;
  border-radius: 4px;
  background: var(--color-gray-200);
  margin: 10px 15px;
}
.skeleton-bar.short {
  width: 60%;
}
</style>
```

### 5.4 Error handling — BadInternetConnectionPage

```vue
<!-- views/BadInternetConnectionPage.vue -->
<template>
  <div class="error-page">
    <img src="/images/error-illustration.png" alt="No connection" />
    <h1>No Internet Connection</h1>
    <p>Please check your network connection and try again.</p>
    <ElevatedButton @click="retry">Retry</ElevatedButton>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
const router = useRouter()
function retry() {
  router.back()
}
</script>
```

### 5.5 Transaction status toasts

Managed via Pinia:

```ts
// stores/toast.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

interface Toast {
  id: string
  message: string
  status: 'loading' | 'success' | 'error'
  dismissible: boolean
}

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<Toast[]>([])
  const active = computed(() => toasts.value)

  function add(message: string, status: Toast['status'] = 'loading') {
    const id = crypto.randomUUID()
    toasts.value.push({ id, message, status, dismissible: status === 'error' })
    return id
  }

  function remove(id: string) {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  function success(message: string) { return add(message, 'success') }
  function error(message: string) { return add(message, 'error') }
  function loading(message: string) { return add(message, 'loading') }

  return { toasts, active, add, remove, success, error, loading }
})
```

### 5.6 Gesture handling

C# uses XAML `TapGestureRecognizer` and `PanGestureRecognizer`. Vue:

```vue
<!-- Tap: @click on any element -->
<div @click="handleTap">Tap me</div>

<!-- Pan (drag): use @stefanobartoletti/vue3-gesture or custom -->
<template>
  <div ref="el" @touchstart="onPanStart" @touchmove="onPanMove" @touchend="onPanEnd">
    <slot />
  </div>
</template>
```

For more complex pan gestures, use `@vueuse/gesture`:

```ts
import { useGesture } from '@vueuse/gesture'
const onPan = useGesture(
  {
    onDrag: ({ event, delta }) => {
      // delta[1] = vertical movement (same as C# PanGestureRecognizer.PanUpdated)
    }
  },
  { target: el }
)
```

### 5.7 Font icons (FontAwesome equivalent)

C# uses `FontImageSource` with FontAwesome glyphs. Vue:

```vue
<!-- Option 1: @fortawesome/vue-fontawesome -->
<script setup>
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { faHeart, faHeartSolid } from '@fortawesome/free-solid-svg-icons'
</script>
<template>
  <font-awesome-icon :icon="favoured ? faHeartSolid : faHeart" />
</template>
```

```vue
<!-- Option 2: Google Material Icons (CDN) -->
<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet">
<span class="material-icons">favorite</span>
<span class="material-icons">favorite_border</span>
```

### 5.8 Image lazy loading

C# uses `FFImageLoading.Maui.CachedImage`. Vue equivalents:

```vue
<!-- Option 1: native lazy loading (modern browsers) -->
<img :src="imageUrl" loading="lazy" />

<!-- Option 2: vue-lazy-hydration -->
<script setup>
import VueLazyHydration from 'vue-lazy-hydration'
import NftImage from '@/components/NftImage.vue'
</script>
<template>
  <HydrateOnVisible component="NftImage" :props="{ src: imageUrl }" />
</template>

<!-- Option 3: unplugin-images (build-time optimisation) -->
<!-- Similar to FFImageLoading's cache: store in IndexedDB -->
```

```ts
// Image cache service (mirrors FFImageLoading's cache)
const imageCache = new Map<string, string>()  // URL -> blob URL

async function loadAndCacheImage(url: string): Promise<string> {
  if (imageCache.has(url)) return imageCache.get(url)!
  const resp = await fetch(url)
  const blob = await resp.blob()
  const blobUrl = URL.createObjectURL(blob)
  imageCache.set(url, blobUrl)
  return blobUrl
}
```

---

## 6. State management (replaces DependencyService + static models)

The C# app uses `DependencyService.Get<T>()` and static model classes like
`OnboardingModel`, `NavigationModel`, `KeysModel`. In Vue:

### 6.1 Pinia stores for shared state

```ts
// stores/onboarding.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useOnboardingStore = defineStore('onboarding', () => {
  const stage = ref<'onboarding' | 'enter-details' | 'questionnaire' | 'kyc' | 'finished'>('onboarding')
  const userRole = ref<string | null>(null)

  const currentShell = computed(() => {
    if (stage.value === 'finished') return 'authenticated'
    if (stage.value === 'onboarding') return 'onboarding'
    return 'no-account'
  })

  function setStage(s: typeof stage.value) {
    stage.value = s
  }

  function setUserRole(role: string) {
    userRole.value = role
  }

  return { stage, userRole, currentShell, setStage, setUserRole }
})
```

```ts
// stores/keys.ts — mirrors KeysModel
import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useKeysStore = defineStore('keys', () => {
  const hasSubstrateKey = ref(false)
  const substrateAddress = ref('')

  function setKey(address: string) {
    substrateAddress.value = address
    hasSubstrateKey.value = true
  }

  function clearKey() {
    substrateAddress.value = ''
    hasSubstrateKey.value = false
  }

  return { hasSubstrateKey, substrateAddress, setKey, clearKey }
})
```

### 6.2 Computed properties (mirrors C# computed view-model properties)

C# app has properties like:

```csharp
public string FullName => User.FullName;
public string PropertyName => Metadata.PropertyName;
```

Vue equivalent:

```ts
const fullName = computed(() => user.value?.fullName ?? '')
const propertyName = computed(() => property.value?.metadata?.propertyName ?? '')
```

---

## 7. Accessibility & platform adaptation

### 7.1 Dark mode

The C# app uses `AppThemeBinding`. In Vue, use the OS preference + manual
override:

```ts
// composables/useTheme.ts
import { ref, watchEffect } from 'vue'

export const useTheme = () => {
  const preferred = ref<'light' | 'dark'>('light')

  // Detect OS preference
  const mq = window.matchMedia('(prefers-color-scheme: dark)')
  preferred.value = mq.matches ? 'dark' : 'light'

  mq.addEventListener('change', (e) => {
    preferred.value = e.matches ? 'dark' : 'light'
  })

  // Apply
  watchEffect(() => {
    document.documentElement.setAttribute('data-theme', preferred.value)
  })

  function toggle() {
    preferred.value = preferred.value === 'light' ? 'dark' : 'light'
  }

  return { preferred, toggle }
}
```

### 7.2 Accessibility

The C# app has limited accessibility. The Vue implementation can improve:

```vue
<!-- Add proper ARIA labels -->
<button aria-label="Open messaging" @click="openMessaging">
  <BellIcon />
</button>

<!-- Add content descriptions to images -->
<img :src="icon" :alt="label" role="img" />

<!-- Respect user font scaling preference -->
<!-- Remove text-size-adjust if accessibility is a priority -->
```

### 7.3 Platform considerations

| C# concern | Vue equivalent |
|---|---|
| `AndroidNotificationHelper` | `vite-plugin-pwa` for push notifications |
| iOS `PrivacyInfo.xcprivacy` | Same — privacy policy in about page |
| `OnPlatform` WinUI | CSS media queries + browser detection |
| Tizen / MacCatalyst | Not supported in web; skip |
| `HideSoftInputOnTapped` | `blur()` on tap outside, or `<form>` with `type="submit"` |

### 7.4 Keyboard handling

```vue
<template>
  <form @submit.prevent="handleSubmit">
    <input
      v-model="answer"
      placeholder="Enter your answer"
      @keydown.enter="handleSubmit"
    />
    <ElevatedButton type="submit">Continue</ElevatedButton>
  </form>
</template>
```

---

## 8. Component catalog mapping

Every C# component has a direct Vue equivalent:

| C# component (PlutoFramework) | Vue equivalent | Notes |
|---|---|---|
| `ClickableCard` | `<ClickableCard>` | Same: card + tap + shadow |
| `Card` | `<Card>` | Extends ClickableCard, adds default tap |
| `ElevatedButton` | `<ElevatedButton>` | Primary button, 48px, pill radius |
| `BasicGrayButton` | `<ElevatedButton>` with secondary class | Outline / secondary |
| `TopNavigationBar` | `<TopNavigationBar>` | Semi-transparent, 45px, back+title+extra |
| `TopNavigationStepperBar` | `<ProgressStepperBar>` | Step dots, back arrow |
| `FormInputView` | `<FormInput>` | Entry in Card, 40px, optional Max pill |
| `FormValueView` | `<FormValueView>` | 120px label + monospace value |
| `XcavateCell` | `<XcavateCell>` | 80px, title + value + arrow |
| `RiskWarningView` | `<RiskWarningBanner>` | Bold text, blue link |
| `PropertyThumbnailView` | `<PropertyThumbnail>` | Image, location, APY, shares, price |
| `SliderView` | `<SliderBar>` | Gradient track, labels, optional thumb |
| `BottomPopupCard` | `<BottomPopupCard>` | Draggable bottom sheet |
| `FullPageLoadingView` | `<FullPageLoader>` | GIF overlay |
| `ExtrinsicStatusStackLayout` | `<ExtrinsicStatusToast>` | Toast notifications stack |
| `TransactionAnalyzerConfirmationView` | `<TransactionReviewPanel>` | Transaction review popup |
| `EnterPasswordPopupView` | `<PasswordPromptModal>` | Password entry dialog |
| `NetworkSelectorView` | `<NetworkSelector>` | Chain/network picker |
| `TransferView` | `<TransferPanel>` | Asset transfer dialog |
| `AssetSelectorView` | `<AssetSelector>` | Asset picker/input |
| `SumsubRejectedView` | `<KYCStatusBanner>` | KYC rejection banner |
| `TwoTabView`, `TabsView` | `<TabsView>` | Tabbed content |
| `NftImageView`, `NftThumbnailView` | `<NftImageView>`, `<NftThumbnail>` | NFT image display |
| `NftAttributeView` | `<AttributeRow>` | Label-value detail row |
| `NoDidPopupView` | `<NoDIDModal>` | DID not found dialog |
| `DAppConnectionView` | `<DAppConnectionPanel>` | Wallet connection request |
| `SearchBarView` | `<SearchBar>` | Search input |
| `StakingDashboardView` | `<StakingDashboard>` | Staking stats |
| `ReferendaView` | `<ReferendaCard>` | Governance item |
| `EventItemView` | `<EventItem>` | Blockchain event row |
| `KeyView` | `<KeyCard>` | Key management item |
| `AddressView` | `<AddressDisplay>` | Wallet address + copy |
| `PropertyTitleWithInfoView` | `<PropertyTitleInfo>` | Title + ⓘ info button |
| `ProgressBar` (MAUI) | `<ProgressStepper>` | Step indicator |
| `SwipeRefresh` | `<SwipeRefresh>` | Pull-to-refresh |

---

## 9. Anti-patterns and improvements over C# app

The Vue implementation can fix the anti-patterns observed in the C# app:

### 9.1 No hardcoded dimensions → fluid sizing

```css
/* C# uses fixed 80px, 200px etc. Vue uses fluid + clamp */
.cell-content {
  min-height: 80px;
  height: clamp(60px, 12vh, 100px);  /* Adapts to screen */
}
```

### 9.2 Structured logging → Winston / pino

```ts
// Instead of Console.WriteLine(ex)
import pino from 'pino'
const logger = pino({ level: 'info' })
try { await fetchData() } catch (err) {
  logger.error({ error: err }, 'Failed to load property')
  // Also surface to user via toast store
  toastStore.error('Failed to load property data')
}
```

### 9.3 Loading states on inputs → Skeleton on form fields

```vue
<!-- Instead of no loading state on inputs -->
<Transition name="fade">
  <SkeletonField v-if="loadingField" class="skeleton-input" />
  <FormInput v-else v-model="value" />
</Transition>
```

### 9.4 Empty-state illustrations → SVG illustrations

```vue
<!-- Instead of plain text -->
<EmptyStateView>
  <template #illustration>
    <svg><!-- Property illustration --></svg>
  </template>
  <p>Your properties will appear here</p>
</EmptyStateView>
```

### 9.5 Accessibility → proper ARIA + font scaling

```css
/* Allow user font scaling (unlike C# where it's disabled) */
:root {
  text-size-adjust: auto;
  -webkit-text-size-adjust: auto;
}
```

---

## 10. Summary: one-to-one mapping

Every practice from the C# app has a Vue equivalent:

1. **Token-driven design** → CSS custom properties on `:root` / `[data-theme="dark"]`
2. **Control-templated pages** → `<PageTemplate>` with named slots
3. **Card-based composition** → `<ClickableCard>` wrapper on every component
4. **Overlay popups** → `<Teleport to="body">` children with Z-index stacking
5. **MVVM** → Pinia stores + Composition API `<script setup>`
6. **AppThemeBinding** → `data-theme="dark"` on `html` element
7. **Custom font** → `@font-face` + `font-family` in CSS
8. **Pull-to-refresh** → `<SwipeRefresh>` wrapper
9. **Infinite scroll** → scroll threshold check (280px, same as C#)
10. **Bottom-sheet interaction** → `<BottomPopupCard>` with touch pan
11. **Transaction toasts** → Pinia toast store + stacked notifications
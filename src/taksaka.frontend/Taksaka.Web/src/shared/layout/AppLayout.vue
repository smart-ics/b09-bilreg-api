<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterLink, RouterView } from 'vue-router'
import { useAppStore } from '../../app/store'
import { connectOperationsHub } from '../api/signalrClient'

const appStore = useAppStore()

const navItems = [
  { label: 'Dashboard', to: '/', icon: 'pi pi-home' },
  { label: 'Queue', to: '/queue', icon: 'pi pi-list' },
  { label: 'Workers', to: '/workers', icon: 'pi pi-cog' },
  { label: 'Scheduler', to: '/scheduler', icon: 'pi pi-calendar' },
  { label: 'Alerts', to: '/alerts', icon: 'pi pi-bell' },
  { label: 'Health', to: '/health', icon: 'pi pi-heart' },
  { label: 'Settings', to: '/settings', icon: 'pi pi-sliders-h' }
]

onMounted(async () => {
  try {
    await connectOperationsHub()
  } catch (error) {
    console.warn('[SignalR] Connection failed (backend may be offline)', error)
  }
})
</script>

<template>
  <div class="layout">
    <aside class="sidebar">
      <h1 class="brand">{{ appStore.title }}</h1>
      <nav>
        <RouterLink
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="nav-link"
        >
          <i :class="item.icon" />
          <span>{{ item.label }}</span>
        </RouterLink>
      </nav>
    </aside>
    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.layout {
  display: flex;
  min-height: 100vh;
}

.sidebar {
  width: 240px;
  background: #1e293b;
  color: #f8fafc;
  padding: 1.5rem 1rem;
}

.brand {
  font-size: 1.1rem;
  margin: 0 0 1.5rem;
  font-weight: 600;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.65rem 0.75rem;
  color: #cbd5e1;
  text-decoration: none;
  border-radius: 0.375rem;
  margin-bottom: 0.25rem;
}

.nav-link:hover,
.nav-link.router-link-active {
  background: #334155;
  color: #fff;
}

.content {
  flex: 1;
  padding: 2rem;
  background: #f8fafc;
}
</style>

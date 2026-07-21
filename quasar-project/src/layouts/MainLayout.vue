<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated class="bg-primary text-white">
      <q-toolbar>
        <q-icon name="hub" size="28px" class="q-mr-sm" />
        <q-toolbar-title>
          <span class="text-weight-bold">WCS Twin</span>
          <span class="text-caption q-ml-sm text-blue-2">실시간 모니터링</span>
        </q-toolbar-title>
      </q-toolbar>

      <q-tabs align="left" class="bg-primary">
        <q-route-tab to="/" label="대시보드" exact />
        <q-route-tab to="/simulation" label="라인뷰" exact />
      </q-tabs>
    </q-header>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup>
</script>
// Configuration for your app
// https://v2.quasar.dev/quasar-cli-vite/quasar-config-file

import { defineConfig } from '#q-app'

export default defineConfig((ctx) => {
  return {
    // app boot file (/src/boot)
    boot: ['axios'],

    // https://v2.quasar.dev/quasar-cli-vite/quasar-config-file#css
    css: ['app.scss'],

    extras: [
      'roboto-font',
      'material-icons'
    ],

    build: {
      target: {
        browser: 'baseline-widely-available'
      },

      // 예전 별칭(layouts/, pages/, boot/ 등)을 살려서 계속 쓰기 위한 설정
      alias: {
        src: ctx.appPaths.srcDir,
        app: ctx.appPaths.appDir,
        components: ctx.appPaths.resolve.src('components'),
        layouts: ctx.appPaths.resolve.src('layouts'),
        pages: ctx.appPaths.resolve.src('pages'),
        assets: ctx.appPaths.resolve.src('assets'),
        boot: ctx.appPaths.resolve.src('boot'),
        stores: ctx.appPaths.resolve.src('stores')
      },

      vueRouterMode: 'hash'
    },

    devServer: {
      port: 8000,
      open: true
    },

    framework: {
      config: {},
      plugins: []
    },

    animations: []
  }
})
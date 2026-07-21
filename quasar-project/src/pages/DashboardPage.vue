<template>
  <q-page class="dashboard-page">
    <!-- 상단 요약 카드 -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-4">
        <q-card flat bordered class="stat-card">
          <q-card-section>
            <div class="stat-label">전체 대차</div>
            <div class="stat-value">{{ cartList.length }}</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-4">
        <q-card flat bordered class="stat-card">
          <q-card-section>
            <div class="stat-label">최근 검사 OK율</div>
            <div class="stat-value text-positive">{{ okRate }}%</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-4">
        <q-card flat bordered class="stat-card">
          <q-card-section>
            <div class="stat-label">최근 이력 건수</div>
            <div class="stat-value">{{ statusHist.length }}</div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- 탭 -->
    <q-card flat bordered>
      <q-tabs
        v-model="activeTab"
        dense
        class="text-grey-8"
        active-color="primary"
        indicator-color="primary"
        align="left"
      >
        <q-tab name="carts" label="대차 상태" />
        <q-tab name="hist" label="설비 상태 이력" />
        <q-tab name="vision" label="비전 검사결과" />
      </q-tabs>

      <q-separator />

      <q-tab-panels v-model="activeTab" animated>
        <!-- 대차 상태 -->
        <q-tab-panel name="carts" class="q-pa-none">
          <q-table
            :rows="cartList"
            :columns="cartColumns"
            row-key="cartId"
            flat
            :rows-per-page-options="[10, 20, 0]"
          >
            <template v-slot:body-cell-cartId="props">
              <q-td :props="props">
                <span class="text-weight-bold">{{ props.value }}</span>
              </q-td>
            </template>
            <template v-slot:body-cell-latestStatus="props">
              <q-td :props="props">
                <q-chip :color="statusColor(props.value)" text-color="white" dense square>
                  {{ props.value ?? '-' }}
                </q-chip>
              </q-td>
            </template>
            <template v-slot:body-cell-latestStatusDttm="props">
              <q-td :props="props">{{ formatTime(props.value) }}</q-td>
            </template>
          </q-table>
        </q-tab-panel>

        <!-- 설비 상태 이력 -->
        <q-tab-panel name="hist" class="q-pa-none">
          <q-table
            :rows="statusHist"
            :columns="statusHistColumns"
            row-key="histId"
            flat
            :rows-per-page-options="[10, 20, 0]"
          >
            <template v-slot:body-cell-createDttm="props">
              <q-td :props="props">{{ formatTime(props.value) }}</q-td>
            </template>
            <template v-slot:body-cell-status="props">
              <q-td :props="props">
                <q-chip :color="statusColor(props.value)" text-color="white" dense square>
                  {{ props.value }}
                </q-chip>
              </q-td>
            </template>
          </q-table>
        </q-tab-panel>

        <!-- 비전 검사결과 -->
        <q-tab-panel name="vision" class="q-pa-none">
          <q-table
            :rows="visionResults"
            :columns="visionColumns"
            row-key="resultId"
            flat
            :rows-per-page-options="[10, 20, 0]"
          >
            <template v-slot:body-cell-inspectDttm="props">
              <q-td :props="props">{{ formatTime(props.value) }}</q-td>
            </template>
            <template v-slot:body-cell-overallResult="props">
              <q-td :props="props">
                <q-chip
                  :color="props.value === 'OK' ? 'positive' : 'negative'"
                  text-color="white"
                  dense
                  square
                  :icon="props.value === 'OK' ? 'check' : 'close'"
                >
                  {{ props.value }}
                </q-chip>
              </q-td>
            </template>
          </q-table>
        </q-tab-panel>
      </q-tab-panels>
    </q-card>
  </q-page>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { api } from 'boot/axios'

const activeTab = ref('carts')

const cartList = ref([])
const statusHist = ref([])
const visionResults = ref([])

const cartColumns = [
  { name: 'cartId', label: '대차ID', field: 'cartId', align: 'left' },
  { name: 'lineType', label: '라인타입', field: 'lineType', align: 'left' },
  { name: 'latestStatusType', label: '최근 상태유형', field: 'latestStatusType', align: 'left' },
  { name: 'latestStatus', label: '상태', field: 'latestStatus', align: 'left' },
  { name: 'latestStatusDttm', label: '최근 갱신시각', field: 'latestStatusDttm', align: 'left' }
]

const statusHistColumns = [
  { name: 'createDttm', label: '시각', field: 'createDttm', align: 'left' },
  { name: 'eqpId', label: '설비ID', field: 'eqpId', align: 'left' },
  { name: 'statusType', label: '상태유형', field: 'statusType', align: 'left' },
  { name: 'cartId', label: '대차ID', field: 'cartId', align: 'left' },
  { name: 'status', label: '상태', field: 'status', align: 'left' }
]

const visionColumns = [
  { name: 'inspectDttm', label: '검사시각', field: 'inspectDttm', align: 'left' },
  { name: 'cartId', label: '대차ID', field: 'cartId', align: 'left' },
  { name: 'overallResult', label: '판정', field: 'overallResult', align: 'left' }
]

// ISO 문자열(2026-07-20T14:33:23)을 시:분:초만 보기 좋게
function formatTime (isoString) {
  if (!isoString) return '-'
  const [datePart, timePart] = isoString.split('T')
  if (!timePart) return isoString
  return `${datePart.slice(5)} ${timePart.slice(0, 8)}` // 07-20 14:33:23
}

function statusColor (status) {
  if (status === 'COMPLETED') return 'positive'
  if (status === 'FAILED') return 'negative'
  if (status === 'REQUESTED') return 'warning'
  return 'grey-6'
}

const okRate = computed(() => {
  if (visionResults.value.length === 0) return 0
  const okCount = visionResults.value.filter(v => v.overallResult === 'OK').length
  return Math.round((okCount / visionResults.value.length) * 100)
})

async function fetchAll () {
  try {
    const [carts, hist, vision] = await Promise.all([
      api.get('/api/dashboard/carts'),
      api.get('/api/dashboard/status-hist', { params: { limit: 20 } }),
      api.get('/api/dashboard/vision-results', { params: { limit: 20 } })
    ])
    cartList.value = carts.data
    statusHist.value = hist.data
    visionResults.value = vision.data
  } catch (err) {
    console.error('대시보드 데이터 조회 실패', err)
  }
}

let pollingTimer = null

onMounted(() => {
  fetchAll()
  pollingTimer = setInterval(fetchAll, 3000)
})

onUnmounted(() => {
  if (pollingTimer) clearInterval(pollingTimer)
})
</script>

<style scoped>
.dashboard-page {
  padding: 24px;
  background: #f5f7fa;
}

.stat-card {
  background: white;
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #1f2937;
} 
</style>
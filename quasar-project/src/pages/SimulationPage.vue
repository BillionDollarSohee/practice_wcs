<template>
  <q-page class="sim-page">
    <div class="text-h6 q-mb-md">실시간 라인뷰</div>

    <q-card flat bordered class="q-pa-lg">
      <div class="track-wrap">
        <!-- 입고/출고/타공장은 라인 공용(라인 배정 전/후) -->
        <div class="zone-box" :style="{ left: '6%', top: COORD.REQ.y + 'px', width: '90px' }">
          <div class="zone-label">입고</div>
        </div>
        <div class="zone-box" :style="{ left: '72%', top: COORD.DISCHARGE.y + 'px', width: '90px' }">
          <div class="zone-label">출고</div>
        </div>
        <div class="zone-box" :style="{ left: '90%', top: COORD.OFFSITE.y + 'px', width: '70px' }">
          <div class="zone-label">타공장</div>
        </div>

        <!-- 트림 / 파이널 / 도어 - 라인별 대기줄 + 검사대(암막, 1자리) -->
        <template v-for="line in LINE_ORDER" :key="line">
          <div class="zone-box zone-queue" :style="{ left: '27%', top: ROW_Y[line] + 'px', width: '70px' }">
            <div class="zone-label">{{ LINE_LABELS[line] }} 대기줄</div>
          </div>
          <div class="zone-box" :style="{ left: '48%', top: ROW_Y[line] + 'px', width: '90px' }">
            <div class="zone-label">{{ LINE_LABELS[line] }} 검사대</div>
          </div>
        </template>

        <!-- 출고 -> 타공장 -> 입고 순환을 나타내는 점선 (대차가 이 선을 타고 움직이진 않음, 새 대차 진입을 의미) -->
        <div class="loop-hint">
          <q-icon name="autorenew" size="16px" /> 출고된 대차는 타공장에서 쿨다운 후 새 사이클로 재입고
        </div>

        <!-- 대차 아이콘 -->
        <div
          v-for="cart in positionedCarts"
          :key="cart.cartId"
          class="cart-group"
          :class="{ 'no-anim': cart.noAnim }"
          :style="{ left: cart.x + '%', top: cart.y + 'px' }"
        >
          <div class="cart-chip" :class="[cartColorClass(cart), { 'cart-chip--alarm': cart.isAlarm }]">
            {{ cart.cartId.replace('CART_', '') }}
          </div>
          <div v-if="cart.phaseLabel" class="cart-source-label">{{ cart.phaseLabel }}</div>
          <q-btn
            v-if="cart.isAlarm"
            size="sm"
            dense
            color="negative"
            label="알람 종료"
            class="q-mt-xs"
            @click="clearAlarm(cart.cartId)"
          />
        </div>
      </div>

      <!-- 범례 -->
      <div class="row q-gutter-md q-mt-md justify-center">
        <div class="row items-center q-gutter-xs">
          <div class="legend-dot bg-warning" /><span class="text-caption">요청중</span>
        </div>
        <div class="row items-center q-gutter-xs">
          <div class="legend-dot bg-positive" /><span class="text-caption">정상완료(OK)</span>
        </div>
        <div class="row items-center q-gutter-xs">
          <div class="legend-dot bg-negative" /><span class="text-caption">NG / 실패</span>
        </div>
        <div class="row items-center q-gutter-xs">
          <div class="legend-dot bg-grey-6" /><span class="text-caption">진행중</span>
        </div>
      </div>
    </q-card>

    <!-- 최근 이벤트 로그 -->
    <q-card flat bordered class="q-mt-lg">
      <q-card-section>
        <div class="text-subtitle2 q-mb-sm">최근 이벤트</div>
        <div class="log-list">
          <div v-for="item in recentLog" :key="item.histId" class="log-row">
            <span class="text-grey-7">{{ formatTime(item.createDttm) }}</span>
            <q-chip dense square :color="logColor(item.status)" text-color="white" class="q-mx-sm">
              {{ item.statusType }}
            </q-chip>
            <span class="text-weight-medium">{{ item.cartId }}</span>
            <span class="text-grey-6 q-ml-sm">{{ item.status }}</span>
          </div>
        </div>
      </q-card-section>
    </q-card>
  </q-page>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { api } from 'boot/axios'

const cartList = ref([])
const statusHist = ref([])
const visionResults = ref([])

// 상태유형 -> 스테이지 키 매핑
// 이제 5단계: REQ(입고) -> QUEUE(대기줄, 아무 작업 안 함) -> INSPECT(검사대, 실제 작업)
//           -> DISCHARGE(출고) -> OFFSITE(타공장, 쿨다운 - 트레일러로 나가서 다른 공장에서 쓰이는 중)
//           -> (쿨다운 끝나면 다시 REQ로)
//
// 중요: RFID Read/Write는 이제 실패해도 곧바로 게이트를 반납하지 않고 재시도하다가,
// 그래도 안되면 사람이 알람을 해제할 때까지 그 자리를 계속 점유한 채 대기한다(RFID_ALARM).
// 즉 FAILED/RETRYING/ALARM 전부 "아직 그 대차가 검사대를 물리적으로 차지하고 있다"는 뜻이라
// 전부 INSPECT로 분류해야 한다. 게이트가 진짜 반납되는 건 성공하거나(COMPLETED) 관리자가
// 알람을 해제해서 수동 처리(MANUAL_OVERRIDE)로 넘어갈 때뿐이고, 그 이후엔 DISCHARGED가 찍힌다.
function resolveStageKey (statusType, status) {
  if (statusType === 'CART_OFFSITE') return 'OFFSITE'
  if (statusType === 'DISCHARGED') return 'DISCHARGE'
  if (statusType === 'INSPECTION_QUEUE') return 'QUEUE'
  if (
    statusType === 'RFID_READ_REQ' ||
    statusType === 'VISION_INSPECTING' ||
    statusType === 'RFID_WRITE_REQ' ||
    statusType === 'RFID_ALARM' ||
    statusType === 'VISION_PRODUCT_COMPLETE'
  ) return 'INSPECT'
  return 'REQ' // VISION_CART_INFO_REQ 또는 알 수 없는 값은 입고로 취급
}

// 지금 이 순간 알람이 울리고 있는 상태인지 (관리자 해제 대기중)
function isAlarmActive (cart) {
  return cart.latestStatusType === 'RFID_ALARM' && cart.latestStatus === 'ACTIVE'
}

// 검사대(INSPECT) 안에서의 세부 단계 라벨
// RFID 읽기요청 -> (재시도중/실패) -> 대차감지 -> 비전검사진행 -> 결과OK/결과NG -> RFID쓰기요청 -> (재시도중/실패) -> 쓰기완료 -> (알람이면 그 위에 알람으로 덮임)
function inspectSubLabel (cart) {
  const { latestStatusType: t, latestStatus: s } = cart

  if (t === 'RFID_ALARM' && s === 'ACTIVE') return '알람'
  if (t === 'RFID_READ_REQ') {
    if (s === 'REQUESTED') return 'RFID 읽기요청'
    if (s === 'RETRYING') return 'RFID 읽기 재시도중'
    if (s === 'FAILED') return 'RFID 읽기 실패'
    if (s === 'MANUAL_OVERRIDE') return '관리자 수동처리(읽기)'
    return '대차감지' // COMPLETED
  }
  if (t === 'VISION_INSPECTING') return '비전검사 진행중'
  if (t === 'VISION_PRODUCT_COMPLETE') {
    const visionResult = latestVisionByCart.value[cart.cartId]?.overallResult
    return visionResult === 'NG' ? '결과 NG' : '결과 OK'
  }
  if (t === 'RFID_WRITE_REQ') {
    if (s === 'REQUESTED') return 'RFID 쓰기요청'
    if (s === 'RETRYING') return 'RFID 쓰기 재시도중'
    if (s === 'FAILED') return 'RFID 쓰기 실패'
    if (s === 'MANUAL_OVERRIDE') return '관리자 수동처리(쓰기)'
    return '쓰기완료' // COMPLETED
  }
  return t
}

// 스테이지 진행 순서 (역행 감지용) - REQ(0) < QUEUE(1) < INSPECT(2) < DISCHARGE(3) < OFFSITE(4)
// OFFSITE 다음엔 다시 REQ(0)로 돌아가는데, 그게 바로 "역행"으로 감지되어 순간이동 처리된다 (새 사이클 시작이므로 의도된 동작)
function stageRank (stageKey) {
  if (stageKey === 'QUEUE') return 1
  if (stageKey === 'INSPECT') return 2
  if (stageKey === 'DISCHARGE') return 3
  if (stageKey === 'OFFSITE') return 4
  return 0
}

// 이전 폴링 시점의 스테이지 순위를 기억해서, "역행(=새 사이클 재시작)"을 감지하는 용도
// (반응형일 필요 없음 - 매 폴링마다 갱신되는 순수 기록용 객체)
const stageRankMemory = {}

// 최신 비전 판정을 대차ID 기준으로 빠르게 찾기 위한 맵
const latestVisionByCart = computed(() => {
  const map = {}
  for (const v of visionResults.value) {
    if (!map[v.cartId] || v.inspectDttm > map[v.cartId].inspectDttm) {
      map[v.cartId] = v
    }
  }
  return map
})

// 대차 하나의 "실제 유효한" 스테이지를 계산
// 비전 NG판정은 백엔드가 이미 슬롯을 반납한 상태라(재작업은 검사대 밖에서 진행)
// 검사 스테이션이 아니라 입고(재입고 대기) 쪽으로 분류해야 대기줄 개수가 실제와 맞는다.
// (RFID 실패는 이제 재시도/알람으로 처리되며 슬롯을 계속 점유하므로 여기 해당 안 됨)
function computeStageKey (cart) {
  let stageKey = resolveStageKey(cart.latestStatusType, cart.latestStatus)

  if (stageKey === 'INSPECT' && cart.latestStatusType === 'VISION_PRODUCT_COMPLETE') {
    const visionResult = latestVisionByCart.value[cart.cartId]?.overallResult
    if (visionResult === 'NG') {
      stageKey = 'REQ'
    }
  }

  return stageKey
}

// 비전 라인 3개(트림/파이널/도어) - CART_MASTER.LINE_TYPE 값과 동일한 코드
const LINE_ORDER = ['TR', 'FL', 'DR']
const LINE_LABELS = { TR: '트림', FL: '파이널', DR: '도어' }

// 라인별 행(row)의 세로 시작 위치(px) - 위에서부터 트림/파이널/도어 순서로 배치
const ROW_Y = { TR: 10, FL: 140, DR: 270 }

// 정거장 좌표 (x: %, y: px) - 입고/출고/타공장은 라인 배정 전/후라 라인 공용 1칸
const COORD = {
  REQ: { x: 12, y: 185 },
  DISCHARGE: { x: 78, y: 185 },
  OFFSITE: { x: 95, y: 185 },
  QUEUE_X: 33,
  QUEUE_Y_GAP: 40,
  BAY_X: 55
}

const positionedCarts = computed(() => {
  // 라인별로 검사대(INSPECT)/대기줄(QUEUE) 대차를 따로 뽑아서 대차ID 순으로 정렬해둔다.
  // 백엔드 게이트가 이제 라인별로 독립이라, 화면도 라인별로 줄을 세워야 인덱스가 맞는다.
  const inspectCartsByLine = {}
  const queueCartsByLine = {}
  for (const line of LINE_ORDER) {
    inspectCartsByLine[line] = cartList.value
      .filter((c) => c.lineType === line && computeStageKey(c) === 'INSPECT')
      .sort((a, b) => a.cartId.localeCompare(b.cartId))
    queueCartsByLine[line] = cartList.value
      .filter((c) => c.lineType === line && computeStageKey(c) === 'QUEUE')
      .sort((a, b) => a.cartId.localeCompare(b.cartId))
  }

  return cartList.value.map((cart) => {
    const stageKey = computeStageKey(cart)
    // CART_MASTER.LINE_TYPE에 없는 값이 들어오면(데이터 오류) 파이널 행에 임시로 붙여서 눈에 띄게 한다.
    const line = LINE_ORDER.includes(cart.lineType) ? cart.lineType : 'FL'
    const rowY = ROW_Y[line]
    const lineLabel = LINE_LABELS[line]
    let x, y, bayLabel = ''

    if (stageKey === 'REQ') {
      x = COORD.REQ.x
      y = COORD.REQ.y
    } else if (stageKey === 'DISCHARGE') {
      x = COORD.DISCHARGE.x
      y = COORD.DISCHARGE.y
    } else if (stageKey === 'OFFSITE') {
      x = COORD.OFFSITE.x
      y = COORD.OFFSITE.y
    } else if (stageKey === 'QUEUE') {
      const queueCarts = queueCartsByLine[line]
      const queueIndex = queueCarts.findIndex((c) => c.cartId === cart.cartId)
      x = COORD.QUEUE_X
      y = rowY + queueIndex * COORD.QUEUE_Y_GAP
      bayLabel = `${lineLabel} 대기중`
    } else {
      // INSPECT(검사대) - 라인별로 물리적 1자리(암막)뿐이라 항상 같은 위치에 배정한다.
      // 정상이라면 이 시점에 같은 라인의 inspectCarts는 항상 0~1대여야 한다.
      // 혹시라도 2대 이상 잡히면(=백엔드 동시성 버그) 숨기지 않고 살짝 어긋나게 겹쳐 그려서
      // "겹침"이 눈에 바로 보이도록 한다.
      const inspectCarts = inspectCartsByLine[line]
      const inspectIndex = inspectCarts.findIndex((c) => c.cartId === cart.cartId)
      x = COORD.BAY_X
      y = rowY + inspectIndex * 10
      bayLabel = inspectCarts.length > 1
        ? `${lineLabel} 검사대 (겹침 ${inspectIndex + 1}/${inspectCarts.length})`
        : `${lineLabel} 검사대`
    }

    // 역행(스테이지 순위가 이전보다 낮아짐) 감지 - 감지되면 이번 이동은 애니메이션 없이 순간이동
    const rank = stageRank(stageKey)
    const prevRank = stageRankMemory[cart.cartId]
    const noAnim = prevRank !== undefined && rank < prevRank
    stageRankMemory[cart.cartId] = rank

    // 세부 단계 라벨 - RFID 읽기요청 -> 대차감지 -> 비전검사진행 -> 결과OK/NG -> RFID쓰기요청 -> 쓰기완료 -> 출고
    let phaseLabel = bayLabel
    if (stageKey === 'INSPECT') {
      phaseLabel += ' · ' + inspectSubLabel(cart)
    } else if (stageKey === 'DISCHARGE') {
      phaseLabel = cart.latestStatus === 'FAILED' ? '기록실패' : '출고완료'
    } else if (stageKey === 'OFFSITE') {
      phaseLabel = '타공장 · 재입고 대기'
    } else if (stageKey === 'REQ' && cart.latestStatusType === 'VISION_PRODUCT_COMPLETE') {
      phaseLabel = '비전 NG · 재입고 대기'
    }

    return {
      cartId: cart.cartId,
      status: cart.latestStatus,
      x,
      y,
      noAnim,
      phaseLabel,
      isAlarm: isAlarmActive(cart),
      visionResult: latestVisionByCart.value[cart.cartId]?.overallResult
    }
  })
})

function cartColorClass (cart) {
  if (cart.status === 'FAILED') return 'bg-negative'
  if (cart.visionResult === 'NG') return 'bg-negative'
  if (cart.status === 'COMPLETED') return 'bg-positive'
  if (cart.status === 'REQUESTED') return 'bg-warning'
  return 'bg-grey-6'
}

function logColor (status) {
  if (status === 'COMPLETED') return 'positive'
  if (status === 'FAILED') return 'negative'
  if (status === 'REQUESTED') return 'warning'
  return 'grey-6'
}

function formatTime (isoString) {
  if (!isoString) return '-'
  const [, timePart] = isoString.split('T')
  return timePart ? timePart.slice(0, 8) : isoString
}

const recentLog = computed(() => statusHist.value.slice(0, 8))

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
    console.error('라인뷰 데이터 조회 실패', err)
  }
}

// 알람 종료 버튼 - eis-socket-service(C#)는 이 요청을 직접 받지 않고,
// DB에 "CLEARED" 기록을 남기면 C# 쪽이 주기적으로 폴링해서 감지하고 재시도 대기를 풀어준다.
const clearingCartIds = ref(new Set())

async function clearAlarm (cartId) {
  if (clearingCartIds.value.has(cartId)) return
  clearingCartIds.value.add(cartId)
  try {
    await api.post('/api/dashboard/alarm/clear', null, { params: { cartId } })
    await fetchAll()
  } catch (err) {
    console.error('알람 해제 실패', err)
  } finally {
    clearingCartIds.value.delete(cartId)
  }
}

let pollingTimer = null

onMounted(() => {
  fetchAll()
  pollingTimer = setInterval(fetchAll, 1500)
})

onUnmounted(() => {
  if (pollingTimer) clearInterval(pollingTimer)
})
</script>

<style scoped>
.sim-page {
  padding: 24px;
  background: #f5f7fa;
}

.track-wrap {
  position: relative;
  height: 400px;
  margin: 30px 20px 10px;
}

.zone-box {
  position: absolute;
  height: 90px;
  border: 1px dashed #cbd5e1;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.5);
}

.zone-queue {
  border-style: dotted;
}

.zone-label {
  position: absolute;
  top: -20px;
  left: 0;
  font-size: 12px;
  color: #6b7280;
  font-weight: 600;
  white-space: nowrap;
}

.loop-hint {
  position: absolute;
  bottom: -20px;
  left: 6%;
  font-size: 11px;
  color: #9ca3af;
}

.cart-group {
  position: absolute;
  transform: translateX(-50%);
  text-align: center;
  transition: left 1.2s ease, top 0.6s ease;
}

/* 역행(새 사이클 재시작) 시에는 애니메이션 없이 순간이동 */
.cart-group.no-anim {
  transition: none;
}

.cart-chip {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 11px;
  font-weight: 600;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
  transition: background-color 0.6s ease;
  margin: 0 auto;
}

.cart-chip--alarm {
  animation: alarm-blink 1s infinite;
}

@keyframes alarm-blink {
  0%, 100% { box-shadow: 0 0 0 0 rgba(198, 40, 40, 0.7); }
  50% { box-shadow: 0 0 0 8px rgba(198, 40, 40, 0); }
}

.cart-source-label {
  margin-top: 2px;
  font-size: 10px;
  color: #6b7280;
  font-weight: 600;
  white-space: nowrap;
}

.legend-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}

.log-list {
  max-height: 240px;
  overflow-y: auto;
}

.log-row {
  padding: 4px 0;
  font-size: 13px;
  border-bottom: 1px solid #f0f0f0;
}
</style>
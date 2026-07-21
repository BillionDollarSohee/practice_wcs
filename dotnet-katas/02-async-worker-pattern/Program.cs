// ============================================================
// Kata 2 : ConcurrentQueue + Fixed Worker Task Pattern
// ------------------------------------------------------------
// 실무 코드의 VisionImageMigrationService.SchedulerVisionImageMigration / ProcessWorker
// 가 쓰는 패턴을 그대로 축소한 연습입니다.
//
// 실무 코드가 하던 일: "옮겨야 할 파일 목록"을 큐에 담고, WorkerCount개의 Task를
// Task.Run으로 동시에 띄워서 각자 큐에서 하나씩 꺼내 처리하게 하고,
// 전부 끝날 때까지 Task.WhenAll(...).GetAwaiter().GetResult()로 기다렸습니다.
//
// 이 kata에서는 "파일 복사" 대신 "가짜 작업(Task.Delay)"으로 흉내를 냅니다.
//
// 목표: 1~30번 작업 아이템을 큐에 넣고, WORKER_COUNT개의 워커가 병렬로
//       각자 큐에서 하나씩 꺼내 처리하다가, 3초 뒤 자동으로 취소 신호가 오면
//       하던 것만 마무리하고 깔끔하게 멈추는 프로그램을 완성하세요.
//
// 완성 조건 (Acceptance Criteria):
//   1) WORKER_COUNT(예:3)개의 워커가 "동시에" 돌면서 큐를 나눠 처리한다
//      (한 워커가 순서대로 다 처리하는 게 아니라, 로그의 Worker 번호가 뒤섞여 찍혀야 함).
//   2) 취소 토큰이 신호를 보내면, 워커들은 현재 처리 중이던 항목만 마무리하고
//      큐에 남은 게 있어도 더 이상 꺼내지 않고 루프를 빠져나온다.
//   3) 모든 워커가 끝난 뒤, "총 몇 개 처리했고 몇 개가 취소로 남았는지" 집계해서 출력한다.
//   4) Main에서 Task.WhenAll로 전체 워커 완료를 기다린다 (fire-and-forget 금지).
//
// 힌트: 실제 코드(VisionImageMigrationService.cs)의 ProcessWorker / SchedulerVisionImageMigration
//       구조를 참고하되, 절대 그 파일을 복사해서 붙여넣지 마세요 — 직접 짜보는 게 이 kata의 목적입니다.
// ============================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private const int TOTAL_ITEMS = 30;
    private const int WORKER_COUNT = 3;
    private const int FAKE_WORK_MS = 300; // 작업 1개당 걸리는 가짜 시간
    private const int CANCEL_AFTER_MS = 3000; // 이 시간 뒤 자동 취소

    // 처리 완료 개수를 여러 워커가 동시에 증가시키므로 스레드 안전한 카운터가 필요하다.
    // TODO 0: 힌트 - int 를 그냥 ++ 하면 동시성 문제가 생깁니다.
    //         System.Threading.Interlocked.Increment 를 검색해보세요.
    private static int _processedCount = 0;

    static async Task Main()
    {
        Console.WriteLine("=== Async Worker Pattern Kata ===");
        Console.WriteLine($"총 작업 {TOTAL_ITEMS}개, 워커 {WORKER_COUNT}개, {CANCEL_AFTER_MS}ms 후 자동 취소");
        Console.WriteLine();

        // TODO 1: 1부터 TOTAL_ITEMS까지의 정수를 담은 ConcurrentQueue<int> 를 만드세요.
        ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
        throw new NotImplementedException("TODO 1: 큐에 1~TOTAL_ITEMS 채우기");

        // TODO 2: CANCEL_AFTER_MS 뒤 자동으로 취소되는 CancellationTokenSource를 만드세요.
        //         (힌트: new CancellationTokenSource(TimeSpan) 생성자를 쓰면 Timer 없이도 자동취소가 됨)

        // TODO 3: WORKER_COUNT개의 Task를 Task.Run(() => WorkerLoop(...)) 으로 생성하세요.

        // TODO 4: Task.WhenAll로 전체 워커가 끝날 때까지 await 하세요.

        Console.WriteLine();
        Console.WriteLine($"처리 완료: {_processedCount} / {TOTAL_ITEMS}");
    }

    /// <summary>
    /// 워커 하나의 동작.
    /// 취소되지 않았고 큐에서 꺼낼 게 있는 동안 반복해서 처리한다.
    /// </summary>
    private static async Task WorkerLoop(int workerId, ConcurrentQueue<int> queue, CancellationToken token)
    {
        Console.WriteLine($"[Worker{workerId}] 시작");

        // TODO 5: while 루프 조건을 작성하세요.
        //   - token이 취소 요청되지 않았고
        //   - queue.TryDequeue로 항목을 하나 꺼낼 수 있는 동안
        //   반복해야 합니다. (VisionImageMigrationService.ProcessWorker의 while 조건과 비슷한 모양)

        // TODO 6: 루프 안에서
        //   1) 꺼낸 itemId를 로그로 출력 (예: "[Worker{workerId}] 처리 중: item {itemId}")
        //   2) await Task.Delay(FAKE_WORK_MS, token) 으로 "작업 시간"을 흉내
        //      (주의: token을 Task.Delay에 넘기면 취소 시 즉시 예외로 빠져나옵니다.
        //       그 예외(TaskCanceledException/OperationCanceledException)를 어떻게 처리할지 고민해보세요 -
        //       실무 코드처럼 "이 항목만 skip"하고 continue 할지, 루프를 바로 빠져나올지는 여러분이 결정하세요.)
        //   3) 처리 완료 시 _processedCount 증가 (TODO 0에서 고른 방법으로)

        Console.WriteLine($"[Worker{workerId}] 종료 (취소여부: {token.IsCancellationRequested})");
    }
}

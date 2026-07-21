package wcs.backend.wcs.controller;

import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import wcs.backend.wcs.service.DashboardService;
import wcs.backend.wcs.vo.CartStatusVo;
import wcs.backend.wcs.vo.EquipmentStatusHistVo;
import wcs.backend.wcs.vo.VisionResultVo;

import java.util.List;

@RestController
@RequestMapping("/api/dashboard")
@RequiredArgsConstructor
public class DashboardController {

    private final DashboardService dashboardService;

    /**
     * Select 대차 상태 목록 조회
     */
    @GetMapping("/carts")
    public List<CartStatusVo> getCartStatusList() {
        return dashboardService.getCartStatusList();
    }

    /**
     * Select 설비 상태 이력 최근 N건 조회
     */
    @GetMapping("/status-hist")
    public List<EquipmentStatusHistVo> getRecentStatusHist(
            @RequestParam(defaultValue = "20") int limit) {
        return dashboardService.getRecentStatusHist(limit);
    }

    /**
     * Select 비전 검사결과 최근 N건 조회
     */
    @GetMapping("/vision-results")
    public List<VisionResultVo> getRecentVisionResults(
            @RequestParam(defaultValue = "20") int limit) {
        return dashboardService.getRecentVisionResults(limit);
    }

    /**
     * RFID 알람 해제 - 관리자가 알람 종료 버튼을 눌렀을 때 호출
     */
    @PostMapping("/alarm/clear")
    public void clearAlarm(@RequestParam String cartId) {
        dashboardService.clearAlarm(cartId);
    }
}

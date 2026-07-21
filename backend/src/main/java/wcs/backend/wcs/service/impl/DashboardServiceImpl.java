package wcs.backend.wcs.service.impl;

import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import wcs.backend.wcs.mapper.DashboardMapper;
import wcs.backend.wcs.service.DashboardService;
import wcs.backend.wcs.vo.CartStatusVo;
import wcs.backend.wcs.vo.EquipmentStatusHistVo;
import wcs.backend.wcs.vo.VisionResultVo;

import java.util.List;

@Service
@RequiredArgsConstructor
public class DashboardServiceImpl implements DashboardService {

    private final DashboardMapper dashboardMapper;

    /**
     * 전체 대차 목록 + 최신 상태 요약 조회
     * @return List<CartStatusVo>
     */
    @Override
    public List<CartStatusVo> getCartStatusList() {
        return dashboardMapper.selectCartStatusList();
    }

    /**
     * 설비 상태 이력 최근 N건 조회
     * @param limit 조회 건수
     * @return List<EquipmentStatusHistVo>
     */
    @Override
    public List<EquipmentStatusHistVo> getRecentStatusHist(int limit) {
        return dashboardMapper.selectRecentStatusHist(limit);
    }

    /**
     * 비전 검사결과 최근 N건 조회
     * @param limit 조회 건수
     * @return List<VisionResultVo>
     */
    @Override
    public List<VisionResultVo> getRecentVisionResults(int limit) {
        return dashboardMapper.selectRecentVisionResults(limit);
    }

    /**
     * RFID 알람 해제 - 관리자가 수동으로 알람을 종료했음을 기록
     * @param cartId 대차 ID
     */
    @Override
    public void clearAlarm(String cartId) {
        dashboardMapper.insertAlarmClear(cartId);
    }
}

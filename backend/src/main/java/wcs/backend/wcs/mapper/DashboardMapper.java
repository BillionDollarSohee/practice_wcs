package wcs.backend.wcs.mapper;

import org.apache.ibatis.annotations.Param;
import wcs.backend.wcs.vo.CartStatusVo;
import wcs.backend.wcs.vo.EquipmentStatusHistVo;
import wcs.backend.wcs.vo.VisionResultVo;

import java.util.List;

public interface DashboardMapper {

    /**
     * 전체 대차 목록 + 최신 상태 요약 조회
     * @return List<CartStatusVo>
     */
    List<CartStatusVo> selectCartStatusList();

    /**
     * 설비 상태 이력 최근 N건 조회
     * @param limit 조회 건수
     * @return List<EquipmentStatusHistVo>
     */
    List<EquipmentStatusHistVo> selectRecentStatusHist(int limit);

    /**
     * 비전 검사결과 최근 N건 조회
     * @param limit 조회 건수
     * @return List<VisionResultVo>
     */
    List<VisionResultVo> selectRecentVisionResults(int limit);

    /**
     * RFID 알람 해제 - 관리자가 수동으로 알람을 종료했음을 기록
     * (eis-socket-service가 이 기록을 폴링해서 감지하고 재시도 대기를 풀어준다)
     * @param cartId 대차 ID
     */
    void insertAlarmClear(@Param("cartId") String cartId);
}
package wcs.backend.wcs.vo;

import lombok.Data;

import java.time.LocalDateTime;

@Data
public class CartStatusVo {
    private String cartId;
    private String cartBarcode;
    private String lineType;
    private LocalDateTime createDttm;

    // 최신 상태 요약
    private String latestStatusType;
    private String latestStatus;
    private LocalDateTime latestStatusDttm;
}

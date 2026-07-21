package wcs.backend.wcs.vo;

import lombok.Data;

import java.time.LocalDateTime;

@Data
public class VisionResultVo {
    private String resultId;
    private String cartId;
    private String overallResult;
    private LocalDateTime inspectDttm;
}

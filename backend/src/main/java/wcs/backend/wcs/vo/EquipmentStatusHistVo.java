package wcs.backend.wcs.vo;

import lombok.Data;

import java.time.LocalDateTime;

@Data
public class EquipmentStatusHistVo {

    private Long histId;
    private String eqpId;
    private String statusType;
    private String cartId;
    private String status;
    private String resultJson;
    private LocalDateTime createDttm;
}


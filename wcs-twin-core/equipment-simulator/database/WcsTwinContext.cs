using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database
{
    // wcs_twin 데이터베이스 연결용 DbContext
    // 모든 서비스(Vision, Modbus, RFID)가 이 프로젝트를 참조해서 직접 사용한다
    public class WcsTwinContext : DbContext
    {
        public DbSet<CartMaster> CartMasters { get; set; }
        public DbSet<EquipmentStatusHist> EquipmentStatusHists { get; set; }
        public DbSet<VisionResult> VisionResults { get; set; }

        // ↓ Kata 4 (DB 폴링/트랜잭션 연습) 전용. 실제 시뮬레이터 서비스들은 이 두 테이블을 쓰지 않습니다.
        public DbSet<KataRawOrder> KataRawOrders { get; set; }
        public DbSet<KataParsedOrder> KataParsedOrders { get; set; }

        public WcsTwinContext(DbContextOptions<WcsTwinContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CartMaster>(entity =>
            {
                entity.ToTable("CART_MASTER");
                entity.HasKey(e => e.CartId);
                entity.Property(e => e.CartId).HasColumnName("CART_ID");
                entity.Property(e => e.CartBarcode).HasColumnName("CART_BARCODE");
                entity.Property(e => e.LineType).HasColumnName("LINE_TYPE");
                entity.Property(e => e.CreateDttm).HasColumnName("CREATE_DTTM");
            });

            modelBuilder.Entity<EquipmentStatusHist>(entity =>
            {
                entity.ToTable("EQUIPMENT_STATUS_HIST");
                entity.HasKey(e => e.HistId);
                entity.Property(e => e.HistId).HasColumnName("HIST_ID");
                entity.Property(e => e.EqpId).HasColumnName("EQP_ID");
                entity.Property(e => e.StatusType).HasColumnName("STATUS_TYPE");
                entity.Property(e => e.CartId).HasColumnName("CART_ID");
                entity.Property(e => e.Status).HasColumnName("STATUS");
                entity.Property(e => e.ResultJson).HasColumnName("RESULT_JSON");
                entity.Property(e => e.CreateDttm).HasColumnName("CREATE_DTTM");
            });

            modelBuilder.Entity<VisionResult>(entity =>
            {
                entity.ToTable("VISION_RESULT");
                entity.HasKey(e => e.ResultId);
                entity.Property(e => e.ResultId).HasColumnName("RESULT_ID");
                entity.Property(e => e.CartId).HasColumnName("CART_ID");
                entity.Property(e => e.OverallResult).HasColumnName("OVERALL_RESULT");
                entity.Property(e => e.InspectDttm).HasColumnName("INSPECT_DTTM");
            });

            // ↓ Kata 4 (DB 폴링/트랜잭션 연습) 전용 매핑
            modelBuilder.Entity<KataRawOrder>(entity =>
            {
                entity.ToTable("KATA_RAW_ORDER");
                entity.HasKey(e => e.RawId);
                entity.Property(e => e.RawId).HasColumnName("RAW_ID");
                entity.Property(e => e.RawData).HasColumnName("RAW_DATA");
                entity.Property(e => e.ProcessStatus).HasColumnName("PROCESS_STATUS");
                entity.Property(e => e.ErrorMsg).HasColumnName("ERROR_MSG");
                entity.Property(e => e.CreateDttm).HasColumnName("CREATE_DTTM");
            });

            modelBuilder.Entity<KataParsedOrder>(entity =>
            {
                entity.ToTable("KATA_PARSED_ORDER");
                entity.HasKey(e => e.ParsedId);
                entity.Property(e => e.ParsedId).HasColumnName("PARSED_ID");
                entity.Property(e => e.RawId).HasColumnName("RAW_ID");
                entity.Property(e => e.PartCd).HasColumnName("PART_CD");
                entity.Property(e => e.Qty).HasColumnName("QTY");
                entity.Property(e => e.Location).HasColumnName("LOCATION");
                entity.Property(e => e.CreateDttm).HasColumnName("CREATE_DTTM");
            });
        }
    }
}
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
        }
    }
}
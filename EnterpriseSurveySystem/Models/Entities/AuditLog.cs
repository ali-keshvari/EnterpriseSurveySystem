using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseSurveySystem.Models.Entities
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        public AuditLog()
        {
            Timestamp = DateTime.Now;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? TokenId { get; set; }

        public int? QuestionnaireId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "نوع عملیات")]
        public string ActionType { get; set; }

        [Display(Name = "زمان ثبت دقیق")]
        public DateTime Timestamp { get; set; }

        [StringLength(50)]
        [Display(Name = "آدرس IP")]
        public string IPAddress { get; set; }

        [Display(Name = "جزئیات رویداد")]
        public string Details { get; set; }

        // Navigation Properties
        [ForeignKey("TokenId")]
        public virtual RespondentToken RespondentToken { get; set; }

        [NotMapped]
        public string ActionPersianTitle
        {
            get
            {
                switch (ActionType)
                {
                    case "PageOpened": return "ورود به پرسش‌نامه (شروع زمان)";
                    case "FormSubmitted": return "ثبت نهایی و ارسال پاسخ‌ها";
                    case "TokenExpired": return "انقضای لینک / پایان مهلت";
                    case "AccessDenied": return "تلاش غیرمجاز / ورود مسدود شده";
                    case "TokensGenerated": return "تولید لینک‌های مخاطبان هدف";
                    case "QuestionnaireCreated": return "ایجاد پرسش‌نامه جدید";
                    default: return ActionType;
                }
            }
        }
    }
}

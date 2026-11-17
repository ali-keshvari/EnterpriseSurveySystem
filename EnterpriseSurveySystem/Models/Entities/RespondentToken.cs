using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseSurveySystem.Models.Entities
{
    [Table("RespondentTokens")]
    public class RespondentToken
    {
        public RespondentToken()
        {
            IsUsed = false;
            Answers = new HashSet<Answer>();
            AuditLogs = new HashSet<AuditLog>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int QuestionnaireId { get; set; }

        [Required]
        [StringLength(64)]
        [Index("IX_RespondentTokens_TokenGuid", IsUnique = true)]
        public string TokenGuid { get; set; }

        [Display(Name = "استفاده شده")]
        public bool IsUsed { get; set; }

        [Display(Name = "زمان اولین ورود (بازگشایی)")]
        public DateTime? OpenedAt { get; set; }

        [Display(Name = "زمان تکمیل و ثبت نهایی")]
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "زمان انقضا")]
        public DateTime? ExpiresAt { get; set; }

        [StringLength(150)]
        [Display(Name = "شناسه / نام مستعار مخاطب")]
        public string RespondentIdentifier { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Navigation Properties
        [ForeignKey("QuestionnaireId")]
        public virtual Questionnaire Questionnaire { get; set; }

        public virtual ICollection<Answer> Answers { get; set; }
        public virtual ICollection<AuditLog> AuditLogs { get; set; }

        // Computed status helper
        [NotMapped]
        public string StatusKey
        {
            get
            {
                if (IsUsed || CompletedAt.HasValue) return "Completed";
                if (ExpiresAt.HasValue && DateTime.Now > ExpiresAt.Value) return "Expired";
                if (OpenedAt.HasValue) return "Viewed";
                return "Pending";
            }
        }

        [NotMapped]
        public string StatusPersianTitle
        {
            get
            {
                switch (StatusKey)
                {
                    case "Completed": return "تکمیل شده";
                    case "Expired": return "منقضی شده";
                    case "Viewed": return "مشاهده شده";
                    default: return "در انتظار";
                }
            }
        }
    }
}

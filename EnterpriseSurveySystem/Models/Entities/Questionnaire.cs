using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseSurveySystem.Models.Entities
{
    [Table("Questionnaires")]
    public class Questionnaire
    {
        public Questionnaire()
        {
            CreatedAt = DateTime.Now;
            Questions = new HashSet<Question>();
            RespondentTokens = new HashSet<RespondentToken>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        [Display(Name = "عنوان پرسش‌نامه")]
        public string Title { get; set; }

        [Display(Name = "توضیحات و راهنمای تکمیل")]
        public string Description { get; set; }

        [Display(Name = "تعداد مخاطبان هدف")]
        public int TargetRespondentsCount { get; set; }

        [Display(Name = "محدودیت زمانی (دقیقه)")]
        public int? TimeLimitMinutes { get; set; }

        [Display(Name = "نمایش نام مخاطب در زمان تکمیل فرم")]
        public bool ShowRespondentName { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<RespondentToken> RespondentTokens { get; set; }
    }
}

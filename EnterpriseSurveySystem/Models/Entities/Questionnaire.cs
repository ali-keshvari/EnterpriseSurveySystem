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

        [StringLength(50)]
        [Display(Name = "قالب ظاهری فرم پاسخ‌دهی")]
        public string TemplateKey { get; set; } = "CorporateGlass";

        /// <summary>
        /// Questionnaire Type:
        /// - "Survey" (نظرسنجی معمولی)
        /// - "Quiz" (آزمون و ارزیابی با کارنامه و پاسخ صحیح)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "نوع پرسش‌نامه")]
        public string QuestionnaireType { get; set; } = "Survey";

        [Display(Name = "نمایش کارنامه به کاربر در پایان")]
        public bool ShowReportCard { get; set; } = true;

        [Display(Name = "نمایش پاسخ‌های صحیح و اشتباهات در کارنامه")]
        public bool ShowCorrectAnswers { get; set; } = true;

        [Display(Name = "حداقل درصد قبولی")]
        public int PassScorePercentage { get; set; } = 60;

        /// <summary>
        /// Quiz Guidance Mode:
        /// - "None" (فقط در کارنامه پایان آزمون)
        /// - "Instant" (هدایت و بازخورد بلادرنگ در زمان پاسخ‌دهی)
        /// </summary>
        [StringLength(50)]
        [Display(Name = "نحوه هدایت به پاسخ درست")]
        public string QuizGuidanceMode { get; set; } = "None";

        [NotMapped]
        public bool IsQuiz => string.Equals(QuestionnaireType, "Quiz", StringComparison.OrdinalIgnoreCase);

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<RespondentToken> RespondentTokens { get; set; }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class CreateQuestionnaireViewModel
    {
        [Required(ErrorMessage = "لطفاً عنوان پرسش‌نامه را وارد فرمایید.")]
        [StringLength(250)]
        [Display(Name = "عنوان پرسش‌نامه")]
        public string Title { get; set; }

        [Display(Name = "توضیحات و راهنمای تکمیل")]
        public string Description { get; set; }

        [Display(Name = "تعداد مخاطبان هدف")]
        public int TargetRespondentsCount { get; set; } = 0;

        [Range(1, 300, ErrorMessage = "محدودیت زمانی باید بین ۱ تا ۳۰۰ دقیقه باشد.")]
        [Display(Name = "محدودیت زمانی (دقیقه)")]
        public int? TimeLimitMinutes { get; set; } = 5;

        [Display(Name = "نمایش نام مخاطب در زمان تکمیل فرم")]
        public bool ShowRespondentName { get; set; } = true;

        [Display(Name = "قالب ظاهری فرم پاسخ‌دهی")]
        public string TemplateKey { get; set; } = "CorporateGlass";

        [Display(Name = "نوع پرسش‌نامه")]
        public string QuestionnaireType { get; set; } = "Survey"; // "Survey" or "Quiz"

        [Display(Name = "نمایش کارنامه در پایان به کاربر")]
        public bool ShowReportCard { get; set; } = true;

        [Display(Name = "نمایش پاسخ‌های صحیح و اشتباهات در کارنامه")]
        public bool ShowCorrectAnswers { get; set; } = true;

        [Display(Name = "حداقل درصد قبولی (حدنصاب)")]
        [Range(0, 100, ErrorMessage = "درصد قبولی باید بین ۰ تا ۱۰۰ باشد.")]
        public int PassScorePercentage { get; set; } = 60;

        [Display(Name = "نحوه هدایت به پاسخ درست")]
        public string QuizGuidanceMode { get; set; } = "None"; // "None" or "Instant"

        [Display(Name = "پرسنل انتخابی جهت صدور لینک اختصاصی")]
        public List<int> SelectedPersonIds { get; set; } = new List<int>();

        public List<CreateQuestionDto> Questions { get; set; } = new List<CreateQuestionDto>();
    }

    public class CreateQuestionDto
    {
        [Required]
        public string QuestionText { get; set; }

        [Required]
        public string QuestionType { get; set; } // Testy, Tashrihi, Select, MultiSelect, Boolean

        public string OptionsRaw { get; set; } // Comma or newline separated in form

        public string ImageUrl { get; set; } // Optional attached image URL

        public string CorrectAnswer { get; set; } // For Quiz mode

        public decimal Score { get; set; } = 1.0m; // Points/Score for question

        public string Explanation { get; set; } // Hint / Guidance to correct answer

        public bool IsRequired { get; set; } = true;
    }
}

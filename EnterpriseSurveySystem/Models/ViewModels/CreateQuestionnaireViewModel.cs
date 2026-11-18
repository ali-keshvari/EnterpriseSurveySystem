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

        public bool IsRequired { get; set; } = true;
    }
}

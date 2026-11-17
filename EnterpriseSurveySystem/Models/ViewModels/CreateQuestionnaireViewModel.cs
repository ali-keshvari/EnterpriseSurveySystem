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

        [Range(1, 10000, ErrorMessage = "تعداد مخاطبان باید حداقل ۱ نفر باشد.")]
        [Display(Name = "تعداد مخاطبان هدف")]
        public int TargetRespondentsCount { get; set; } = 25;

        [Range(1, 300, ErrorMessage = "محدودیت زمانی باید بین ۱ تا ۳۰۰ دقیقه باشد.")]
        [Display(Name = "محدودیت زمانی (دقیقه)")]
        public int? TimeLimitMinutes { get; set; } = 5;

        [Display(Name = "تولید خودکار لینک‌های اختصاصی پس از ایجاد")]
        public bool AutoGenerateTokens { get; set; } = true;

        public List<CreateQuestionDto> Questions { get; set; } = new List<CreateQuestionDto>();
    }

    public class CreateQuestionDto
    {
        [Required]
        public string QuestionText { get; set; }

        [Required]
        public string QuestionType { get; set; } // Testy, Tashrihi, Select, MultiSelect, Boolean

        public string OptionsRaw { get; set; } // Comma or newline separated in form

        public bool IsRequired { get; set; } = true;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseSurveySystem.Models.Entities
{
    [Table("Questions")]
    public class Question
    {
        public Question()
        {
            OrderIndex = 1;
            IsRequired = true;
            Answers = new HashSet<Answer>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int QuestionnaireId { get; set; }

        [Required]
        [Display(Name = "متن سوال")]
        public string QuestionText { get; set; }

        /// <summary>
        /// Question types:
        /// - Testy (Single choice radio)
        /// - Tashrihi (Descriptive / Textarea)
        /// - Select (Dropdown list)
        /// - MultiSelect (Multiple choices checkbox)
        /// - Boolean (Yes / No or True / False)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "نوع سوال")]
        public string QuestionType { get; set; }

        /// <summary>
        /// JSON array of choices for Testy, Select, and MultiSelect questions
        /// E.g. ["کاملاً موافقم", "موافقم", "نظری ندارم", "مخالفم", "کاملاً مخالفم"]
        /// </summary>
        [Display(Name = "گزینه‌ها (JSON)")]
        public string Options { get; set; }

        [Display(Name = "ترتیب نمایش")]
        public int OrderIndex { get; set; }

        [Display(Name = "پاسخ اجباری")]
        public bool IsRequired { get; set; }

        [StringLength(500)]
        [Display(Name = "آدرس تصویر پیوست سوال")]
        public string ImageUrl { get; set; }

        [Display(Name = "پاسخ صحیح (برای حالت آزمون)")]
        public string CorrectAnswer { get; set; }

        [Display(Name = "بارم / نمره سوال")]
        public decimal Score { get; set; } = 1.0m;

        [Display(Name = "توضیحات و راهنمای پاسخ صحیح جهت هدایت کاربر")]
        public string Explanation { get; set; }

        // Navigation Properties
        [ForeignKey("QuestionnaireId")]
        public virtual Questionnaire Questionnaire { get; set; }

        public virtual ICollection<Answer> Answers { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseSurveySystem.Models.Entities
{
    [Table("Answers")]
    public class Answer
    {
        public Answer()
        {
            CreatedAt = DateTime.Now;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TokenId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Display(Name = "مقدار پاسخ")]
        public string AnswerValue { get; set; }

        [Display(Name = "تاریخ ثبت")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("TokenId")]
        public virtual RespondentToken RespondentToken { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }
    }
}

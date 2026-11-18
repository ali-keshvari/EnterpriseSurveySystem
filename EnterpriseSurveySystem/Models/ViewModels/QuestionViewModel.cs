using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public string ImageUrl { get; set; }
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }
        public string CurrentAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public decimal Score { get; set; } = 1.0m;
        public string Explanation { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal? EarnedScore { get; set; }
    }
}

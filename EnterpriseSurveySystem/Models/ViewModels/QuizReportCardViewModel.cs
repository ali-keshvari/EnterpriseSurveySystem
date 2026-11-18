using System;
using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class QuizReportCardViewModel
    {
        public string TokenGuid { get; set; }
        public int QuestionnaireId { get; set; }
        public string QuestionnaireTitle { get; set; }
        public string TemplateKey { get; set; } = "CorporateGlass";
        public string RespondentIdentifier { get; set; }
        public string PersonFullName { get; set; }
        public int? PersonelCode { get; set; }
        public string DepartmentName { get; set; }
        public string PositionName { get; set; }
        public bool ShowRespondentName { get; set; } = true;

        public DateTime? CompletedAt { get; set; }
        public decimal TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public decimal ScorePercentage { get; set; }
        public int PassScorePercentage { get; set; }
        public bool IsPassed { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectAnswersCount { get; set; }
        public int WrongAnswersCount { get; set; }
        public int UnansweredCount { get; set; }

        public bool ShowReportCard { get; set; } = true;
        public bool ShowCorrectAnswers { get; set; } = true;
        public List<QuizQuestionReportDto> Questions { get; set; } = new List<QuizQuestionReportDto>();
    }

    public class QuizQuestionReportDto
    {
        public int QuestionId { get; set; }
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public string UserAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public decimal Score { get; set; }
        public decimal EarnedScore { get; set; }
        public string Explanation { get; set; }
    }
}

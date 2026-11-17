using System;
using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class QuestionnaireDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TargetRespondentsCount { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public bool ShowRespondentName { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public int TotalTokens { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int ViewedCount { get; set; }
        public int ExpiredCount { get; set; }
        public double CompletionPercentage { get; set; }

        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
        public List<RespondentTokenDto> Tokens { get; set; } = new List<RespondentTokenDto>();
    }

    public class RespondentTokenDto
    {
        public int Id { get; set; }
        public string TokenGuid { get; set; }
        public string RespondentIdentifier { get; set; }
        public int? PersonId { get; set; }
        public string PersonFullName { get; set; }
        public int? PersonelCode { get; set; }
        public string DepartmentName { get; set; }
        public string PositionName { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string StatusKey { get; set; }
        public string StatusPersianTitle { get; set; }
        public string SurveyUrl { get; set; }
    }
}

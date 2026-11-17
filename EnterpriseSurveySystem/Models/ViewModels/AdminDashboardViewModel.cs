using System;
using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalQuestionnaires { get; set; }
        public int TotalTokens { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalPending { get; set; }
        public int TotalViewed { get; set; }
        public int TotalExpired { get; set; }
        public double OverallCompletionRate { get; set; }

        public List<QuestionnaireSummaryDto> Questionnaires { get; set; } = new List<QuestionnaireSummaryDto>();
        public List<AuditLogDto> RecentLogs { get; set; } = new List<AuditLogDto>();
    }

    public class QuestionnaireSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int TargetCount { get; set; }
        public int GeneratedTokensCount { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int ExpiredCount { get; set; }
        public double CompletionPercentage { get; set; }
        public double CompletionRate => CompletionPercentage;
        public int TotalTokens => GeneratedTokensCount;
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogDto
    {
        public int Id { get; set; }
        public int? TokenId { get; set; }
        public string ActionType { get; set; }
        public string ActionPersianTitle { get; set; }
        public DateTime Timestamp { get; set; }
        public string IPAddress { get; set; }
        public string Details { get; set; }
        public string TokenGuid { get; set; }
        public string QuestionnaireTitle { get; set; }
    }
}

using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class AnalyticsViewModel
    {
        public int QuestionnaireId { get; set; }
        public string Title { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int TotalTokens { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int ViewedCount { get; set; }
        public int ExpiredCount { get; set; }
        public double CompletionRate { get; set; }
        public double AverageCompletionSeconds { get; set; }

        public string FormattedAverageTime
        {
            get
            {
                if (AverageCompletionSeconds <= 0) return "-";
                int m = (int)(AverageCompletionSeconds / 60);
                int s = (int)(AverageCompletionSeconds % 60);
                return $"{m} دقیقه و {s} ثانیه";
            }
        }

        public List<QuestionAnalyticsDto> QuestionStats { get; set; } = new List<QuestionAnalyticsDto>();
        public List<TimeDistributionItem> TimeDistribution { get; set; } = new List<TimeDistributionItem>();
    }

    public class QuestionAnalyticsDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string ImageUrl { get; set; }
        public string QuestionType { get; set; }
        public int TotalResponses { get; set; }
        public List<OptionStatDto> Options { get; set; } = new List<OptionStatDto>();
        public List<string> TextResponses { get; set; } = new List<string>();
    }

    public class OptionStatDto
    {
        public string OptionText { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class TimeDistributionItem
    {
        public string Label { get; set; } // e.g. "دقیقه ۱", "دقیقه ۲"
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}

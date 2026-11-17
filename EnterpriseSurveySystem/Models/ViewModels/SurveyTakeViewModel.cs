using System;
using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class SurveyTakeViewModel
    {
        public string TokenGuid { get; set; }
        public string RespondentIdentifier { get; set; }
        public int QuestionnaireId { get; set; }
        public string QuestionnaireTitle { get; set; }
        public string QuestionnaireDescription { get; set; }
        public string Description
        {
            get { return QuestionnaireDescription; }
            set { QuestionnaireDescription = value; }
        }
        public int? TimeLimitMinutes { get; set; }
        public bool ShowRespondentName { get; set; } = true;
        public DateTime? OpenedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int SecondsRemaining { get; set; }
        public int RemainingSeconds
        {
            get { return SecondsRemaining; }
            set { SecondsRemaining = value; }
        }
        public bool IsExpired { get; set; }
        public bool IsCompleted { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();

        // Formatting helpers
        public string FormattedTimeRemaining
        {
            get
            {
                if (SecondsRemaining <= 0) return "۰۰:۰۰";
                int m = SecondsRemaining / 60;
                int s = SecondsRemaining % 60;
                return $"{m:D2}:{s:D2}";
            }
        }
    }
}

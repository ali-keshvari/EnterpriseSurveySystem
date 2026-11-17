using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class AuditLogFilterViewModel
    {
        public int? QuestionnaireId { get; set; }
        public string ActionType { get; set; }
        public string SearchTerm { get; set; }
        public List<AuditLogDto> Logs { get; set; } = new List<AuditLogDto>();
        public List<QuestionnaireSummaryDto> Questionnaires { get; set; } = new List<QuestionnaireSummaryDto>();
    }
}

using System.Collections.Generic;
using EnterpriseSurveySystem.Models.Entities;
using EnterpriseSurveySystem.Models.ViewModels;

namespace EnterpriseSurveySystem.Services
{
    public interface ISurveyService
    {
        // Respondent operations
        SurveyTakeViewModel GetSurveyForRespondent(string tokenGuid, string ipAddress);
        ApiResponse SubmitSurvey(SurveySubmitModel model, string ipAddress);

        // Admin operations
        AdminDashboardViewModel GetDashboardData();
        QuestionnaireDetailsViewModel GetQuestionnaireDetails(int questionnaireId, string baseUrl);
        int CreateQuestionnaire(CreateQuestionnaireViewModel model, string ipAddress);
        int GenerateTokens(int questionnaireId, int count, string ipAddress);
        int GenerateTokensForPersons(int questionnaireId, List<PersonDto> persons, string ipAddress);
        AnalyticsViewModel GetAnalytics(int questionnaireId);
        List<RespondentTokenDto> GetTokensForExport(int questionnaireId, string baseUrl);
    }
}

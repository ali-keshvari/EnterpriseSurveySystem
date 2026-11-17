using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class SurveySubmitModel
    {
        public string TokenGuid { get; set; }
        public List<AnswerSubmissionDto> Answers { get; set; } = new List<AnswerSubmissionDto>();
    }

    public class AnswerSubmissionDto
    {
        public int QuestionId { get; set; }
        public string Value { get; set; }
    }
}

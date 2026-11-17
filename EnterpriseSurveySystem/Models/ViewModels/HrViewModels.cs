using System.Collections.Generic;

namespace EnterpriseSurveySystem.Models.ViewModels
{
    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public int DepHdrId { get; set; }
        public string DepNameFa { get; set; }
        public int DepLevel { get; set; }
        public bool HasChildren { get; set; }
        public List<DepartmentDto> Children { get; set; } = new List<DepartmentDto>();
    }

    public class PositionDto
    {
        public int PositionId { get; set; }
        public string PositionNameFa { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class PersonDto
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public int PersonelCode { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int PositionId { get; set; }
        public string PositionName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
    }

    public class GeneratePersonTokensRequest
    {
        public int QuestionnaireId { get; set; }
        public List<int> PersonIds { get; set; }
    }
}

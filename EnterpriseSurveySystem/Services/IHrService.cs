using System.Collections.Generic;
using EnterpriseSurveySystem.Models.ViewModels;

namespace EnterpriseSurveySystem.Services
{
    public interface IHrService
    {
        List<DepartmentDto> GetActiveDepartments();
        List<PositionDto> GetActivePositions(List<int> departmentIds = null);
        List<PersonDto> GetActivePersons(List<int> departmentIds = null, List<int> positionIds = null);
        List<PersonDto> GetPersonsByIds(List<int> personIds);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnterpriseSurveySystem.Models.Entities;
using EnterpriseSurveySystem.Models.ViewModels;

namespace EnterpriseSurveySystem.Services
{
    public interface IAuditService
    {
        void Log(string actionType, int? tokenId, int? questionnaireId, string ipAddress, string details);
        List<AuditLogDto> GetLogs(int? questionnaireId = null, string actionType = null, int take = 100);
    }
}

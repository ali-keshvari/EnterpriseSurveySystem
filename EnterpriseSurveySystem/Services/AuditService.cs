using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EnterpriseSurveySystem.Data;
using EnterpriseSurveySystem.Models.Entities;
using EnterpriseSurveySystem.Models.ViewModels;

namespace EnterpriseSurveySystem.Services
{
    public class AuditService : IAuditService
    {
        public void Log(string actionType, int? tokenId, int? questionnaireId, string ipAddress, string details)
        {
            try
            {
                using (var db = new SurveyDbContext())
                {
                    var log = new AuditLog
                    {
                        ActionType = actionType,
                        TokenId = tokenId,
                        QuestionnaireId = questionnaireId,
                        IPAddress = ipAddress ?? "127.0.0.1",
                        Details = details,
                        Timestamp = DateTime.Now
                    };

                    db.AuditLogs.Add(log);
                    db.SaveChanges();
                }
            }
            catch
            {
                // Never let audit logging failure crash critical flow, but record locally if needed
            }
        }

        public List<AuditLogDto> GetLogs(int? questionnaireId = null, string actionType = null, int take = 100)
        {
            using (var db = new SurveyDbContext())
            {
                var query = db.AuditLogs
                    .Include(l => l.RespondentToken)
                    .Include(l => l.RespondentToken.Questionnaire)
                    .AsNoTracking();

                if (questionnaireId.HasValue && questionnaireId.Value > 0)
                {
                    query = query.Where(l => l.QuestionnaireId == questionnaireId.Value ||
                                            (l.RespondentToken != null && l.RespondentToken.QuestionnaireId == questionnaireId.Value));
                }

                if (!string.IsNullOrWhiteSpace(actionType))
                {
                    query = query.Where(l => l.ActionType == actionType);
                }

                var list = query
                    .OrderByDescending(l => l.Timestamp)
                    .Take(take)
                    .ToList();

                return list.Select(l => new AuditLogDto
                {
                    Id = l.Id,
                    ActionType = l.ActionType,
                    ActionPersianTitle = l.ActionPersianTitle,
                    Timestamp = l.Timestamp,
                    IPAddress = l.IPAddress,
                    Details = l.Details,
                    TokenGuid = l.RespondentToken?.TokenGuid,
                    QuestionnaireTitle = l.RespondentToken?.Questionnaire?.Title
                }).ToList();
            }
        }
    }
}

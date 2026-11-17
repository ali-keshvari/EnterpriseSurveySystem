using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using EnterpriseSurveySystem.Data;
using EnterpriseSurveySystem.Models.Entities;
using EnterpriseSurveySystem.Models.ViewModels;
using Newtonsoft.Json;

namespace EnterpriseSurveySystem.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly IAuditService _auditService;

        public SurveyService() : this(new AuditService())
        {
        }

        public SurveyService(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public SurveyTakeViewModel GetSurveyForRespondent(string tokenGuid, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(tokenGuid))
                return null;

            tokenGuid = tokenGuid.Trim();

            using (var db = new SurveyDbContext())
            {
                using (var tx = db.Database.BeginTransaction())
                {
                    var token = db.RespondentTokens
                        .Include(t => t.Questionnaire)
                        .Include(t => t.Questionnaire.Questions)
                        .FirstOrDefault(t => t.TokenGuid == tokenGuid);

                    if (token == null)
                    {
                        _auditService.Log("AccessDenied", null, null, ipAddress, $"تلاش برای ورود با توکن نامعتبر: {tokenGuid}");
                        return null;
                    }

                    var now = DateTime.Now;
                    var q = token.Questionnaire;

                    var vm = new SurveyTakeViewModel
                    {
                        TokenGuid = token.TokenGuid,
                        RespondentIdentifier = token.RespondentIdentifier,
                        QuestionnaireId = q.Id,
                        QuestionnaireTitle = q.Title,
                        QuestionnaireDescription = q.Description,
                        TimeLimitMinutes = q.TimeLimitMinutes,
                        OpenedAt = token.OpenedAt,
                        ExpiresAt = token.ExpiresAt
                    };

                    // Check if already completed
                    if (token.IsUsed || token.CompletedAt.HasValue)
                    {
                        vm.IsCompleted = true;
                        tx.Commit();
                        return vm;
                    }

                    // Check if this is the FIRST page load
                    if (!token.OpenedAt.HasValue)
                    {
                        token.OpenedAt = now;
                        if (q.TimeLimitMinutes.HasValue && q.TimeLimitMinutes.Value > 0)
                        {
                            token.ExpiresAt = now.AddMinutes(q.TimeLimitMinutes.Value);
                        }
                        
                        // Add audit log within same transaction to prevent nested connection deadlock
                        db.AuditLogs.Add(new AuditLog
                        {
                            ActionType = "PageOpened",
                            TokenId = token.Id,
                            QuestionnaireId = q.Id,
                            IPAddress = ipAddress ?? "127.0.0.1",
                            Details = $"ورود اولیه مخاطب به پرسش‌نامه. مهلت مجاز: {q.TimeLimitMinutes ?? 0} دقیقه.",
                            Timestamp = now
                        });

                        db.SaveChanges();

                        vm.OpenedAt = token.OpenedAt;
                        vm.ExpiresAt = token.ExpiresAt;
                    }
                    else
                    {
                        // Check if time has already expired
                        if (token.ExpiresAt.HasValue && now > token.ExpiresAt.Value)
                        {
                            vm.IsExpired = true;
                            db.AuditLogs.Add(new AuditLog
                            {
                                ActionType = "TokenExpired",
                                TokenId = token.Id,
                                QuestionnaireId = q.Id,
                                IPAddress = ipAddress ?? "127.0.0.1",
                                Details = "ورود به لینک پس از اتمام زمان مجاز. دسترسی مسدود است.",
                                Timestamp = now
                            });
                            db.SaveChanges();
                            tx.Commit();
                            return vm;
                        }
                    }

                    // Calculate remaining time
                    if (token.ExpiresAt.HasValue)
                    {
                        var remaining = (token.ExpiresAt.Value - now).TotalSeconds;
                        vm.SecondsRemaining = remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
                        if (vm.SecondsRemaining <= 0)
                        {
                            vm.IsExpired = true;
                            tx.Commit();
                            return vm;
                        }
                    }
                    else
                    {
                        vm.SecondsRemaining = 86400; // Unlimited / large
                    }

                    // Populate questions
                    var orderedQuestions = q.Questions.OrderBy(x => x.OrderIndex).ThenBy(x => x.Id).ToList();
                    foreach (var question in orderedQuestions)
                    {
                        var qvm = new QuestionViewModel
                        {
                            Id = question.Id,
                            QuestionText = question.QuestionText,
                            QuestionType = question.QuestionType,
                            OrderIndex = question.OrderIndex,
                            IsRequired = question.IsRequired
                        };

                        if (!string.IsNullOrWhiteSpace(question.Options))
                        {
                            try
                            {
                                qvm.Options = JsonConvert.DeserializeObject<List<string>>(question.Options) ?? new List<string>();
                            }
                            catch
                            {
                                qvm.Options = new List<string>();
                            }
                        }

                        vm.Questions.Add(qvm);
                    }

                    tx.Commit();
                    return vm;
                }
            }
        }

        public ApiResponse SubmitSurvey(SurveySubmitModel model, string ipAddress)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.TokenGuid))
            {
                return ApiResponse.Fail("اطلاعات ارسال شده نامعتبر است.");
            }

            var tokenGuid = model.TokenGuid.Trim();

            using (var db = new SurveyDbContext())
            {
                using (var tx = db.Database.BeginTransaction())
                {
                    var token = db.RespondentTokens
                        .Include(t => t.Questionnaire)
                        .Include(t => t.Questionnaire.Questions)
                        .FirstOrDefault(t => t.TokenGuid == tokenGuid);

                    if (token == null)
                    {
                        _auditService.Log("AccessDenied", null, null, ipAddress, $"ارسال فرم با توکن ناموجود: {tokenGuid}");
                        return ApiResponse.Fail("شناسه توکن نامعتبر است.");
                    }

                    if (token.IsUsed || token.CompletedAt.HasValue)
                    {
                        _auditService.Log("AccessDenied", token.Id, token.QuestionnaireId, ipAddress, "تلاش برای ارسال مجدد فرم تکمیل‌شده.");
                        return ApiResponse.Fail("این پرسش‌نامه قبلاً تکمیل و ثبت نهایی شده است و امکان ارسال مجدد وجود ندارد.", "/Survey/Completed/" + tokenGuid);
                    }

                    var now = DateTime.Now;

                    // SERVER-SIDE TIMER ENFORCEMENT
                    if (token.ExpiresAt.HasValue && now > token.ExpiresAt.Value)
                    {
                        db.AuditLogs.Add(new AuditLog
                        {
                            ActionType = "TokenExpired",
                            TokenId = token.Id,
                            QuestionnaireId = token.QuestionnaireId,
                            IPAddress = ipAddress ?? "127.0.0.1",
                            Details = $"رد درخواست ثبت پاسخ: زمان در تاریخ {token.ExpiresAt.Value:yyyy/MM/dd HH:mm:ss} منقضی شده بود و زمان ارسال {now:yyyy/MM/dd HH:mm:ss} است.",
                            Timestamp = now
                        });
                        db.SaveChanges();
                        tx.Commit();
                        return ApiResponse.Fail("مهلت زمانی پاسخ‌گویی به این پرسش‌نامه به پایان رسیده است و پاسخ‌ها ثبت نخواهند شد.", "/Survey/Expired/" + tokenGuid);
                    }

                    // Save answers
                    var validQuestionIds = token.Questionnaire.Questions.Select(q => q.Id).ToHashSet();

                    if (model.Answers != null && model.Answers.Count > 0)
                    {
                        foreach (var answerDto in model.Answers)
                        {
                            if (validQuestionIds.Contains(answerDto.QuestionId) && !string.IsNullOrWhiteSpace(answerDto.Value))
                            {
                                var answer = new Answer
                                {
                                    TokenId = token.Id,
                                    QuestionId = answerDto.QuestionId,
                                    AnswerValue = answerDto.Value.Trim(),
                                    CreatedAt = now
                                };
                                db.Answers.Add(answer);
                            }
                        }
                    }

                    // Mark token as completed
                    token.IsUsed = true;
                    token.CompletedAt = now;

                    db.AuditLogs.Add(new AuditLog
                    {
                        ActionType = "FormSubmitted",
                        TokenId = token.Id,
                        QuestionnaireId = token.QuestionnaireId,
                        IPAddress = ipAddress ?? "127.0.0.1",
                        Details = $"پاسخ‌های پرسش‌نامه با موفقیت ثبت نهایی شد. تعداد پاسخ‌های معتبر: {model.Answers?.Count ?? 0}",
                        Timestamp = now
                    });

                    db.SaveChanges();
                    tx.Commit();

                    return ApiResponse.Ok("پاسخ‌های شما با موفقیت ثبت گردید. با تشکر از مشارکت شما.", null, "/Survey/Completed/" + tokenGuid);
                }
            }
        }

        public AdminDashboardViewModel GetDashboardData()
        {
            using (var db = new SurveyDbContext())
            {
                var questionnaires = db.Questionnaires
                    .Include(q => q.RespondentTokens)
                    .OrderByDescending(q => q.CreatedAt)
                    .ToList();

                var now = DateTime.Now;

                var vm = new AdminDashboardViewModel
                {
                    TotalQuestionnaires = questionnaires.Count,
                    TotalTokens = db.RespondentTokens.Count(),
                    TotalCompleted = db.RespondentTokens.Count(t => t.IsUsed || t.CompletedAt.HasValue),
                    TotalPending = db.RespondentTokens.Count(t => !t.OpenedAt.HasValue && !t.IsUsed),
                    TotalViewed = db.RespondentTokens.Count(t => t.OpenedAt.HasValue && !t.IsUsed && (!t.ExpiresAt.HasValue || t.ExpiresAt.Value >= now)),
                    TotalExpired = db.RespondentTokens.Count(t => !t.IsUsed && t.ExpiresAt.HasValue && t.ExpiresAt.Value < now)
                };

                if (vm.TotalTokens > 0)
                {
                    vm.OverallCompletionRate = Math.Round((double)vm.TotalCompleted / vm.TotalTokens * 100, 1);
                }

                foreach (var q in questionnaires)
                {
                    var total = q.RespondentTokens.Count;
                    var completed = q.RespondentTokens.Count(t => t.IsUsed || t.CompletedAt.HasValue);
                    var pending = q.RespondentTokens.Count(t => !t.OpenedAt.HasValue && !t.IsUsed);
                    var expired = q.RespondentTokens.Count(t => !t.IsUsed && t.ExpiresAt.HasValue && t.ExpiresAt.Value < now);

                    vm.Questionnaires.Add(new QuestionnaireSummaryDto
                    {
                        Id = q.Id,
                        Title = q.Title,
                        TimeLimitMinutes = q.TimeLimitMinutes,
                        TargetCount = q.TargetRespondentsCount,
                        GeneratedTokensCount = total,
                        CompletedCount = completed,
                        PendingCount = pending,
                        ExpiredCount = expired,
                        CompletionPercentage = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
                        CreatedAt = q.CreatedAt
                    });
                }

                vm.RecentLogs = _auditService.GetLogs(null, null, 15);
                return vm;
            }
        }

        public QuestionnaireDetailsViewModel GetQuestionnaireDetails(int questionnaireId, string baseUrl)
        {
            using (var db = new SurveyDbContext())
            {
                var q = db.Questionnaires
                    .Include(x => x.Questions)
                    .Include(x => x.RespondentTokens)
                    .FirstOrDefault(x => x.Id == questionnaireId);

                if (q == null) return null;

                var now = DateTime.Now;
                var tokens = q.RespondentTokens.OrderByDescending(t => t.Id).ToList();

                var vm = new QuestionnaireDetailsViewModel
                {
                    Id = q.Id,
                    Title = q.Title,
                    Description = q.Description,
                    TargetRespondentsCount = q.TargetRespondentsCount,
                    TimeLimitMinutes = q.TimeLimitMinutes,
                    CreatedAt = q.CreatedAt,
                    TotalTokens = tokens.Count,
                    CompletedCount = tokens.Count(t => t.IsUsed || t.CompletedAt.HasValue),
                    PendingCount = tokens.Count(t => !t.OpenedAt.HasValue && !t.IsUsed),
                    ViewedCount = tokens.Count(t => t.OpenedAt.HasValue && !t.IsUsed && (!t.ExpiresAt.HasValue || t.ExpiresAt.Value >= now)),
                    ExpiredCount = tokens.Count(t => !t.IsUsed && t.ExpiresAt.HasValue && t.ExpiresAt.Value < now)
                };

                if (vm.TotalTokens > 0)
                {
                    vm.CompletionPercentage = Math.Round((double)vm.CompletedCount / vm.TotalTokens * 100, 1);
                }

                foreach (var qu in q.Questions.OrderBy(x => x.OrderIndex))
                {
                    var qvm = new QuestionViewModel
                    {
                        Id = qu.Id,
                        QuestionText = qu.QuestionText,
                        QuestionType = qu.QuestionType,
                        OrderIndex = qu.OrderIndex,
                        IsRequired = qu.IsRequired
                    };

                    if (!string.IsNullOrWhiteSpace(qu.Options))
                    {
                        try { qvm.Options = JsonConvert.DeserializeObject<List<string>>(qu.Options) ?? new List<string>(); }
                        catch { }
                    }
                    vm.Questions.Add(qvm);
                }

                foreach (var t in tokens)
                {
                    vm.Tokens.Add(new RespondentTokenDto
                    {
                        Id = t.Id,
                        TokenGuid = t.TokenGuid,
                        RespondentIdentifier = t.RespondentIdentifier ?? $"مخاطب #{t.Id}",
                        IsUsed = t.IsUsed,
                        OpenedAt = t.OpenedAt,
                        CompletedAt = t.CompletedAt,
                        ExpiresAt = t.ExpiresAt,
                        StatusKey = t.StatusKey,
                        StatusPersianTitle = t.StatusPersianTitle,
                        SurveyUrl = $"{baseUrl.TrimEnd('/')}/Survey/Take/{t.TokenGuid}"
                    });
                }

                return vm;
            }
        }

        public int CreateQuestionnaire(CreateQuestionnaireViewModel model, string ipAddress)
        {
            using (var db = new SurveyDbContext())
            {
                var q = new Questionnaire
                {
                    Title = model.Title,
                    Description = model.Description,
                    TargetRespondentsCount = model.TargetRespondentsCount,
                    TimeLimitMinutes = model.TimeLimitMinutes,
                    CreatedAt = DateTime.Now
                };

                db.Questionnaires.Add(q);
                db.SaveChanges();

                if (model.Questions != null && model.Questions.Count > 0)
                {
                    int order = 1;
                    foreach (var qDto in model.Questions)
                    {
                        if (string.IsNullOrWhiteSpace(qDto.QuestionText)) continue;

                        var question = new Question
                        {
                            QuestionnaireId = q.Id,
                            QuestionText = qDto.QuestionText.Trim(),
                            QuestionType = qDto.QuestionType,
                            OrderIndex = order++,
                            IsRequired = qDto.IsRequired
                        };

                        if (question.QuestionType == "Boolean")
                        {
                            question.Options = JsonConvert.SerializeObject(new List<string> { "بله", "خیر" });
                        }
                        else if (!string.IsNullOrWhiteSpace(qDto.OptionsRaw))
                        {
                            var opts = qDto.OptionsRaw
                                .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToList();

                            question.Options = JsonConvert.SerializeObject(opts);
                        }

                        db.Questions.Add(question);
                    }
                    db.SaveChanges();
                }

                _auditService.Log("QuestionnaireCreated", null, q.Id, ipAddress,
                    $"ایجاد پرسش‌نامه جدید: {q.Title} با {model.Questions?.Count ?? 0} سوال و سقف {q.TargetRespondentsCount} پاسخ‌دهنده.");

                if (model.AutoGenerateTokens && model.TargetRespondentsCount > 0)
                {
                    GenerateTokens(q.Id, model.TargetRespondentsCount, ipAddress);
                }

                return q.Id;
            }
        }

        public int GenerateTokens(int questionnaireId, int count, string ipAddress)
        {
            if (count <= 0) return 0;
            if (count > 5000) count = 5000;

            using (var db = new SurveyDbContext())
            {
                var q = db.Questionnaires.Find(questionnaireId);
                if (q == null) return 0;

                int existingCount = db.RespondentTokens.Count(t => t.QuestionnaireId == questionnaireId);

                var list = new List<RespondentToken>();
                for (int i = 1; i <= count; i++)
                {
                    // Generate secure crypto-random token string
                    byte[] randomBytes = new byte[16];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(randomBytes);
                    }
                    string tokenGuid = BitConverter.ToString(randomBytes).Replace("-", "").ToLowerInvariant();

                    list.Add(new RespondentToken
                    {
                        QuestionnaireId = questionnaireId,
                        TokenGuid = tokenGuid,
                        RespondentIdentifier = $"مخاطب #{existingCount + i}",
                        IsUsed = false,
                        OpenedAt = null,
                        ExpiresAt = null,
                        CompletedAt = null
                    });
                }

                db.RespondentTokens.AddRange(list);
                db.SaveChanges();

                _auditService.Log("TokensGenerated", null, questionnaireId, ipAddress,
                    $"تولید {count} توکن یکتا و لینک اختصاصی امن برای پرسش‌نامه {q.Title}.");

                return count;
            }
        }

        public AnalyticsViewModel GetAnalytics(int questionnaireId)
        {
            using (var db = new SurveyDbContext())
            {
                var q = db.Questionnaires
                    .Include(x => x.Questions)
                    .Include(x => x.RespondentTokens)
                    .FirstOrDefault(x => x.Id == questionnaireId);

                if (q == null) return null;

                var now = DateTime.Now;
                var tokens = q.RespondentTokens.ToList();
                var completedTokens = tokens.Where(t => t.IsUsed && t.OpenedAt.HasValue && t.CompletedAt.HasValue).ToList();

                var vm = new AnalyticsViewModel
                {
                    QuestionnaireId = q.Id,
                    Title = q.Title,
                    TimeLimitMinutes = q.TimeLimitMinutes,
                    TotalTokens = tokens.Count,
                    CompletedCount = tokens.Count(t => t.IsUsed || t.CompletedAt.HasValue),
                    PendingCount = tokens.Count(t => !t.OpenedAt.HasValue && !t.IsUsed),
                    ViewedCount = tokens.Count(t => t.OpenedAt.HasValue && !t.IsUsed && (!t.ExpiresAt.HasValue || t.ExpiresAt.Value >= now)),
                    ExpiredCount = tokens.Count(t => !t.IsUsed && t.ExpiresAt.HasValue && t.ExpiresAt.Value < now)
                };

                if (vm.TotalTokens > 0)
                {
                    vm.CompletionRate = Math.Round((double)vm.CompletedCount / vm.TotalTokens * 100, 1);
                }

                // Average response duration
                if (completedTokens.Count > 0)
                {
                    var totalSeconds = completedTokens
                        .Select(t => (t.CompletedAt.Value - t.OpenedAt.Value).TotalSeconds)
                        .Where(sec => sec > 0)
                        .DefaultIfEmpty(0)
                        .Average();

                    vm.AverageCompletionSeconds = totalSeconds;
                }

                // Time distribution buckets
                var timeBuckets = new Dictionary<int, int>
                {
                    { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
                };

                foreach (var ct in completedTokens)
                {
                    var dur = (ct.CompletedAt.Value - ct.OpenedAt.Value).TotalMinutes;
                    int minuteBucket = (int)Math.Ceiling(dur);
                    if (minuteBucket < 1) minuteBucket = 1;
                    if (minuteBucket > 5) minuteBucket = 5;

                    timeBuckets[minuteBucket]++;
                }

                int totalCompletedForDist = completedTokens.Count;
                foreach (var kvp in timeBuckets)
                {
                    vm.TimeDistribution.Add(new TimeDistributionItem
                    {
                        Label = $"دقیقه {kvp.Key}",
                        Count = kvp.Value,
                        Percentage = totalCompletedForDist > 0 ? Math.Round((double)kvp.Value / totalCompletedForDist * 100, 1) : 0
                    });
                }

                // Question by question stats
                var questionIds = q.Questions.Select(x => x.Id).ToList();
                var answers = db.Answers
                    .Where(a => questionIds.Contains(a.QuestionId))
                    .ToList();

                foreach (var qu in q.Questions.OrderBy(x => x.OrderIndex))
                {
                    var qAnswers = answers.Where(a => a.QuestionId == qu.Id).ToList();

                    var qStat = new QuestionAnalyticsDto
                    {
                        QuestionId = qu.Id,
                        QuestionText = qu.QuestionText,
                        QuestionType = qu.QuestionType,
                        TotalResponses = qAnswers.Count
                    };

                    List<string> optionsList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(qu.Options))
                    {
                        try { optionsList = JsonConvert.DeserializeObject<List<string>>(qu.Options) ?? new List<string>(); }
                        catch { }
                    }

                    if (qu.QuestionType == "Boolean")
                    {
                        if (optionsList.Count == 0)
                        {
                            optionsList = new List<string> { "بله", "خیر" };
                        }
                    }

                    if (qu.QuestionType == "Tashrihi")
                    {
                        qStat.TextResponses = qAnswers
                            .Select(a => a.AnswerValue)
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .ToList();
                    }
                    else
                    {
                        // Calculate frequencies
                        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        foreach (var opt in optionsList)
                        {
                            counts[opt] = 0;
                        }

                        foreach (var ans in qAnswers)
                        {
                            if (string.IsNullOrWhiteSpace(ans.AnswerValue)) continue;

                            if (qu.QuestionType == "MultiSelect")
                            {
                                var parts = ans.AnswerValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var p in parts)
                                {
                                    var cleanP = p.Trim();
                                    if (counts.ContainsKey(cleanP)) counts[cleanP]++;
                                    else counts[cleanP] = 1;
                                }
                            }
                            else
                            {
                                var cleanVal = ans.AnswerValue.Trim();
                                if (counts.ContainsKey(cleanVal)) counts[cleanVal]++;
                                else counts[cleanVal] = 1;
                            }
                        }

                        int sumResponses = qStat.TotalResponses > 0 ? qStat.TotalResponses : 1;
                        foreach (var opt in optionsList)
                        {
                            int count = counts.ContainsKey(opt) ? counts[opt] : 0;
                            qStat.Options.Add(new OptionStatDto
                            {
                                OptionText = opt,
                                Count = count,
                                Percentage = Math.Round((double)count / sumResponses * 100, 1)
                            });
                        }
                    }

                    vm.QuestionStats.Add(qStat);
                }

                return vm;
            }
        }

        public List<RespondentTokenDto> GetTokensForExport(int questionnaireId, string baseUrl)
        {
            using (var db = new SurveyDbContext())
            {
                var tokens = db.RespondentTokens
                    .Where(t => t.QuestionnaireId == questionnaireId)
                    .OrderBy(t => t.Id)
                    .ToList();

                return tokens.Select(t => new RespondentTokenDto
                {
                    Id = t.Id,
                    TokenGuid = t.TokenGuid,
                    RespondentIdentifier = t.RespondentIdentifier,
                    IsUsed = t.IsUsed,
                    OpenedAt = t.OpenedAt,
                    CompletedAt = t.CompletedAt,
                    ExpiresAt = t.ExpiresAt,
                    StatusKey = t.StatusKey,
                    StatusPersianTitle = t.StatusPersianTitle,
                    SurveyUrl = $"{baseUrl.TrimEnd('/')}/Survey/Take/{t.TokenGuid}"
                }).ToList();
            }
        }
    }
}

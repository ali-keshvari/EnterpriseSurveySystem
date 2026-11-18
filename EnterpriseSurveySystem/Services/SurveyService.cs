using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
                        ShowRespondentName = q.ShowRespondentName,
                        TemplateKey = string.IsNullOrWhiteSpace(q.TemplateKey) ? "CorporateGlass" : q.TemplateKey,
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
                            ImageUrl = question.ImageUrl,
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
                    ShowRespondentName = q.ShowRespondentName,
                    TemplateKey = string.IsNullOrWhiteSpace(q.TemplateKey) ? "CorporateGlass" : q.TemplateKey,
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
                        ImageUrl = qu.ImageUrl,
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
                        PersonId = t.PersonId,
                        PersonFullName = t.PersonFullName,
                        PersonelCode = t.PersonelCode,
                        DepartmentName = t.DepartmentName,
                        PositionName = t.PositionName,
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
                    ShowRespondentName = model.ShowRespondentName,
                    TemplateKey = string.IsNullOrWhiteSpace(model.TemplateKey) ? "CorporateGlass" : model.TemplateKey.Trim(),
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
                            ImageUrl = string.IsNullOrWhiteSpace(qDto.ImageUrl) ? null : qDto.ImageUrl.Trim(),
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
                    $"ایجاد پرسش‌نامه جدید: {q.Title} با {model.Questions?.Count ?? 0} سوال.");

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

        public int GenerateTokensForPersons(int questionnaireId, List<PersonDto> persons, string ipAddress)
        {
            if (persons == null || persons.Count == 0) return 0;

            using (var db = new SurveyDbContext())
            {
                var q = db.Questionnaires.Find(questionnaireId);
                if (q == null) return 0;

                // Prevent duplicates: find PersonIds that already have an issued token for this questionnaire
                var existingPersonIds = db.RespondentTokens
                    .Where(t => t.QuestionnaireId == questionnaireId && t.PersonId.HasValue)
                    .Select(t => t.PersonId.Value)
                    .Distinct()
                    .ToList();

                var newPersons = persons
                    .Where(p => !existingPersonIds.Contains(p.PersonId))
                    .GroupBy(p => p.PersonId)
                    .Select(g => g.First())
                    .ToList();

                if (newPersons.Count == 0) return 0;

                var list = new List<RespondentToken>();
                foreach (var p in newPersons)
                {
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
                        PersonId = p.PersonId,
                        PersonFullName = p.FullName,
                        PersonelCode = p.PersonelCode,
                        DepartmentName = p.DepartmentName,
                        PositionName = p.PositionName,
                        RespondentIdentifier = $"{p.FullName} ({p.PersonelCode})",
                        IsUsed = false,
                        OpenedAt = null,
                        ExpiresAt = null,
                        CompletedAt = null
                    });
                }

                db.RespondentTokens.AddRange(list);

                // Ensure target count reflects total
                int currentTotal = db.RespondentTokens.Count(t => t.QuestionnaireId == questionnaireId);
                if (q.TargetRespondentsCount < currentTotal + list.Count)
                {
                    q.TargetRespondentsCount = currentTotal + list.Count;
                }

                db.SaveChanges();

                _auditService.Log("TokensGenerated", null, questionnaireId, ipAddress,
                    $"تولید اختصاصی {list.Count} لینک نظرسنجی برای پرسنل جدید پرسش‌نامه «{q.Title}».");

                return list.Count;
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

                // Time distribution buckets based on TimeLimitMinutes
                int maxMinutes = q.TimeLimitMinutes.HasValue && q.TimeLimitMinutes.Value > 0
                    ? q.TimeLimitMinutes.Value
                    : 5;

                var timeBuckets = new Dictionary<int, int>();
                for (int m = 1; m <= maxMinutes; m++)
                {
                    timeBuckets[m] = 0;
                }

                foreach (var ct in completedTokens)
                {
                    var dur = (ct.CompletedAt.Value - ct.OpenedAt.Value).TotalMinutes;
                    int minuteBucket = (int)Math.Ceiling(dur);
                    if (minuteBucket < 1) minuteBucket = 1;
                    if (minuteBucket > maxMinutes) minuteBucket = maxMinutes;

                    timeBuckets[minuteBucket]++;
                }

                int totalCompletedForDist = completedTokens.Count;
                foreach (var kvp in timeBuckets)
                {
                    string label = maxMinutes == 1 
                        ? "دقیقه ۱ (۰ تا ۱)" 
                        : $"دقیقه {kvp.Key} ({kvp.Key - 1} تا {kvp.Key})";

                    vm.TimeDistribution.Add(new TimeDistributionItem
                    {
                        Label = label,
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
                        ImageUrl = qu.ImageUrl,
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
                                List<string> selectedOptions = null;
                                var rawVal = ans.AnswerValue.Trim();

                                if (rawVal.StartsWith("[") && rawVal.EndsWith("]"))
                                {
                                    try
                                    {
                                        selectedOptions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(rawVal);
                                    }
                                    catch { }
                                }

                                if (selectedOptions == null || selectedOptions.Count == 0)
                                {
                                    var parts = rawVal.Trim('[', ']').Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                    selectedOptions = parts.Select(p => p.Trim().Trim('"', '\'')).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                                }

                                foreach (var p in selectedOptions)
                                {
                                    var cleanP = p.Trim();
                                    var matchedKey = counts.Keys.FirstOrDefault(k => string.Equals(k.Trim(), cleanP, StringComparison.OrdinalIgnoreCase));
                                    if (matchedKey != null) counts[matchedKey]++;
                                    else counts[cleanP] = 1;
                                }
                            }
                            else
                            {
                                var cleanVal = ans.AnswerValue.Trim().Trim('"', '\'');
                                var matchedKey = counts.Keys.FirstOrDefault(k => string.Equals(k.Trim(), cleanVal, StringComparison.OrdinalIgnoreCase));
                                if (matchedKey != null) counts[matchedKey]++;
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
                    PersonId = t.PersonId,
                    PersonFullName = t.PersonFullName,
                    PersonelCode = t.PersonelCode,
                    DepartmentName = t.DepartmentName,
                    PositionName = t.PositionName,
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

        public RespondentFullResponseDto GetRespondentResponse(int tokenId)
        {
            using (var db = new SurveyDbContext())
            {
                var token = db.RespondentTokens
                    .Include("Questionnaire")
                    .Include("Questionnaire.Questions")
                    .FirstOrDefault(t => t.Id == tokenId);

                if (token == null) return null;

                var answers = db.Answers
                    .Where(a => a.TokenId == tokenId)
                    .ToList();

                var answerDict = answers.ToDictionary(a => a.QuestionId, a => a.AnswerValue);

                var dto = new RespondentFullResponseDto
                {
                    TokenId = token.Id,
                    TokenGuid = token.TokenGuid,
                    RespondentName = token.PersonFullName ?? token.RespondentIdentifier ?? "مخاطب بدون نام",
                    PersonelCode = token.PersonelCode,
                    DepartmentName = token.DepartmentName ?? "-",
                    PositionName = token.PositionName ?? "-",
                    IsCompleted = (token.StatusKey == "Completed"),
                    OpenedAt = token.OpenedAt,
                    CompletedAt = token.CompletedAt,
                    StatusPersian = token.StatusPersianTitle,
                    QuestionnaireTitle = token.Questionnaire?.Title
                };

                if (token.OpenedAt.HasValue && token.CompletedAt.HasValue)
                {
                    var diff = token.CompletedAt.Value - token.OpenedAt.Value;
                    var totalSec = (int)Math.Max(0, diff.TotalSeconds);
                    var mins = totalSec / 60;
                    var secs = totalSec % 60;
                    dto.DurationFormatted = (mins > 0) ? $"{mins} دقیقه و {secs} ثانیه" : $"{secs} ثانیه";
                }
                else
                {
                    dto.DurationFormatted = "-";
                }

                var questions = token.Questionnaire?.Questions?.OrderBy(q => q.OrderIndex).ThenBy(q => q.Id).ToList() 
                                ?? new List<Question>();

                int qIndex = 1;
                foreach (var q in questions)
                {
                    var qDetail = new RespondentAnswerDetailDto
                    {
                        QuestionId = q.Id,
                        QuestionNumber = qIndex++,
                        QuestionText = q.QuestionText,
                        ImageUrl = q.ImageUrl,
                        QuestionType = q.QuestionType
                    };

                    switch (q.QuestionType)
                    {
                        case "Testy": qDetail.QuestionTypePersian = "تستی (تک انتخابی)"; break;
                        case "MultiSelect": qDetail.QuestionTypePersian = "چند انتخابی"; break;
                        case "Tashrihi": qDetail.QuestionTypePersian = "تشریحی"; break;
                        case "Boolean": qDetail.QuestionTypePersian = "بله / خیر"; break;
                        case "Select": qDetail.QuestionTypePersian = "منوی کشویی"; break;
                        default: qDetail.QuestionTypePersian = q.QuestionType; break;
                    }

                    if (answerDict.ContainsKey(q.Id))
                    {
                        var raw = answerDict[q.Id] ?? string.Empty;
                        if (q.QuestionType == "MultiSelect")
                        {
                            List<string> selected = null;
                            if (raw.Trim().StartsWith("[") && raw.Trim().EndsWith("]"))
                            {
                                try
                                {
                                    selected = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(raw);
                                }
                                catch { }
                            }
                            if (selected == null || selected.Count == 0)
                            {
                                selected = raw.Trim('[', ']').Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(s => s.Trim().Trim('"', '\''))
                                              .Where(s => !string.IsNullOrWhiteSpace(s))
                                              .ToList();
                            }
                            qDetail.MultiSelectAnswers = selected;
                            qDetail.FormattedAnswer = (selected.Count > 0) ? string.Join("، ", selected) : "پاسخی ثبت نشده است.";
                        }
                        else
                        {
                            qDetail.FormattedAnswer = raw.Trim().Trim('"', '\'');
                        }
                    }
                    else
                    {
                        qDetail.FormattedAnswer = "پاسخی ثبت نشده است.";
                    }

                    dto.Answers.Add(qDetail);
                }

                return dto;
            }
        }

        public byte[] ExportResponsesToExcel(int questionnaireId, string baseUrl, out string fileName)
        {
            using (var db = new SurveyDbContext())
            {
                var questionnaire = db.Questionnaires
                    .Include("Questions")
                    .FirstOrDefault(q => q.Id == questionnaireId);

                if (questionnaire == null)
                {
                    fileName = "Survey_NotFound.xls";
                    return new byte[0];
                }

                var questions = questionnaire.Questions.OrderBy(q => q.OrderIndex).ThenBy(q => q.Id).ToList();
                var tokens = db.RespondentTokens
                    .Where(t => t.QuestionnaireId == questionnaireId)
                    .OrderBy(t => t.Id)
                    .ToList();

                var tokenIds = tokens.Select(t => t.Id).ToList();
                var allAnswers = db.Answers
                    .Where(a => tokenIds.Contains(a.TokenId))
                    .ToList();

                var answersLookup = allAnswers.ToLookup(a => a.TokenId);

                fileName = $"Survey_{questionnaireId}_Responses_{DateTime.Now:yyyyMMdd_HHmm}.xls";

                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
                sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
                sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
                sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");

                sb.AppendLine(" <Styles>");
                sb.AppendLine("  <Style ss:ID=\"Default\" ss:Name=\"Normal\">");
                sb.AppendLine("   <Alignment ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\"/>");
                sb.AppendLine("   <Font ss:FontName=\"Tahoma\" ss:Size=\"9\" ss:Color=\"#000000\"/>");
                sb.AppendLine("  </Style>");
                sb.AppendLine("  <Style ss:ID=\"HeaderStyle\">");
                sb.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:WrapText=\"1\" ss:ReadingOrder=\"RightToLeft\"/>");
                sb.AppendLine("   <Borders>");
                sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#000000\"/>");
                sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#000000\"/>");
                sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#000000\"/>");
                sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#000000\"/>");
                sb.AppendLine("   </Borders>");
                sb.AppendLine("   <Font ss:FontName=\"Tahoma\" ss:Size=\"10\" ss:Color=\"#FFFFFF\" ss:Bold=\"1\"/>");
                sb.AppendLine("   <Interior ss:Color=\"#1E40AF\" ss:Pattern=\"Solid\"/>");
                sb.AppendLine("  </Style>");
                sb.AppendLine("  <Style ss:ID=\"RowStyle\">");
                sb.AppendLine("   <Alignment ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\" ss:WrapText=\"1\"/>");
                sb.AppendLine("   <Borders>");
                sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/>");
                sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/>");
                sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/>");
                sb.AppendLine("   </Borders>");
                sb.AppendLine("   <Font ss:FontName=\"Tahoma\" ss:Size=\"9\"/>");
                sb.AppendLine("  </Style>");
                sb.AppendLine("  <Style ss:ID=\"RowStyleAlt\">");
                sb.AppendLine("   <Alignment ss:Vertical=\"Center\" ss:ReadingOrder=\"RightToLeft\" ss:WrapText=\"1\"/>");
                sb.AppendLine("   <Borders>");
                sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/>");
                sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/>");
                sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/>");
                sb.AppendLine("   </Borders>");
                sb.AppendLine("   <Font ss:FontName=\"Tahoma\" ss:Size=\"9\"/>");
                sb.AppendLine("   <Interior ss:Color=\"#F8FAFC\" ss:Pattern=\"Solid\"/>");
                sb.AppendLine("  </Style>");
                sb.AppendLine(" </Styles>");

                sb.AppendLine(" <Worksheet ss:Name=\"پاسخ‌های مخاطبان\">");
                sb.AppendLine("  <Table>");
                sb.AppendLine("   <Column ss:Width=\"40\"/>");
                sb.AppendLine("   <Column ss:Width=\"130\"/>");
                sb.AppendLine("   <Column ss:Width=\"80\"/>");
                sb.AppendLine("   <Column ss:Width=\"150\"/>");
                sb.AppendLine("   <Column ss:Width=\"150\"/>");
                sb.AppendLine("   <Column ss:Width=\"80\"/>");
                sb.AppendLine("   <Column ss:Width=\"120\"/>");
                sb.AppendLine("   <Column ss:Width=\"120\"/>");
                sb.AppendLine("   <Column ss:Width=\"100\"/>");
                foreach (var q in questions)
                {
                    sb.AppendLine("   <Column ss:Width=\"180\"/>");
                }
                sb.AppendLine("   <Column ss:Width=\"220\"/>");

                // Header Row
                sb.AppendLine("   <Row ss:Height=\"28\" ss:StyleID=\"HeaderStyle\">");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">ردیف</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">نام و نام خانوادگی</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">کد پرسنلی</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">دپارتمان</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">سمت سازمانی</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">وضعیت</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">زمان بازگشایی</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">زمان ثبت نهایی</Data></Cell>");
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">مدت پاسخ‌گویی</Data></Cell>");

                int qNum = 1;
                foreach (var q in questions)
                {
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">سوال {qNum++}: {EscapeXml(q.QuestionText)}</Data></Cell>");
                }
                sb.AppendLine("    <Cell><Data ss:Type=\"String\">لینک اختصاصی</Data></Cell>");
                sb.AppendLine("   </Row>");

                // Data Rows
                int rowIndex = 1;
                foreach (var t in tokens)
                {
                    string styleId = (rowIndex % 2 == 0) ? "RowStyleAlt" : "RowStyle";
                    sb.AppendLine($"   <Row ss:Height=\"22\" ss:StyleID=\"{styleId}\">");
                    sb.AppendLine($"    <Cell><Data ss:Type=\"Number\">{rowIndex}</Data></Cell>");
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(t.PersonFullName ?? t.RespondentIdentifier ?? "-")}</Data></Cell>");
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(t.PersonelCode.HasValue ? t.PersonelCode.Value.ToString() : "-")}</Data></Cell>");
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(t.DepartmentName ?? "-")}</Data></Cell>");
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(t.PositionName ?? "-")}</Data></Cell>");

                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(t.StatusPersianTitle)}</Data></Cell>");

                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{(t.OpenedAt.HasValue ? t.OpenedAt.Value.ToString("yyyy/MM/dd HH:mm:ss") : "-")}</Data></Cell>");
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{(t.CompletedAt.HasValue ? t.CompletedAt.Value.ToString("yyyy/MM/dd HH:mm:ss") : "-")}</Data></Cell>");

                    string durationStr = "-";
                    if (t.OpenedAt.HasValue && t.CompletedAt.HasValue)
                    {
                        var diff = t.CompletedAt.Value - t.OpenedAt.Value;
                        int totalSec = (int)Math.Max(0, diff.TotalSeconds);
                        int m = totalSec / 60;
                        int s = totalSec % 60;
                        durationStr = (m > 0) ? $"{m} دقیقه و {s} ثانیه" : $"{s} ثانیه";
                    }
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{durationStr}</Data></Cell>");

                    var respondentAnswers = answersLookup[t.Id].ToDictionary(a => a.QuestionId, a => a.AnswerValue);
                    foreach (var q in questions)
                    {
                        string ansStr = "-";
                        if (respondentAnswers.ContainsKey(q.Id))
                        {
                            var raw = respondentAnswers[q.Id] ?? "";
                            if (q.QuestionType == "MultiSelect")
                            {
                                List<string> selected = null;
                                if (raw.Trim().StartsWith("[") && raw.Trim().EndsWith("]"))
                                {
                                    try
                                    {
                                        selected = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(raw);
                                    }
                                    catch { }
                                }
                                if (selected == null || selected.Count == 0)
                                {
                                    selected = raw.Trim('[', ']').Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(s => s.Trim().Trim('"', '\''))
                                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                                  .ToList();
                                }
                                ansStr = (selected.Count > 0) ? string.Join("، ", selected) : "-";
                            }
                            else
                            {
                                ansStr = raw.Trim().Trim('"', '\'');
                            }
                        }
                        sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(ansStr)}</Data></Cell>");
                    }

                    string tokenUrl = $"{baseUrl.TrimEnd('/')}/Survey/Take/{t.TokenGuid}";
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(tokenUrl)}</Data></Cell>");
                    sb.AppendLine("   </Row>");
                    rowIndex++;
                }

                sb.AppendLine("  </Table>");
                sb.AppendLine("  <WorksheetOptions xmlns=\"urn:schemas-microsoft-com:office:excel\">");
                sb.AppendLine("   <DisplayRightToLeft/>");
                sb.AppendLine("   <FreezePanes/>");
                sb.AppendLine("   <FrozenNoSplit/>");
                sb.AppendLine("   <SplitHorizontal>1</SplitHorizontal>");
                sb.AppendLine("   <TopRowBottomPane>1</TopRowBottomPane>");
                sb.AppendLine("   <ActivePane>2</ActivePane>");
                sb.AppendLine("  </WorksheetOptions>");
                sb.AppendLine(" </Worksheet>");
                sb.AppendLine("</Workbook>");

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
        }

        public bool ReactivateToken(int tokenId, string ipAddress)
        {
            using (var db = new SurveyDbContext())
            {
                var token = db.RespondentTokens.Include("Questionnaire").FirstOrDefault(t => t.Id == tokenId);
                if (token == null) return false;

                // Cannot reactivate already completed surveys
                if (token.IsUsed || token.CompletedAt.HasValue) return false;

                token.OpenedAt = null;
                token.ExpiresAt = null;

                // Also remove any partial/draft answers for this token so they can take the survey fresh
                var draftAnswers = db.Answers.Where(a => a.TokenId == tokenId).ToList();
                if (draftAnswers.Count > 0)
                {
                    db.Answers.RemoveRange(draftAnswers);
                }

                db.SaveChanges();

                _auditService.Log("TokenReactivated", token.Id, token.QuestionnaireId, ipAddress,
                    $"فعال‌سازی مجدد مهلت پاسخ‌دهی برای مخاطب: {token.PersonFullName ?? token.RespondentIdentifier} (توکن #{token.Id}).");

                return true;
            }
        }

        private static string EscapeXml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}

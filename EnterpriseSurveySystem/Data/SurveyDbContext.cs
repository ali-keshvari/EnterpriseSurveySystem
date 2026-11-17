using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using EnterpriseSurveySystem.Models.Entities;

namespace EnterpriseSurveySystem.Data
{
    public class SurveyDbContext : DbContext
    {
        public const string DefaultConnectionString = "Data Source=.;Initial Catalog=EnterpriseSurveyDb;Integrated Security=True;MultipleActiveResultSets=True;";

        static SurveyDbContext()
        {
            // Database-First: Disable Code-First database initializers
            Database.SetInitializer<SurveyDbContext>(null);
        }

        public SurveyDbContext() : base(GetConnectionString())
        {
            // Configure EF behavior
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
        }

        public SurveyDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
        }

        private static string GetConnectionString()
        {
            if (System.Configuration.ConfigurationManager.ConnectionStrings["EnterpriseSurveyConnection"] != null)
            {
                return "name=EnterpriseSurveyConnection";
            }
            return DefaultConnectionString;
        }

        public DbSet<Questionnaire> Questionnaires { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<RespondentToken> RespondentTokens { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Disable pluralizing table names
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Relationships and cascading rules
            modelBuilder.Entity<Question>()
                .HasRequired(q => q.Questionnaire)
                .WithMany(qn => qn.Questions)
                .HasForeignKey(q => q.QuestionnaireId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<RespondentToken>()
                .HasRequired(t => t.Questionnaire)
                .WithMany(qn => qn.RespondentTokens)
                .HasForeignKey(t => t.QuestionnaireId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Answer>()
                .HasRequired(a => a.RespondentToken)
                .WithMany(t => t.Answers)
                .HasForeignKey(a => a.TokenId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Answer>()
                .HasRequired(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AuditLog>()
                .HasOptional(al => al.RespondentToken)
                .WithMany(t => t.AuditLogs)
                .HasForeignKey(al => al.TokenId)
                .WillCascadeOnDelete(false);
        }
    }
}

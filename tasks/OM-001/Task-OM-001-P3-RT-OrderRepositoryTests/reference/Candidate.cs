using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Candidate_BE.Models
{
    [Table("candidates")]
    public class Candidate
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("years_experience")]
        public float? YearsExperience { get; set; }

        [Column("coding")]
        public float? Coding { get; set; }

        [Column("detailed_design")]
        public float? DetailedDesign { get; set; }

        [Column("instructor")]
        public float? Instructor { get; set; }

        [Column("integration_test")]
        public float? IntegrationTest { get; set; }

        [Column("leader")]
        public float? Leader { get; set; }

        [Column("maintenance")]
        public float? Maintenance { get; set; }

        [Column("operation")]
        public float? Operation { get; set; }

        [Column("overall_design")]
        public float? OverallDesign { get; set; }

        [Column("project_manager")]
        public float? ProjectManager { get; set; }

        [Column("scrum_master")]
        public float? ScrumMaster { get; set; }

        [Column("specification")]
        public float? Specification { get; set; }

        [Column("unit_test")]
        public float? UnitTest { get; set; }

        [Column("uiux_real")]
        public float? UiuxReal { get; set; }

        [Column("requirements_real")]
        public float? RequirementsReal { get; set; }

        [Column("is_approved")]
        public bool IsApproved { get; set; }

        [Column("job_title")]
        public string JobTitle { get; set; }

        public List<Cloud> Clouds { get; set; }
        public List<Database> Databases { get; set; }
        public List<FrameworkBackend> FrameworksBackend { get; set; }
        public List<FrameworkFrontend> FrameworksFrontend { get; set; }
        public List<OS> OS { get; set; }
        public List<ProgrammingLanguage> ProgrammingLanguages { get; set; }
    }

    [Table("cloud")]
    public class Cloud
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }

    [Table("databases")]
    public class Database
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }

    [Table("frameworks_backend")]
    public class FrameworkBackend
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }

    [Table("frameworks_frontend")]
    public class FrameworkFrontend
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }

    [Table("os")]
    public class OS
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }

    [Table("programming_languages")]
    public class ProgrammingLanguage
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }
}

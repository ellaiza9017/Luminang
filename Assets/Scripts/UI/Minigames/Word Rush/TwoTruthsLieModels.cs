using System;
using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic;

// TODO: DELETE THIS ENTIRE FILE
// The table this file maps to (two_truths_lie_challenges) is being removed from Supabase.
// Minigame content is now handled locally via JSON files.
namespace Luminang.Database
{
    [Table("two_truths_lie_challenges")]
    public class TruthsAndLiesChallengeModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("vocabulary_id")]
        public string VocabularyId { get; set; }

        [Column("content_text")]
        public string ContentText { get; set; }

        [Column("is_lie")]
        public bool IsLie { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    // Helper class for the Manager to hold a full round of data
    public class TruthsAndLiesRoundData
    {
        public VocabularyTranslationModel TargetWord;
        public List<TruthsAndLiesChallengeModel> AllChallenges;
        public List<TruthsAndLiesChallengeModel> Truths;
        public List<TruthsAndLiesChallengeModel> Lies;
    }
}

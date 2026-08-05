using Postgrest.Attributes;
using Postgrest.Models;
using System;

// TODO: DELETE THIS ENTIRE FILE
// The tables this file maps to (lesson_categories, vocabulary, vocabulary_translations,
// word_rush_prompts) are being removed from Supabase.
// Content is now handled locally via LessonsData.json and LuminangPhrases.json.
namespace Luminang.Database
{
    [Table("languages")]
    public class LanguageModel : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("code")]
        public string Code { get; set; }
    }

    [Table("lesson_categories")]
    public class LessonCategoryModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }

    [Table("vocabulary")]
    public class VocabularyModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("category_id")]
        public string CategoryId { get; set; }

        [Column("english_term")]
        public string EnglishTerm { get; set; }

        [Column("meaning_en")]
        public string MeaningEn { get; set; }

        [Column("usage_en")]
        public string UsageEn { get; set; }

        [Column("icon_url")]
        public string IconUrl { get; set; }

        [Column("illustration_url")]
        public string IllustrationUrl { get; set; }
    }

    [Table("vocabulary_translations")]
    public class VocabularyTranslationModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("vocabulary_id")]
        public string VocabularyId { get; set; }

        [Column("language_id")]
        public int LanguageId { get; set; }

        [Column("translated_text")]
        public string TranslatedText { get; set; }

        [Column("audio_url")]
        public string AudioUrl { get; set; }

        // We can link the Vocabulary base data here if needed
        [Reference(typeof(VocabularyModel))]
        public VocabularyModel Vocabulary { get; set; }
    }

    [Table("word_rush_prompts")]
    public class WordRushPromptModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("vocabulary_translation_id")]
        public string VocabularyTranslationId { get; set; }

        [Column("clue_text")]
        public string ClueText { get; set; }

        [Column("happy_feedback")]
        public string HappyFeedback { get; set; }

        [Column("confused_feedback")]
        public string ConfusedFeedback { get; set; }

        [Column("idle_image_url")]
        public string IdleImageUrl { get; set; }

        [Column("happy_image_url")]
        public string HappyImageUrl { get; set; }

        [Column("confused_image_url")]
        public string ConfusedImageUrl { get; set; }

        [Reference(typeof(VocabularyTranslationModel))]
        public VocabularyTranslationModel Translation { get; set; }
    }
}

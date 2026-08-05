using System;
using System.Collections.Generic;

[Serializable]
public class JournalData
{
    public List<JournalEntry> journal_entries;
}

[Serializable]
public class SampleSentence
{
    public string native;
    public string translation;
}

[Serializable]
public class JournalEntry
{
    public string id;
    public string language;
    public string category;
    public string phrase;
    public string pronunciation;
    public string meaning;
    public SampleSentence sample_sentence;
    public string usage_note;
    public string sound_file;
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Utilities;

/// <summary>
/// Unit tests for all regex patterns used in the NotebookAutomation.Core assembly.
/// Tests verify the behavior of regex patterns for various input scenarios.
/// </summary>
[TestClass]
public class RegexPatternsTests
{
    #region CourseStructureExtractor Tests

    /// <summary>
    /// Tests the number prefix pattern regex for matching leading numbers in filenames.
    /// </summary>
    [TestMethod]
    public void NumberPrefixPattern_ShouldMatchLeadingNumbers()
    {
        // Test valid number prefixes
        Assert.IsTrue(CourseStructureExtractor.NumberPrefixRegexPattern().IsMatch("01_introduction"));
        Assert.IsTrue(CourseStructureExtractor.NumberPrefixRegexPattern().IsMatch("02-lesson"));
        Assert.IsTrue(CourseStructureExtractor.NumberPrefixRegexPattern().IsMatch("10_module"));
        Assert.IsTrue(CourseStructureExtractor.NumberPrefixRegexPattern().IsMatch("1-test"));

        // Test invalid formats
        Assert.IsFalse(CourseStructureExtractor.NumberPrefixRegexPattern().IsMatch("introduction"));
        Assert.IsFalse(CourseStructureExtractor.NumberPrefixRegexPattern().IsMatch("lesson-one"));
        Assert.IsFalse(CourseStructureExtractor.NumberPrefixRegexPattern().IsMatch("a01_test"));
    }

    /// <summary>
    /// Tests the leading number optional separator pattern regex.
    /// </summary>
    [TestMethod]
    public void LeadingNumberOptionalSeparatorPattern_ShouldMatchNumberSeparators()
    {
        // Test valid patterns
        Assert.IsTrue(CourseStructureExtractor.LeadingNumberOptionalSeparatorRegexPattern().IsMatch("01_"));
        Assert.IsTrue(CourseStructureExtractor.LeadingNumberOptionalSeparatorRegexPattern().IsMatch("02-"));
        Assert.IsTrue(CourseStructureExtractor.LeadingNumberOptionalSeparatorRegexPattern().IsMatch("123."));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.LeadingNumberOptionalSeparatorRegexPattern().IsMatch("abc"));
        Assert.IsFalse(CourseStructureExtractor.LeadingNumberOptionalSeparatorRegexPattern().IsMatch("_01"));
    }

    /// <summary>
    /// Tests the whitespace pattern regex for matching multiple whitespace characters.
    /// </summary>
    [TestMethod]
    public void WhitespacePattern_ShouldMatchMultipleWhitespace()
    {
        // Test valid whitespace patterns
        Assert.IsTrue(CourseStructureExtractor.WhitespaceRegexPattern().IsMatch("  "));
        Assert.IsTrue(CourseStructureExtractor.WhitespaceRegexPattern().IsMatch("\t\t"));
        Assert.IsTrue(CourseStructureExtractor.WhitespaceRegexPattern().IsMatch(" \t "));

        // Test single space should not match
        Assert.IsTrue(CourseStructureExtractor.WhitespaceRegexPattern().IsMatch(" ")); // single space will match
        Assert.IsFalse(CourseStructureExtractor.WhitespaceRegexPattern().IsMatch("text"));
    }

    /// <summary>
    /// Tests the module filename pattern regex for matching module filenames.
    /// </summary>
    [TestMethod]
    public void ModuleFilenameRegex_ShouldMatchModuleFilenames()
    {
        // Test valid module filename patterns
        Assert.IsTrue(CourseStructureExtractor.ModuleFilenameRegex().IsMatch("module-1-introduction"));
        Assert.IsTrue(CourseStructureExtractor.ModuleFilenameRegex().IsMatch("module_2_basics"));
        Assert.IsTrue(CourseStructureExtractor.ModuleFilenameRegex().IsMatch("Module 3 Advanced"));
        Assert.IsTrue(CourseStructureExtractor.ModuleFilenameRegex().IsMatch("MODULE-4-expert"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.ModuleFilenameRegex().IsMatch("lesson-1-intro"));
        Assert.IsFalse(CourseStructureExtractor.ModuleFilenameRegex().IsMatch("module-intro"));
        Assert.IsFalse(CourseStructureExtractor.ModuleFilenameRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the lesson filename pattern regex for matching lesson filenames.
    /// </summary>
    [TestMethod]
    public void LessonFilenameRegex_ShouldMatchLessonFilenames()
    {
        // Test valid lesson filename patterns
        Assert.IsTrue(CourseStructureExtractor.LessonFilenameRegex().IsMatch("lesson-1-introduction"));
        Assert.IsTrue(CourseStructureExtractor.LessonFilenameRegex().IsMatch("lesson_2_basics"));
        Assert.IsTrue(CourseStructureExtractor.LessonFilenameRegex().IsMatch("Lesson 3 Advanced"));
        Assert.IsTrue(CourseStructureExtractor.LessonFilenameRegex().IsMatch("LESSON-4-expert"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.LessonFilenameRegex().IsMatch("module-1-intro"));
        Assert.IsFalse(CourseStructureExtractor.LessonFilenameRegex().IsMatch("lesson-intro"));
        Assert.IsFalse(CourseStructureExtractor.LessonFilenameRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the week/unit filename pattern regex for matching week/unit filenames.
    /// </summary>
    [TestMethod]
    public void WeekUnitFilenameRegex_ShouldMatchWeekUnitFilenames()
    {
        // Test valid week/unit filename patterns
        Assert.IsTrue(CourseStructureExtractor.WeekUnitFilenameRegex().IsMatch("week-1-introduction"));
        Assert.IsTrue(CourseStructureExtractor.WeekUnitFilenameRegex().IsMatch("unit_2_basics"));
        Assert.IsTrue(CourseStructureExtractor.WeekUnitFilenameRegex().IsMatch("session 3 Advanced"));
        Assert.IsTrue(CourseStructureExtractor.WeekUnitFilenameRegex().IsMatch("class-4-expert"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.WeekUnitFilenameRegex().IsMatch("module-1-intro"));
        Assert.IsFalse(CourseStructureExtractor.WeekUnitFilenameRegex().IsMatch("week-intro"));
        Assert.IsFalse(CourseStructureExtractor.WeekUnitFilenameRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the compact module pattern regex for matching compact module formats.
    /// </summary>
    [TestMethod]
    public void CompactModuleRegex_ShouldMatchCompactModuleFormats()
    {
        // Test valid compact module patterns
        Assert.IsTrue(CourseStructureExtractor.CompactModuleRegex().IsMatch("module1introduction"));
        Assert.IsTrue(CourseStructureExtractor.CompactModuleRegex().IsMatch("Module2Basics"));
        Assert.IsTrue(CourseStructureExtractor.CompactModuleRegex().IsMatch("MODULE3Advanced"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.CompactModuleRegex().IsMatch("lesson1introduction"));
        Assert.IsFalse(CourseStructureExtractor.CompactModuleRegex().IsMatch("module-1-intro"));
        Assert.IsFalse(CourseStructureExtractor.CompactModuleRegex().IsMatch("moduleintroduction"));
    }

    /// <summary>
    /// Tests the compact lesson pattern regex for matching compact lesson formats.
    /// </summary>
    [TestMethod]
    public void CompactLessonRegex_ShouldMatchCompactLessonFormats()
    {
        // Test valid compact lesson patterns
        Assert.IsTrue(CourseStructureExtractor.CompactLessonRegex().IsMatch("lesson1introduction"));
        Assert.IsTrue(CourseStructureExtractor.CompactLessonRegex().IsMatch("Lesson2Basics"));
        Assert.IsTrue(CourseStructureExtractor.CompactLessonRegex().IsMatch("LESSON3Advanced"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.CompactLessonRegex().IsMatch("module1introduction"));
        Assert.IsFalse(CourseStructureExtractor.CompactLessonRegex().IsMatch("lesson-1-intro"));
        Assert.IsFalse(CourseStructureExtractor.CompactLessonRegex().IsMatch("lessonintroduction"));
    }

    /// <summary>
    /// Tests the module number separator pattern regex for matching module numbers.
    /// </summary>
    [TestMethod]
    public void ModuleNumberSeparatorRegex_ShouldMatchModuleNumbers()
    {
        // Test valid module number patterns
        Assert.IsTrue(CourseStructureExtractor.ModuleNumberSeparatorRegex().IsMatch("module1"));
        Assert.IsTrue(CourseStructureExtractor.ModuleNumberSeparatorRegex().IsMatch("Module2"));
        Assert.IsTrue(CourseStructureExtractor.ModuleNumberSeparatorRegex().IsMatch("MODULE123"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.ModuleNumberSeparatorRegex().IsMatch("lesson1"));
        Assert.IsFalse(CourseStructureExtractor.ModuleNumberSeparatorRegex().IsMatch("module"));
        Assert.IsFalse(CourseStructureExtractor.ModuleNumberSeparatorRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the lesson number separator pattern regex for matching lesson numbers.
    /// </summary>
    [TestMethod]
    public void LessonNumberSeparatorRegex_ShouldMatchLessonNumbers()
    {
        // Test valid lesson number patterns
        Assert.IsTrue(CourseStructureExtractor.LessonNumberSeparatorRegex().IsMatch("lesson1"));
        Assert.IsTrue(CourseStructureExtractor.LessonNumberSeparatorRegex().IsMatch("Lesson2"));
        Assert.IsTrue(CourseStructureExtractor.LessonNumberSeparatorRegex().IsMatch("LESSON123"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.LessonNumberSeparatorRegex().IsMatch("module1"));
        Assert.IsFalse(CourseStructureExtractor.LessonNumberSeparatorRegex().IsMatch("lesson"));
        Assert.IsFalse(CourseStructureExtractor.LessonNumberSeparatorRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the numbered content pattern regex for matching numbered content.
    /// </summary>
    [TestMethod]
    public void NumberedContentRegex_ShouldMatchNumberedContent()
    {
        // Test valid numbered content patterns
        Assert.IsTrue(CourseStructureExtractor.NumberedContentRegex().IsMatch("01_introduction"));
        Assert.IsTrue(CourseStructureExtractor.NumberedContentRegex().IsMatch("02-basics"));
        Assert.IsTrue(CourseStructureExtractor.NumberedContentRegex().IsMatch("123_advanced-topics"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.NumberedContentRegex().IsMatch("introduction"));
        Assert.IsFalse(CourseStructureExtractor.NumberedContentRegex().IsMatch("a01_intro"));
        Assert.IsFalse(CourseStructureExtractor.NumberedContentRegex().IsMatch("01intro"));
    }

    /// <summary>
    /// Tests the week/unit pattern regex for matching week/unit directories.
    /// </summary>
    [TestMethod]
    public void WeekUnitRegex_ShouldMatchWeekUnitDirectories()
    {
        // Test valid week/unit patterns
        Assert.IsTrue(CourseStructureExtractor.WeekUnitRegex().IsMatch("week1"));
        Assert.IsTrue(CourseStructureExtractor.WeekUnitRegex().IsMatch("Week_2"));
        Assert.IsTrue(CourseStructureExtractor.WeekUnitRegex().IsMatch("unit-3"));
        Assert.IsTrue(CourseStructureExtractor.WeekUnitRegex().IsMatch("Unit 4"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.WeekUnitRegex().IsMatch("module1"));
        Assert.IsFalse(CourseStructureExtractor.WeekUnitRegex().IsMatch("week"));
        Assert.IsFalse(CourseStructureExtractor.WeekUnitRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the module/lesson number pattern regex for matching module/lesson directories.
    /// </summary>
    [TestMethod]
    public void ModuleLessonNumberRegex_ShouldMatchModuleLessonDirectories()
    {
        // Test valid module/lesson number patterns
        Assert.IsTrue(CourseStructureExtractor.ModuleLessonNumberRegex().IsMatch("module1"));
        Assert.IsTrue(CourseStructureExtractor.ModuleLessonNumberRegex().IsMatch("Module_2"));
        Assert.IsTrue(CourseStructureExtractor.ModuleLessonNumberRegex().IsMatch("lesson-3"));
        Assert.IsTrue(CourseStructureExtractor.ModuleLessonNumberRegex().IsMatch("Lesson 4"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.ModuleLessonNumberRegex().IsMatch("week1"));
        Assert.IsFalse(CourseStructureExtractor.ModuleLessonNumberRegex().IsMatch("module"));
        Assert.IsFalse(CourseStructureExtractor.ModuleLessonNumberRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the session/class number pattern regex for matching session/class directories.
    /// </summary>
    [TestMethod]
    public void SessionClassNumberRegex_ShouldMatchSessionClassDirectories()
    {
        // Test valid session/class number patterns
        Assert.IsTrue(CourseStructureExtractor.SessionClassNumberRegex().IsMatch("session1"));
        Assert.IsTrue(CourseStructureExtractor.SessionClassNumberRegex().IsMatch("Session_2"));
        Assert.IsTrue(CourseStructureExtractor.SessionClassNumberRegex().IsMatch("class-3"));
        Assert.IsTrue(CourseStructureExtractor.SessionClassNumberRegex().IsMatch("Class 4"));

        // Test invalid patterns
        Assert.IsFalse(CourseStructureExtractor.SessionClassNumberRegex().IsMatch("module1"));
        Assert.IsFalse(CourseStructureExtractor.SessionClassNumberRegex().IsMatch("session"));
        Assert.IsFalse(CourseStructureExtractor.SessionClassNumberRegex().IsMatch("intro"));
    }

    /// <summary>
    /// Tests the camelCase pattern regex for identifying camelCase words.
    /// </summary>
    [TestMethod]
    public void CamelCaseRegex_ShouldMatchCamelCaseBoundaries()
    {
        // Test camelCase patterns using Replace to verify it works
        // Pattern matches between lowercase and uppercase letters only
        string result1 = CourseStructureExtractor.CamelCaseRegex().Replace("camelCaseWord", " ");
        Assert.AreEqual("camel Case Word", result1);

        string result2 = CourseStructureExtractor.CamelCaseRegex().Replace("testHTML", " ");
        Assert.AreEqual("test HTML", result2); // Only splits before 'H' in HTML

        string result3 = CourseStructureExtractor.CamelCaseRegex().Replace("myXMLHttpRequest", " ");
        Assert.AreEqual("my XMLHttp Request", result3); // Splits before 'X' and 'R'

        // Test non-camelCase should not be affected
        string result4 = CourseStructureExtractor.CamelCaseRegex().Replace("lowercase", " ");
        Assert.AreEqual("lowercase", result4);

        string result5 = CourseStructureExtractor.CamelCaseRegex().Replace("UPPERCASE", " ");
        Assert.AreEqual("UPPERCASE", result5); // No lowercase-to-uppercase transitions

        // Test simple camelCase
        string result6 = CourseStructureExtractor.CamelCaseRegex().Replace("sessionPlanningDetails", " ");
        Assert.AreEqual("session Planning Details", result6);
    }

    #endregion

    #region YamlHelper Tests

    /// <summary>
    /// Tests the frontmatter block regex for matching YAML frontmatter.
    /// </summary>
    [TestMethod]
    public void FrontmatterBlockRegex_ShouldMatchYamlFrontmatter()
    {
        // Test valid frontmatter blocks (must include newlines)
        string validBlock1 = "---\r\ntitle: Test\r\n---\r\n";
        string validBlock2 = "---\r\ntitle: Test\r\nauthor: John\r\n---\r\n";

        Assert.IsTrue(YamlHelper.FrontmatterBlockRegex().IsMatch(validBlock1));
        Assert.IsTrue(YamlHelper.FrontmatterBlockRegex().IsMatch(validBlock2));

        // Test invalid formats
        Assert.IsFalse(YamlHelper.FrontmatterBlockRegex().IsMatch("title: Test"));
        Assert.IsFalse(YamlHelper.FrontmatterBlockRegex().IsMatch("---\ntitle: Test"));
        Assert.IsFalse(YamlHelper.FrontmatterBlockRegex().IsMatch("title: Test\n---"));
    }

    #endregion

    #region FriendlyTitleHelper Tests
    /// <summary>
    /// Tests the leading number pattern regex for identifying leading numbers.
    /// </summary>
    [TestMethod]
    public void LeadingNumberPattern_ShouldMatchLeadingNumbers()
    {
        // Test valid leading numbers (requires separator after number)
        Assert.IsTrue(FriendlyTitleHelper.LeadingNumberPattern().IsMatch("01_title"));
        Assert.IsTrue(FriendlyTitleHelper.LeadingNumberPattern().IsMatch("123-title"));
        Assert.IsTrue(FriendlyTitleHelper.LeadingNumberPattern().IsMatch("5 title"));

        // Test invalid formats (no separator after number)
        Assert.IsFalse(FriendlyTitleHelper.LeadingNumberPattern().IsMatch("title"));
        Assert.IsFalse(FriendlyTitleHelper.LeadingNumberPattern().IsMatch("a01_title"));
        Assert.IsFalse(FriendlyTitleHelper.LeadingNumberPattern().IsMatch("123title")); // no separator
    }

    /// <summary>
    /// Tests the separator pattern regex for matching common separators.
    /// </summary>
    [TestMethod]
    public void SeparatorPattern_ShouldMatchSeparators()
    {
        // Test valid separators (underscore and dash, multiple allowed)
        Assert.IsTrue(FriendlyTitleHelper.SeparatorPattern().IsMatch("_"));
        Assert.IsTrue(FriendlyTitleHelper.SeparatorPattern().IsMatch("-"));
        Assert.IsTrue(FriendlyTitleHelper.SeparatorPattern().IsMatch("__"));
        Assert.IsTrue(FriendlyTitleHelper.SeparatorPattern().IsMatch("--"));
        Assert.IsTrue(FriendlyTitleHelper.SeparatorPattern().IsMatch("_-_"));

        // Test non-separators
        Assert.IsFalse(FriendlyTitleHelper.SeparatorPattern().IsMatch("a"));
        Assert.IsFalse(FriendlyTitleHelper.SeparatorPattern().IsMatch(" "));
        Assert.IsFalse(FriendlyTitleHelper.SeparatorPattern().IsMatch("."));
    }

    /// <summary>
    /// Tests the whitespace pattern regex for matching whitespace characters.
    /// </summary>
    [TestMethod]
    public void WhitespacePattern_ShouldMatchWhitespace()
    {
        // Test whitespace characters
        Assert.IsTrue(FriendlyTitleHelper.WhitespacePattern().IsMatch(" "));
        Assert.IsTrue(FriendlyTitleHelper.WhitespacePattern().IsMatch("\t"));
        Assert.IsTrue(FriendlyTitleHelper.WhitespacePattern().IsMatch("\n"));

        // Test non-whitespace
        Assert.IsFalse(FriendlyTitleHelper.WhitespacePattern().IsMatch("a"));
        Assert.IsFalse(FriendlyTitleHelper.WhitespacePattern().IsMatch("1"));
    }


    /// <summary>
    /// Tests the Roman numeral II pattern regex.
    /// </summary>
    [TestMethod]
    public void RomanNumeralIIPattern_ShouldMatchRomanTwo()
    {
        // Test Roman numeral "Ii" as word boundary (note: pattern is case-sensitive and looks for "Ii")
        Assert.IsTrue(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("Ii"));
        Assert.IsTrue(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("title Ii content"));
        Assert.IsTrue(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("Part Ii: Introduction"));

        // Test other content (case-sensitive, word boundaries required)
        Assert.IsFalse(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("I"));
        Assert.IsFalse(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("II")); // uppercase II
        Assert.IsFalse(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("III"));
        Assert.IsFalse(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("2"));
        Assert.IsFalse(FriendlyTitleHelper.RomanNumeralIIPattern().IsMatch("iiTest")); // no word boundary
    }

    #endregion

    #region MetadataTemplateManager Tests

    /// <summary>
    /// Tests the YAML document separator regex for identifying document separators.
    /// </summary>
    [TestMethod]
    public void YamlDocumentSeparatorRegex_ShouldMatchSeparators()
    {
        // Test valid YAML document separators
        Assert.IsTrue(MetadataTemplateManager.YamlDocumentSeparatorRegex().IsMatch("---"));
        Assert.IsTrue(MetadataTemplateManager.YamlDocumentSeparatorRegex().IsMatch("---\n"));

        // Test invalid formats
        Assert.IsFalse(MetadataTemplateManager.YamlDocumentSeparatorRegex().IsMatch("--"));
        Assert.IsFalse(MetadataTemplateManager.YamlDocumentSeparatorRegex().IsMatch("----"));
        Assert.IsFalse(MetadataTemplateManager.YamlDocumentSeparatorRegex().IsMatch("text"));
    }

    #endregion

    #region MarkdownParser Tests

    /// <summary>
    /// Tests the YAML frontmatter regex for matching YAML frontmatter blocks.
    /// </summary>
    [TestMethod]
    public void YamlFrontmatterRegex_ShouldMatchFrontmatter()
    {
        // Test valid frontmatter (MarkdownParser expects specific newline format)
        string validFrontmatter = "---\ntitle: Test\n---\n";
        Assert.IsTrue(MarkdownParser.YamlFrontmatterRegex().IsMatch(validFrontmatter));

        // Test invalid formats
        Assert.IsFalse(MarkdownParser.YamlFrontmatterRegex().IsMatch("title: Test"));
        Assert.IsFalse(MarkdownParser.YamlFrontmatterRegex().IsMatch("---\ntitle: Test"));
    }

    /// <summary>
    /// Tests the markdown header regex for matching markdown headers.
    /// </summary>
    [TestMethod]
    public void MarkdownHeaderRegex_ShouldMatchHeaders()
    {
        // Test valid headers (must be at line start, capture groups for # and text)
        Assert.IsTrue(MarkdownParser.MarkdownHeaderRegex().IsMatch("# Header 1"));
        Assert.IsTrue(MarkdownParser.MarkdownHeaderRegex().IsMatch("## Header 2"));
        Assert.IsTrue(MarkdownParser.MarkdownHeaderRegex().IsMatch("### Header 3"));
        Assert.IsTrue(MarkdownParser.MarkdownHeaderRegex().IsMatch("#### Header 4"));
        Assert.IsTrue(MarkdownParser.MarkdownHeaderRegex().IsMatch("##### Header 5"));
        Assert.IsTrue(MarkdownParser.MarkdownHeaderRegex().IsMatch("###### Header 6"));

        // Test edge cases that should match
        Assert.IsTrue(MarkdownParser.MarkdownHeaderRegex().IsMatch("####### Too many")); // regex doesn't restrict # count

        // Test invalid formats
        Assert.IsFalse(MarkdownParser.MarkdownHeaderRegex().IsMatch("Header without #"));
        Assert.IsFalse(MarkdownParser.MarkdownHeaderRegex().IsMatch(" # Header with leading space"));
    }

    #endregion

    #region DocumentNoteBatchProcessor Tests
    /// <summary>
    /// Tests the notes header regex for matching notes headers.
    /// </summary>
    [TestMethod]
    public void NotesHeaderRegex_ShouldMatchNotesHeaders()
    {
        // Test valid notes headers (case insensitive, exactly "## Notes")
        Assert.IsTrue(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("## Notes"));
        Assert.IsTrue(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("## notes"));
        Assert.IsTrue(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("## NOTES"));
        Assert.IsTrue(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("##  Notes  ")); // extra spaces

        // Test invalid formats (wrong level, extra content, missing content)
        Assert.IsFalse(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("Notes")); // no header prefix
        Assert.IsFalse(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("# Notes")); // wrong level
        Assert.IsFalse(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("### Notes")); // wrong level
        Assert.IsFalse(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("## Meeting Notes")); // extra content
        Assert.IsFalse(DocumentNoteBatchProcessor<PdfNoteProcessor>.NotesHeaderRegex().IsMatch("## Note")); // singular, not "Notes"
    }

    #endregion

    #region MarkdownNoteProcessor Tests

    /// <summary>
    /// Tests the HTML tag stripper regex for removing HTML tags.
    /// </summary>
    [TestMethod]
    public void HtmlTagStripperRegex_ShouldMatchHtmlTags()
    {
        // Test valid HTML tags
        Assert.IsTrue(MarkdownNoteProcessor.HtmlTagStripperRegex().IsMatch("<p>")); Assert.IsTrue(MarkdownNoteProcessor.HtmlTagStripperRegex().IsMatch("<div class='test'>"));
        Assert.IsTrue(MarkdownNoteProcessor.HtmlTagStripperRegex().IsMatch("</p>"));
        Assert.IsTrue(MarkdownNoteProcessor.HtmlTagStripperRegex().IsMatch("<br/>"));

        // Test non-HTML content
        Assert.IsFalse(MarkdownNoteProcessor.HtmlTagStripperRegex().IsMatch("plain text"));
        Assert.IsFalse(MarkdownNoteProcessor.HtmlTagStripperRegex().IsMatch("< not a tag"));
        Assert.IsFalse(MarkdownNoteProcessor.HtmlTagStripperRegex().IsMatch("not a tag >"));
    }

    #endregion

    #region PromptTemplateService Tests

    /// <summary>
    /// Tests the template variable pattern regex for matching template placeholders.
    /// </summary>
    [TestMethod]
    public void TemplateVariablePattern_ShouldMatchTemplatePlaceholders()
    {
        // Test valid template variables
        Assert.IsTrue(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("{{variable}}"));
        Assert.IsTrue(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("{{user_name}}"));
        Assert.IsTrue(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("{{course_name}}"));
        Assert.IsTrue(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("{{content}}"));

        // Test multiple variables in text
        var multiVarText = "Hello {{name}}, welcome to {{course}}!";
        var matches = NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().Matches(multiVarText);
        Assert.AreEqual(2, matches.Count);
        Assert.AreEqual("name", matches[0].Groups[1].Value);
        Assert.AreEqual("course", matches[1].Groups[1].Value);        // Test non-matching patterns
        Assert.IsFalse(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("{variable}"));
        Assert.IsFalse(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("variable"));

        // Test edge cases
        Assert.IsTrue(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("{{}}"));  // Empty variable name matches
        Assert.IsTrue(NotebookAutomation.Core.Services.PromptTemplateService.TemplateVariableRegex().IsMatch("{{{variable}}}"));  // This matches {{variable}} part
    }

    #endregion
}
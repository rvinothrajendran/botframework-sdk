﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace Microsoft.Botframework.LUParser.parser
{
    class LuParser
    {
        static Object ParseWithRef(string text, LuResource luResource)
        {
            if (String.IsNullOrEmpty(text))
            {
                return new LuResource(new List<Section>(), String.Empty, new List<Error>());
            }

            // TODO: bool? sectionEnabled = luResource != null ? IsSectionEnabled(luResource.Sections) : null;

            return null;
        }

        static LuResource ExtractFileContent(LUFileParser.FileContext fileContent, string content, List<Error> errors, bool? sectionEnabled)
        {
            var sections = new List<Section>();

            try
            {
                //var modelInfoSections = ExtractModelInfoSections(fileContent);
            } catch
            {

            }

            try
            {
                var isSectionEnabled = sectionEnabled == null ? IsSectionEnabled(sections) : sectionEnabled;

                var nestedIntentSections = ExtractNestedIntentSections(fileContent, content);
                foreach (var section in nestedIntentSections)
                {
                    errors.AddRange(section.Errors);
                }
                if (isSectionEnabled.HasValue ? isSectionEnabled.Value : false)
                {
                    sections.AddRange(nestedIntentSections);
                }
                else
                {
                    foreach (var section in nestedIntentSections)
                    {
                        var emptyIntentSection = new SimpleIntentSection();
                        emptyIntentSection.Name = section.Name;
                        emptyIntentSection.Id = $"{emptyIntentSection.SectionType}_{emptyIntentSection.Name}";

                        // get the end character index
                        // this is default value
                        // it will be reset in function extractSectionBody()
                        var endCharacter = section.Name.Length + 2;

                        var range = new Range { Start = section.Range.Start, End = new Position { Line = section.Range.Start.Line, Character = endCharacter } };
                        emptyIntentSection.Range = range;
                        var errorMsg = $"no utterances found for intent definition: \"# {emptyIntentSection.Name}\"";
                        var error = Diagnostic.BuildDiagnostic(
                            message: errorMsg,
                            range: emptyIntentSection.Range,
                            severity: DiagnosticSeverity.Warn
                        );

                        errors.Add(error);
                        sections.Add(emptyIntentSection);

                        foreach (var subSection in section.SimpleIntentSections)
                        {
                            sections.Add(subSection);
                            errors.AddRange(subSection.Errors);
                        }
                    }
                }
            } catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing nested intent section: {err.Message}"
                    )
                );
            }

            try
            {
                var simpleIntentSections = ExtractSimpleIntentSections(fileContent, content);
                foreach (var section in simpleIntentSections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(simpleIntentSections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing simple intent section: {err.Message}"
                    )
                );
            }

            try
            {
                var entitySections = ExtractEntitiesSections(fileContent, content);
                foreach (var section in entitySections)
                {
                    errors.AddRange(section.Errors);
                }
                sections.AddRange(entitySections);
            }
            catch (Exception err)
            {
                errors.Add(
                    Diagnostic.BuildDiagnostic(
                        message: $"Error happened when parsing entities: {err.Message}"
                    )
                );
            }

            return null;
        }

        static List<ModelInfoSection> ExtractModelInfoSections(LUFileParser.FileContext fileContext)
        {
            if (fileContext == null)
            {
                return new List<ModelInfoSection>();
            }

            var modelInfoSections = fileContext.paragraph().Select(x => x.modelInfoSection()).Where(x => x != null);

            var modelInfoSectionList = modelInfoSections.Select(x => new ModelInfoSection(x));

            return null;
        }

        static List<NestedIntentSection> ExtractNestedIntentSections(LUFileParser.FileContext fileContext, string content)
        {
            if (fileContext == null)
            {
                return new List<NestedIntentSection>();
            }

            var nestedIntentSections = fileContext.paragraph().Select(x => x.nestedIntentSection()).Where(x => x != null);
            var nestedIntentSectionsList = nestedIntentSections.Select(x => new NestedIntentSection(x, content)).ToList();

            return nestedIntentSectionsList;
        }

        static List<SimpleIntentSection> ExtractSimpleIntentSections(LUFileParser.FileContext fileContext, string content)
        {
            if (fileContext == null)
            {
                return new List<SimpleIntentSection>();
            }

            var simpleIntentSections = fileContext.paragraph().Select(x => x.simpleIntentSection()).Where(x => x != null && x.intentDefinition() != null);
            var simpleIntentSectionsList = simpleIntentSections.Select(x => new SimpleIntentSection(x, content)).ToList();

            return simpleIntentSectionsList;
        }

        static List<Entity> ExtractEntitiesSections(LUFileParser.FileContext fileContext, string content)
        {
            if (fileContext == null)
            {
                return new List<Entity>();
            }

            var entitySections = fileContext.paragraph().Select(x => x.entitySection()).Where(x => x != null && x.entityDefinition() != null);
            var entitySectionsList = entitySections.Select(x => new Entity(x)).ToList();

            return entitySectionsList;
        }


        static Object GetFileContent(string text)
        {
            var chars = new AntlrInputStream(text);
            var lexer = new LUFileLexer(chars);
            var tokens = new CommonTokenStream(lexer);
            var parser = new LUFileParser(tokens);

            var fileContent = parser.file();

            var modelInfoSectionList = fileContent.paragraph().Select(x => x.modelInfoSection());

            return null;
        }

        static bool IsSectionEnabled(List<Section> sections)
        {
            var modelInfoSections = sections.Where(s => s.SectionType == SectionType.ModelInfoSection);
            bool enableSections = false;

            if (modelInfoSections.Any())
            {
                foreach (ModelInfoSection modelInfo in modelInfoSections)
                {
                    var line = modelInfo.ModelInfo;
                    var kvPair = Regex.Split(line, @"@\b(enableSections).(.*)=").Select(item => item.Trim()).ToArray();
                    if (kvPair.Length == 4)
                    {
                        if (String.Equals(kvPair[1], "enableSections", StringComparison.InvariantCultureIgnoreCase) && String.Equals(kvPair[3], "true", StringComparison.InvariantCultureIgnoreCase))
                        {
                            enableSections = true;
                            break;
                        }
                    }
                }
            }

            // TODO: this is a mock behavior
            return enableSections;
        }
    }
}

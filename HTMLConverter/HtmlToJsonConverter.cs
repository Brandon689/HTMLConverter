using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class HtmlToJsonParser
{
    public enum ParserMode
    {
        Generic,
        Table,
        JsonLd
    }

    public enum NewLineConversion
    {
        None,
        Space,
        Comma,
        CommaSpace,
        SemiColon,
        SemiColonSpace
    }

    public class ParserOptions
    {
        public string AttributePrefix { get; set; } = "@";
        public string TextPropertyName { get; set; } = "#text";
        public NewLineConversion ValueNewLineConversion { get; set; } = NewLineConversion.None;
        public bool Indent { get; set; } = true;
        public bool UnescapeJson { get; set; } = false;
        public bool TrimInsideWords { get; set; } = false;
        public bool ConvertAllTables { get; set; } = false;
    }

    public static async Task<string> ParseHtmlToJson(string html, ParserMode mode, ParserOptions options = null)
    {
        options = options ?? new ParserOptions();

        if (options.UnescapeJson)
        {
            html = UnescapeJsonString(html);
        }

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        JToken result;

        switch (mode)
        {
            case ParserMode.Generic:
                result = ParseNodeGeneric(document.DocumentElement, options);
                break;
            case ParserMode.Table:
                result = ParseTables(document, options);
                break;
            case ParserMode.JsonLd:
                result = ParseJsonLd(document);
                break;
            default:
                throw new ArgumentException("Unsupported parser mode");
        }

        return JsonConvert.SerializeObject(result, options.Indent ? Formatting.Indented : Formatting.None);
    }

    private static JObject ParseNodeGeneric(IElement element, ParserOptions options)
    {
        var result = new JObject();

        // Add attributes
        foreach (var attr in element.Attributes)
        {
            var key = string.IsNullOrEmpty(options.AttributePrefix) ? attr.Name.ToLower() : options.AttributePrefix + attr.Name.ToLower();
            result.Add(key, attr.Value);
        }

        // Process child nodes
        var childElements = element.Children.ToList();
        var textNodes = element.ChildNodes.OfType<IText>()
            .Where(t => !string.IsNullOrWhiteSpace(t.TextContent))
            .ToList();

        if (childElements.Count > 0)
        {
            var groupedChildren = childElements.GroupBy(e => e.TagName.ToLower());

            foreach (var group in groupedChildren)
            {
                if (group.Count() == 1)
                {
                    result.Add(group.Key, ParseNodeGeneric(group.First(), options));
                }
                else
                {
                    result.Add(group.Key, new JArray(group.Select(e => ParseNodeGeneric(e, options))));
                }
            }
        }

        if (textNodes.Count > 0)
        {
            var combinedText = string.Join(" ", textNodes.Select(t => ProcessText(t.TextContent, options)));
            if (childElements.Count == 0)
            {
                // If there are no child elements, set the text content directly
                return new JObject { { element.TagName.ToLower(), combinedText } };
            }
            else
            {
                // If there are child elements, add the text content as a property
                result.Add(options.TextPropertyName, combinedText);
            }
        }

        return result;
    }

    private static JToken ParseTables(IDocument document, ParserOptions options)
    {
        var tables = document.QuerySelectorAll("table");
        if (!options.ConvertAllTables)
        {
            return ParseTable(tables.FirstOrDefault(), options);
        }

        var result = new JArray();
        foreach (var table in tables)
        {
            result.Add(ParseTable(table, options));
        }
        return result;
    }

    private static JArray ParseTable(IElement tableElement, ParserOptions options)
    {
        if (tableElement == null)
            return new JArray();

        var rows = tableElement.QuerySelectorAll("tr").ToList();
        if (rows.Count < 2)
            return new JArray();

        var headerCells = rows[0].QuerySelectorAll("th").Select(th => ProcessText(th.TextContent, options)).ToList();
        var result = new JArray();

        for (int i = 1; i < rows.Count; i++)
        {
            var rowData = new JObject();
            var cells = rows[i].QuerySelectorAll("td").ToList();

            for (int j = 0; j < Math.Min(headerCells.Count, cells.Count); j++)
            {
                var value = ProcessText(cells[j].TextContent, options);
                if (int.TryParse(value, out int intValue))
                {
                    rowData.Add(headerCells[j], intValue);
                }
                else
                {
                    rowData.Add(headerCells[j], value);
                }
            }

            result.Add(rowData);
        }

        return result;
    }

    private static JArray ParseJsonLd(IDocument document)
    {
        var jsonLdScripts = document.QuerySelectorAll("script[type='application/ld+json']");
        var result = new JArray();

        foreach (var script in jsonLdScripts)
        {
            try
            {
                var jsonContent = script.TextContent;
                var jsonObject = JObject.Parse(jsonContent);
                result.Add(jsonObject);
            }
            catch (JsonException)
            {
                // Skip invalid JSON
            }
        }

        return result;
    }

    private static string ProcessText(string text, ParserOptions options)
    {
        if (options.TrimInsideWords)
        {
            text = Regex.Replace(text, @"\s+", " ");
        }

        text = text.Trim();

        switch (options.ValueNewLineConversion)
        {
            case NewLineConversion.Space:
                text = Regex.Replace(text, @"\s*\n\s*", " ");
                break;
            case NewLineConversion.Comma:
                text = Regex.Replace(text, @"\s*\n\s*", ",");
                break;
            case NewLineConversion.CommaSpace:
                text = Regex.Replace(text, @"\s*\n\s*", ", ");
                break;
            case NewLineConversion.SemiColon:
                text = Regex.Replace(text, @"\s*\n\s*", ";");
                break;
            case NewLineConversion.SemiColonSpace:
                text = Regex.Replace(text, @"\s*\n\s*", "; ");
                break;
        }

        return text;
    }

    private static string UnescapeJsonString(string input)
    {
        try
        {
            return JsonConvert.DeserializeObject<string>(input);
        }
        catch
        {
            return input;
        }
    }
}
using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class HtmlToJsonParser
{
    private static readonly HashSet<string> VoidElements = new HashSet<string>
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input",
        "link", "meta", "param", "source", "track", "wbr"
    };

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
        public bool PreserveEmptyAttributes { get; set; } = false;
        public bool BooleanAttributesAsFlags { get; set; } = false;
        public bool PreserveNamespaces { get; set; } = false;
        public bool SkipClassAttributes { get; set; } = false;
        public bool SkipAllAttributes { get; set; } = false;
        public Action<string> LogAction { get; set; }
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
                result = CreateJObjectFromKeyValuePairs(ParseNodeGeneric(document.DocumentElement, options));
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

    private static JObject CreateJObjectFromKeyValuePairs(List<KeyValuePair<string, JToken>> pairs)
    {
        var jObject = new JObject();
        foreach (var pair in pairs)
        {
            jObject.Add(pair.Key, pair.Value);
        }
        return jObject;
    }

    private static List<KeyValuePair<string, JToken>> ParseNodeGeneric(IElement element, ParserOptions options)
    {
        options.LogAction?.Invoke($"Processing element: {element.TagName}");
        var result = new List<KeyValuePair<string, JToken>>();

        // Add attributes
        if (!options.SkipAllAttributes)
        {
            foreach (var attr in element.Attributes)
            {
                if (options.SkipClassAttributes && attr.Name.ToLower() == "class")
                    continue;

                if (!options.PreserveEmptyAttributes && string.IsNullOrEmpty(attr.Value))
                    continue;

                var key = string.IsNullOrEmpty(options.AttributePrefix) ? attr.Name.ToLower() : options.AttributePrefix + attr.Name.ToLower();
                var value = options.BooleanAttributesAsFlags && string.IsNullOrEmpty(attr.Value) ? true : (object)attr.Value;
                result.Add(new KeyValuePair<string, JToken>(key, JToken.FromObject(value)));
            }

            if (options.PreserveNamespaces && !string.IsNullOrEmpty(element.NamespaceUri))
            {
                result.Add(new KeyValuePair<string, JToken>("@xmlns", element.NamespaceUri));
            }
        }

        if (VoidElements.Contains(element.TagName.ToLower()))
        {
            return result;
        }

        // Handle child nodes
        var childContent = HandleChildNodes(element.ChildNodes, options);
        if (childContent != null)
        {
            // If childContent is a JArray with only one item, unwrap it
            if (childContent is JArray childArray && childArray.Count == 1)
            {
                result.Add(new KeyValuePair<string, JToken>(element.TagName.ToLower(), childArray[0]));
            }
            else
            {
                result.Add(new KeyValuePair<string, JToken>(element.TagName.ToLower(), childContent));
            }
        }

        // Handle comments
        var comments = element.ChildNodes.OfType<IComment>().ToList();
        if (comments.Any())
        {
            result.Add(new KeyValuePair<string, JToken>("comments", new JArray(comments.Select(c => c.TextContent))));
        }

        // Handle CDATA sections
        var cdataSections = element.ChildNodes
            .Where(n => n.NodeType == NodeType.CharacterData)
            .Select(n => n.TextContent)
            .ToList();
        if (cdataSections.Any())
        {
            result.Add(new KeyValuePair<string, JToken>("cdata", new JArray(cdataSections)));
        }

        return result;
    }


    private static JToken HandleChildNodes(INodeList childNodes, ParserOptions options)
    {
        var textNodes = childNodes.OfType<IText>().Where(t => !string.IsNullOrWhiteSpace(t.TextContent)).ToList();
        var elementNodes = childNodes.OfType<IElement>().ToList();

        if (!elementNodes.Any() && textNodes.Count == 1)
        {
            return ProcessText(textNodes[0].TextContent, options);
        }

        var result = new JArray();
        foreach (var node in childNodes)
        {
            if (node is IText textNode && !string.IsNullOrWhiteSpace(textNode.TextContent))
            {
                result.Add(ProcessText(textNode.TextContent, options));
            }
            else if (node is IElement elementNode)
            {
                result.Add(CreateJObjectFromKeyValuePairs(ParseNodeGeneric(elementNode, options)));
            }
        }

        return result.Count > 0 ? result : null;
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
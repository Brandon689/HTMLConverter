class Program
{
    static async Task Main(string[] args)
    {
        await TestGenericMode();
        await TestTableMode();
        await TestJsonLdMode();
        await TestNewLineConversionOptions();
        await TestAttributePrefixOptions();
        await TestTextPropertyNameOptions();
        await TestIndentationOptions();
        await TestUnescapeJsonOption();
        await TestTrimInsideWordsOption();
        await TestConvertAllTablesOption();
    }

    static async Task TestGenericMode()
    {
        Console.WriteLine("Testing Generic Mode:");
        string html = @"
        <html>
        <body>
        <div id='content'>
            <h1>Hello, World!</h1>
            <p>This is a test.</p>
        </div>
        </body>
        </html>";

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Generic);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestTableMode()
    {
        Console.WriteLine("Testing Table Mode:");
        string html = @"
        <html>
        <body>
        <table>
            <tr><th>Name</th><th>Age</th></tr>
            <tr><td>John</td><td>30</td></tr>
            <tr><td>Jane</td><td>25</td></tr>
        </table>
        </body>
        </html>";

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Table);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestJsonLdMode()
    {
        Console.WriteLine("Testing JSON-LD Mode:");
        string html = @"
        <html>
        <body>
        <script type='application/ld+json'>
        {
            ""@context"": ""https://schema.org"",
            ""@type"": ""Person"",
            ""name"": ""John Doe"",
            ""age"": 30
        }
        </script>
        </body>
        </html>";

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.JsonLd);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestNewLineConversionOptions()
    {
        Console.WriteLine("Testing New Line Conversion Options:");
        string html = @"
        <html>
        <body>
        <p>Line 1
        Line 2
        Line 3</p>
        </body>
        </html>";

        var options = new HtmlToJsonParser.ParserOptions
        {
            ValueNewLineConversion = HtmlToJsonParser.NewLineConversion.CommaSpace
        };

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Generic, options);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestAttributePrefixOptions()
    {
        Console.WriteLine("Testing Attribute Prefix Options:");
        string html = @"
        <html>
        <body>
        <div id='content' class='main'>
            <p>Test</p>
        </div>
        </body>
        </html>";

        var options = new HtmlToJsonParser.ParserOptions
        {
            AttributePrefix = ""
        };

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Generic, options);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestTextPropertyNameOptions()
    {
        Console.WriteLine("Testing Text Property Name Options:");
        string html = @"
        <html>
        <body>
        <div>
            Text before
            <p>Paragraph</p>
            Text after
        </div>
        </body>
        </html>";

        var options = new HtmlToJsonParser.ParserOptions
        {
            TextPropertyName = "content"
        };

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Generic, options);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestIndentationOptions()
    {
        Console.WriteLine("Testing Indentation Options:");
        string html = @"
        <html>
        <body>
        <div>
            <p>Test</p>
        </div>
        </body>
        </html>";

        var options = new HtmlToJsonParser.ParserOptions
        {
            Indent = false
        };

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Generic, options);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestUnescapeJsonOption()
    {
        Console.WriteLine("Testing Unescape JSON Option:");
        string html = @"{""html"":""<div>Test</div>""}";

        var options = new HtmlToJsonParser.ParserOptions
        {
            UnescapeJson = true
        };

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Generic, options);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestTrimInsideWordsOption()
    {
        Console.WriteLine("Testing Trim Inside Words Option:");
        string html = @"
        <html>
        <body>
        <p>This   is   a   test</p>
        </body>
        </html>";

        var options = new HtmlToJsonParser.ParserOptions
        {
            TrimInsideWords = true
        };

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Generic, options);
        Console.WriteLine(result);
        Console.WriteLine();
    }

    static async Task TestConvertAllTablesOption()
    {
        Console.WriteLine("Testing Convert All Tables Option:");
        string html = @"
        <html>
        <body>
        <table>
            <tr><th>Name</th><th>Age</th></tr>
            <tr><td>John</td><td>30</td></tr>
        </table>
        <table>
            <tr><th>City</th><th>Country</th></tr>
            <tr><td>New York</td><td>USA</td></tr>
        </table>
        </body>
        </html>";

        var options = new HtmlToJsonParser.ParserOptions
        {
            ConvertAllTables = true
        };

        var result = await HtmlToJsonParser.ParseHtmlToJson(html, HtmlToJsonParser.ParserMode.Table, options);
        Console.WriteLine(result);
        Console.WriteLine();
    }
}

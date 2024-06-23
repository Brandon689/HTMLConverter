using Newtonsoft.Json.Linq;
using System.Text;
using System.Web;

public class JsonToHtmlConverter
{
    public static string ConvertJsonToHtml(string json)
    {
        var jObject = JObject.Parse(json);
        return ConvertJObjectToHtml(jObject);
    }

    private static string ConvertJObjectToHtml(JObject obj)
    {
        var sb = new StringBuilder();

        foreach (var prop in obj.Properties())
        {
            if (prop.Name != "comments") // Skip the comments property
            {
                sb.Append(ConvertPropertyToHtml(prop.Name, prop.Value));
            }
        }

        return sb.ToString();
    }

    private static string ConvertPropertyToHtml(string name, JToken value)
    {
        if (name.StartsWith("@") || name == "comments")
        {
            return ""; // Attributes and comments are handled separately
        }

        var sb = new StringBuilder();
        sb.Append($"<{name}");

        // Handle attributes
        if (value is JObject childObj)
        {
            foreach (var attr in childObj.Properties().Where(p => p.Name.StartsWith("@")))
            {
                sb.Append($" {attr.Name.Substring(1)}=\"{HttpUtility.HtmlAttributeEncode(attr.Value.ToString())}\"");
            }
        }

        // Special handling for img elements
        if (name.ToLower() == "img")
        {
            // Ensure src attribute is present
            if (value is JObject imgObj && imgObj.ContainsKey("@src"))
            {
                sb.Append($" src=\"{HttpUtility.HtmlAttributeEncode(imgObj["@src"].ToString())}\"");
            }
            sb.Append(">");
            return sb.ToString(); // Return early for img elements
        }

        sb.Append(">");

        // Handle content
        if (value is JValue jValue)
        {
            sb.Append(HttpUtility.HtmlEncode(jValue.ToString()));
        }
        else if (value is JObject jObject)
        {
            // Handle text content
            if (jObject.ContainsKey("#text"))
            {
                sb.Append(HttpUtility.HtmlEncode(jObject["#text"].ToString()));
            }

            // Handle child elements
            foreach (var prop in jObject.Properties().Where(p => !p.Name.StartsWith("@") && p.Name != "#text" && p.Name != "comments"))
            {
                sb.Append(ConvertPropertyToHtml(prop.Name, prop.Value));
            }
        }
        else if (value is JArray jArray)
        {
            foreach (var item in jArray)
            {
                if (item is JObject itemObj)
                {
                    sb.Append(ConvertJObjectToHtml(itemObj));
                }
                else if (item is JValue itemValue)
                {
                    sb.Append(HttpUtility.HtmlEncode(itemValue.ToString()));
                }
            }
        }

        // Handle void elements
        if (!IsVoidElement(name))
        {
            sb.Append($"</{name}>");
        }

        return sb.ToString();
    }


    private static bool IsVoidElement(string tagName)
    {
        var voidElements = new[] { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };
        return voidElements.Contains(tagName.ToLower());
    }
}

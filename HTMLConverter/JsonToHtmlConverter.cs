using Newtonsoft.Json.Linq;
using System.Text;

public class JsonToHtmlConverter
{
    public static string ConvertJsonToHtml(string json)
    {
        var jObject = JObject.Parse(json);
        return ConvertObjectToHtml(jObject);
    }

    private static string ConvertObjectToHtml(JObject obj)
    {
        var sb = new StringBuilder();

        foreach (var prop in obj.Properties())
        {
            sb.Append($"<{prop.Name}");

            if (prop.Value is JObject childObj)
            {
                // Add attributes
                foreach (var childProp in childObj.Properties())
                {
                    if (childProp.Name.StartsWith("@"))
                    {
                        sb.Append($" {childProp.Name.Substring(1)}=\"{childProp.Value}\"");
                    }
                }

                sb.Append(">");

                // Add content
                foreach (var childProp in childObj.Properties())
                {
                    if (!childProp.Name.StartsWith("@"))
                    {
                        sb.Append(ConvertTokenToHtml(childProp.Value));
                    }
                }

                sb.Append($"</{prop.Name}>");
            }
            else
            {
                sb.Append(">");
                sb.Append(ConvertTokenToHtml(prop.Value));
                sb.Append($"</{prop.Name}>");
            }
        }

        return sb.ToString();
    }

    private static string ConvertTokenToHtml(JToken token)
    {
        if (token is JObject obj)
        {
            return ConvertObjectToHtml(obj);
        }
        else if (token is JArray arr)
        {
            return ConvertArrayToHtml(arr);
        }
        else
        {
            return token.ToString();
        }
    }

    private static string ConvertArrayToHtml(JArray arr)
    {
        var sb = new StringBuilder();
        foreach (var item in arr)
        {
            sb.Append(ConvertTokenToHtml(item));
        }
        return sb.ToString();
    }
}

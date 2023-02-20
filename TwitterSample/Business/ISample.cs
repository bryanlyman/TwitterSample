using System.Text.RegularExpressions;

namespace TwitterSample
{
    public interface ISample
    {
        //re-usable compiled regex for matching hashtags
        public static Regex RXHashTags = new Regex(@"(^|\B)#(?!([x]{1}|[0-9_])+[^; ]{1}\b)(([a-zA-Z0-9_]){1,30})(\b|\r)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        int SubItems { get; set; }

        List<string> HashTags { get; set; }

    }
}

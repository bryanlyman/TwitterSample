using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TwitterSample.Models
{
	public class RedditSample : ISample
	{
		private static Regex _rxHashTags = new Regex("#[^;]+$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

		public RedditSample() { }

		public RedditSample(JsonNode child)
		{
			var subreddit = child?["data"]?["subreddit_name_prefixed"]?.ToString() ?? "";
			if (subreddit != string.Empty) HashTags.Add(subreddit);

			var selfText = child?["data"]?["selftext"]?.ToString() ?? "";
			if (selfText != string.Empty)
			{
				var matches = _rxHashTags.Matches(selfText);
				if (matches.Count > 0) foreach (Match match in matches) HashTags.Add(match.Value);
			}

			JsonArray parentReddits = (child?["data"]?["crosspost_parent_list"] ?? new JsonArray()) as JsonArray;
			foreach (var post in parentReddits)
			{
				SubItems++;

				var cSubreddit = post?["subreddit_name_prefixed"]?.ToString() ?? "";
				if (cSubreddit != string.Empty) HashTags.Add(cSubreddit);

				var cSelfText = post?["selftext"]?.ToString() ?? "";
				if (cSelfText != string.Empty)
				{
					var matches = _rxHashTags.Matches(cSelfText);
					if (matches.Count > 0) foreach (Match match in matches) HashTags.Add(match.Value);
				}
			}
		}

		public int SubItems { get; set; }

		public List<string> HashTags { get; set; } = new List<string>();

	}
}

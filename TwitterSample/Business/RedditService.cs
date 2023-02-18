using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TwitterSample.Models;

namespace TwitterSample
{

	public static class ServiceExtensions
	{
		public static IApplicationBuilder UseRedditSampleStream(this IApplicationBuilder app)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			app.UseWebSockets();

			ISampleService service = app.ApplicationServices.GetService<ISampleService<RedditService>>();
			if (service == null) service = ServiceFactory.GetService(eServiceType.Reddit);

			app.Use(service.SampleStreamMiddleware);

			return app;
		}
	}

	public class RedditService : HttpClient, ISampleService<RedditService>
	{

		private string _sampleSocketPath = "/redditstream";
		private string _accessToken = null;

		private string _baseURL = "https://www.reddit.com";
		private readonly string _redditUsername = "BryanLyman"; //TODO: on release, use secure config or assembly values
		private readonly string _redditPassword = "8Ctw!FwGFQG9XUp"; //TODO: on release, use secure config or assembly values
		private readonly string _appId = "p5uHV6J1hZIzH8dXT8i7wQ";
		private readonly string _appSecret = "428K0A2fu7GWo15GR6v-NXSgbKTmGA"; //TODO: on release, use secure config or assembly values

		public eServiceType ServiceType => eServiceType.Reddit;

		public RedditService() : base()
		{
		}

		private class AuthToken
		{
			[JsonPropertyName("access_token")]
			public string Access_Token { get; set; }

			[JsonPropertyName("token_type")]
			public string Token_Type { get; set; }

			[JsonPropertyName("expires_in")]
			public int Expires_In { get; set; }

			[JsonPropertyName("scope")]
			public string Scope { get; set; }
		}


		private void AuthorizeRequest(HttpRequestMessage request)
		{
			var productHeader = $"{_appId}/1.0 by {_redditUsername?.ToString() ?? ""}";

			//oauth
			if (_accessToken == null)
			{
				var values = new List<KeyValuePair<string, string>> {
					new KeyValuePair<string, string>("grant_type", "password"),
					new KeyValuePair<string, string>("username", _redditUsername.ToString() ?? ""),
					new KeyValuePair<string, string>("password", _redditPassword.ToString() ?? "")
				};

				var authRequest = new HttpRequestMessage(HttpMethod.Post, _baseURL + "/api/v1/access_token")
				{
					Content = new FormUrlEncodedContent(values)
				};
				//authRequest.Headers.TryAddWithoutValidation("Content-Type", "application/json");
				authRequest.Headers.Authorization = new BasicAuthenticationHeaderValue(_appId, _appSecret);
				authRequest.Headers.TryAddWithoutValidation("User-Agent", productHeader);

				var authResponse = base.Send(authRequest);
				if (!authResponse.IsSuccessStatusCode)
					throw new HttpRequestException("Unable To Authorize Reddit App", null, authResponse.StatusCode);

				var contentTask = authResponse.Content.ReadAsStringAsync();
				contentTask.Wait();
				var content = contentTask.Result;
				if (string.IsNullOrEmpty(content))
					throw new HttpRequestException("Reddit Authorization Content Empty");

				var token = JsonSerializer.Deserialize<AuthToken>(content);
				_accessToken = token?.Access_Token;
				_baseURL = "https://oauth.reddit.com";
			}


			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
			request.Headers.TryAddWithoutValidation("User-Agent", productHeader);
			var pathAndQuery = request.RequestUri.PathAndQuery;
			request.RequestUri = new Uri(_baseURL + pathAndQuery); //oauth path reset
		}

		public new async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
		{
			return await this.SendAsync(request, CancellationToken.None);
		}

		public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			AuthorizeRequest(request);
			var response = await base.SendAsync(request, cancellationToken);
			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) //refresh access token
			{
				_accessToken = null;
				_baseURL = "https://www.reddit.com";
				AuthorizeRequest(request);
				response = await base.SendAsync(request, cancellationToken);
			}

			return response;
		}


		private static int _lastCount = 0; //persist in application memory
		private static string? _lastId = null; //persist in application memory
		public async Task<IEnumerable<ISample>> GetSampleAsync(bool reset = false, int limit = 0)
		{
			if (reset) { _lastCount = 0; _lastId = null; }

			var request = new HttpRequestMessage(HttpMethod.Get, _baseURL + "/r/all/new?show=all&raw_json=1" +
				(_lastId != null ? $"&after={_lastId}" : "") +
				(_lastCount > 0 ? $"&count={_lastCount}" : "") +
				(limit > 0 ? $"&limit={limit}" : "")
				);

			var response = await this.SendAsync(request);
			List<JsonNode> children = null;
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
				var listing = JsonSerializer.Deserialize<JsonNode>(content)?["data"];
				_lastCount = (int)(listing?["dist"] ?? 0);
				_lastId = listing?["after"]?.ToString();
				if (_lastCount > 0 && listing?["children"] != null)
					children = new List<JsonNode>(listing["children"] as IEnumerable<JsonNode>);
			}

			var retList = new List<ISample>();
			if (children != null)
				foreach (var child in children)
				{
					retList.Add(new RedditSample(child));
				}

			return retList;
		}

		public string GetSampleSocketURL(Uri baseAddress) =>
			(baseAddress.Scheme.ToLower().Contains("https") ? "wss://" : "ws://") +
			baseAddress.Host +
			(baseAddress.Port > 0 ? $":{baseAddress.Port}" : "") +
			_sampleSocketPath;


		private static Thread _socketThread = null;
		public async Task SampleStreamMiddleware(HttpContext context, RequestDelegate next)
		{
			if (_socketThread == null && context.WebSockets.IsWebSocketRequest && context.Request.Path.Value.ToLower().StartsWith(_sampleSocketPath))
			{
				_socketThread = new Thread(async () =>
				{
					var cancel = new CancellationTokenSource();
					WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
					do
					{
						if (socket.State == WebSocketState.Open)
						{
							var samples = await GetSampleAsync();
							if (samples?.Count() > 0)
							{
								var datastring = JsonSerializer.Serialize(samples);
								var data = new ArraySegment<byte>(ASCIIEncoding.ASCII.GetBytes(datastring));
								//TODO: error if size over 65536 bytes, add message chunking code here later to handle large sample sizes
								try
								{
									await socket.SendAsync(data, WebSocketMessageType.Binary, false, cancel.Token);
								}
								catch (Exception ex)
								{
								}
							}
						}
						Thread.Sleep(1000);
					} while (!socket.CloseStatus.HasValue || socket.State != WebSocketState.Aborted);
					cancel.Cancel();
				});
				_socketThread.Start();
			}

			await next.Invoke(context);
		}

	}
}

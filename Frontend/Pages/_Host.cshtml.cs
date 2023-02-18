using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Frontend.Pages
{
	public class HostModel : PageModel
	{
		public class HashTagKey : IComparable<HashTagKey>
		{
			public int Occurs { get; set; }

			public string Value { get; set; }

			public override string ToString()
			{
				return $"Occurs:{Occurs}";
			}

			public override bool Equals(object? obj)
			{
				if (obj?.GetType() == typeof(HashTagKey))
					return ((HashTagKey)obj).Value.Equals(this.Value);

				if (obj?.GetType() == typeof(string))
					return ((string)obj).Equals(this.Value);

				return base.Equals(obj);
			}

			public int CompareTo(HashTagKey? other)
			{
				var dir = (other?.Occurs ?? 1) - Occurs;
				if (!other.Value.Equals(this.Value) && dir == 0) dir = 1;
				return dir;
			}
		}

		public static SortedList<HashTagKey, string> HashTagRanks = new SortedList<HashTagKey, string>();
		public static int GrandTotal = 0;
		public override void OnPageHandlerExecuted(PageHandlerExecutedContext context)
		{
			base.OnPageHandlerExecuted(context);
			//ConnectToStream(); //some flaws with websockets, resort to polling method
			StartPollThread();
		}


		private CancellationTokenSource _cancelPoll = new CancellationTokenSource();
		private async void StartPollThread()
		{
			var request = HttpContext.Request;
			var urlstring = request.Scheme + "://" + request.Host;
			var httpClient = new HttpClient();

			var thread = new Thread(async () =>
			{
				while (!_cancelPoll.IsCancellationRequested)
				{
					var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, urlstring + "/api/v1/Reddit/Sample"), _cancelPoll.Token);
					if (response.IsSuccessStatusCode)
					{
						var jsonString = await response.Content.ReadAsStringAsync(_cancelPoll.Token);
						if (!string.IsNullOrEmpty(jsonString))
						{
							var samples = JsonSerializer.Deserialize<JsonNode>(jsonString);
							ProcessSocketData(samples);
						}
					}
					Thread.Sleep(1000);
				}
			});
			thread.Start();
		}

		public async void ConnectToStream()
		{
			var request = HttpContext.Request;
			var urlstring = request.Scheme + "://" + request.Host;
			var httpClient = new HttpClient();

			var response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, urlstring + "/api/v1/Reddit/SampleStream"));
			if (response.IsSuccessStatusCode)
			{
				var webSocketUrl = await response.Content.ReadAsStringAsync();

				var cancel = new CancellationTokenSource();
				var socketHandler = new SocketsHttpHandler();
				var socketPool = new ClientWebSocket();
				socketPool.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
				socketPool.Options.HttpVersion = HttpVersion.Version20;
				socketPool.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
				socketHandler.EnableMultipleHttp2Connections = true;

				var thread = new Thread(async () =>
				{
					try
					{
						await socketPool.ConnectAsync(new Uri(webSocketUrl), new HttpMessageInvoker(socketHandler), cancel.Token);
					}
					catch { }
					while (socketPool.State == WebSocketState.Open)
					{
						try
						{
							var bytes = new byte[65535]; //? didn't see any way around this allocation for websockets in core
							var buffer = new Memory<byte>(bytes);
							var result = await socketPool.ReceiveAsync(buffer, cancel.Token);
							if (result.MessageType == WebSocketMessageType.Close)
							{
								await socketPool.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancel.Token);
							}
							else
							{
								ProcessSocketData(bytes.Take(result.Count));
							}
						}
						catch { }
					}
				});
				thread.Start();
			}
		}

		private async void ProcessSocketData(IEnumerable<byte> data)
		{
			var jsonString = ASCIIEncoding.ASCII.GetString(data.ToArray());
			var samples = JsonSerializer.Deserialize<JsonNode>(jsonString);
			ProcessSocketData(samples);
		}

		private async void ProcessSocketData(JsonNode samplesArray)
		{
			var samples = samplesArray as IEnumerable<JsonNode>;
			var total = 0;
			var hashTags = new List<string>();
			foreach (var sample in samples)
			{
				total++;
				var subItems = sample["SubItems"].GetValue<int>();
				total += subItems;
				var hashNodes = sample["HashTags"] as IEnumerable<JsonNode>;
				if (hashNodes?.Count() > 0) foreach (var node in hashNodes) hashTags.Add(node.ToString());
			}

			GrandTotal += total;
			foreach (var hashTag in hashTags)
			{
				var index = HashTagRanks.IndexOfValue(hashTag.ToLower());
				if (index < 0)
				{
					var newRank = new HashTagKey { Value = hashTag, Occurs = 1 };
					HashTagRanks.Add(newRank, hashTag.ToLower());
				}
				else
				{
					var rankUpdate = HashTagRanks.GetKeyAtIndex(index);
					rankUpdate.Occurs++;
					HashTagRanks.RemoveAt(index); //remove and re-add to sort
					HashTagRanks.Add(rankUpdate, hashTag.ToLower());
				}
			}

			UpdateComponents();
		}

		private async void UpdateComponents()
		{
			Frontend.Pages.Index.CurrentCount = GrandTotal;

		}


	}
}

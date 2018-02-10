namespace Chocola.Scraper
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Akka.Actor;
    using Akka.Event;
    using AutoMapper;
    using Newtonsoft.Json;

    using RestSharp;

    public class RedditScraperActor : ReceiveActor, IWithUnboundedStash, ILogReceive
    {
        private readonly ILoggingAdapter log = Logging.GetLogger(Context);
        private readonly IMapper mapper;

        private string accessToken;
        private long expiresIn;

        public RedditScraperActor()
        {
            this.mapper = new MapperConfiguration(config =>
            {
                config.CreateMap<ChildData, Post>().ForMember(a => a.CreatedUtc, a => a.MapFrom(b => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(b.CreatedUtc)));
            }).CreateMapper();
            this.Become(this.Anonymous);
        }

        public IStash Stash { get; set; }

        private void Anonymous()
        {
            this.log.Info("Became anonymous.");
            this.Receive<CredentialsMessage>(message =>
            {
                this.log.Info("Credentials message received.");
                this.accessToken = message.AccessToken;
                this.expiresIn = message.ExpiresIn;
                this.Become(this.Authenticated);
                this.Stash.UnstashAll();
                return true;
            });
            this.Receive<ScrapeMessage>(message =>
            {
                this.log.Info("Scrape message received.");
                this.Stash.Stash();
                return true;
            });
        }

        private void Authenticated()
        {
            this.log.Info("Became authenticated.");
            this.Receive<ScrapeMessage>(message =>
            {
                this.log.Info("Scrape message received.");
                var request = new RestRequest();
                request.Method = Method.GET;
                request.Resource = "/r/" + message.Subreddit + "/new";
                request.AddHeader("Authorization", "Bearer " + this.accessToken);
                if (message.After != null)
                {
                    request.AddQueryParameter("after", message.After);
                }
                //request.AddQueryParameter("count", message.Count.ToString("D", CultureInfo.InvariantCulture));
                request.AddQueryParameter("limit", "100");
                request.AddQueryParameter("show", "all");
                var client = new RestClient();
                client.BaseUrl = new Uri("https://oauth.reddit.com");
                var response = client.Execute<Listing>(request);
                if (response.Data == null)
                {
                    throw new Exception("Something went wrong.");
                }
                var posts = this.mapper.Map<Post[]>(response.Data.Data.Children.Select(a => a.Data));
                this.Sender.Tell(new PageScrapedMessage(posts), this.Self);
                if (response.Data.Data.After != null)
                {
                    this.Self.Tell(new ScrapeMessage(message.Subreddit, response.Data.Data.After, message.Count + posts.Length), this.Sender);
                }
                else
                {
                    this.Sender.Tell(new CompletedMessage(), this.Self);
                }
                return true;
            });
        }

        public static Props GetProps()
        {
            return Props.Create<RedditScraperActor>();
        }

        public class CompletedMessage
        {
        }

        public class PageScrapedMessage
        {
            public PageScrapedMessage(IEnumerable<Post> posts)
            {
                this.Posts = posts.ToArray();
            }

            public Post[] Posts { get; }
        }

        public class CredentialsMessage
        {
            public CredentialsMessage(string accessToken, long expiresIn)
            {
                this.AccessToken = accessToken;
                this.ExpiresIn = expiresIn;
            }

            public string AccessToken { get; }

            public long ExpiresIn { get; }
        }

        public class ScrapeMessage
        {
            public ScrapeMessage(string subreddit, string before, int count)
            {
                this.Subreddit = subreddit;
                this.After = before;
                this.Count = count;
            }

            public string After { get; }

            public int Count { get; }

            public string Subreddit { get; }
        }

        private class Listing
        {
            [JsonProperty("data")]
            public ListingData Data { get; set; }
        }

        private class ListingData
        {
            [JsonProperty("after")]
            public string After { get; set; }

            [JsonProperty("before")]
            public string Before { get; set; }

            [JsonProperty("children")]
            public List<Child> Children { get; set; }

            [JsonProperty("dist")]
            public int Distance { get; set; }
        }

        private class Child
        {
            [JsonProperty("data")]
            public ChildData Data { get; set; }
        }

        private class ChildData
        {
            [JsonProperty("author")]
            public string Author { get; set; }

            [JsonProperty("created_utc")]
            public long CreatedUtc { get; set; }

            [JsonProperty("domain")]
            public string Domain { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("permalink")]
            public string Permalink { get; set; }

            [JsonProperty("post_hint")]
            public string PostHint { get; set; }

            [JsonProperty("subreddit")]
            public string Subreddit { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }
        }
    }
}

namespace Chocola.Scraper
{
    using System;
    using System.Collections.Generic;

    using Akka.Actor;
    using Akka.Event;

    using Newtonsoft.Json;

    using RestSharp;

    public class RedditScraperActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ILoggingAdapter log = Logging.GetLogger(Context);

        private string accessToken;
        private int expiresIn;

        public RedditScraperActor()
        {
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
                this.log.Info("Starting scrape.");
                var request = new RestRequest();
                request.Method = Method.GET;
                request.Resource = "/r/" + message.Subreddit + "/new";
                request.AddHeader("Authorization", "Bearer " + this.accessToken);
                var client = new RestClient();
                client.BaseUrl = new Uri("https://oauth.reddit.com");
                var response = client.Execute<Listing>(request);
                this.log.Info("Scrape completed.");
                return true;
            });
        }

        public static Props GetProps()
        {
            return Props.Create<RedditScraperActor>();
        }

        public class CredentialsMessage
        {
            public CredentialsMessage(string accessToken, int expiresIn)
            {
                this.AccessToken = accessToken;
                this.ExpiresIn = expiresIn;
            }

            public string AccessToken { get; }

            public int ExpiresIn { get; }
        }

        public class ScrapeMessage
        {
            public ScrapeMessage(string subreddit)
            {
                this.Subreddit = subreddit;
            }

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
            public int CreatedOn { get; set; }

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

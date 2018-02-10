namespace Chocola.Scraper
{
    using System;

    public class Post
    {
        public string Author { get; set; }

        public DateTime CreatedUtc { get; set; }

        public string Domain { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Permalink { get; set; }

        public string PostHint { get; set; }

        public string Subreddit { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }
    }
}

namespace Chocola.Scraper
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Akka.Actor;
    using Akka.Configuration;

    using Microsoft.Extensions.Configuration;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var settings = GetChocolaSettings();
            var config = await GetAkkaConfigurationAsync();
            var system = ActorSystem.Create("chocola", config);
            var redditActor = system.ActorOf(RedditActor.GetProps(settings.Reddit), "reddit");
            Console.Write("Which subreddit should be scraped? ");
            var subreddit = Console.ReadLine();
            redditActor.Tell(new RedditActor.StartMessage(subreddit));
            Console.ReadLine();
            await system.Terminate();
        }

        private static async Task<Config> GetAkkaConfigurationAsync()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var path = Path.Combine(currentDirectory, "akka.hocon");
            var text = await File.ReadAllTextAsync(path);
            return ConfigurationFactory.ParseString(text);
        }

        private static ChocolaSettings GetChocolaSettings()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(currentDirectory);
            builder.AddJsonFile("application.json");
            var configuration = builder.Build();
            var settings = new ChocolaSettings();
            configuration.GetSection("Chocola").Bind(settings);
            return settings;
        }
    }
}

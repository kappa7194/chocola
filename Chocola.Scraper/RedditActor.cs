namespace Chocola.Scraper
{
    using Akka.Actor;
    using Akka.Event;

    public class RedditActor : ReceiveActor, IWithUnboundedStash, ILogReceive
    {
        private readonly ILoggingAdapter log = Logging.GetLogger(Context);
        private readonly RedditSettings settings;

        private IActorRef archiverActor;
        private IActorRef authenticatorActor;
        private IActorRef scraperActor;

        public RedditActor(RedditSettings settings)
        {
            this.settings = settings;
            this.Become(this.Anonymous);
        }

        public IStash Stash { get; set; }

        protected override void PreStart()
        {
            base.PreStart();
            this.archiverActor = Context.ActorOf(RedditArchiverActor.GetProps(this.settings.ArchivePath), "archiver");
            this.authenticatorActor = Context.ActorOf(RedditAuthenticatorActor.GetProps(this.settings), "authenticator");
            this.scraperActor = Context.ActorOf(RedditScraperActor.GetProps(), "scraper");
        }

        private void Anonymous()
        {
            this.log.Info("Became anonymous.");
            this.Receive<StartMessage>(message =>
            {
                this.log.Info("Start message received.");
                this.Stash.Stash();
                this.authenticatorActor.Tell(new RedditAuthenticatorActor.AuthenticateMessage(), this.Self);
                return true;
            });
            this.Receive<RedditAuthenticatorActor.AuthenticationSuccessfulMessage>(message =>
            {
                this.log.Info("Authentication successful message received.");
                this.scraperActor.Tell(new RedditScraperActor.CredentialsMessage(message.AccessToken, message.ExpiresIn), this.Self);
                this.Become(this.Authenticated);
                this.Stash.UnstashAll();
                return true;
            });
        }

        private void Authenticated()
        {
            this.log.Info("Became authenticated.");
            this.Receive<StartMessage>(message =>
            {
                this.log.Info("Start message received.");
                this.scraperActor.Tell(new RedditScraperActor.ScrapeMessage(message.Subreddit, null, 0), this.Self);
                return true;
            });
            this.Receive<RedditScraperActor.PageScrapedMessage>(message =>
            {
                this.log.Info("Page scraped message received.");
                this.archiverActor.Tell(new RedditArchiverActor.AddItemsMessage(message.Posts), this.Self);
                return true;
            });
            this.Receive<RedditScraperActor.CompletedMessage>(message =>
            {
                this.log.Info("Scrape completed message received.");
                this.archiverActor.Tell(new RedditArchiverActor.WriteArchiveMessage(), this.Self);
                return true;
            });
            this.Receive<RedditArchiverActor.ArchiveWrittenMessage>(message =>
            {
                this.log.Info("Archive written message received.");
                this.log.Info("All done.");
                return true;
            });
        }

        public static Props GetProps(RedditSettings settings)
        {
            return Props.Create<RedditActor>(settings);
        }

        public class StartMessage
        {
            public StartMessage(string subreddit)
            {
                this.Subreddit = subreddit;
            }

            public string Subreddit { get; }
        }
    }
}

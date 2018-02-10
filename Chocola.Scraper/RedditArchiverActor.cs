namespace Chocola.Scraper
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Akka.Actor;
    using Akka.Event;

    using OfficeOpenXml;
    using OfficeOpenXml.Style;

    public class RedditArchiverActor : ReceiveActor, ILogReceive
    {
        private readonly ILoggingAdapter log = Logging.GetLogger(Context);
        private readonly string path;
        private readonly List<Post> posts = new List<Post>();

        public RedditArchiverActor(string path)
        {
            this.path = path;
            this.Receive<AddItemsMessage>(message =>
            {
                this.log.Info("Add items message received.");
                this.posts.AddRange(message.Posts);
                return true;
            });
            this.Receive<WriteArchiveMessage>(message =>
            {
                this.log.Info("Write archive message received.");
                using (var stream = new FileStream(this.path, FileMode.Create))
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Posts");
                        worksheet.Cells[1, 1].Value = "Author";
                        worksheet.Cells[1, 2].Value = "CreatedUtc";
                        worksheet.Cells[1, 3].Value = "Domain";
                        worksheet.Cells[1, 4].Value = "Id";
                        worksheet.Cells[1, 5].Value = "Name";
                        worksheet.Cells[1, 6].Value = "Permalink";
                        worksheet.Cells[1, 7].Value = "PostHint";
                        worksheet.Cells[1, 8].Value = "Subreddit";
                        worksheet.Cells[1, 9].Value = "Title";
                        worksheet.Cells[1, 10].Value = "Url";
                        for (var i = 0; i < this.posts.Count; i++)
                        {
                            var row = i + 2;
                            var post = this.posts[i];
                            worksheet.Cells[row, 1].Value = post.Author;
                            worksheet.Cells[row, 2].Value = post.CreatedUtc;
                            worksheet.Cells[row, 2].Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                            worksheet.Cells[row, 3].Value = post.Domain;
                            worksheet.Cells[row, 4].Value = post.Id;
                            worksheet.Cells[row, 5].Value = post.Name;
                            worksheet.Cells[row, 6].Value = post.Permalink;
                            worksheet.Cells[row, 7].Value = post.PostHint;
                            worksheet.Cells[row, 8].Value = post.Subreddit;
                            worksheet.Cells[row, 9].Value = post.Title;
                            worksheet.Cells[row, 10].Value = post.Url;
                        }
                        worksheet.View.FreezePanes(2, 1);
                        package.Save();
                    }
                }
                this.posts.Clear();
                this.Sender.Tell(new ArchiveWrittenMessage(), this.Self);
                return true;
            });
        }

        public static Props GetProps(string path)
        {
            return Props.Create<RedditArchiverActor>(path);
        }

        public class AddItemsMessage
        {
            public AddItemsMessage(IEnumerable<Post> posts)
            {
                this.Posts = posts.ToArray();
            }

            public Post[] Posts { get; }
        }

        public class ArchiveWrittenMessage
        {
        }

        public class WriteArchiveMessage
        {
        }
    }
}

using System;
using System.Globalization;
using Humanizer;
using Microsoft.Build.Framework;

namespace TwitterBuild
{
    public class GetHomeTimeline : BaseTwitterTask
    {
        public override bool Execute()
        {
            var tweeter = new Tweeter(ConsumerKey, TokenKey, ConsumerSecret, TokenSecret);

            try {
                var result = tweeter.GetHomeTimelineAsync().GetAwaiter().GetResult();
                var tweets = result.Unwrap();

                foreach (var tweet in tweets) {
                    var date = DateTime.ParseExact(tweet.CreatedAt, "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.CurrentCulture);
                    var humanTime = date.Humanize();
                    Log.LogMessage(MessageImportance.High, $"@{tweet.User.ScreenName}, {humanTime}: {tweet.Text}.");
                }

                return true;
            } catch (Exception e) {
                Log.LogErrorFromException(e, true, true, null);
                return false;
            }
        }
    }
}

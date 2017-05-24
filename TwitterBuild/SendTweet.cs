using System;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace TwitterBuild
{
    public class SendTweet : BaseTwitterTask
    {
        [Required]
        public string TweetText { get; set; }

        public override bool Execute()
        {
            if (TweetText.Length > 140) {
                Log.LogError("Tweet is too long, tweet must be <140 charaters.");
                return false;
            }

            var tweeter = new Tweeter(ConsumerKey, TokenKey, ConsumerSecret, TokenSecret);

            try {
                var tweetResult = tweeter.PostTweetAsync(TweetText).ConfigureAwait(false).GetAwaiter().GetResult();
                var tweet = tweetResult.Unwrap();
                Log.LogMessage(
                    MessageImportance.High, 
                    $"Posted tweet at https://twitter.com/{tweet.User.ScreenName}/status/{tweet.Id}"
                );
                return true;
            } catch (Exception e) {
                Log.LogErrorFromException(e, true, true, null);
                return false;
            }
        }
    }
}

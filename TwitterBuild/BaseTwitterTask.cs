using System;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace TwitterBuild
{
    public abstract class BaseTwitterTask : Task
    {
        [Required]
        public string ConsumerKey { get; set; }

        [Required]
        public string TokenKey { get; set; }

        [Required]
        public string ConsumerSecret { get; set; }

        [Required]
        public string TokenSecret { get; set; }

        public abstract override bool Execute();
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMd.Shared
{
    public class WebHook
    {
        public string Secret { get; set; }
        public string PersonalAccessToken { get; set; }
        public string RemoteUrl { get; set; }
    }
}

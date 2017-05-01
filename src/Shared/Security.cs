using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMd.Shared
{
    public class Security
    {
        public string Role { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Access Access { get; set; }
    }
}

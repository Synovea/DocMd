using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace DocMd.Shared.Content
{
    public class Node
    {
        public string Title { get; set; }
        public string Excerpt { get; set; }

        public string Path { get; set; }
        public string Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public NodeProperties? Properties { get; set; }

        public DateTime ChangedDateTime { get; set; }

        public List<Node> Children { get; set; } = new List<Node>();

        public string GetExcerpt(int Length, string Ellipses = "...")
        {
            var excerpt = Excerpt.Substring(0, (Excerpt.Length > Length) ? Length : Excerpt.Length);

            if (Excerpt.Length >= Length)
            {
                excerpt += Ellipses;
            }

            return excerpt;
        }

        public string GetShortTitle(int Length, string Ellipses = "...")
        {
            var shortTitle = Title.Substring(0, (Title.Length > Length) ? Length : Title.Length);

            if (Title.Length >= Length)
            {
                shortTitle += Ellipses;
            }

            return shortTitle;
        }

        public bool HasMimeType(string Mimetype, bool IncludeChildren = false)
        {
            var hasMimeType = false;
            var childrenMimetype = false;

            hasMimeType = Type.Equals(Mimetype);

            if (IncludeChildren)
            {
                foreach (var child in Children)
                {
                    childrenMimetype |= child.HasMimeType(Mimetype, IncludeChildren);
                }

                hasMimeType |= childrenMimetype;
            }

            return hasMimeType;
        }

        public bool ContainsPath(string Path, bool IncludeChildren = false)
        {
            var hasPath = false;
            var childrenPath = false;

            hasPath = (!string.IsNullOrWhiteSpace(this.Path)) ? this.Path.ToLower().Equals(Path.ToLower()) : false;

            if (IncludeChildren)
            {
                foreach (var child in Children)
                {
                    childrenPath |= child.ContainsPath(Path, IncludeChildren);
                }

                hasPath |= childrenPath;
            }

            return hasPath;
        }
    }
}

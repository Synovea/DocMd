using System;

namespace DocMd.Shared.Content
{
    [Flags]
    public enum NodeProperties
    {
        Generated,
        MustRecurse,
        Hidden
    }
}
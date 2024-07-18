﻿namespace Jifer.Models.Post
{
    using Jifer.Data.Models;

    public class TimelineViewModel
    {
        public List<JGo> Posts { get; set; }
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}

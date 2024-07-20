namespace Jifer.Services.Models.Post
{
    using System.Collections.Generic;
    using Jifer.Data.Models;

    public class MyJGosViewModel
    {
        public List<JGo> JGos { get; set; } = new List<JGo>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

}

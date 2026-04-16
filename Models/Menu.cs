namespace NewsAggregator.Models
{
    public class Menu
    {
        public int MenuID {get; set;}
        public string MenuName {get; set;} = string.Empty;
        public bool IsActive {get; set;} = true;
        public string? ControllerName {get; set; }
        public string? ActionName {get; set;}
        public int Levels {get; set;} = 1;
        public int ParentID {get; set;} = 0;
        public string? Link {get; set;}
        public int MenuOrder {get; set;} = 0;
        public int Position {get; set;}=1;
        public ICollection<Post> Posts {get; set;} = new List<Post>();

    }
}
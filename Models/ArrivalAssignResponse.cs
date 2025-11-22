namespace ai_indoor_nav_api.Models
{
    public class ArrivalAssignResponse
    {
        public int Level { get; set; }
        public string VisitorId { get; set; } = string.Empty;
        public DecisionInfo Decision { get; set; } = new();
        public string TraceId { get; set; } = string.Empty;
    }

    public class DecisionInfo
    {
        public bool IsDisabled { get; set; }
        public int Age { get; set; }
        public double AgeCutoff { get; set; }
        public double Alpha1 { get; set; }
        public double PDisabled { get; set; }
        public double ShareLeftForOld { get; set; }
        public double TauQuantile { get; set; }
        public Dictionary<int, int> Occupancy { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }
}

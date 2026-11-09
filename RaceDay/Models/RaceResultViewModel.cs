namespace RaceDay.Models
{
    public class RaceResultViewModel
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }

        public int EventId { get; set; }

        public string EventName { get; set; }
            = string.Empty;

        public int ParticipantId { get; set; }

        public string ParticipantName { get; set; }
            = string.Empty;

        public string CategoryName { get; set; }
            = string.Empty;

        public string FinishTime { get; set; }
            = string.Empty;

        public int FinishPosition { get; set; }

        public DateTime CapturedAt { get; set; }
    }
}

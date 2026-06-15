using System;
using System.Collections.Generic;
using System.Text;

namespace TourPlanner.Domain
{
    public class TourLog
    {
        public Guid Id { get; set; }
        public Guid TourId { get; set; }
        public required string TourName { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.Now;
        public string? Comment { get; set; }
        public Difficulty Difficulty { get; set; }
        public int TotalDistance { get; set; } = 0;
        public TimeSpan Duration { get; set; }
        public int Rating { get; set; }
    }

    public enum Difficulty
    {
        Unknown = 0,
        Easy = 1,
        Medium = 2,
        Hard = 3
    }
}

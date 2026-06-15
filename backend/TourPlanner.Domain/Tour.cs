using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TourPlanner.Domain
{
    public class Tour
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string From { get; set; }
        public required string To { get; set; }
        public required TransportType TransportType { get; set; }
        public int Distance { get; set; }     
        public int Duration { get; set; }
        public TourStatus Status { get; set; }
        public string Color { get; set; }
        public string? ImageUrl { get; set; }
    }

    public enum TransportType
    {
        Unknown = 0,
        Walking = 1,
        Cycling = 2,
        Driving = 3
    }

    public enum TourStatus
    {
        Unknown = 0,
        Planned = 1,
        Completed = 2
    }
}

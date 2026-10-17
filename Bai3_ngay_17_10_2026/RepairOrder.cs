using System;

namespace Bai3_ngay_17_10_2026
{
    public enum RepairStatus
    {
        New,
        InProgress,
        WaitingParts,
        Completed,
        Cancelled
    }

    public class RepairOrder
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string DeviceType { get; set; }
        public string Description { get; set; }
        public DateTime ReceivedDate { get; set; }
        public decimal EstimatedCost { get; set; }
        public RepairStatus Status { get; set; }
        public string ImagePath { get; set; }
    }
}

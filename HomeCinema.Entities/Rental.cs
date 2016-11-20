﻿using System;

namespace HomeCinema.Entities
{
    public class Rental : IEntityBase
    {
        public int ID { get; set; }
        public int CustomerId { get; set; }
        public int StockId { get; set; }
        public DateTime RentalDate { get; set; }
        public Nullable<DateTime> ReturnedDate { get; set; }
        public virtual Stock Stock { get; set; }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingStrategy
{
    public sealed class ChinaStock : ITradingObject
    {
        public string Code { get; protected set; }

        public string Name { get; protected set; }

        public int MinCountPerHandForBuying { get; protected set;}

        public int MinCountPerHandForSelling { get; protected set;}

        public double MinPriceUnit { get; protected set;}

        public ChinaStock(string code, string name)
            : this(code, name, 100, 1, 0.01)
        {
        }

        public ChinaStock(
            string code, 
            string name, 
            int minCountPerHandForBuying, 
            int minCountPerHandForSelling, 
            double minPriceUnit)
        {
            Code = code;
            Name = name;
            MinCountPerHandForBuying = minCountPerHandForBuying;
            MinCountPerHandForSelling = minCountPerHandForSelling;
            MinPriceUnit = minPriceUnit;
        }
    }
}
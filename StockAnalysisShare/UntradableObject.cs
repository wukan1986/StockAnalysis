﻿namespace StockAnalysis.Share
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.ChineseMarket;

    public sealed class UntradableObject
    {
        private static string[] untradableSymbols = new string[]
        {
            StockBoardIndex.GrowingBoardIndexName.Symbol.NormalizedSymbol,
            StockBoardIndex.MainBoardIndexName.Symbol.NormalizedSymbol,
            StockBoardIndex.SmallMiddleBoardIndexName.Symbol.NormalizedSymbol
        };

        private static Dictionary<string, UntradableObject> untradableObjects;

        public string Symbol { get; private set; }

        public UntradableObject(string symbol)
        {
            Symbol = symbol;
        }

        static UntradableObject()
        {
            untradableObjects = untradableSymbols.ToDictionary(s => s, s => new UntradableObject(s));
        }

        public static bool IsUntradable(string normalizedSymbol)
        {
            return untradableObjects.ContainsKey(normalizedSymbol);
        }
    }
}

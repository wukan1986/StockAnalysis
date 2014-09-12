﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingStrategy.Strategy
{
    public abstract class MetricBasedTraceStopLossMarketExiting<T>
        : MetricBasedTradingStrategyComponentBase<T>
        where T : IRuntimeMetric
    {
        protected abstract double CalculateStopLossPrice(ITradingObject tradingObject, double currentPrice);

        public override void Evaluate(ITradingObject tradingObject, StockAnalysis.Share.Bar bar)
        {
            base.Evaluate(tradingObject, bar);

            if (Context.ExistsPosition(tradingObject.Code))
            {
                double stopLossPrice = CalculateStopLossPrice(tradingObject, bar.ClosePrice);

                foreach (var position in Context.GetPositionDetails(tradingObject.Code))
                {
                    if (position.IsStopLossPriceInitialized())
                    {
                        // increase stop loss price if possible.
                        if (position.StopLossPrice < stopLossPrice)
                        {
                            position.SetStopLossPrice(stopLossPrice);
                        }
                    }
                }
            }
        }

        public virtual bool ShouldExit(ITradingObject tradingObject, out string comments)
        {
            comments = string.Empty;
            return false;
        }
    }
}

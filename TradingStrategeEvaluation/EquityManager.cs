﻿using System;
using System.Collections.Generic;
using System.Linq;
using StockAnalysis.Share;

using TradingStrategy;

namespace TradingStrategyEvaluation
{
    public sealed class EquityManager
    {
        private struct PositionToBeSold
        {
            public int Index;
            public int Volume;

            public PositionToBeSold(int index, int volume)
            {
                Index = index;
                Volume = volume;
            }
        }

        private readonly Dictionary<string, List<Position>> _activePositions = new Dictionary<string, List<Position>>();

        private readonly List<Position> _closedPositions = new List<Position>();
        
        public double InitialCapital { get; private set; }

        public double Leverager { get; private set; }

        public double CurrentCapital { get; private set; }

        public IEnumerable<Position> ClosedPositions { get { return _closedPositions; } }

        public EquityManager(double initialCapital, double leverager, double currentCapital = double.NaN)
        {
            InitialCapital = initialCapital;

            Leverager = leverager;

            CurrentCapital = double.IsNaN(currentCapital) ? initialCapital * leverager : currentCapital;
        }

        private void AddPosition(Position position)
        {
            if (!_activePositions.ContainsKey(position.Code))
            {
                _activePositions.Add(position.Code, new List<Position>());
            }

            _activePositions[position.Code].Add(position);
        }

        public void ManualAddPosition(Position position)
        {
            AddPosition(position);
        }

        public bool ExecuteTransaction(
            Transaction transaction, 
            bool allowNegativeCapital,
            out CompletedTransaction completedTransaction, 
            out string error)
        {
            error = string.Empty;
            completedTransaction = null;

            if (transaction.Action == TradingAction.OpenLong)
            {
                var charge = transaction.Price * transaction.Volume + transaction.Commission;

                if (CurrentCapital < charge && !allowNegativeCapital)
                {
                    error = "No enough capital for the transaction";
                    return false;
                }

                var position = new Position(transaction);

                AddPosition(position);

                // charge money
                CurrentCapital -= charge;

                return true;
            }

            if (transaction.Action == TradingAction.CloseLong)
            {
                var code = transaction.Code;

                if (!_activePositions.ContainsKey(code))
                {
                    error = string.Format("Transaction object {0} does not exists", code);
                    return false;
                }

                var positions = _activePositions[code].ToArray();

                var positionsToBeSold = IdentifyPositionToBeSold(positions, transaction).ToArray();

                if (positionsToBeSold.Count() == 0)
                {
                    return true;
                }

                // note: the position could be sold partially and we need to consider the situation
                // everywhere in the code

                var buyCost = 0.0;
                var buyCommission = 0.0;

                foreach (var ptbs in positionsToBeSold)
                {
                    buyCost += positions[ptbs.Index].BuyPrice * ptbs.Volume;
                    buyCommission += positions[ptbs.Index].BuyCommission 
                                     * ptbs.Volume / positions[ptbs.Index].Volume;
                }

                foreach (var ptbs in positionsToBeSold)
                {
                    var position = positions[ptbs.Index];
                    if (position == null)
                    {
                        throw new InvalidOperationException();
                    }

                    // for partial selling, we need to split position firstly.
                    Position newPosition = null;
                    if (ptbs.Volume < position.Volume)
                    {
                        newPosition = position.Split(ptbs.Volume);
                    }

                    position.Close(
                        new Transaction
                        {
                            Action = transaction.Action,
                            Code = transaction.Code,
                            Name = transaction.Name,
                            Comments = transaction.Comments,
                            Commission = transaction.Commission / transaction.Volume * position.Volume,
                            Error = transaction.Error,
                            ExecutionTime = transaction.ExecutionTime,
                            InstructionId = transaction.InstructionId,
                            Price = transaction.Price,
                            RelatedObjects = transaction.RelatedObjects,
                            SubmissionTime = transaction.SubmissionTime,
                            Succeeded = transaction.Succeeded,
                            Volume = position.Volume
                        });

                    // move closed position to history
                    _closedPositions.Add(position);

                    // use new position to replace old position.
                    positions[ptbs.Index] = newPosition;
                }

                // update positions for given code
                var remainingPositions = positions.Where(e => e != null).ToList();

                if (remainingPositions == null || remainingPositions.Count == 0)
                {
                    _activePositions.Remove(code);
                }
                else
                {
                    _activePositions[code] = remainingPositions;
                }

                // update current capital
                var earn = transaction.Price * transaction.Volume - transaction.Commission;
                CurrentCapital += earn;

                // create completed transaction object
                completedTransaction = new CompletedTransaction
                {
                    Code = code,
                    Name = transaction.Name,
                    ExecutionTime = transaction.ExecutionTime,
                    Volume = transaction.Volume,
                    BuyCost = buyCost,
                    AverageBuyPrice = buyCost / transaction.Volume,
                    SoldPrice = transaction.Price,
                    SoldGain = transaction.Price * transaction.Volume,
                    Commission = transaction.Commission + buyCommission,
                };

                return true;
            }
            
            throw new InvalidOperationException(
                string.Format("unsupported action {0}", transaction.Action));
        }

        /// <summary>
        /// Identify all positions that to be sold
        /// </summary>
        /// <param name="positions">existing positions to be examined</param>
        /// <param name="transaction">transaction to be executed</param>
        /// <returns>Tuples that identify the position and volume to be sold</returns>
        private IEnumerable<PositionToBeSold> IdentifyPositionToBeSold(Position[] positions, Transaction transaction)
        {
            if (positions == null || transaction == null)
            {
                throw new ArgumentNullException();
            }

            if (positions.Length == 0 || transaction.Action != TradingAction.CloseLong)
            {
                throw new ArgumentException();
            }

            var remainingVolume = transaction.Volume;
            switch (transaction.SellingType)
            {
                case SellingType.ByPositionId:
                    for (var i = 0; i < positions.Length; ++i)
                    {
                        if (positions[i].Id == transaction.PositionIdForSell)
                        {
                            yield return new PositionToBeSold(i, positions[i].Volume);
                            yield break;
                        }
                    }
                    break;
                case SellingType.ByStopLossPrice:
                    for (var i = 0; i < positions.Length; ++i)
                    {
                        if (positions[i].StopLossPrice > transaction.StopLossPriceForSelling)
                        {
                            remainingVolume -= positions[i].Volume;

                            yield return new PositionToBeSold(i, positions[i].Volume);
                        }
                    }
                    break;
                case SellingType.ByVolume:
                    var totalVolume = positions.Sum(e => e.Volume);
                    if (totalVolume < transaction.Volume)
                    {
                        throw new InvalidOperationException("There is no enough volume for selling");
                    }

                    for (var i = 0; i < positions.Length && remainingVolume > 0; ++i)
                    {
                        if (positions[i].Volume <= remainingVolume)
                        {
                            remainingVolume -= positions[i].Volume;
                            yield return new PositionToBeSold(i, positions[i].Volume);
                        }
                        else
                        {
                            yield return new PositionToBeSold(i, remainingVolume);

                            remainingVolume = 0;
                        }
                    }

                    break;
            }

            if (remainingVolume != 0)
            {
                throw new InvalidOperationException("The volume specified in transaction does not match the positions affected by the transaction");
            }
        }

        public int PositionCount { get { return _activePositions.Count; } }

        public IEnumerable<Position> GetPositionDetails(string code)
        {
            return _activePositions[code];
        }

        public IEnumerable<string> GetAllPositionCodes()
        {
            return _activePositions.Keys.ToArray();
        }

        public bool ExistsPosition(string code)
        {
            return _activePositions.ContainsKey(code);
        }

        public double GetTotalEquity(
            ITradingDataProvider provider, 
            DateTime period, 
            EquityEvaluationMethod method)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }


            if (method == EquityEvaluationMethod.InitialEquity)
            {
                return InitialCapital;
            }

            double equity = CurrentCapital;

            // cash is the core equity
            if (method == EquityEvaluationMethod.CoreEquity)
            {
                return equity;
            }

            foreach (var kvp in _activePositions)
            {
                var code = kvp.Key;

                Bar bar;

                var index = provider.GetIndexOfTradingObject(code);
                if (index < 0)
                {
                    throw new InvalidOperationException(string.Format("Can't get index for code {0}", code));
                }

                if (!provider.GetLastEffectiveBar(index, period, out bar))
                {
                    throw new InvalidOperationException(
                        string.Format("Can't get data from data provider for code {0}, time {1}", code, period));
                }

                if (method == EquityEvaluationMethod.TotalEquity
                    || method == EquityEvaluationMethod.LossControlTotalEquity
                    || method == EquityEvaluationMethod.LossControlInitialEquity)
                {
                    var volume = kvp.Value.Sum(e => e.Volume);
                    equity += volume * bar.ClosePrice;
                }
                else if (method == EquityEvaluationMethod.ReducedTotalEquity 
                        || method == EquityEvaluationMethod.LossControlReducedTotalEquity)
                {
                    equity += kvp.Value.Sum(position => position.Volume * Math.Min(bar.ClosePrice, position.StopLossPrice));
                }
            }

            if (method == EquityEvaluationMethod.TotalEquity
                || method == EquityEvaluationMethod.ReducedTotalEquity)
            {
                return equity;
            }
            else if (method == EquityEvaluationMethod.LossControlInitialEquity)
            {
                return equity > InitialCapital
                    ? InitialCapital
                    : 2 * equity - InitialCapital;
            }
            else if (method == EquityEvaluationMethod.LossControlTotalEquity)
            {
                return equity > InitialCapital
                    ? equity
                    : 2 * equity - InitialCapital;
            }
            else if (method == EquityEvaluationMethod.LossControlReducedTotalEquity)
            {
                return equity > InitialCapital
                    ? equity
                    : 2 * equity - InitialCapital;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public double GetPositionMarketValue(ITradingDataProvider provider, string code, DateTime time)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            double equity = 0;

            if (_activePositions.ContainsKey(code))
            { 
                var volume = _activePositions[code].Sum(e => e.Volume);

                Bar bar;

                var index = provider.GetIndexOfTradingObject(code);
                if (!provider.GetLastEffectiveBar(index, time, out bar))
                {
                    throw new InvalidOperationException(
                        string.Format("Can't get data from data provider for code {0}, time {1}", code, time));
                }

                equity += volume * bar.ClosePrice;
            }

            return equity;
        }
    }
}

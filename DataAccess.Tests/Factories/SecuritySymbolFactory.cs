using StockAnalysis.Common.SymbolName;
using StockAnalysis.Common.Exchange;

// <copyright file="SecuritySymbolFactory.cs">Copyright ©  2018</copyright>

using System;
using Microsoft.Pex.Framework;

namespace StockAnalysis.Share
{
    /// <summary>A factory for StockAnalysis.Share.SecuritySymbol instances</summary>
    public static partial class SecuritySymbolFactory
    {
        /// <summary>A factory for StockAnalysis.Share.SecuritySymbol instances</summary>
        [PexFactoryMethod(typeof(SecuritySymbol))]
        public static SecuritySymbol Create(string rawSymbol_s, string normalizedSymbol_s1, ExchangeId exchangeId_i)
        {
            SecuritySymbol securitySymbol
               = new SecuritySymbol(rawSymbol_s, normalizedSymbol_s1, exchangeId_i);
            return securitySymbol;

            // TODO: Edit factory method of SecuritySymbol
            // This method should be able to configure the object in all possible ways.
            // Add as many parameters as needed,
            // and assign their values to each field by using the API.
        }
    }
}

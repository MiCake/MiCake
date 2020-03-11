using System.Collections.Generic;
using Xunit;
using MiCake.Core.Util.Collections;
using System.Linq;
using System;

namespace MiCake.Core.Tests.Util
{
    public class ListExtension_Tests
    {
        public ListExtension_Tests()
        {

        }

        [Fact]
        public void ListExchangeOrderUsePredicate_ShouldRightOrder()
        {
            List<string> strList = new List<string>() { "A", "B", "C" };

            strList.ExchangeOrder(s => s.Equals("B"), strList.Count - 1);

            Assert.Equal("B", strList.Last());
        }

        [Fact]
        public void ListExchangeOrder_ShouldRightOrder()
        {
            List<string> strList = new List<string>() { "A", "B", "C" };

            strList.ExchangeOrder("B", strList.Count - 1);

            Assert.Equal("B", strList.Last());
        }

        [Fact]
        public void ListExchangeOrder_WrongPredictShoudError()
        {
            List<string> strList = new List<string>() { "A", "B", "C" };

            Assert.Throws<ArgumentException>(() =>
            {
                strList.ExchangeOrder(s => s.Equals("D"), strList.Count - 1);
            });
        }
    }
}

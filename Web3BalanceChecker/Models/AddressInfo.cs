using System.Collections.Generic;

namespace Web3BalanceChecker.Models
{
    public class AddressInfo
    {
        public string Address { get; set; }
        public double Ethereum { get; set; }
        public double BNBChain { get; set; }
        public double Optimism { get; set; }
        public double Polygon { get; set; }
        public double Fantom { get; set; }
        public double Arbitrum { get; set; }
        public double Avalanche { get; set; }
        public double Total { get; set; }
        
        public AddressInfo () {}

        public AddressInfo(string address, List<double> balanceList, double total)
        {
            Address = address;
            Ethereum = balanceList[0];
            BNBChain = balanceList[1];
            Optimism = balanceList[2];
            Polygon = balanceList[3];
            Fantom = balanceList[4];
            Arbitrum = balanceList[5];
            Avalanche = balanceList[6];
            Total = total;
        }
    }
}
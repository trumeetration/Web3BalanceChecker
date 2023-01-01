using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Web3BalanceChecker.Annotations;
using Web3BalanceChecker.Commands;
using Web3BalanceChecker.Models;

namespace Web3BalanceChecker
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private BackgroundWorker _bw;
        private List<int> ChainIdList = new() { 1, 56, 10, 137, 250, 42161, 43114  };
        public ObservableCollection<AddressInfo> AddressInfos { get; set; }
        public ObservableCollection<string> ReadAddresses { get; set; }

        public ViewModel()
        {
            AddressInfos = new ObservableCollection<AddressInfo>();
            ReadAddresses = new ObservableCollection<string>();
            _bw = new BackgroundWorker()
            {
                WorkerReportsProgress = true
            };
            _bw.DoWork += BwOnDoWork;
            _bw.ProgressChanged += BwOnProgressChanged;
            _bw.RunWorkerCompleted += BwOnRunWorkerCompleted;
        }

        private void BwOnRunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
            ProgressState = 0;
        }

        private void BwOnProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if ((AddressInfo)e.UserState != null)
            {
                AddressInfos.Add((AddressInfo)e.UserState);
            }

            ProgressState = e.ProgressPercentage;
        }

        private void BwOnDoWork(object? sender, DoWorkEventArgs e)
        {
            var bw = (BackgroundWorker) sender;
            for (int i = 0; i < ReadAddresses.Count; i++)
            {
                Debug.WriteLine(ReadAddresses[i]);
                var line = ReadAddresses[i].Trim();
                if (line.Length < 42)
                    continue;
                var address = "";
                if (line.Contains(':'))
                {
                    var parts = line.Split(':');
                    line = parts.FirstOrDefault((x) => x.Length is 42 or 66 && x.StartsWith("0x"))?.ToString();
                }
                address = line.Length is 42 ? line : new Nethereum.Web3.Accounts.Account(line).Address;
                
                HttpClientHandler handler = new HttpClientHandler();
                handler.AutomaticDecompression = DecompressionMethods.All;

                var client = new HttpClient(handler);

                var balanceList = new List<double>();
                var awaitRequests = new List<Task<HttpResponseMessage>>();
                
                foreach (var request in ChainIdList.Select(chainId => new HttpRequestMessage(HttpMethod.Get, $"https://account.metafi.codefi.network/accounts/{address.ToLower()}?chainId={chainId}&includePrices=true")))
                {
                    Debug.WriteLine("test");
                    request.Headers.Add("referer", "https://portfolio.metamask.io/");

                    var responseTask = client.SendAsync(request);
                    awaitRequests.Add(responseTask);
                }

                var requestsResults = Task.WhenAll(awaitRequests).Result;
                
                foreach (var responseTask in requestsResults)
                {
                    responseTask.EnsureSuccessStatusCode();
                    var responseBody = responseTask.Content.ReadAsStringAsync().Result;
                    var responseJson = JObject.Parse(responseBody);
                    double balance = 0;
                    try
                    {
                        balance = Math.Round((double)responseJson["value"]["marketValue"], 2);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                    
                    balanceList.Add(balance);
                }

                var total = balanceList.Sum();

                var addressInfo = new AddressInfo(address, balanceList, total);
                bw.ReportProgress((int)((i + 1) / (float)ReadAddresses.Count * 100), addressInfo);
            }
        }

        private int _progress;
        public int ProgressState
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(ProgressState));
            }
        }

        public ICommand LoadDataFromFileCommand =>
            new RelayCommand(() =>
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = "C:\\";
                dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                dialog.FilterIndex = 2;
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == true)
                {
                    var fs = dialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string line;
                        while (true)
                        {
                            line = reader.ReadLine();
                            if (line != null)
                            {
                                ReadAddresses.Add(line.Trim('\n', '\r'));
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                }
            }, () => true);

        public ICommand ScanAddresses =>
            new RelayCommand(() =>
            {
                _bw.RunWorkerAsync(this);
            }, () => true);
    }
}
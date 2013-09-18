using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using JsonDataManipulator.DTOs;
using JsonDataManipulator.Enums;
using JsonDataManipulator.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using csEnum = JsonDataManipulator.Enums.ConnectionStatus;

namespace NZBStatusUI
{
    public class JsonDataManipulator
    {
        private readonly string _url;
        private readonly string _baseURL;
        private readonly string _apiKey;
        private JToken _jsonString;
        private JArray _slots;
        private readonly WebClient _webClient;
        private csEnum _connectionStatus;
        private csEnum _connectionCommandStatus;
        private string _result;
        private bool _commandResult;
        private readonly List<string> _errorList;
        private string _lastError;

        public string GetLastError()
        {
            if (!string.IsNullOrEmpty(_lastError))
            {
                return _lastError;
            }
            return string.Empty;
        }

        public decimal TotalMB
        {
            get { return GetRootValue<decimal>("mb"); }
        }

        public decimal MBLeft
        {
            get { return GetRootValue<decimal>("mbleft"); }
        }

        public int TotalPercentage
        {
            get
            {
                var downloaded = (TotalMB - MBLeft);
                return TotalMB > 0 ? Convert.ToInt32(Math.Round(downloaded * 100 / TotalMB, 0)) : 0;
            }
        }

        public string ETA
        {
            get { return GetRootValue<string>("timeleft"); }
        }

        public string Version
        {
            get { return GetRootValue<string>("version"); }
        }

        public decimal Speed
        {
            get { return GetRootValue<decimal>("kbpersec"); }
        }

        public decimal AlreadyDownloadedMB
        {
            get { return GetRootValue<decimal>("mb") - GetRootValue<decimal>("mbleft"); }
        }

        public csEnum ConnectionStatus()
        {
            return _connectionStatus;
        }

        public bool IsDownloading
        {
            get { return Status == "Downloading"; }
        }

        public bool IsPaused
        {
            get { return GetRootValue<bool>("paused"); }
        }

        public string Status
        {
            get { return GetRootValue<string>("status"); }
        }

        public Slot GetCurrentSlot()
        {
            if (_slots.Count > 0)
            {
                return JsonConvert.DeserializeObject<Slot>(_slots[0].ToString());
            }
            return new Slot();
        }

        public HashSet<Slot> GetAllSlots()
        {
            var result = new HashSet<Slot>();
            foreach (JToken slot in _slots)
            {
                result.Add(JsonConvert.DeserializeObject<Slot>(slot.ToString()));
            }
            return result;
        }

        private TClassType GetRootValue<TClassType>(string key)
        {
            JToken token = _jsonString.SelectToken(key);
            if (token != null && new JRaw(token).Value.ToString() != string.Empty)
            {
               
                
                var value = token.Value<TClassType>();
                return value;
            }
            return default(TClassType);
        }

        private TClassType GetCurrentSlotValue<TClassType>(string key)
        {
            if (_slots.Count > 0)
            {
                var token = _slots[0].SelectToken(key);
                var value = token != null ? token.Value<TClassType>() : default(TClassType);
                return value;
            }
            return default(TClassType);
        }

        public JsonDataManipulator(string url, string apiKey)
            : this(url, apiKey, false)
        { }

        public JsonDataManipulator(string url, string apiKey, bool dontParse)
            :this(url,apiKey,dontParse,"queue")
        {}

        public JsonDataManipulator(string url, string apiKey, bool dontParse, string mode)
        {
            _baseURL = url;
            _url = GetBaseURL(url, mode);
            _apiKey = apiKey;
            _slots = new JArray();
            _jsonString = new JObject();
            _webClient = new WebClient();
            _errorList = new List<string>();
            _webClient.DownloadStringCompleted += DownloadFinished;
            if (!dontParse)
            {
                InitializeData();
            }
            else
            {
                GetData();
            }
        }

        private static string GetBaseURL(string url, string mode)
        {
            return GetBaseURL(url, mode, null);
        }

        private static string GetBaseURL(string url, string mode, string name)
        {
            return GetBaseURL(url, mode, name, null);
        }

        private static string GetBaseURL(string url, string mode, string name, string value)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = "&name=" + name;
            }

            if (!string.IsNullOrEmpty(value))
            {
                value = "&value=" + value;
            }

            return string.Format("{0}/sabnzbd/api?mode={1}{2}{3}&output=json", url, mode, name, value);
        }

        private bool InitializeData()
        {
            GetData();
            if (!string.IsNullOrEmpty(_result) && _connectionStatus == csEnum.Ok)
            {
                _jsonString = JObject.Parse(_result ?? "{}").GetValue("queue") ?? new JObject();
                _slots = (JArray)_jsonString["slots"] ?? new JArray();
                // this ensures that the true will be returned on next pass only where there is new data downloaded
                _result = string.Empty;
                return true;
            }
            return false;
        }

        private void GetData()
        {
            try
            {
                if (!_webClient.IsBusy && !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_baseURL))
                {
                    var url = new Uri(string.Format("{0}&apikey={1}", _url, _apiKey));
                    _webClient.DownloadStringAsync(url);
                }
            }
            catch (WebException e)
            {

                switch (e.Status)
                {
                    case WebExceptionStatus.Timeout:
                        _connectionStatus = csEnum.Timeout;
                        break;
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.NameResolutionFailure:
                        _connectionStatus = csEnum.CantConnect;
                        break;
                }
            }
        }

        public bool RefreshData()
        {
            return InitializeData();
        }

        private void DownloadFinished(object senderm, DownloadStringCompletedEventArgs eventArgs)
        {
            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                // BUG resolve what happens when wrong api is supplied
                _result = eventArgs.Result;
                _connectionStatus = csEnum.Ok;
            }
            else
            {
                if (eventArgs.Error != null)
                {
                    LogError(eventArgs.Error, "DownloadFinished");
                }
            }
        }

        private void CommandFinished(object senderm, DownloadStringCompletedEventArgs eventArgs)
        {
            
            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                // BUG resolve what happens when wrong api is supplied
                _commandResult = JObject.Parse(eventArgs.Result).Value<bool>("status");
                _connectionCommandStatus = csEnum.Ok;
            }
            else
            {
                if (eventArgs.Error != null)
                {
                    LogError(eventArgs.Error, "CommandFinished");
                }
            }
        }

        public bool ResumeMain()
        {
            return CommandResult(Command.Resume);
        }

        public bool PauseMain()
        {
            return CommandResult(Command.Pause);
        }

        public bool SetSpeedLimit(int speedLimit)
        {
            return CommandResult(Command.Config, Name.SpeedLimit, speedLimit.ToString(CultureInfo.InvariantCulture));
        }

        public int SpeedLimit
        {
            get
            {
                return GetRootValue<int>("speedlimit");
            }
        }


        public bool Pause(string nzo_id)
        {
            return CommandResult(Command.Queue, Name.Pause, nzo_id);
        }

        public bool Resume(string nzo_id)
        {
            return CommandResult(Command.Queue, Name.Resume, nzo_id);
        }

        public bool Delete(string nzo_id)
        {
            return CommandResult(Command.Queue, Name.Delete, nzo_id);
        }

        private bool CommandResult(Command command)
        {
            return CommandResult(command, Name.None, string.Empty);
        }

        private bool CommandResult(Command command, Name name)
        {
            return CommandResult(command, name, string.Empty);
        }

        private bool CommandResult(Command command, Name name, string value)
        {
            try
            {
                var webClient = new WebClient();
                webClient.DownloadStringCompleted += CommandFinished;
                if (!webClient.IsBusy)
                {
                    var url = new Uri(string.Format("{0}&apikey={1}", GetBaseURL(_baseURL, command.Description(), name.Description(), value), _apiKey));
                    webClient.DownloadStringAsync(url);
                }
            }
            catch (WebException e)
            {
                switch (e.Status)
                {
                    case WebExceptionStatus.Timeout:
                        _connectionStatus = csEnum.Timeout;
                        break;
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.NameResolutionFailure:
                        _connectionStatus = csEnum.CantConnect;
                        break;
                }
            }
            return _commandResult;
        }

        private void LogError(Exception e, string source)
        {
            var error = string.Format("[{0}] - [{1}] - {2}", DateTime.Now, source, e.Message);
            _errorList.Add(error);
            _lastError = error;
        }
    }
}
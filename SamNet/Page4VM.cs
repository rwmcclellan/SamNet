// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using SharedToolbox;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using SamNet.Native;

namespace SamNet
{
    public class Page4VM : ObservableObject, IPageVM
    {

        #region Vars and Constructor

        private Action<PageMessage>? PageAction;
        private SamWorker? Work;
        private ConcurrentQueue<object>? MessageToWorker;

        private EdwardsMessageBus eventBus;

        private AutoResetEvent are;

        private int thisPage = 4;


        private int _SelectedTabIndex;
        private int _TabItemState;

        ObservableCollection<LogSnippet> _LogSnippets;
        private string _ReviewMessage = "";
        private string _ReviewStatusMessage = "";
        private string _UserMessage = "";
        private Brush _ReviewStatusColor = Brushes.Red;
        #endregion

        #region Constructor And Focus

        public Page4VM(Action<PageMessage> act, SamWorker? work, ConcurrentQueue<object>? messageToWorker)
        {
            PageAction = act;
            MessageToWorker = messageToWorker;
            Work = work;

            eventBus = (EdwardsMessageBus)App.Current.Resources["EventBus"];

            are = new AutoResetEvent(false);

            ReviewMessage = "Event Viewer - Newest event on top";
            ReviewStatusMessage = "Code Green";
            ReviewStatusColor = Brushes.Green;

            _LogSnippets = new ObservableCollection<LogSnippet>();

        }
        public void IsInFocus(int caller, object? payload = null)
        {
            SamModel.CurrentPage = thisPage;
            Action<UiMessage> UiMessageCallBack = new Action<UiMessage>(UiMessageAction);
            Action<ByteArrayMessageOut> ByteArrayMessageCallBack = new Action<ByteArrayMessageOut>(ByteArrayMessageAction);
            Work?.SetActionDestinations(UiMessageCallBack, ByteArrayMessageCallBack);
            TabItemState = (int)P4Tabs.Console;
            if (caller == 1)
            {
                TabItemState = (int)(P4Tabs.SamSetup | P4Tabs.Console);
            }
            else if (caller == 2)
            {
                TabItemState = (int)(P4Tabs.Edit | P4Tabs.Console);
            }
            else if (caller == 3)
            {
                TabItemState = (int)(P4Tabs.DataSet | P4Tabs.Console);
            }
            SelectedTabIndex = 0;

            int count = 0;
            _LogSnippets.Clear();
            for (int i = SamLog.Logger.Count - 1; i >= 0; i--)
            {
                _LogSnippets.Add(new LogSnippet(SamLog.Logger[i]));
                count += SamLog.Logger[i].LineCount;
                if (count > 48) break;
            }


        }

        #endregion

        #region Properties
        public ObservableCollection<LogSnippet> LogSnippets
        {
            get
            {
                return _LogSnippets;
            }
        }
        public string ReviewMessage
        {
            get { return _ReviewMessage; }
            set
            {
                if (_ReviewMessage != value)
                {
                    _ReviewMessage = value;
                    OnPropertyChanged("ReviewMessage");
                }
            }
        }
        public Brush ReviewStatusColor
        {
            get { return _ReviewStatusColor; }
            set
            {
                if (_ReviewStatusColor != value)
                {
                    _ReviewStatusColor = value;
                    OnPropertyChanged("ReviewStatusColor");
                }
            }
        }
        public string ReviewStatusMessage
        {
            get { return _ReviewStatusMessage; }
            set
            {
                if (_ReviewStatusMessage != value)
                {
                    _ReviewStatusMessage = value;
                    OnPropertyChanged("ReviewStatusMessage");
                }
            }
        }
        public int SelectedTabIndex
        {
            get { return _SelectedTabIndex; }
            set
            {
                if (_SelectedTabIndex != value)
                {
                    _SelectedTabIndex = value;
                    OnPropertyChanged("SelectedTabIndex");
                }
            }
        }
        public string Name
        {
            get { return "Page 4"; }
        }
        public int TabItemState
        {
            get { return _TabItemState; }
            set
            {
                if (_TabItemState != value)
                {
                    _TabItemState = value;
                    OnPropertyChanged("TabItemState");
                }
            }
        }

        public string UserMessage
        {
            get { return _UserMessage; }
            set
            {
                if (_UserMessage != value)
                {
                    _UserMessage = value;
                    OnPropertyChanged("UserMessage");
                }
            }
        }

        #endregion

        #region Events

        public void ButtonPressed(object sender, EventArgs e)
        {
            Button tb = (Button)sender;
            string? tag = tb.Tag.ToString();
            if (tag is not null)
            {
                if (tag.Equals("NothingB"))
                {

                }
            }
        }

        public void TabControlChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl == false) return;
            TabItem? tab = (sender as TabControl)?.SelectedItem as TabItem;
            if (tab == null) return;
            string? tag = tab.Tag.ToString();
            if (tag is not null)
            {
                if (tag.Equals("Console"))
                {  // View snippets latest first,  todo scroll back
                    _LogSnippets.Clear();
                    int count = 0;
                    for (int i = SamLog.Logger.Count - 1; i >= 0; i--)
                    {
                        _LogSnippets.Add(new LogSnippet(SamLog.Logger[i]));
                        count += SamLog.Logger[i].LineCount;
                        if (count > 48) break;
                    }
                }
                else if (tag.Equals("SamSetup"))   //  Swap views
                {
                    PageAction?.Invoke(new PageMessage(1, thisPage));
                }
                else if (tag.Equals("Edit"))   //  Swap views
                {
                    PageAction?.Invoke(new PageMessage(2, thisPage));
                }
                else if (tag.Equals("DataSet"))   //  Swap views
                {
                    PageAction?.Invoke(new PageMessage(3, thisPage));
                }

            }
        }

        #endregion

        #region Methods

        #endregion

        #region Callbacks

        private void UiMessageAction(UiMessage uimess)
        {

        }

        private void ByteArrayMessageAction(ByteArrayMessageOut bamo)
        {

        }


        #endregion

    }


    public class LogSnippet : ObservableObject
    {
        public string _Time = string.Empty;
        public string _Caller = string.Empty;
        public string _Message = string.Empty;
        public string Time
        {
            get { return _Time; }
            set
            {
                if (_Time != value)
                {
                    _Time = value;
                    OnPropertyChanged("Time");
                }
            }
        }
        public string Caller
        {
            get { return _Caller; }
            set
            {
                if (_Caller != value)
                {
                    _Caller = value;
                    OnPropertyChanged("Caller");
                }
            }
        }
        public string Message
        {
            get { return _Message; }
            set
            {
                if (_Message != value)
                {
                    _Message = value;
                    OnPropertyChanged("Message");
                }
            }
        }

        public LogSnippet(LogEntry logentry)
        {
            Time = logentry.Time;
            Caller = logentry.Caller;
            Message = logentry.Message;
        }
    }

    public enum P4Tabs
    {
        SamSetup = 1,
        Edit = 2,
        DataSet = 4,
        Console = 32
    }

}
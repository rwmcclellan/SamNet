// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using SharedToolbox;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Media;
using SamNet.Native;

namespace SamNet
{
    public class MainWindowVM : ObservableObject
    {

        #region Vars and Constructor

        private bool _IsPopupOpen;
        private Brush? _PopupBrush;
        private string _PopupMessage = "";

        private IPageVM? _currentPageVM;
        private List<IPageVM>? _pageVMs;
        private MainWindow main;
        private SamWorker? Work;
        private ConcurrentQueue<object>? MessageToWorker;


        public MainWindowVM(MainWindow mainWin)
        {
            //         pageBus = (MessageBus)App.Current.Resources["PageBus"];
            //         pageBus.Subscribe<int>(BusChangePage);
            //        pageBus.Subscribe<PopupMessenger>(PopupMessengerHandler);

            Action<PageMessage> PageAction = new Action<PageMessage>(PageMailbox);
            MessageToWorker = new ConcurrentQueue<object>();
            CreateWorkerObject();

            PageVMs.Add(null!);
            PageVMs.Add(new Page1VM(PageAction, Work, MessageToWorker));
            PageVMs.Add(new Page2VM(PageAction, Work, MessageToWorker));
            PageVMs.Add(new Page3VM(PageAction, Work, MessageToWorker));
            PageVMs.Add(new Page4VM(PageAction, Work, MessageToWorker));

            CurrentPageVM = PageVMs[1];
            CurrentPageVM.IsInFocus(0, null);

            main = mainWin;
            string procName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName("SamNet");
        }

        #endregion

        #region Properties / Commands

        /*
        public ICommand ChangePageCommand
        {
            get
            {
                if (_changePageCommand == null)
                {
                    _changePageCommand = new RelayCommand(
                        p => ChangeViewModel((IPageVM)p),
                        p => p is IPageVM);
                }

                return _changePageCommand;
            }
        }
        */
        public IPageVM CurrentPageVM
        {
            get { return _currentPageVM!; }
            set
            {
                if (_currentPageVM != value)
                {
                    _currentPageVM = value;
                    OnPropertyChanged("CurrentPageVM");
                }
            }
        }

        public bool IsPopupOpen
        {
            get { return _IsPopupOpen; }
            set
            {
                if (_IsPopupOpen != value)
                {
                    _IsPopupOpen = value;
                    OnPropertyChanged("IsPopupOpen");
                }
            }
        }

        public List<IPageVM> PageVMs
        {
            get
            {
                if (_pageVMs == null) _pageVMs = new List<IPageVM>();
                return _pageVMs;
            }
        }

        public Brush PopupBrush
        {
            get { return _PopupBrush!; }
            set
            {
                if (_PopupBrush != value)
                {
                    _PopupBrush = value;
                    OnPropertyChanged("PopupBrush");
                }
            }
        }

        public string PopupMessage
        {
            get { return _PopupMessage; }
            set
            {
                if (_PopupMessage != value)
                {
                    _PopupMessage = value;
                    OnPropertyChanged("PopupMessage");
                }
            }
        }

        #endregion

        #region Methods / Events

        private void CreateWorkerObject()
        {
            WorkerConfig wc = new WorkerConfig();
            wc.IsDebug = true;
            wc.DebugLevel = 0;
            Work = new SamWorker(wc, MessageToWorker);
        }


        private void PageMailbox(PageMessage iPage)
        {
            if (iPage.NextPage > 0)
            {
                if (PageVMs[iPage.NextPage] == null) return;
                CurrentPageVM = PageVMs[iPage.NextPage];
                CurrentPageVM.IsInFocus(iPage.ThisPage, iPage.Payload);
            }
            else
            {

            }

        }

        #endregion

    }

    public class EventMessage
    {
        public int Dest;
        public int Source;
        public EventEnum EventType;

        public EventMessage(int d, int s, EventEnum m)
        {
            Dest = d;
            Source = s;
            EventType = m;
        }
    }

    public class PageMessage
    {
        public int NextPage;
        public int ThisPage;
        public object? Payload;

        public PageMessage(int next, int current, object? payload = null)
        {
            NextPage = next;
            ThisPage = current;
            Payload = payload;
        }
    }

    public class PopupMessage
    {
        public Brush color;
        public string message;

        public PopupMessage(Brush c, string m)
        {
            color = c;
            message = m;
        }
    }

    public enum EventEnum
    {
        Close = 1,
        ResetXaml = 2
    }
}
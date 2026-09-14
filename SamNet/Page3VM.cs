// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SharedToolbox;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SamNet.Native;
using Open.IP;

namespace SamNet
{
    public class Page3VM : ObservableObject, IPageVM
    {

        #region Vars and Constructor

        private Action<PageMessage>? PageAction;
        private SamWorker? Work;
        private ConcurrentQueue<object>? MessageToWorker;

        private EdwardsMessageBus eventBus;

        private AutoResetEvent are;

        private int thisPage = 3;

        private BitmapSource? _BS;
        private BitmapSource? _BS_Roi;
        private BitmapSource? _BS_Three;

        private DataSetPayload Info;

        private bool _IsEditMainName;
        private bool _IsExportButtonShowing;
        private bool _IsExportPathAvailable;
        private bool _IsLinesPopulated;
        private bool _IsShowMainLabelButton;
        private bool _IsShowSubLabelButton;

        private bool AreRadioButtonsActive = false;
        private bool UseDummyData = false;

        private float _DetectionAScore = 0.0f;
        private float _DetectionBScore = 0.0f;
        private float _DetectionCScore = 0.0f;

        private int _BS_CanvasHeight;
        private int _BS_CanvasWidth;
        private int _BS_Height;
        private int _BS_Left;
        private int _BS_Top;
        private int _BS_Width;
        private int _BS_Roi_Height;
        private int _BS_Roi_Left;
        private int _BS_Roi_Top;
        private int _BS_Roi_Width;
        private int _BS_Three_Height;
        private int _BS_Three_Left;
        private int _BS_Three_Top;
        private int _BS_Three_Width;

        private int _EditSubLabelState;
        private int _TabItemState;
        private int _PreviousTabIndex;
        private int _RBMaskViewState;

        private int _SelectedTabFlag;
        private int _SelectedTabIndex;

        private double BS_Max_Width = 1316;   // Was 1300
        private double BS_Max_Height = 832;   // Was 822

        private string ChangeMainLabelUncleaned = "";
        private string ChangeSubLabelUncleaned = "";
        private string ExportNameUncleaned = "";
        private string MainLabelUncleaned = "";
        private string SubLabelUncleaned = "";

        private string _ChangeMainLabel = "";
        private string _CurrentMainLabel = "";
        private string _ChangeSubLabel = "";
        private string _CurrentSubLabel = "";
        private string _DetectionMessage = "";
        private string _ExportFolder = "";
        private string _ExportName = "";
        private string _ExportPath = "";
        private string _LabelMessage = "";
        private string _MainLabel = "";

        private string _SelectedMain = "";
        private int _SelectedMainIndex;
        private int _SelectedSubIndex;
        private string _SubLabel = "";
        private string _UserMessage = "";

        DispatcherTimer UpdateTimer;

        ObservableCollection<string> _Mains;
        ObservableCollection<SubLabelInfo> _Subs;
        List<SubLabelInfo> _Dummys;


        #endregion

        #region Constructor And Focus
        public Page3VM(Action<PageMessage> act, SamWorker? work, ConcurrentQueue<object>? messageToWorker)
        {
            PageAction = act;
            MessageToWorker = messageToWorker;
            Work = work;

            UserMessage = "SamNet - DataSet";

            eventBus = (EdwardsMessageBus)App.Current.Resources["EventBus"];

            BS_CanvasWidth = 1320;   // was 1320
            BS_CanvasHeight = 836;   // was 836
            BS_Width = (int)BS_Max_Width;
            BS_Height = (int)BS_Max_Height;
            BS_Left = 5;
            BS_Top = 5;

            Info = new DataSetPayload();

            are = new AutoResetEvent(false);

            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Interval = TimeSpan.FromMilliseconds(50);
            UpdateTimer.Tick += UpdateTimer_Tick;
            UpdateTimer.Stop();

            _Mains = new ObservableCollection<string>();
            _Subs = new ObservableCollection<SubLabelInfo>();
            _Dummys = new List<SubLabelInfo>();

            RBMaskViewState = (int)RBMaskView.FullMask;
        }
        public void IsInFocus(int caller, object? payload = null)
        {
            Action<UiMessage> UiMessageCallBack = new Action<UiMessage>(UiMessageAction);
            Action<ByteArrayMessageOut> ByteArrayMessageCallBack = new Action<ByteArrayMessageOut>(ByteArrayMessageAction);
            Work?.SetActionDestinations(UiMessageCallBack, ByteArrayMessageCallBack);

            if (payload != null)
            {
                if (payload is DataSetPayload)
                {
                    Info = (DataSetPayload)payload;
                    if (Info.AddDetections) AddDetections();
                }
            }
            IsEditMainName = true;
            EditSubLabelState = 1;

            //         Resize_Bitmap();
            BS = null;
            UpdateTimer.Tag = "CombineMasks";
            UpdateTimer.Start();
            AreRadioButtonsActive = true;
            MainLabel = "";
            //      _Mains = new ObservableCollection<string> { "Alice", "Bob", "Charlie", "David", "Eve", "Frank" };
            //      SelectedMain = Mains[0]; // Default selection
            _Mains = new ObservableCollection<string>();
            _Subs = new ObservableCollection<SubLabelInfo>();
            _Dummys = new List<SubLabelInfo>();
            _Dummys.Add(new SubLabelInfo("Frank", "Box", 1));
            _Dummys.Add(new SubLabelInfo("Frank", "", 2));
            _Dummys.Add(new SubLabelInfo("Frank", "ElephantElephantElephant", 3));
            _Dummys.Add(new SubLabelInfo("Charlie", "Dog", 4));
            _Dummys.Add(new SubLabelInfo("Charlie", "Kitten", 5));
            _Dummys.Add(new SubLabelInfo("David", "Broccoli", 6));
            _Dummys.Add(new SubLabelInfo("David", "Carrots", 7));
            _Dummys.Add(new SubLabelInfo("Alice", "Subaru", 8));
            _Dummys.Add(new SubLabelInfo("Alice", "Ford", 9));
            _Dummys.Add(new SubLabelInfo("Alice", "Chevrolet", 10));
            _Dummys.Add(new SubLabelInfo("Alice", "Toyota", 11));
            _Dummys.Add(new SubLabelInfo("Bob", "Air Conditioner", 12));
            _Dummys.Add(new SubLabelInfo("Eve", "Summer", 13));

            if ((SamModel.mtMask is null) || (SamModel.mtMask.Rows != SamModel.mtImage.Rows))
            {
                SamModel.mtMask = new Mat(SamModel.mtImage.Rows, SamModel.mtImage.Cols, DepthType.Cv8U, 1);
                SamModel.mtMask.SetTo(new MCvScalar(0));
            }

            // Populate the actual data 
            UseDummyData = false;
            GenerateMains(UseDummyData);

            if ((SelectedMainIndex >= Mains.Count()) || SelectedSubIndex >= Subs.Count())
            {
                CurrentMainLabel = "";
                CurrentSubLabel = "";
                SelectedTabIndex = 0;

                return;
            }

            if (caller == 1)  //  This is an import or Sam3,  the dataset exists but not the detections if an import
            {
                CurrentMainLabel = Mains[SelectedMainIndex];
                CurrentSubLabel = Subs[SelectedSubIndex].SubLabel;
                RefreshShownImages(Subs[SelectedSubIndex].UniqueID);
                BS = ImageSupportWPF.ToBitmapSource(SamModel.mtImage);
                SelectedTabIndex = 0;
            }
            else if (caller == 2)  //  This is after a Sam Op or an edit
            {
                if ((int)Info.Operation > 0)
                {
                    BS = ImageSupportWPF.ToBitmapSource(SamModel.mtImage);
                    SamDataSet? sds = SamModel.DataSet.FirstOrDefault(a => a.UniqueID == Info.CurrentUniqueID);
                    if (sds != null)
                    {
                        GenerateMains(UseDummyData, sds.Label);   // We want to select the Label sent in
                    }
                    else if (SamModel.DataSet.Count() > 0)  // No Unique Id supplied
                    {
                        GenerateMains(UseDummyData);
                    }
                    else
                    {
                        Mains.Clear();
                        Subs.Clear();
                    }
                    for (int i = 0; i < Subs.Count(); i++)   // Find where the right subs record for the unique ID
                    {
                        if ((Subs[i].UniqueID) == Info.CurrentUniqueID)
                        {
                            for (int j = 0; j < Mains.Count(); j++)
                            {
                                if (Mains[j].Equals(Subs[i].MainLabel))
                                {
                                    SelectedMainIndex = j;
                                    SelectedSubIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }

                if ((SelectedMainIndex < 0) || (SelectedMainIndex >= Mains.Count())) return;
                if ((SelectedSubIndex < 0) || (SelectedSubIndex >= Subs.Count())) return;


                CurrentMainLabel = Mains[SelectedMainIndex];
                CurrentSubLabel = Subs[SelectedSubIndex].SubLabel;
                RefreshShownImages(Subs[SelectedSubIndex].UniqueID);
                SelectedTabIndex = (int)(Math.Log((int)P3Tabs.DataSet, 2));
            }
            else if (caller == 4)
            {
                SelectedTabIndex = PreviousTabIndex;
            }
            else
            {
                SelectedTabIndex = 0;
            }

            ExportName = "Default";

            if ((int)SamModel.SEnum < 1)
            {
                TabItemState = (int)(P3Tabs.Console | P3Tabs.DataSet | P3Tabs.Edit | P3Tabs.Export);
            }
            else
            {
                TabItemState = (int)(P3Tabs.Console | P3Tabs.DataSet | P3Tabs.Edit | P3Tabs.Export | P3Tabs.NewSam);
            }
        }

        #endregion

        #region Properties
        public ObservableCollection<string> Mains
        {
            get
            {
                return _Mains;
            }
        }
        public ObservableCollection<SubLabelInfo> Subs
        {
            get
            {
                return _Subs;
            }
        }
        public BitmapSource? BS
        {

            get { return _BS; }
            set
            {
                _BS = value;
                OnPropertyChanged("BS");
            }
        }
        public BitmapSource? BS_Roi
        {

            get { return _BS_Roi; }
            set
            {
                _BS_Roi = value;
                OnPropertyChanged("BS_Roi");
            }
        }
        public BitmapSource? BS_Three
        {

            get { return _BS_Three; }
            set
            {
                _BS_Three = value;
                OnPropertyChanged("BS_Three");
            }
        }
        public int BS_CanvasHeight
        {
            get { return _BS_CanvasHeight; }
            set
            {
                if (_BS_CanvasHeight != value)
                {
                    _BS_CanvasHeight = value;
                    OnPropertyChanged("BS_CanvasHeight");
                }
            }
        }
        public int BS_CanvasWidth
        {
            get { return _BS_CanvasWidth; }
            set
            {
                if (_BS_CanvasWidth != value)
                {
                    _BS_CanvasWidth = value;
                    OnPropertyChanged("BS_CanvasWidth");
                }
            }
        }
        public int BS_Height
        {
            get { return _BS_Height; }
            set
            {
                if (_BS_Height != value)
                {
                    _BS_Height = value;
                    OnPropertyChanged("BS_Height");
                }
            }
        }
        public int BS_Left
        {
            get { return _BS_Left; }
            set
            {
                if (_BS_Left != value)
                {
                    _BS_Left = value;
                    OnPropertyChanged("BS_Left");
                }
            }
        }
        public int BS_Top
        {
            get { return _BS_Top; }
            set
            {
                if (_BS_Top != value)
                {
                    _BS_Top = value;
                    OnPropertyChanged("BS_Top");
                }
            }
        }
        public int BS_Width
        {
            get { return _BS_Width; }
            set
            {
                if (_BS_Width != value)
                {
                    _BS_Width = value;
                    OnPropertyChanged("BS_Width");
                }
            }
        }
        public int BS_Roi_Height
        {
            get { return _BS_Roi_Height; }
            set
            {
                if (_BS_Roi_Height != value)
                {
                    _BS_Roi_Height = value;
                    OnPropertyChanged("BS_Roi_Height");
                }
            }
        }
        public int BS_Roi_Left
        {
            get { return _BS_Roi_Left; }
            set
            {
                if (_BS_Roi_Left != value)
                {
                    _BS_Roi_Left = value;
                    OnPropertyChanged("BS_Roi_Left");
                }
            }
        }
        public int BS_Roi_Top
        {
            get { return _BS_Roi_Top; }
            set
            {
                if (_BS_Roi_Top != value)
                {
                    _BS_Roi_Top = value;
                    OnPropertyChanged("BS_Roi_Top");
                }
            }
        }
        public int BS_Roi_Width
        {
            get { return _BS_Roi_Width; }
            set
            {
                if (_BS_Roi_Width != value)
                {
                    _BS_Roi_Width = value;
                    OnPropertyChanged("BS_Roi_Width");
                }
            }
        }
        public int BS_Three_Height
        {
            get { return _BS_Three_Height; }
            set
            {
                if (_BS_Three_Height != value)
                {
                    _BS_Three_Height = value;
                    OnPropertyChanged("BS_Three_Height");
                }
            }
        }
        public int BS_Three_Left
        {
            get { return _BS_Three_Left; }
            set
            {
                if (_BS_Three_Left != value)
                {
                    _BS_Three_Left = value;
                    OnPropertyChanged("BS_Three_Left");
                }
            }
        }
        public int BS_Three_Top
        {
            get { return _BS_Three_Top; }
            set
            {
                if (_BS_Three_Top != value)
                {
                    _BS_Three_Top = value;
                    OnPropertyChanged("BS_Three_Top");
                }
            }
        }
        public int BS_Three_Width
        {
            get { return _BS_Three_Width; }
            set
            {
                if (_BS_Three_Width != value)
                {
                    _BS_Three_Width = value;
                    OnPropertyChanged("BS_Three_Width");
                }
            }
        }
        public string ChangeMainLabel
        {
            get { return _ChangeMainLabel; }
            set
            {
                if ((_ChangeMainLabel != value) || (_ChangeMainLabel != ChangeMainLabelUncleaned))
                {
                    _ChangeMainLabel = value;
                    OnPropertyChanged("ChangeMainLabel");
                }
            }
        }
        public string ChangeSubLabel
        {
            get { return _ChangeSubLabel; }
            set
            {
                if ((_ChangeSubLabel != value) || (_ChangeSubLabel != ChangeSubLabelUncleaned))
                {
                    _ChangeSubLabel = value;
                    OnPropertyChanged("ChangeSubLabel");
                }
            }
        }
        public string CurrentMainLabel
        {
            get { return _CurrentMainLabel; }
            set
            {
                if (_CurrentMainLabel != value)
                {
                    _CurrentMainLabel = value;
                    OnPropertyChanged("CurrentMainLabel");
                }
            }
        }
        public string CurrentSubLabel
        {
            get { return _CurrentSubLabel; }
            set
            {
                if (_CurrentSubLabel != value)
                {
                    _CurrentSubLabel = value;
                    OnPropertyChanged("CurrentSubLabel");
                }
            }
        }
        public float DetectionAScore
        {
            get { return _DetectionAScore; }
            set
            {
                if (_DetectionAScore != value)
                {
                    _DetectionAScore = value;
                    OnPropertyChanged("DetectionAScore");
                }
            }
        }
        public float DetectionBScore
        {
            get { return _DetectionBScore; }
            set
            {
                if (_DetectionBScore != value)
                {
                    _DetectionBScore = value;
                    OnPropertyChanged("DetectionBScore");
                }
            }
        }
        public float DetectionCScore
        {
            get { return _DetectionCScore; }
            set
            {
                if (_DetectionCScore != value)
                {
                    _DetectionCScore = value;
                    OnPropertyChanged("DetectionCScore");
                }
            }
        }
        public string DetectionMessage
        {
            get { return _DetectionMessage; }
            set
            {
                if (_DetectionMessage != value)
                {
                    _DetectionMessage = value;
                    OnPropertyChanged("DetectionMessage");
                }
            }
        }
        public int EditSubLabelState
        {
            get { return _EditSubLabelState; }
            set
            {
                if (_EditSubLabelState != value)
                {
                    _EditSubLabelState = value;
                    OnPropertyChanged("EditSubLabelState");
                }
            }
        }
        public string ExportFolder
        {
            get { return _ExportFolder; }
            set
            {
                if (_ExportFolder != value)
                {
                    _ExportFolder = value;
                    OnPropertyChanged("ExportFolder");
                }
            }
        }
        public string ExportName
        {
            get { return _ExportName; }
            set
            {
                if ((_ExportName != value) || (_ExportName != ExportNameUncleaned))
                {
                    _ExportName = value;
                    OnPropertyChanged("ExportName");
                }
            }
        }
        public string ExportPath
        {
            get { return _ExportPath; }
            set
            {
                if ((_ExportPath != value) || (_ExportPath != ExportNameUncleaned))
                {
                    _ExportPath = value;
                    OnPropertyChanged("ExportPath");
                }
            }
        }
        public bool IsEditMainName
        {
            get { return _IsEditMainName; }
            set
            {
                if (_IsEditMainName != value)
                {
                    _IsEditMainName = value;
                    OnPropertyChanged("IsEditMainName");
                }
            }
        }
        public bool IsExportButtonShowing
        {
            get { return _IsExportButtonShowing; }
            set
            {
                if (_IsExportButtonShowing != value)
                {
                    _IsExportButtonShowing = value;
                    OnPropertyChanged("IsExportButtonShowing");
                }
            }
        }
        public bool IsExportPathAvailable
        {
            get { return _IsExportPathAvailable; }
            set
            {
                if (_IsExportPathAvailable != value)
                {
                    _IsExportPathAvailable = value;
                    OnPropertyChanged("IsExportPathAvailable");
                }
            }
        }
        public bool IsLinesPopulated
        {
            get { return _IsLinesPopulated; }
            set
            {
                if (_IsLinesPopulated != value)
                {
                    _IsLinesPopulated = value;
                    OnPropertyChanged("IsLinesPopulated");
                }
            }
        }
        public bool IsShowMainLabelButton
        {
            get { return _IsShowMainLabelButton; }
            set
            {
                if (_IsShowMainLabelButton != value)
                {
                    _IsShowMainLabelButton = value;
                    OnPropertyChanged("IsShowMainLabelButton");
                }
            }
        }
        public bool IsShowSubLabelButton
        {
            get { return _IsShowSubLabelButton; }
            set
            {
                if (_IsShowSubLabelButton != value)
                {
                    _IsShowSubLabelButton = value;
                    //      if (value == false) IsDetectionUpdateWaiting = false;
                    OnPropertyChanged("IsShowSubLabelButton");
                }
            }
        }
        public string LabelMessage
        {
            get { return _LabelMessage; }
            set
            {
                if (_LabelMessage != value)
                {
                    _LabelMessage = value;
                    OnPropertyChanged("LabelMessage");
                }
            }
        }
        public string MainLabel
        {
            get { return _MainLabel; }
            set
            {
                if ((_MainLabel != value) || (_MainLabel != MainLabelUncleaned))
                {
                    _MainLabel = value;
                    OnPropertyChanged("MainLabel");
                }
            }
        }
        public string Name
        {
            get { return "Page 3"; }
        }
        public int PreviousTabIndex
        {
            get { return _PreviousTabIndex; }
            set
            {
                if (_PreviousTabIndex != value)
                {
                    _PreviousTabIndex = value;
                    OnPropertyChanged("PreviousTabIndex");
                }
            }
        }
        public int RBMaskViewState
        {
            get { return _RBMaskViewState; }
            set
            {
                if (_RBMaskViewState != value)
                {
                    _RBMaskViewState = value;
                    OnPropertyChanged("RBMaskViewState");
                }
            }
        }
        public int SelectedTabFlag
        {
            get { return _SelectedTabFlag; }
            set
            {
                if (_SelectedTabFlag != value)
                {
                    _SelectedTabFlag = value;
                    OnPropertyChanged("SelectedTabFlag");
                }
            }
        }
        public string SelectedMain
        {
            get { return _SelectedMain; }
            set
            {
                if (_SelectedMain != value)
                {
                    _SelectedMain = value;
                    OnPropertyChanged("SelectedMain");
                }
            }
        }
        public int SelectedMainIndex
        {
            get { return _SelectedMainIndex; }
            set
            {
                if (_SelectedMainIndex != value)
                {
                    _SelectedMainIndex = value;
                    if ((SelectedMainIndex >= 0) && (SelectedMainIndex < Mains.Count))
                    {
                        CurrentMainLabel = Mains[SelectedMainIndex];
                        Subs.Clear();
                        GenerateSubs(UseDummyData, CurrentMainLabel);
                    }
                    OnPropertyChanged("SelectedMainIndex");
                }
            }
        }
        public int SelectedSubIndex
        {
            get { return _SelectedSubIndex; }
            set
            {
                if (_SelectedSubIndex != value)
                {
                    _SelectedSubIndex = value;
                    if ((SelectedSubIndex >= 0) && (SelectedSubIndex < Subs.Count))
                    {
                        CurrentSubLabel = Subs[SelectedSubIndex].SubLabel;
                        int unique = Subs[SelectedSubIndex].UniqueID;
                        RefreshShownImages(unique);
                    }
                    OnPropertyChanged("SelectedSubIndex");
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
                    PreviousTabIndex = _SelectedTabIndex;
                    _SelectedTabIndex = value;
                    SelectedTabFlag = 1 << _SelectedTabIndex;
                    OnPropertyChanged("SelectedTabIndex");
                }
            }
        }
        public string SubLabel
        {
            get { return _SubLabel; }
            set
            {
                if ((_SubLabel != value) || (_SubLabel != SubLabelUncleaned))
                {
                    _SubLabel = value;
                    OnPropertyChanged("SubLabel");
                }
            }
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
                if (tag.Equals("Export"))
                {
                    IsExportButtonShowing = false;
                    UpdateTimer.Tag = "Export";
                    UpdateTimer.Start();
                }
                else if (tag.Equals("Overwrite"))
                {
                    IsExportButtonShowing = false;
                    UpdateTimer.Tag = "Export";
                    UpdateTimer.Start();
                }
                else if (tag.Equals("ChangeMainLabel"))
                {
                    if (SelectedMainIndex < Mains.Count)
                    {
                        string mainString = Mains[SelectedMainIndex];
                        for (int i = 0; i < SamModel.DataSet.Count; i++)
                        {
                            if (SamModel.DataSet[i].Label.Equals(mainString))
                            {
                                SamModel.DataSet[i].Label = MainLabel;
                            }
                        }
                        Mains.Clear();
                        Subs.Clear();
                        GenerateMains(UseDummyData);
                        for (int i = 0; i < Mains.Count(); i++)
                        {
                            if (Mains[i].Equals(MainLabel))
                            {
                                SelectedMainIndex = i;
                                break;
                            }
                        }
                        MainLabel = "";
                    }
                }
                else if (tag.Equals("ChangeSubLabel"))
                {
                    int sourceSelectedSubIndex = SelectedSubIndex;
                    if (SelectedSubIndex < Subs.Count)
                    {
                        int uniqueID = Subs[SelectedSubIndex].UniqueID;
                        for (int i = 0; i < SamModel.DataSet.Count; i++)
                        {
                            if (SamModel.DataSet[i].UniqueID == uniqueID)
                            {
                                SamModel.DataSet[i].SubLabel = SubLabel;
                                break;
                            }
                        }
                        SubLabel = "";
                        Mains.Clear();
                        Subs.Clear();
                        GenerateMains(UseDummyData); ;
                        for (int i = 0; i < Subs.Count(); i++)
                        {
                            if (Subs[i].UniqueID == uniqueID)
                            {
                                SelectedSubIndex = i;
                                break;
                            }
                        }

                    }
                }
                else if (tag.Equals("ChangeParentLabel"))
                {
                    if (SelectedSubIndex < Subs.Count)
                    {
                        int uniqueID = Subs[SelectedSubIndex].UniqueID;
                        for (int i = 0; i < SamModel.DataSet.Count; i++)
                        {
                            if (SamModel.DataSet[i].UniqueID == uniqueID)
                            {
                                SamModel.DataSet[i].Label = SubLabel;
                                break;
                            }
                        }
                        SubLabel = "";
                        Mains.Clear();
                        Subs.Clear();
                        GenerateMains(UseDummyData);
                    }
                }
                else if (tag.Equals("DeleteMainLabel"))
                {
                    if (SelectedMainIndex < Mains.Count)
                    {
                        string mainString = Mains[SelectedMainIndex];
                        for (int i = SamModel.DataSet.Count - 1; i >= 0; i--)
                        {
                            if (SamModel.DataSet[i].Label == mainString)
                            {
                                SamModel.DataSet.RemoveAt(i);
                            }
                        }
                        SubLabel = "";
                        Mains.Clear();
                        Subs.Clear();
                        GenerateMains(UseDummyData);
                    }
                }
                else if (tag.Equals("DeleteSubLabel"))
                {
                    if (SelectedSubIndex < Subs.Count)
                    {
                        int uniqueID = Subs[SelectedSubIndex].UniqueID;
                        for (int i = SamModel.DataSet.Count - 1; i >= 0; i--)
                        {
                            if (SamModel.DataSet[i].UniqueID == uniqueID)
                            {
                                SamModel.DataSet.RemoveAt(i);
                                break;
                            }
                        }
                        SubLabel = "";
                        Mains.Clear();
                        Subs.Clear();
                        GenerateMains(UseDummyData);
                    }
                }
            }
        }
        public void MainLabelSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.SelectedIndex < Mains.Count())
            {
                SelectedMainIndex = lb.SelectedIndex;
            }
        }

        public void SubLabelSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.SelectedIndex < Subs.Count())
            {
                SelectedSubIndex = lb.SelectedIndex;
            }
        }

        public void RadioButtonChecked(object sender, EventArgs e)
        {
            if (AreRadioButtonsActive == false) return;
            RadioButton tb = (RadioButton)sender;
            string? tag = tb.Tag.ToString();
            string? group = tb.GroupName;
            if ((tag is null) || (group is null)) return;
            int itag = 0;
            bool tagSuccess = int.TryParse(tag, out itag);
            if (tagSuccess)
            {
                if (group.Equals("RBMaskView"))
                {
                    RBMaskViewState = itag;    // See RBMaskView Enum
                    RefreshShownImages(Subs[SelectedSubIndex].UniqueID, itag - 1);
                }
            }


            if (tag!.Equals("ViewOriginal"))
            {
                RefreshShownImages(Subs[SelectedSubIndex].UniqueID, 1);
            }
            else if (tag!.Equals("ViewFullMask"))
            {
                RefreshShownImages(Subs[SelectedSubIndex].UniqueID, 0);
            }
            else if (tag!.Equals("ViewOverlay"))
            {
                RefreshShownImages(Subs[SelectedSubIndex].UniqueID, 2);
            }
            else if (tag!.Equals("MainName"))
            {
                IsEditMainName = true;
            }
            else if (tag!.Equals("MainDelete"))
            {
                IsEditMainName = false;
            }
            else if (tag!.Equals("SubName"))
            {
                EditSubLabelState = 1;
            }
            else if (tag!.Equals("SubDelete"))
            {
                EditSubLabelState = 2;
            }
            else if (tag!.Equals("SubParent"))
            {
                EditSubLabelState = 4;
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
                {
                    PageAction?.Invoke(new PageMessage(4, thisPage, PreviousTabIndex));
                }
                else if (tag.Equals("Export"))   //  Swap views
                {
                    TabItemState = (int)(P3Tabs.Console | P3Tabs.DataSet | P3Tabs.Export);
                    ExportFolder = "C:\\Temp\\ExportFolderHardcoded";
                    string candidate = Properties.Settings.Default.CocoExportFolder.ToString();
                    if (candidate.Length > 0) ExportFolder = candidate;
                    if (ExportFolder.EndsWith("\\") == false) ExportFolder += "\\";
                    PerformExportCheck();
                }
                else if (tag.Equals("NewSam"))   //  Swap views
                {
                    TabItemState = (int)(P3Tabs.Console | P3Tabs.DataSet);
                    PageAction?.Invoke(new PageMessage(1, thisPage, new DataSetPayload(OperationEnum.SamNew, -1)));
                }
                else if (tag.Equals("DataSet"))   //  Swap views
                {

                }
                else if (tag.Equals("Edit"))   //  Swap views
                {
                    List<int> members = new List<int>();
                    foreach (SubLabelInfo info in Subs)
                    {
                        if (info.MainLabel.Equals(CurrentMainLabel)) members.Add(info.UniqueID);
                    }
                    DataSetPayload payload = new DataSetPayload(OperationEnum.Edit, members);
                    payload.CurrentUniqueID = -1;
                    if ((SelectedSubIndex >= 0) && (SelectedSubIndex < Subs.Count())) payload.CurrentUniqueID = Subs[SelectedSubIndex].UniqueID;
                    PageAction?.Invoke(new PageMessage(2, thisPage, payload));
                }
            }
        }

        public void TextBoxChangedExport(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            ExportNameUncleaned = tb.Text;
            string cleanedText = Regex.Replace(tb.Text ?? string.Empty, @"[^a-zA-Z0-9 _-]", "");
            ExportName = cleanedText;
            Application.Current.Dispatcher.BeginInvoke(() =>     // Possible race condition on update,  wait till done
            {
                tb.CaretIndex = tb.Text!.Length;
                tb.ScrollToEnd();
                if (ExportName.Length > 0) PerformExportCheck();
            });
        }

        public void TextBoxGotFocusExport(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text.Length > 0)
            {
                tb.CaretIndex = tb.Text!.Length;
                tb.ScrollToEnd();
            }
        }

        public void TextBoxChangedMainLabel(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            MainLabelUncleaned = tb.Text;
            string cleanedText = Regex.Replace(tb.Text ?? string.Empty, @"[^a-zA-Z0-9 _-]", "");
            MainLabel = cleanedText;
            Application.Current.Dispatcher.BeginInvoke(() =>     // Possible race condition on update,  wait till done
            {
                tb.CaretIndex = tb.Text!.Length;
                tb.ScrollToEnd();
            });
            IsShowMainLabelButton = MainLabel.Length > 3;

        }

        public void TextBoxChangedSubLabel(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            SubLabelUncleaned = tb.Text;
            string cleanedText = Regex.Replace(tb.Text ?? string.Empty, @"[^a-zA-Z0-9 _-]", "");
            SubLabel = cleanedText;
            Application.Current.Dispatcher.BeginInvoke(() =>     // Possible race condition on update,  wait till done
            {
                tb.CaretIndex = tb.Text!.Length;
                tb.ScrollToEnd();
            });
            IsShowSubLabelButton = SubLabel.Length > 3;
        }


        #endregion

        #region Methods
        private void AddDetections()
        {
            for (int i = 0; i < SamModel.WorkingDetections.Count(); i++)
            {
                OpenSafeListOfInt safeList = OpenIP.BinaryMatToListOfInt(SamModel.WorkingDetections[i].mtMask);
                if (safeList.IsSuccess)
                {
                    System.Drawing.Rectangle Rect = SamModel.WorkingDetections[i].Rect;
                    SamDataSet sds = new SamDataSet(MainLabel, SubLabel, safeList.ListOfInt, Rect, 0,
                        new List<int> { SamModel.mtImage.Rows, SamModel.mtImage.Cols }, SamModel.WorkingDetections[i].Score);
                    sds.Label = SamModel.WorkingDetections[i].Label;
                    sds.SubLabel = SamModel.WorkingDetections[i].SubLabel;
                    int dataSetId = sds.UniqueID;
                    SamModel.WorkingDetections[i].DataSetUniqueID = dataSetId;
                    SamModel.DataSet.Add(sds);
                }
            }
        }
        private void GenerateMains(bool isDummyData, string mainsSelect = "")
        {
            List<string> names = new List<string>();
            if (isDummyData)
            {
                names = _Dummys.Select(a => a.MainLabel).Distinct().OrderBy(b => b).ToList();
            }
            else
            {
                for (int i = 0; i < SamModel.DataSet.Count(); i++) names.Add(SamModel.DataSet[i].Label);
                names = names.Distinct().OrderBy(b => b).ToList();
            }
            if (names.Count == 0) return;
            _Mains.Clear();
            for (int i = 0; i < names.Count(); i++) _Mains.Add(names[i]);
            CurrentMainLabel = Mains[0];
            SelectedMainIndex = 0;
            if ((Mains.Count > 0) && (mainsSelect.Length > 0))
            {
                int index = Mains.IndexOf(mainsSelect);
                if (index >= 0)
                {
                    CurrentMainLabel = Mains[index];
                    SelectedMainIndex = index;
                }
            }
            GenerateSubs(isDummyData, CurrentMainLabel);
        }
        private void GenerateSubs(bool isDummyData, string str)
        {
            if (SamModel.DataSet.Count() == 0) return;
            List<SubLabelInfo> info = new List<SubLabelInfo>();
            if (isDummyData)
            {
                info = _Dummys.Where(a => a.MainLabel == str).ToList();
            }
            else
            {
                foreach (SamDataSet infoA in SamModel.DataSet)
                {
                    if (infoA.Label == str) info.Add(new SubLabelInfo(infoA.Label, infoA.SubLabel, infoA.UniqueID));
                }
            }
            info = info.OrderBy(a => a.SubLabel).ToList();
            _Subs.Clear();
            foreach (SubLabelInfo infoB in info) _Subs.Add(infoB);
            SelectedSubIndex = 0;
        }
        private void RefreshShownImages(int uniqueId, int mode = 0)
        {
            SamDataSet? sds = SamModel.DataSet.FirstOrDefault(a => a.UniqueID == uniqueId);
            if (sds is null)
            {
                UserMessage = "Cannot display as unique id was not found";
                SamLog.AddEntry("DataSet Display", "Cannot display as unique id was not found");
                return;
            }
            UserMessage = CurrentSubLabel + " - Score: " + sds.Score.ToString();
            Mat mtRoi = new Mat();
            int unique = sds.UniqueID;
            //         OpenSafeMat osm = OpenIP.ListOfIntToBinaryMask(sds.MaskRle, sds.MaskSize[0], sds.MaskSize[1]);
            OpenSafeMat osm = SamModel.CombineListIntDetectionMasks(unique);
            if (osm.IsSuccess)
            {
                if (mode == 1)
                {
                    mtRoi = new Mat(SamModel.mtImage, sds.Rect);
                }
                else if (mode == 2)
                {
                    OpenSafeMat osmImage = OpenIP.CreateRoiMat(SamModel.mtImage, sds.Rect);
                    if (osmImage.IsSuccess)
                    {
                        OpenSafeMat osmMask = OpenIP.CreateRoiMat(osm.Mt, sds.Rect);
                        if (osmMask.IsSuccess)
                        {
                            OpenSafeMat osmOutline = OpenIP.CreateOutlineMat(osmImage.Mt, osmMask.Mt);
                            if (osmOutline.IsSuccess)
                            {
                                mtRoi = osmOutline.Mt;
                            }
                            else
                            {
                                SamLog.AddEntry("CreateOutlineMat", osmOutline);
                            }
                            if (osmMask.Mt != null) osmMask.Mt.Dispose();
                        }
                        else
                        {
                            SamLog.AddEntry("CreateRoiMat mtMask", osmMask);
                            return;
                        }
                        if (osmImage.Mt != null) osmImage.Mt.Dispose();
                    }
                    else
                    {
                        SamLog.AddEntry("CreateRoimat mtImage", osmImage);
                        return;
                    }
                }
                else
                {
                    mtRoi = new Mat(osm.Mt, sds.Rect);
                }
                Resize_BitmapA(mtRoi);
                BS_Roi = ImageSupportWPF.ToBitmapSource(mtRoi);
                if (sds.Rect.Width > 0) CvInvoke.Rectangle(osm.Mt, sds.Rect, new MCvScalar(255), 2);   // Pretty safe call in the CV world
                BS_Three = ImageSupportWPF.ToBitmapSource(osm.Mt);
                if (osm.Mt != null) osm.Mt.Dispose();
            }
            if (mtRoi != null) mtRoi.Dispose();
        }
        private void PerformExport()
        {
            //     if (Directory.Exists(ExportPath)) return;
            DirectoryInfo di = Directory.CreateDirectory(ExportPath);
            if (di.Exists)
            {
                string tmpDir = ExportPath + "\\" + "Annotations";
                if (Directory.Exists(tmpDir) == false) Directory.CreateDirectory(tmpDir);
                tmpDir = ExportPath + "\\" + "Images";
                if (Directory.Exists(tmpDir) == false) Directory.CreateDirectory(tmpDir);
                tmpDir = ExportPath + "\\" + "PNG_Masks";
                if (Directory.Exists(tmpDir) == false) Directory.CreateDirectory(tmpDir);
                OpenSafeBool osb = CocoSupport.ExportCoco(ExportPath, SamModel.DataSet);
                if (osb.IsSuccess)
                {
                    UserMessage = "Export Completed Successfully";
                    SamLog.AddEntry("Coco Export", "Completed Successfully");
                    SelectedTabIndex = (int)Math.Log((int)P3Tabs.DataSet, 2);  // Switch to Dataset tab
                }
                else
                {
                    SamLog.AddEntry("Coco Export", osb);
                }
            }
        }
        private void PerformExportCheck()
        {
            ExportPath = ExportFolder + ExportName;
            IsExportPathAvailable = false;
            if (Directory.Exists(ExportPath) == false)
            {
                IsExportPathAvailable = true;
            }
            IsExportButtonShowing = true;
        }
        private void Resize_BitmapA(Mat mt)
        {
            if ((mt.Rows == 0) || (mt.Cols == 0)) return;
            double rowRatio = BS_Max_Height / mt.Rows;
            double colRatio = BS_Max_Width / mt.Cols;

            if (rowRatio < colRatio)
            {
                BS_Roi_Height = (int)BS_Max_Height - 1;
                BS_Roi_Top = 1;
                double newWidth = mt.Cols * rowRatio;
                BS_Roi_Width = (int)newWidth;
                //      BS_Roi_Left = (((int)(BS_Max_Width) - BS_Roi_Width) >> 1) + 5;
                BS_Roi_Left = ((int)(BS_Max_Width - BS_Roi_Width - 1));
                if (BS_Roi_Left < 100)  // fit too perfect
                {
                    BS_Roi_Left = 100;
                    BS_Roi_Width = (int)BS_Max_Width - BS_Roi_Left - 1;
                    double resize = BS_Roi_Width / newWidth;
                    BS_Roi_Height = (int)(BS_Roi_Height * resize);
                }
                Resize_BitmapB(BS_Max_Height, BS_Roi_Left);
            }
            else
            {
                BS_Roi_Width = (int)BS_Max_Width - 1;
                BS_Roi_Left = 1;
                double newHeight = mt.Rows * colRatio;
                BS_Roi_Height = (int)newHeight;
                //         BS_Roi_Top = (((int)(BS_Max_Height) - BS_Roi_Height) >> 1) + 5;
                BS_Roi_Top = ((int)(BS_Max_Height - BS_Roi_Height - 1));
                if (BS_Roi_Top < 100)  // fit too perfect
                {
                    BS_Roi_Top = 100;
                    BS_Roi_Height = (int)BS_Max_Height - BS_Roi_Top - 1;
                    double resize = BS_Roi_Height / newHeight;
                    BS_Roi_Width = (int)(BS_Roi_Width * resize);
                }
                Resize_BitmapB(BS_Roi_Top, BS_Max_Width);
            }
        }

        private void Resize_BitmapB(double maxHeight, double maxWidth)  // Fill in remaining space from top left after picking out max Roi size
        {
            if ((SamModel.mtImage.Rows == 0) || (SamModel.mtImage.Cols == 0)) return;
            if (maxHeight < 4) maxHeight = 4;
            if (maxWidth < 4) maxWidth = 4;
            double rowRatio = maxHeight / SamModel.mtImage.Rows;
            double colRatio = maxWidth / SamModel.mtImage.Cols;

            if (rowRatio < colRatio)
            {
                BS_Height = (int)maxHeight - 1;
                BS_Top = 1;
                double newWidth = SamModel.mtImage.Cols * rowRatio;
                BS_Width = (int)newWidth;
                BS_Left = 1;
                if ((maxWidth - BS_Width) < 200)  // fit too perfect
                {
                    BS_Width = (int)maxWidth - 201;
                    double resize = BS_Width / newWidth;
                    BS_Height = (int)(BS_Height * resize);
                }
                Resize_BitmapC(6, BS_Width + 6, BS_Height - 6, maxWidth - BS_Width - 12);
            }
            else
            {
                BS_Width = (int)maxWidth - 1;
                BS_Left = 1;
                double newHeight = SamModel.mtImage.Rows * colRatio;
                BS_Height = (int)newHeight;
                BS_Top = 1;
                if ((maxHeight - BS_Height) < 200)  // fit too perfect
                {
                    BS_Height = (int)maxHeight - 201;
                    double resize = BS_Height / newHeight;
                    BS_Width = (int)(BS_Width * resize);
                }
                Resize_BitmapC(BS_Height + 6, 6, maxHeight - BS_Height - 12, BS_Width - 6);
            }
        }
        private void Resize_BitmapC(double top, double left, double maxHeight, double maxWidth)  // Fill in remaining space 
        {
            if ((SamModel.mtImage.Rows == 0) || (SamModel.mtImage.Cols == 0)) return;
            if (maxHeight < 4) maxHeight = 4;
            if (maxWidth < 4) maxWidth = 4;
            double rowRatio = maxHeight / SamModel.mtImage.Rows;
            double colRatio = maxWidth / SamModel.mtImage.Cols;

            if (rowRatio < colRatio)
            {
                BS_Three_Height = (int)maxHeight - 1;
                BS_Three_Top = (int)top;
                double newWidth = SamModel.mtImage.Cols * rowRatio;
                BS_Three_Width = (int)newWidth;
                BS_Three_Left = (int)left + (((int)(maxWidth - BS_Three_Width)) >> 1);
                if (BS_Three_Left < left) BS_Three_Left = (int)left + 2;
            }
            else
            {
                BS_Three_Width = (int)maxWidth - 1;
                BS_Three_Left = (int)left;
                double newHeight = SamModel.mtImage.Rows * colRatio;
                BS_Three_Height = (int)newHeight;
                BS_Three_Top = (int)top + (((int)(maxHeight - BS_Three_Height)) >> 1);
                if (BS_Three_Top < top) BS_Three_Top = (int)top + 2;
            }
        }

        #endregion

        #region Timers

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTimer.Stop();
            if (UpdateTimer.Tag is not null)
            {
                if (UpdateTimer.Tag is string)
                {
                    string request = (string)UpdateTimer.Tag;
                    if (request.Equals("Export"))
                    {
                        PerformExport();
                    }
                }
            }
        }

        #endregion

        #region Callbacks

        private void UiMessageAction(UiMessage uimess)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (uimess.Code == UiEnum.StatusMessage)
                {
                    UserMessage = uimess.Message;
                }
                else if (uimess.Code == UiEnum.Exception)
                {
                    SamLog.AddEntry("Exception", uimess.Message);
                    if (uimess.Ob is Exception)
                    {
                        Exception ex = (Exception)uimess.Ob;
                        SamLog.AddEntry("Exception", ex.Message);
                        if (ex.StackTrace != null) SamLog.AddEntry("Exception", ex.StackTrace);
                    }
                }
                else if (uimess.Code == UiEnum.ReadyToQuit)
                {
                    Application.Current.Shutdown();
                }
                else if (uimess.Code == UiEnum.TaskCompleted)
                {

                }

            }));

        }

        private void ByteArrayMessageAction(ByteArrayMessageOut bamo)
        {
            return;

        }


        #endregion

    }

    public class SubLabelInfo : ObservableObject
    {
        public int UniqueID { get; set; }
        public string MainLabel { get; set; }
        public string SubLabel { get; set; }

        public SubLabelInfo()
        {
            UniqueID = -1;
            MainLabel = String.Empty;
            SubLabel = String.Empty;
        }

        public SubLabelInfo(string label, string subLabel, int unique)
        {
            MainLabel = label;
            SubLabel = subLabel;
            UniqueID = unique;
        }

    }

    public class DataSetPayload
    {
        public bool AddDetections { get; set; }

        public int CurrentUniqueID { get; set; }

        public List<int> UniqueIDList { get; set; }

        public OperationEnum Operation { get; set; }

        public DataSetPayload()
        {
            AddDetections = false;
            CurrentUniqueID = -1;
            UniqueIDList = new List<int>();
        }

        public DataSetPayload(OperationEnum operation, bool addDetections)
        {
            CurrentUniqueID = -1;
            AddDetections = addDetections;
            Operation = operation;
            UniqueIDList = new List<int>();
        }

        public DataSetPayload(OperationEnum operation, int uniqueID)
        {
            AddDetections = false;
            CurrentUniqueID = uniqueID;
            Operation = operation;
            UniqueIDList = new List<int>();
        }

        public DataSetPayload(OperationEnum operation, List<int> uniqueIDList)
        {
            AddDetections = false;
            CurrentUniqueID = -1;
            Operation = operation;
            UniqueIDList = uniqueIDList;
        }
    }

    public enum RBMaskView
    {
        NotAssigned = 0,
        FullMask = 1,
        Original = 2,
        Overlay = 3
    }

    public enum P3Tabs
    {
        DataSet = 1,
        Edit = 2,
        NewSam = 4,
        Export = 8,
        Console = 32
    }

}
// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Open.IP;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Win32;
using SamNet.Native;
using SharedToolbox;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SamNet
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]


    public partial class Page1VM : ObservableObject, IPageVM
    {
        #region Variables

        private Action<PageMessage>? PageAction;
        private SamWorker? Work;
        private ConcurrentQueue<object>? MessageToWorker;

        private EdwardsMessageBus eventBus;

        private AutoResetEvent are;

        private int thisPage = 1;

        private int UpdateClock = 0;

        private BitmapSource? _BS;

        private Brush _ModelStatusColor = Brushes.Red;
        private Brush _Sam3RadioBrush = Brushes.LightGray;

        private bool _IsClearPoints;
        private bool _IsClickListPopulated;
        private bool _IsExportFolderValid;
        private bool _IsSam3Enabled;
        private bool _IsUIEnabled;
        private bool _Is_RB_Sam2PVS_Checked;
        private bool _Is_RB_Sam3PCS_Checked;
        private bool _Is_RB_Sam2Single_Checked;
        private bool _Is_RB_Sam2MultiMask_Checked;
        private bool _Is_RB_Sam2Multiple_Checked;
        private bool _IsRunButtonAllowed;
        private bool _IsSamInProgress;
        private bool _IsSetupSamEnabled;
        private bool _IsSetupImportEnabled;
        private bool _IsSetupEncodeButtonShowing;
        private bool _IsSetupImageButtonShowing;
        private bool _IsSetupModelButtonShowing;
        private bool _IsShowImportButton;
        private bool _IsTextPromptReady;
        private bool _IsViewable;

        private bool IsMouseDown = false;
        private bool IsRightMouseDown = false;

        private double MouseDownX = 0;
        private double MouseDownY = 0;

        private int _BS_CanvasHeight;
        private int _BS_CanvasWidth;
        private int _BS_Height;
        private int _BS_Left;
        private int _BS_Top;
        private int _BS_Width;

        private int _TabItemState;
        private int _PreviousTabIndex;
        private int _RightColumnState;
        private int _SelectedTabIndex;
        private int _SetupItemState;

        private float _ConfidenceValue = 0.5f;
        private float _OverlapValue = 0.1f;

        private double BS_Max_Width = 1300;
        private double BS_Max_Height = 822;
        private double ImageRatio = 0;

        private int NextTabIndex = 0;

        private DataSetPayload Info;
        ObservableCollection<string> _ExportFolders;
        private ObservableCollection<SamPoint> _SamPoints;
        private ObservableCollection<SamRectangle> _SamRectangles;

        private string ImagePath = "";
        private string ModelPath = "";
        private string _EncodingStatusMessage = "";
        private string _ExportFolder = "";
        private string _ImageShortName = "";
        private string _ModelMessage = "";
        private string _ModelStatusMessage = "";
        private string _SelectedExportFolder = "";
        private string _TextPrompt = "";
        private string _UserMessage = "";

        DispatcherTimer UpdateTimer;

        #endregion

        #region Constructor And Focus

        public Page1VM(Action<PageMessage> act, SamWorker? work, ConcurrentQueue<object>? messageToWorker)
        {
            PageAction = act;
            MessageToWorker = messageToWorker;
            Work = work;

            UserMessage = "Welcome to SamNet - Choose a working Mode";

            BS_CanvasWidth = 1320;
            BS_CanvasHeight = 836;
            BS_Width = (int)BS_Max_Width;
            BS_Height = (int)BS_Max_Height;
            BS_Left = 5;
            BS_Top = 5;

            Info = new DataSetPayload();
            eventBus = (EdwardsMessageBus)App.Current.Resources["EventBus"];
            eventBus.Subscribe<EventMessage>(Window_Closing);

            are = new AutoResetEvent(false);
            TabItemState = (int)(P1Tabs.Console | P1Tabs.Setup);

            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Interval = TimeSpan.FromSeconds(1);
            UpdateTimer.Tick += UpdateTimer_Tick;
            UpdateTimer.Stop();

            ModelMessage = "No Model Selected";
            ModelStatusMessage = "Not Loaded";
            ModelStatusColor = Brushes.Red;

            SetupItemState = (int)SetupEnum.ImageEnabled;
            IsViewable = true;

            _ExportFolders = new ObservableCollection<string>();
            _SamPoints = new ObservableCollection<SamPoint>();
            _SamRectangles = new ObservableCollection<SamRectangle>();

            IsClearPoints = false;
            IsClickListPopulated = false;
            Is_RB_Sam2Single_Checked = true;

            
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
                }
            }

            IsRunButtonAllowed = true;
            if (caller == 2)
            {
                SamPoints.Clear();
                SamRectangles.Clear();
                if (Info.Operation.HasFlag(OperationEnum.SamNew))
                {
                    SelectedTabIndex = (int)(Math.Log((int)P1Tabs.Sam, 2));
                }
                else if (Info.Operation.HasFlag(OperationEnum.Setup))
                {
                    SelectedTabIndex = (int)(Math.Log((int)P1Tabs.Setup, 2));
                }
            }
            else if (caller == 3)
            {
                if (Info.Operation.HasFlag(OperationEnum.SamNew))
                {
                    SamPoints.Clear();
                    SamRectangles.Clear();
                    SelectedTabIndex = (int)(Math.Log((int)P1Tabs.Sam, 2));
                }
                else
                {

                }
            }
            else if (caller == 4)
            {

                SelectedTabIndex = PreviousTabIndex;
            }
            else
            {
                IsSetupSamEnabled = true;
                IsSetupImportEnabled = true;
                IsSetupModelButtonShowing = false;
                IsSetupImageButtonShowing = true;
                IsSetupEncodeButtonShowing = false;
                Work?.WorkerStartupStatus();
                FetchExportFolderInfo();
                IsUIEnabled = true;
            }
        }

        #endregion

        #region Properties

        public BitmapSource? BS
        {

            get { return _BS; }
            set
            {
                _BS = value;
                OnPropertyChanged("BS");
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
        public float ConfidenceValue
        {
            get { return _ConfidenceValue; }
            set
            {
                if (_ConfidenceValue != value)
                {
                    _ConfidenceValue = value;
                    OnPropertyChanged("ConfidenceValue");
                }
            }
        }
        public string EncodingStatusMessage
        {
            get { return _EncodingStatusMessage; }
            set
            {
                if (_EncodingStatusMessage != value)
                {
                    _EncodingStatusMessage = value;
                    OnPropertyChanged("EncodingStatusMessage");
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
        public string ImageShortName
        {
            get { return _ImageShortName; }
            set
            {
                if (_ImageShortName != value)
                {
                    _ImageShortName = value;
                    OnPropertyChanged("ImageShortName");
                }
            }
        }
        public bool IsClearPoints
        {
            get { return _IsClearPoints; }
            set
            {
                if (_IsClearPoints != value)
                {
                    _IsClearPoints = value;
                    OnPropertyChanged("IsClearPoints");
                }
            }
        }
        public bool IsClickListPopulated
        {
            get { return _IsClickListPopulated; }
            set
            {
                if (_IsClickListPopulated != value)
                {
                    _IsClickListPopulated = value;
                    OnPropertyChanged("IsClickListPopulated");
                }
            }
        }

        public bool IsExportFolderValid
        {
            get { return _IsExportFolderValid; }
            set
            {
                if (_IsExportFolderValid != value)
                {
                    _IsExportFolderValid = value;
                    OnPropertyChanged("IsExportFolderValid");
                }
            }
        }   

        public bool IsUIEnabled
        {
            get { return _IsUIEnabled; }
            set
            {
                if (_IsUIEnabled != value)
                {
                    _IsUIEnabled = value;
                    OnPropertyChanged("IsUIEnabled");
                }
            }
        }
        public bool Is_RB_Sam2PVS_Checked
        {
            get { return _Is_RB_Sam2PVS_Checked; }
            set
            {
                if (_Is_RB_Sam2PVS_Checked != value)
                {
                    _Is_RB_Sam2PVS_Checked = value;
                    OnPropertyChanged("Is_RB_Sam2PVS_Checked");
                }
            }
        }
        public bool Is_RB_Sam3PCS_Checked
        {
            get { return _Is_RB_Sam3PCS_Checked; }
            set
            {
                if (_Is_RB_Sam3PCS_Checked != value)
                {
                    _Is_RB_Sam3PCS_Checked = value;
                    OnPropertyChanged("Is_RB_Sam3PCS_Checked");
                }
            }
        }
        public bool Is_RB_Sam2Single_Checked
        {
            get { return _Is_RB_Sam2Single_Checked; }
            set
            {
                if (_Is_RB_Sam2Single_Checked != value)
                {
                    _Is_RB_Sam2Single_Checked = value;
                    OnPropertyChanged("Is_RB_Sam2Single_Checked");
                }
            }
        }
        public bool Is_RB_Sam2MultiMask_Checked
        {
            get { return _Is_RB_Sam2MultiMask_Checked; }
            set
            {
                if (_Is_RB_Sam2MultiMask_Checked != value)
                {
                    _Is_RB_Sam2MultiMask_Checked = value;
                    OnPropertyChanged("Is_RB_Sam2MultiMask_Checked");
                }
            }
        }
        public bool Is_RB_Sam2Multiple_Checked
        {
            get { return _Is_RB_Sam2Multiple_Checked; }
            set
            {
                if (_Is_RB_Sam2Multiple_Checked != value)
                {
                    _Is_RB_Sam2Multiple_Checked = value;
                    OnPropertyChanged("Is_RB_Sam2Multiple_Checked");
                }
            }
        }
        public bool IsRunButtonAllowed
        {
            get { return _IsRunButtonAllowed; }
            set
            {
                if (_IsRunButtonAllowed != value)
                {
                    _IsRunButtonAllowed = value;
                    OnPropertyChanged("IsRunButtonAllowed");
                }
            }
        }
        public bool IsSam3Enabled
        {
            get { return _IsSam3Enabled; }
            set
            {
                if (_IsSam3Enabled != value)
                {
                    _IsSam3Enabled = value;
                    OnPropertyChanged("IsSam3Enabled");
                }
            }
        }
        public bool IsSamInProgress
        {
            get { return _IsSamInProgress; }
            set
            {
                if (_IsSamInProgress != value)
                {
                    _IsSamInProgress = value;
                    OnPropertyChanged("IsSamInProgress");
                }
            }
        }
        public bool IsSetupSamEnabled
        {
            get { return _IsSetupSamEnabled; }
            set
            {
                if (_IsSetupSamEnabled != value)
                {
                    _IsSetupSamEnabled = value;
                    OnPropertyChanged("IsSetupSamEnabled");
                }
            }
        }
        public bool IsSetupImportEnabled
        {
            get { return _IsSetupImportEnabled; }
            set
            {
                if (_IsSetupImportEnabled != value)
                {
                    _IsSetupImportEnabled = value;
                    OnPropertyChanged("IsSetupImportEnabled");
                }
            }
        }
        public bool IsShowImportButton
        {
            get { return _IsShowImportButton; }
            set
            {
                if (_IsShowImportButton != value)
                {
                    _IsShowImportButton = value;
                    OnPropertyChanged("IsShowImportButton");
                }
            }
        }
        public bool IsSetupEncodeButtonShowing
        {
            get { return _IsSetupEncodeButtonShowing; }
            set
            {
                if (_IsSetupEncodeButtonShowing != value)
                {
                    _IsSetupEncodeButtonShowing = value;
                    OnPropertyChanged("IsSetupEncodeButtonShowing");
                }
            }
        }
        public bool IsSetupImageButtonShowing
        {
            get { return _IsSetupImageButtonShowing; }
            set
            {
                if (_IsSetupImageButtonShowing != value)
                {
                    _IsSetupImageButtonShowing = value;
                    OnPropertyChanged("IsSetupImageButtonShowing");
                }
            }
        }
        public bool IsSetupModelButtonShowing
        {
            get { return _IsSetupModelButtonShowing; }
            set
            {
                if (_IsSetupModelButtonShowing != value)
                {
                    _IsSetupModelButtonShowing = value;
                    OnPropertyChanged("IsSetupModelButtonShowing");
                }
            }
        }
        public bool IsTextBoxReady
        {
            get { return _IsTextPromptReady; }
            set
            {
                if (_IsTextPromptReady != value)
                {
                    _IsTextPromptReady = value;
                    OnPropertyChanged("IsTextBoxReady");
                }
            }
        }
        public bool IsViewable
        {
            get { return _IsViewable; }
            set
            {
                if (_IsViewable != value)
                {
                    _IsViewable = value;
                    OnPropertyChanged("IsViewable");
                }
            }
        }
        public ObservableCollection<string> ExportFolders
        {
            get { return _ExportFolders; }
        }
        public ObservableCollection<SamPoint> SamPoints
        {
            get { return _SamPoints; }
        }
        public ObservableCollection<SamRectangle> SamRectangles
        {
            get { return _SamRectangles; }
        }
        public string ModelMessage
        {
            get { return _ModelMessage; }
            set
            {
                if (_ModelMessage != value)
                {
                    _ModelMessage = value;
                    OnPropertyChanged("ModelMessage");
                }
            }
        }
        public Brush ModelStatusColor
        {
            get { return _ModelStatusColor; }
            set
            {
                if (_ModelStatusColor != value)
                {
                    _ModelStatusColor = value;
                    OnPropertyChanged("ModelStatusColor");
                }
            }
        }
        public string ModelStatusMessage
        {
            get { return _ModelStatusMessage; }
            set
            {
                if (_ModelStatusMessage != value)
                {
                    _ModelStatusMessage = value;
                    OnPropertyChanged("ModelStatusMessage");
                }
            }
        }
        public string Name
        {
            get { return "Page 1"; }
        }
        public float OverlapValue
        {
            get { return _OverlapValue; }
            set
            {
                if (_OverlapValue != value)
                {
                    _OverlapValue = value;
                    OnPropertyChanged("OverlapValue");
                }
            }
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
        public int RightColumnState
        {
            get { return _RightColumnState; }
            set
            {
                if (_RightColumnState != value)
                {
                    _RightColumnState = value;
                    OnPropertyChanged("RightColumnState");
                    SamPoints.Clear();
                    SamRectangles.Clear();
                }
            }
        }
        public Brush Sam3RadioBrush
        {
            get { return _Sam3RadioBrush; }
            set
            {
                if (_Sam3RadioBrush != value)
                {
                    _Sam3RadioBrush = value;
                    OnPropertyChanged("Sam3RadioBrush");
                }
            }
        }
        public string SelectedExportFolder
        {
            get { return _SelectedExportFolder; }
            set
            {
                if (_SelectedExportFolder != value)
                {
                    _SelectedExportFolder = value;
                    OnPropertyChanged("SelectedExportFolder");
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
                    OnPropertyChanged("SelectedTabIndex");
                }
            }
        }
        public int SetupItemState
        {
            get { return _SetupItemState; }
            set
            {
                if (_SetupItemState != value)
                {
                    _SetupItemState = value;
                    OnPropertyChanged("SetupItemState");
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
        public string TextPrompt
        {
            get { return _TextPrompt; }
            set
            {
                if (_TextPrompt != value)
                {
                    _TextPrompt = value;
                    OnPropertyChanged("TextPrompt");
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
                if (tag.Equals("SelectGGML"))
                {
                    OpenModelFileButtonClicked();
                    if (ModelPath.Length > 0)
                    {
                        MessageToWorker?.Enqueue(new MessageToWorker(MessageEnum.Task, "LoadModel", ModelPath));
                        IsUIEnabled = false;
                        UserMessage = "Loading " + ModelMessage + " - Please Wait ";
                        UpdateTimer.Tag = "Load";
                        UpdateTimer.Start();
                    }
                }
                else if (tag.Equals("SelectImage"))
                {
                    OpenImagesFileButtonClicked();
                    if (ImagePath.Length > 0)
                    {
                        Mat mtRgb = CvInvoke.Imread(ImagePath, ImreadModes.ColorBgr);
                        int rows = mtRgb.Rows;
                        int cols = mtRgb.Cols;
                        SamModel.mtImage = mtRgb.Clone();
                        CvInvoke.CvtColor(mtRgb, mtRgb, ColorConversion.Bgr2Rgb);
                        byte[] rawData = mtRgb.GetRawData();
                        if (mtRgb is not null) mtRgb.Dispose();
                        ImageInfo info = new ImageInfo(rawData, rows, cols);
                        MessageToWorker?.Enqueue(new MessageToWorker(MessageEnum.Task, "LoadImage", info));
                        IsUIEnabled = false;
                    }
                }
                else if (tag.Equals("Encode"))
                {
                    MessageToWorker?.Enqueue(new MessageToWorker(MessageEnum.Task, "Encode"));
                    IsUIEnabled = false;
                    UserMessage = "Encoding " + ImageShortName + " - Please Wait ";
                    UpdateClock = 0;
                    UpdateTimer.Tag = "Encode";
                    UpdateTimer.Start();
                    EncodingStatusMessage = "Encoding in progress - this may take over 1 minute for large models";
                    IsSetupEncodeButtonShowing = false;
                    //        SetupItemState = (int)SetupEnum.None;
                }
                else if (tag.Equals("CleanRestart"))
                {
                    UserMessage = "Restart requested - Choose a working Mode";
                    ModelPath = "";
                    ModelMessage = "No Model Loaded";
                    ModelStatusMessage = "";
                    EncodingStatusMessage = "";
                    IsSam3Enabled = false;
                    Is_RB_Sam3PCS_Checked = false;
                    Is_RB_Sam2PVS_Checked = false;
                    SetupItemState = (int)SetupEnum.ImageEnabled;
                    IsSetupModelButtonShowing = false;
                    IsSetupImageButtonShowing = true;
                    IsSetupEncodeButtonShowing = false;
                    IsSetupImportEnabled = true;
                    IsSetupSamEnabled = true;
                    SamModel.ClearSamDetections();
                    SamModel.ClearDataSet();
                    SelectedTabIndex = (int)Math.Log((int)P1Tabs.Setup, 2);  // Switch to Sam Tab
                }
                else if (tag.Equals("ClearLastClick"))
                {
                    if (IsClearPoints)
                    {
                        if (SamPoints.Count > 0)
                        {
                            SamPoints.RemoveAt(SamPoints.Count() - 1);
                        }
                    }
                    else
                    {
                        if (SamRectangles.Count > 0)
                        {
                            SamRectangles.RemoveAt(SamRectangles.Count() - 1);
                        }
                    }
                    if ((SamPoints.Count() == 0) && (SamRectangles.Count() == 0)) IsClickListPopulated = false;
                }
                else if (tag.Equals("ClearAllClicks"))
                {
                    if (IsClearPoints)
                    {
                        SamPoints.Clear();
                    }
                    else
                    {
                        SamRectangles.Clear();
                    }
                    if ((SamPoints.Count() == 0) && (SamRectangles.Count() == 0)) IsClickListPopulated = false;
                }
                else if (tag.Equals("CreateExportFolder"))
                {
                    Directory.CreateDirectory(ExportFolder);
                    IsExportFolderValid = true;
                }
                else if (tag.Equals("Import"))
                {
                    ProcessImportRequest(SelectedExportFolder);
                }
                else if (tag.Equals("Sam2Point"))
                {
                    if (ImageRatio != 0)
                    {
                        List<SamInfo> si = new List<SamInfo>();
                        for (int i = 0; i < SamPoints.Count(); i++)
                        {
                            SamEnum se = SamEnum.PointPositive;
                            if (SamPoints[i].IsPositive == false) se = SamEnum.PointNegative;
                            si.Add(new SamInfo(se, (int)(SamPoints[i].X / ImageRatio), (int)(SamPoints[i].Y / ImageRatio)));
                            IsClickListPopulated = true;
                        }
                        for (int i = 0; i < SamRectangles.Count(); i++)
                        {
                            SamEnum se = SamEnum.BoxPositive;
                            if (SamRectangles[i].IsPositive == false) se = SamEnum.BoxNegative;
                            int newX = (int)(SamRectangles[i].X / ImageRatio);
                            int newY = (int)(SamRectangles[i].Y / ImageRatio);
                            int newW = (int)(SamRectangles[i].Width / ImageRatio);
                            int newH = (int)(SamRectangles[i].Height / ImageRatio);
                            System.Drawing.Rectangle r = new System.Drawing.Rectangle(newX, newY, newW, newH);
                            si.Add(new SamInfo(se, r));
                            IsClickListPopulated = true;
                        }
                        SamEnum smode = SamEnum.Sam2Single;
                        if (RightColumnState == (int)OperationEnum.Sam2MultiMask) smode = SamEnum.Sam2MultiMask;
                        if (RightColumnState == (int)OperationEnum.Sam2Multiple) smode = SamEnum.Sam2Multiple;
                        SamMessage smess = new SamMessage(smode, si);
                        SamModel.ClearSamDetections();  //  Clear the results buffer
                        SamModel.SEnum = smode;
                        SamModel.TextPrompt = "";
                        MessageToWorker?.Enqueue(new MessageToWorker(MessageEnum.Task, "Sam2Point", smess));
                        UserMessage = "Sam2 working on segmentation" + " - Please Wait ";
                        UpdateClock = 0;
                        UpdateTimer.Tag = "Sam2";
                        UpdateTimer.Start();
                        IsRunButtonAllowed = false;
                        IsSamInProgress = true;
                    }
                }
                else if (tag.Equals("Sam3Prompt"))
                {
                    if (ImageRatio != 0)
                    {
                        List<SamInfo> si = new List<SamInfo>();
                        for (int i = 0; i < SamRectangles.Count(); i++)
                        {
                            SamEnum se = SamEnum.BoxPositive;
                            if (SamRectangles[i].IsPositive == false) se = SamEnum.BoxNegative;
                            int newX = (int)(SamRectangles[i].X / ImageRatio);
                            int newY = (int)(SamRectangles[i].Y / ImageRatio);
                            int newW = (int)(SamRectangles[i].Width / ImageRatio);
                            int newH = (int)(SamRectangles[i].Height / ImageRatio);
                            System.Drawing.Rectangle r = new System.Drawing.Rectangle(newX, newY, newW, newH);
                            si.Add(new SamInfo(se, r));
                        }
                        SamModel.ClearSamDetections();  //  Clear the results buffer
                        SamMessage smess = new SamMessage(SamEnum.Points, si, TextPrompt, ConfidenceValue, OverlapValue);
                        SamModel.SEnum = SamEnum.Points;
                        SamModel.TextPrompt = TextPrompt;
                        MessageToWorker?.Enqueue(new MessageToWorker(MessageEnum.Task, "Sam3Prompt", smess));
                        UserMessage = "Sam3 working on segmentation" + " - Please Wait ";
                        UpdateClock = 0;
                        UpdateTimer.Tag = "Sam3";
                        UpdateTimer.Start();
                        IsRunButtonAllowed = false;
                        IsSamInProgress = true;
                    }
                }
                else if (tag.Equals("ManualSegmentation"))
                {
                    ModelMessage = "Manual Segmentation Chosen";
                    IsSetupModelButtonShowing = false;
                    PageAction?.Invoke(new PageMessage(2, thisPage, new DataSetPayload(OperationEnum.Edit, true)));
                }
            }
        }
        public void ExportFolderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.SelectedItem != null)
            {
                int index = lb.SelectedIndex;
                SelectedExportFolder = (string)lb.SelectedItem;
                IsShowImportButton = true;
            }
        }
        public void ImageMouseDown(object sender, MouseEventArgs e)
        {
            if (IsViewable == false) return;
            Canvas c = (Canvas)sender;
            Point p = e.GetPosition(c);
            MouseDownX = p.X;
            MouseDownY = p.Y;
            IsMouseDown = true;
            if (e.RightButton == MouseButtonState.Pressed)
            {
                // AddSamPoint(false, (int)MouseDownX, (int)MouseDownY);
                IsMouseDown = false;
                IsRightMouseDown = true;
            }
            else
            {
                IsMouseDown = true;   // could be a point or a box
                IsRightMouseDown = false;
            }
        }
        public void ImageMouseUp(object sender, MouseEventArgs e)
        {
            bool wasMouseDown = IsMouseDown;
            bool wasRightMouseDown = IsRightMouseDown;
            IsMouseDown = false;
            IsRightMouseDown = false;
            if (IsViewable == false) return;
            if ((wasMouseDown) || (wasRightMouseDown))
            {
                Canvas c = (Canvas)sender;
                Point p = e.GetPosition(c);
                int deltaX = (int)Math.Abs(p.X - MouseDownX);
                int deltaY = (int)Math.Abs(p.Y - MouseDownY);
                bool isPositive = true;
                if (wasRightMouseDown) isPositive = false;
                if ((deltaX < 4) && (deltaY < 4))  // little or no movement, it is a point
                {
                    AddSamPoint(isPositive, (int)MouseDownX, (int)MouseDownY);
                }
                else
                {
                    AddSamBox(true, isPositive, (int)Math.Min(MouseDownX, p.X), (int)Math.Min(MouseDownY, p.Y), deltaX, deltaY);
                }
            }
        }
        public void ImageMouseMove(object sender, MouseEventArgs e)
        {
            if (IsViewable == false) return;

            if ((IsMouseDown) || (IsRightMouseDown))
            {
                Canvas c = (Canvas)sender;
                Point p = e.GetPosition(c);
                int deltaX = (int)Math.Abs(p.X - MouseDownX);
                int deltaY = (int)Math.Abs(p.Y - MouseDownY);
                if ((deltaX >= 4) || (deltaY >= 4))
                {
                    bool isPositive = true;
                    if (IsRightMouseDown) isPositive = false;
                    AddSamBox(false, isPositive, (int)Math.Min(MouseDownX, p.X), (int)Math.Min(MouseDownY, p.Y), deltaX, deltaY);
                }
            }
        }
        public void ImageMouseLeave(object sender, MouseEventArgs e)
        {
            IsMouseDown = false;
            IsRightMouseDown = false;
        }
        public void RadioButtonChecked(object sender, EventArgs e)
        {
            RadioButton tb = (RadioButton)sender;
            string? tag = tb.Tag.ToString();
            if (tag!.Equals("Sam2Single"))
            {
                RightColumnState = (int)OperationEnum.Sam2Single;
            }
            else if (tag!.Equals("Sam2MultiMask"))
            {
                RightColumnState = (int)OperationEnum.Sam2MultiMask;
            }
            else if (tag!.Equals("Sam2Multiple"))
            {
                RightColumnState = (int)OperationEnum.Sam2Multiple;
            }
            else if (tag!.Equals("Sam2PVS"))
            {
                RightColumnState = (int)OperationEnum.Sam2Single;
            }
            else if (tag!.Equals("Sam3PCS"))
            {
                RightColumnState = (int)OperationEnum.Sam3PCS;
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
                if (tag.Equals("Console"))   //  Swap views
                {
                    PageAction?.Invoke(new PageMessage(4, thisPage, PreviousTabIndex));
                }
                else if (tag.Equals("Sam"))   //  Swap views
                {

                }
                else if (tag.Equals("Setup"))   //  Swap views
                {
                    SetupItemState = (int)SetupEnum.ImageEnabled;
                }
                else if (tag.Equals("Page"))   //  Swap views
                {
                    PageAction?.Invoke(new PageMessage(2, thisPage));
                }
                else if (tag.Equals("Project"))   //  Swap views
                {
                    PageAction?.Invoke(new PageMessage(3, thisPage));
                }
            }
        }

        public void SliderUp(object sender, EventArgs e)
        {
            Slider sl = (Slider)sender;
            string? tag = sl.Tag.ToString();
            if (tag is not null)
            {
                if (tag.Equals("ConfidenceSlider"))
                {
                    // No action at this time                    
                }
                else if (tag.Equals("OverlapSlider"))
                {
                    // No action at this time                    
                }
            }
        }

        public void TextChange(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string? tag = tb.Tag.ToString();
            if (tag is not null)
            {
                if (tag.Equals("TextPrompt"))
                {
                    TextPrompt = tb.Text;
                    if (TextPrompt.Length > 2)
                    {
                        IsTextBoxReady = true;
                    }
                    else
                    {
                        IsTextBoxReady = false;
                    }
                }
            }
        }

        #endregion

        #region Methods

        public void AddSamBox(bool isSolid, bool isPositive, int x, int y, int deltaX, int deltaY)
        {
            if (RightColumnState == (int)OperationEnum.None) return;
            if (RightColumnState == (int)OperationEnum.Sam2Single)
            {
                SamRectangles.Clear();   // only 1 box allowed with a single
                if (isPositive == false) return;  //  no negative boxes allowed with a single   
            }
            if (RightColumnState == (int)OperationEnum.Sam2MultiMask)
            {
                SamRectangles.Clear();   // no boxes allowed with a multimask
                return;
            }
            if (RightColumnState == (int)OperationEnum.Sam2Multiple)
            {
                if (isPositive == false) return;  //  no negative boxes allowed with Sam2 
            }
            for (int i = SamRectangles.Count() - 1; i >= 0; i--)
            {
                if (SamRectangles[i].Solid == false) SamRectangles.RemoveAt(i);  //  remove previous mouse moves
            }
            SamRectangles.Add(new SamRectangle(isSolid, isPositive, x, y, deltaX, deltaY));
            IsClickListPopulated = true;
        }
        public void AddSamPoint(bool isPositive, int x, int y)
        {
            if (RightColumnState == (int)OperationEnum.Sam3PCS) return;   // No points with Sam3 PCS
            if (RightColumnState == (int)OperationEnum.None) return;
            if (isPositive)
            {
                if (RightColumnState == (int)OperationEnum.Sam2MultiMask)
                {
                    for (int i = SamPoints.Count() - 1; i >= 0; i--)
                    {
                        if (SamPoints[i].IsPositive) SamPoints.RemoveAt(i);  //  Only 1 positive point in a multimask
                    }
                }
            }

            SamPoints.Add(new SamPoint(isPositive, x, y));
            IsClickListPopulated = true;
        }

        public void CalculateScale(int mode, double deltaX, double deltaY)
        {

            double percentX = deltaX / BS_Width;
            double percentY = deltaY / BS_Height;
            double pixelsX = 4400 * percentX;
            double pixelsY = 3296 * percentY;
            double TrayWidth = 15.0;
            double TrayHeight = 11.0;
            if (mode == 0)
            {
                double xScale = pixelsX / TrayWidth;
                double yScale = pixelsY / TrayHeight;
                UserMessage += "If Tray Corners: ScaleX: " + Math.Round(xScale, 2).ToString() + " ScaleY: " + Math.Round(yScale, 2).ToString() + "\r\n";
            }
            else
            {
                double tapeDistInch = 15.84375;
                double tapeDistCm = tapeDistInch * 2.54;
                double yScale = pixelsY / tapeDistCm;
                UserMessage += "If Tape: ScaleY: " + Math.Round(yScale, 2).ToString() + "\r\n";
            }

        }
        private void OpenModelFileButtonClicked()
        {
            var openFileDialog = new OpenFileDialog();

            string modelsPath = Properties.Settings.Default.ModelsFolder;

            if (Directory.Exists(modelsPath))
            {
                openFileDialog.InitialDirectory = modelsPath;
            }
            else
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            // Configure filters and other properties
            openFileDialog.Filter = "ggml files (*.ggml)|*.ggml";
            openFileDialog.Title = "Select a Model";

            // Show the dialog and check result
            if (openFileDialog.ShowDialog() == true)
            {
                ModelPath = openFileDialog.FileName;
                ModelMessage = openFileDialog.SafeFileName;
                ModelStatusMessage = "Not Encoded";
                ModelStatusColor = Brushes.DarkSalmon;
                IsSam3Enabled = false;
                Sam3RadioBrush = Brushes.LightGray;
                if (ModelMessage.ToLower().Contains("sam3"))
                {
                    IsSam3Enabled = true;
                    Sam3RadioBrush = Brushes.Black;
                    Is_RB_Sam3PCS_Checked = true;
                }
                else
                {
                    Is_RB_Sam2PVS_Checked = true;
                }
                IsSetupModelButtonShowing = false;
                IsSetupEncodeButtonShowing = true;
            }
            else
            {
                ModelPath = "";
                modelsPath = "";
                ModelMessage = "Model Not Selected";
            }
        }
        private void OpenImagesFileButtonClicked()
        {
            var openFileDialog = new OpenFileDialog();

            string modelsPath = Properties.Settings.Default.ImagesFolder;

            if (Directory.Exists(modelsPath))
            {
                openFileDialog.InitialDirectory = modelsPath;
            }
            else
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            // Configure filters and other properties
            openFileDialog.Filter = "Image Files (*.png; *.jpg; *.tiff)|*.png;*.jpg;*.tiff|All Files (*.*)|*.*";
            openFileDialog.Title = "Select an Image";

            // Show the dialog and check result
            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
                ImageShortName = openFileDialog.SafeFileName;
                SamModel.OriginalImageName = ImageShortName;
                IsSetupImportEnabled = false;
                IsSetupModelButtonShowing = true;
                IsSetupImageButtonShowing = false;
            }
            else
            {
                ImagePath = "";
            }
        }
        private bool FetchExportFolderInfo()
        {
            ExportFolder = "C:\\Temp\\ExportFolderHardcoded";
            string candidate = Properties.Settings.Default.CocoExportFolder.ToString();
            if (candidate.Length > 0) ExportFolder = candidate;
            if (Directory.Exists(ExportFolder) == false) return false;
            if (ExportFolder.EndsWith("\\") == false) ExportFolder += "\\";

            string[] firstLevelSubdirs = Directory.GetDirectories(ExportFolder, "*", SearchOption.TopDirectoryOnly);

            foreach (string dir in firstLevelSubdirs)
            {
                ExportFolders.Add(dir);
            }
            IsExportFolderValid = true;
            return true;
        }
        private void ProcessImportRequest(string folder)
        {
            OpenSafeBool osb = CocoSupport.LoadCocoJson(folder);
            if (osb.IsSuccess == false)
            {
                UserMessage = "Error during LoadCocoJson,  Check ErrorLog";
                SamLog.AddEntry("LoadCocoJson", osb);
            }
            else
            {
                UserMessage = "Json successfully loaded: " + folder;
                IsSetupSamEnabled = false;
                IsSetupImportEnabled = false;
                PageAction?.Invoke(new PageMessage(3, thisPage));
            }
        }
        private void Resize_Bitmap()
        {
            if ((SamModel.mtImage.Rows == 0) || (SamModel.mtImage.Cols == 0)) return;
            double rowRatio = BS_Max_Height / SamModel.mtImage.Rows;
            double colRatio = BS_Max_Width / SamModel.mtImage.Cols;

            if (rowRatio < colRatio)
            {
                BS_Height = (int)BS_Max_Height;
                BS_Top = 5;
                double newWidth = SamModel.mtImage.Cols * rowRatio;
                BS_Width = (int)newWidth;
                BS_Left = (((int)(BS_Max_Width) - BS_Width) >> 1) + 5;
                ImageRatio = rowRatio;
            }
            else
            {
                BS_Width = (int)BS_Max_Width;
                BS_Left = 5;
                double newHeight = SamModel.mtImage.Rows * colRatio;
                BS_Height = (int)newHeight;
                BS_Top = (((int)(BS_Max_Height) - BS_Height) >> 1) + 5;
                ImageRatio = colRatio;
            }

        }
        private void Window_Closing(EventMessage mess)
        {
            // Close what needs to close down here
            if (mess.Dest == 1)
            {
                if (mess.EventType.Equals(EventEnum.Close))
                {
                    MessageToWorker?.Enqueue(new MessageToWorker(MessageEnum.Quit));
                }
                if (MessageToWorker!.Count() > 2) Application.Current.Shutdown();   // Queue is blocked, force shutdown
            }
            UserMessage = "Shutdown requested - Please wait for worker to finish";
        }

        #endregion

        #region Timers

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClock++;
            if (UpdateTimer.Tag.Equals("Encode"))
            {
                UserMessage = "Encoding " + ImageShortName + " Please wait - Elapsed: " + UpdateClock.ToString() + " sec.";
            }
            else if (UpdateTimer.Tag.Equals("Sam2"))
            {
                UserMessage = "Sam2 working on segmentation" + " - Please wait - Elapsed: " + UpdateClock.ToString() + " sec.";
            }
            else if (UpdateTimer.Tag.Equals("Sam3"))
            {
                UserMessage = "Sam3 working on segmentation" + " - Please wait - Elapsed: " + UpdateClock.ToString() + " sec.";
            }
            else if (UpdateTimer.Tag.Equals("ChangeTab"))
            {
                SelectedTabIndex = NextTabIndex;
                UpdateTimer.Stop();
            }
            else
            {
                UserMessage = "Loading " + ModelMessage + " - Please Wait - Elapsed: " + UpdateClock.ToString() + " sec.";
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
                    SamLog.AddEntry("Startup", uimess.Message);
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
                    IsUIEnabled = true;
                    if (uimess.Message.Equals("LoadModel"))
                    {
                        UpdateTimer.Stop();
                        SetupItemState = (int)SetupEnum.None;
                        if (uimess.Ob is SamResult)
                        {
                            SamResult result = (SamResult)uimess.Ob;
                            if (result.IsSuccess)
                            {
                                ModelStatusMessage = "Model Loaded";
                                ModelStatusColor = Brushes.Green;
                                SetupItemState = (int)(SetupEnum.ModelEnabled | SetupEnum.ImageEnabled | SetupEnum.EncodeEnabled);
                                UserMessage = "Model Load Complete";
                                SamLog.AddEntry("Load Model", UserMessage);
                            }
                            else
                            {
                                ModelStatusMessage = "Model Load Failed";
                                ModelStatusColor = Brushes.Red;
                                UserMessage = "Model Load Failed - See Event Log";
                                SamLog.AddEntry("Load Model", result.Message);
                                if (result.IsException) SamLog.AddEntry("Load Model", result.ExMessage);
                            }
                        }
                    }
                    else if (uimess.Message.Equals("LoadImage"))
                    {
                        SetupItemState = (int)SetupEnum.None;
                        if (uimess.Ob is SamResult)
                        {
                            SamResult result = (SamResult)uimess.Ob;
                            if (result.IsSuccess)
                            {
                                SetupItemState = (int)(SetupEnum.ModelEnabled | SetupEnum.ImageEnabled);
                                UserMessage = ImageShortName + " - Loaded";
                                BS = ImageSupportWPF.ToBitmapSource(SamModel.mtImage);
                                SamLog.AddEntry("Load Image", UserMessage);
                            }
                            else
                            {
                                UserMessage = ImageShortName + " - Load Failed";
                                SamLog.AddEntry("Load Model", result.Message);
                                if (result.IsException) SamLog.AddEntry("Load Model", result.ExMessage);
                            }
                        }
                        else
                        {
                            UserMessage = "Unexpected data type in Load Image Task Complete";
                            SamLog.AddEntry("Load Image", UserMessage);
                        }

                    }
                    else if (uimess.Message.Equals("Encode"))
                    {
                        SetupItemState = (int)SetupEnum.None;
                        UpdateTimer.Stop();
                        if (uimess.Ob is SamResult)
                        {
                            SamResult result = (SamResult)uimess.Ob;
                            if (result.IsSuccess)
                            {
                                EncodingStatusMessage = "Encoding successful";
                                ModelStatusMessage = "Model Encoded";
                                ModelStatusColor = Brushes.Green;
                                SetupItemState = (int)SetupEnum.None;
                                UserMessage = "Model Encode Complete";
                                TabItemState = (int)(P1Tabs.Console | P1Tabs.Setup | P1Tabs.Sam);
                                NextTabIndex = (int)Math.Log((int)P1Tabs.Sam, 2);  // Switch to Sam Tab
                                Resize_Bitmap();
                                UpdateTimer.Tag = "ChangeTab";
                                UpdateTimer.Start();  // Change index after a second to allow user to see the message
                                SamLog.AddEntry("Encode", "Model Encode Complete!");
                            }
                            else
                            {
                                EncodingStatusMessage = "Encoding failed";
                                ModelStatusMessage = "Model Encode Failed";
                                ModelStatusColor = Brushes.Red;
                                UserMessage = "Model Encode Failed";
                                SamLog.AddEntry("Encode", "Model Encode Failed");
                            }
                            SetupItemState = (int)(SetupEnum.ModelEnabled | SetupEnum.ImageEnabled);

                        }
                    }
                    else if (uimess.Message.Equals("Sam2Point"))
                    {
                        UpdateTimer.Stop();
                        SetupItemState = (int)SetupEnum.None;
                        if (uimess.Ob is SamResult)
                        {
                            SamResult sr = (SamResult)uimess.Ob;
                            bool success = sr.IsSuccess;
                            if (success)
                            {
                                SetupItemState = (int)(SetupEnum.ModelEnabled | SetupEnum.ImageEnabled | SetupEnum.EncodeEnabled);
                                UserMessage = ImageShortName + " - Segmented";
                                DataSetPayload dsp = new DataSetPayload(OperationEnum.Sam2Single, -1);
                                if (RightColumnState == (int)OperationEnum.Sam2MultiMask)
                                    dsp = new DataSetPayload(OperationEnum.Sam2MultiMask, -1);
                                if (RightColumnState == (int)OperationEnum.Sam2Multiple)
                                    dsp = new DataSetPayload(OperationEnum.Sam2Multiple, -1);
                                PageAction?.Invoke(new PageMessage(2, thisPage, dsp));
                            }
                            else
                            {
                                UserMessage = ImageShortName + " - Issues Encountered during segmenation";
                            }
                        }
                        IsRunButtonAllowed = true;
                        IsSamInProgress = false;
                    }
                    else if (uimess.Message.Equals("Sam3Prompt"))
                    {
                        UpdateTimer.Stop();
                        SetupItemState = (int)SetupEnum.None;
                        if (uimess.Ob is SamResult)
                        {
                            SamResult sr = (SamResult)uimess.Ob;
                            bool success = sr.IsSuccess;

                            if (success)
                            {
                                if (SamModel.WorkingDetections.Count() == 0)
                                {
                                    UserMessage = ImageShortName + " - No detections found by Sam3";
                                    SamLog.AddEntry("Sam3", "Detection Complete: " + ImageShortName + " - No Detections");
                                }
                                else
                                {
                                    SetupItemState = (int)(SetupEnum.ModelEnabled | SetupEnum.ImageEnabled | SetupEnum.EncodeEnabled);
                                    UserMessage = ImageShortName + " - Segmented";
                                    PageAction?.Invoke(new PageMessage(3, thisPage, new DataSetPayload(OperationEnum.Sam3PCS, true)));
                                    SamLog.AddEntry("Sam3", "Detection Complete: " + ImageShortName + " - Segmented");
                                }
                            }
                            else
                            {
                                UserMessage = ImageShortName + " - Issues Encountered during segmentation";
                                SamLog.AddEntry("Sam3", "Issues Encountered during segmentation");
                            }
                        }
                        IsRunButtonAllowed = true;
                        IsSamInProgress = false;
                    }

                }

                else if (uimess.Code == UiEnum.SamDetection)
                {
                    if (uimess.Ob is SamDetection)
                    {
                        SamDetection sam = (SamDetection)uimess.Ob;
                        OpenSafeMat osm = OpenIP.ByteArrayToMat(sam.Data, sam.ImageHeight, sam.ImageWidth);
                        if (osm.IsSuccess)
                        {
                            if (sam.Rect.Width == 0)
                            {
                                OpenSafeRectangle safe = OpenIP.FindMaskBoundingBox(osm.Mt);  // Not included in pvs
                                if (safe.IsSuccess)
                                {
                                    sam.Rect = new System.Drawing.Rectangle(safe.Rect.X, safe.Rect.Y, safe.Rect.Width, safe.Rect.Height);
                                }
                            }
                            WorkingDetection work = new WorkingDetection(sam.Senum, sam.Index, osm.Mt, sam.Score, sam.Iou, sam.Rect, sam.Label, sam.SubLabel);
                            SamModel.WorkingDetections.Add(work);
                            SamLog.AddEntry("Sam Detection", "Index " + sam.Index.ToString() + " " + sam.Label + " " + sam.SubLabel);
                        }
                        else
                        {
                            SamLog.AddEntry("Sam Detection", "Index " + sam.Index.ToString() + " " + sam.Label + " " + sam.SubLabel + " - Error converting byte array to Mat");
                            return;
                        }                       

                    }
                    else  // Its a detection count
                    {
                        string rob = uimess.Message;
                    }
                }
                //      UserMessage += uimess.Message + "\r\n";
            }));

        }

        private void ByteArrayMessageAction(ByteArrayMessageOut bamo)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (bamo.Message == "ExportLogs")
                {
                    File.WriteAllBytes("c:\\Temp\\MAF\\LogZip.zip", bamo.Data);
                }
                else if (bamo.Message == "Export")
                {
                    File.WriteAllBytes("c:\\Temp\\MAF\\ExportZip.zip", bamo.Data);
                }
            }));

            return;

        }


        #endregion


    }

    public class SamPoint : ObservableObject
    {
        public bool IsPositive { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Ax1 { get; set; }
        public int Ax2 { get; set; }
        public int Ay1 { get; set; }
        public int Ay2 { get; set; }
        public int Bx1 { get; set; }
        public int Bx2 { get; set; }
        public int By1 { get; set; }
        public int By2 { get; set; }
        public Brush Color { get; set; }

        public SamPoint(bool isPositive, int x, int y)
        {
            IsPositive = isPositive;
            X = x;
            Y = y;
            if (isPositive)
            {
                Ax1 = x - 10;
                Ay1 = y;
                Ax2 = x + 10;
                Ay2 = y;
                Bx1 = x;
                By1 = y - 10;
                Bx2 = x;
                By2 = y + 10;
                Color = Brushes.White;
            }
            else
            {
                Ax1 = x - 7;
                Ay1 = y - 7;
                Ax2 = x + 7;
                Ay2 = y + 7;
                Bx1 = x - 7;
                By1 = y + 7;
                Bx2 = x + 7;
                By2 = y - 7;
                Color = Brushes.Red;
            }
        }
    }

    public class SamRectangle : ObservableObject
    {
        public bool Solid { get; set; }
        public bool IsPositive { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public DoubleCollection Dash { get; set; }
        public Brush Color { get; set; }

        public SamRectangle(bool s, bool ip, int x, int y, int w, int h)
        {
            Solid = s;
            IsPositive = ip;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            if (IsPositive)
            {
                Color = Brushes.White;
            }
            else
            {
                Color = Brushes.Red;
            }
            if (Solid == false)
            {
                Dash = new DoubleCollection(new double[] { 4, 4 });
            }
            else
            {
                Dash = new DoubleCollection();
            }
        }
    }
    public enum P1Tabs
    {
        Setup = 1,
        Sam = 2,
        Setup2 = 4,
        Spare = 8,
        Project = 16,
        Console = 32
    }

    public enum SetupEnum
    {
        None = 0,
        ImageEnabled = 1,
        ModelEnabled = 2,
        EncodeEnabled = 4,
    }

    public enum OperationEnum
    {
        None = 0,
        Sam2Single = 1,
        Sam2MultiMask = 2,
        Sam2Multiple = 4,
        Sam3PCS = 8,
        SamNew = 16,
        Edit = 32,
        Setup = 64

    }
}
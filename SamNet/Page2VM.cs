// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SharedToolbox;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SamNet.Native;
using Open.IP;

namespace SamNet
{
    public class Page2VM : ObservableObject, IPageVM
    {

        #region Vars and Constructor

        private Action<PageMessage>? PageAction;
        private SamWorker? Work;
        private ConcurrentQueue<object>? MessageToWorker;

        private EdwardsMessageBus eventBus;

        private AutoResetEvent are;

        private int thisPage = 2;

        private BitmapSource? _BS;

        private Brush _ReviewStatusColor = Brushes.Red;

        private bool _IsArrowLeftShowing;
        private bool _IsArrowRightShowing;
        private bool _IsBoxPadding;
        private bool _IsClearPoints;
        private bool _IsClickListPopulated;
        private bool _IsLinesPopulated;
        private bool _IsMainLinesPopulated;
        private bool _IsMultipleDetections;
        private bool _IsRoiCanvasShowing;
        private bool _IsRoiGroupShowing;
        private bool _IsSamMultiMask;
        private bool _IsSamMultiMaskButtonsShown;
        private bool _IsShowAcceptLabelButton;
        private bool _IsShowUpdateLabelButton;
        private bool _IsSplitContourMode;

        private bool IsMouseDown = false;
        private bool IsLeftMouse = false;
        private bool IsDetectionUpdateWaiting = false;
        private bool AreRadioButtonsActive = false;
        private bool AreFullMasksNeeded = false;


        private double MouseDownX = 0;
        private double MouseDownY = 0;
        private double MouseStartX = 0;
        private double MouseStartY = 0;

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

        private int _CurrentRoi;

        private int _MultiMaskState = 0;
        private int _OperationState = 0;
        private int _TabItemState;
        private int _PreviousTabIndex;
        private int _RBFullImageOrBoxState;
        private int _RBMaskViewState;
        private int _SelectedTabFlag;
        private int _SelectedTabIndex;

        private double BS_Max_Width = 1300;
        private double BS_Max_Height = 822;

        private int DetectionView = 2;
        private int EditIndex = 0;

        private DataSetPayload Info;
        private ObservableCollection<LineSegment> _Lines;
        private ObservableCollection<LineSegment> _MainLines;

        private string MainLabelUncleaned = "";
        private string SubLabelUncleaned = "";

        private string _DetectionMessage = "";
        private string _LabelMessage = "";
        private string _ReviewStatusMessage = "";
        private string _MainLabel = "";
        private string _SubLabel = "";
        private string _UserMessage = "";

        System.Drawing.Rectangle DisplayedRect;

        DispatcherTimer UpdateTimer;

        #endregion

        #region Constructor And Focus

        public Page2VM(Action<PageMessage> act, SamWorker? work, ConcurrentQueue<object>? messageToWorker)
        {
            PageAction = act;
            MessageToWorker = messageToWorker;
            Work = work;

            UserMessage = "SamNet - Review and edit";

            eventBus = (EdwardsMessageBus)App.Current.Resources["EventBus"];

            BS_CanvasWidth = 1320;
            BS_CanvasHeight = 836;
            BS_Width = (int)BS_Max_Width;
            BS_Height = (int)BS_Max_Height;
            BS_Left = 5;
            BS_Top = 5;

            Info = new DataSetPayload();
            eventBus = (EdwardsMessageBus)App.Current.Resources["EventBus"];

            are = new AutoResetEvent(false);
            TabItemState = (int)(P2Tabs.Console | P2Tabs.DataSet | P2Tabs.Review);

            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Interval = TimeSpan.FromSeconds(1);
            UpdateTimer.Tick += UpdateTimer_Tick;
            UpdateTimer.Stop();

            ReviewStatusMessage = "No review status available";
            ReviewStatusColor = Brushes.Red;

            _Lines = new ObservableCollection<LineSegment>();
            _MainLines = new ObservableCollection<LineSegment>();

            IsClearPoints = false;
            IsClickListPopulated = false;
            DisplayedRect = new System.Drawing.Rectangle(0, 0, 0, 0);
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


            SelectedTabIndex = 0;
            Resize_Bitmap();
            BS = ImageSupportWPF.ToBitmapSource(SamModel.mtImage);
            UpdateTimer.Tag = "CombineMasks";
            UpdateTimer.Start();
            CurrentRoi = -1;
            CurrentRoi = 0;
            AreRadioButtonsActive = true;
            IsMultipleDetections = SamModel.WorkingDetections.Count() > 1 ? true : false;
            MainLabel = "";
            IsRoiGroupShowing = false;
            ShowFullImageRegion();
            if (caller == 1)
            {
                RBFullImageOrBoxState = (int)RBFullImageOrBox.FullImage;
                RBMaskViewState = (int)RBMaskView.Overlay;
                OperationState = (int)Info.Operation;
                MainLabel = "New Detection";
                if (OperationState == (int)OperationEnum.Sam2Multiple)
                {
                    int count = SamModel.WorkingDetections.Count();
                    if (count > 0) DetectionMessage = "Showing Detection " + (CurrentRoi + 1).ToString() + " of " + count.ToString();
                }
                if (Info.Operation.HasFlag(OperationEnum.Edit))
                {
                    SelectedTabIndex = (int)Math.Log((int)P2Tabs.Edit, 2);
                    OperationState = (int)OperationEnum.Sam2Multiple;
                    SamModel.mtMask = new Mat(SamModel.mtImage.Rows, SamModel.mtImage.Cols, DepthType.Cv8U, 1);
                    SamModel.mtMask.SetTo(new MCvScalar(0));
                    SamModel.mtOutlineMask = new Mat(SamModel.mtImage.Rows, SamModel.mtImage.Cols, DepthType.Cv8U, 1);
                    SamModel.mtOutlineMask.SetTo(new MCvScalar(0));
                    TabItemState = (int)(P2Tabs.Console | P2Tabs.Edit | P2Tabs.Setup);
                }
                else
                {
                    TabItemState = (int)(P2Tabs.Console | P2Tabs.Review | P2Tabs.Setup);
                }
            }
            else if (caller == 3)
            {
                if (Info.Operation.HasFlag(OperationEnum.SamNew))
                {
                    TabItemState = (int)(P2Tabs.Console | P2Tabs.Review | P2Tabs.Setup);
                    IsRoiGroupShowing = true;
                    ShowRoiImageRegion(true);
                    SelectedTabIndex = (int)Math.Log((int)P2Tabs.Edit, 2);
                }
                else if (Info.Operation.HasFlag(OperationEnum.Edit))
                {
                    TabItemState = (int)(P2Tabs.Console | P2Tabs.Edit | P2Tabs.Setup);
                    if (Info.UniqueIDList.Count > 0)
                    {
                        CurrentRoi = -1;
                        CurrentRoi = 0;
                        SamModel.WorkingDetections.Clear();
                        for (int j = 0; j < Info.UniqueIDList.Count(); j++)
                        {
                            for (int i = 0; i < SamModel.DataSet.Count; i++)
                            {
                                if (Info.UniqueIDList[j] == SamModel.DataSet[i].UniqueID)
                                {
                                    OpenSafeMat osm = OpenIP.ListOfIntToBinaryMask(SamModel.DataSet[i].MaskRle
                                   , SamModel.DataSet[i].MaskSize[0], SamModel.DataSet[i].MaskSize[1]);
                                    if (osm.IsSuccess)
                                    {
                                        System.Drawing.Rectangle rect = SamModel.DataSet[i].Rect;
                                        WorkingDetection detection = new WorkingDetection(SamEnum.Sam2Multiple, j, osm.Mt, 0.0f, 0.0f, rect);
                                        detection.DataSetUniqueID = SamModel.DataSet[i].UniqueID;
                                        detection.Label = SamModel.DataSet[i].Label;
                                        detection.SubLabel = SamModel.DataSet[i].SubLabel;
                                        detection.Score = SamModel.DataSet[i].Score;
                                        SamModel.WorkingDetections.Add(detection);
                                        if (Info.CurrentUniqueID == detection.DataSetUniqueID)
                                        {
                                            CurrentRoi = SamModel.WorkingDetections.Count() - 1;
                                            MainLabel = SamModel.WorkingDetections[CurrentRoi].Label;
                                            SubLabel = SamModel.WorkingDetections[CurrentRoi].SubLabel;
                                        }
                                    }
                                    break;
                                }
                            }
                        }

                        RBFullImageOrBoxState = (int)RBFullImageOrBox.Box;
                        RBMaskViewState = (int)RBMaskView.Overlay;
                        OperationState = (int)Info.Operation;
                        OperationState = (int)OperationEnum.Sam2Multiple;
                        SelectedTabIndex = (int)Math.Log((int)P2Tabs.Edit, 2);
                        int count = SamModel.WorkingDetections.Count();
                        if (count > 0)
                        {
                            DetectionMessage = "Showing Detection " + (CurrentRoi + 1).ToString() + " of " + count.ToString();
                            int updateRoiArrows = CurrentRoi;
                            CurrentRoi = -1;
                            CurrentRoi = updateRoiArrows;
                        }
                        ShowRoiImageRegion(true);
                        RefreshShownImages();
                    }
                }
            }
            else if (caller == 4)
            {
                SelectedTabIndex = PreviousTabIndex;
            }
            if (SamModel.DataSet.Count() > 0) TabItemState |= (int)P2Tabs.DataSet;
        }

        #endregion

        #region Properties
        public ObservableCollection<LineSegment> Lines
        {
            get
            {
                IsLinesPopulated = _Lines.Any();
                return _Lines;
            }
        }
        public ObservableCollection<LineSegment> MainLines
        {
            get
            {
                IsMainLinesPopulated = _MainLines.Any();
                return _MainLines;
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
        public int CurrentRoi
        {
            get { return _CurrentRoi; }
            set
            {
                if (_CurrentRoi != value)
                {
                    _CurrentRoi = value;
                    if ((SamModel.WorkingDetections is null) || (SamModel.WorkingDetections.Count() < 2))
                    {
                        IsArrowLeftShowing = false;
                        IsArrowRightShowing = false;
                    }
                    else
                    {
                        if (CurrentRoi == 0)
                        {
                            IsArrowLeftShowing = false;
                        }
                        else
                        {
                            IsArrowLeftShowing = true;
                        }
                        if (CurrentRoi == (SamModel.WorkingDetections.Count() - 1))
                        {
                            IsArrowRightShowing = false;
                        }
                        else
                        {
                            IsArrowRightShowing = true;
                        }
                    }
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
        public bool IsArrowLeftShowing
        {
            get { return _IsArrowLeftShowing; }
            set
            {
                if (_IsArrowLeftShowing != value)
                {
                    _IsArrowLeftShowing = value;
                    OnPropertyChanged("IsArrowLeftShowing");
                }
            }
        }
        public bool IsArrowRightShowing
        {
            get { return _IsArrowRightShowing; }
            set
            {
                if (_IsArrowRightShowing != value)
                {
                    _IsArrowRightShowing = value;
                    OnPropertyChanged("IsArrowRightShowing");
                }
            }
        }

        public bool IsBoxPadding
        {
            get { return _IsBoxPadding; }
            set
            {
                if (_IsBoxPadding != value)
                {
                    _IsBoxPadding = value;
                    if (_IsBoxPadding) IsSplitContourMode = false;
                    OnPropertyChanged("IsBoxPadding");
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
        public bool IsMainLinesPopulated
        {
            get { return _IsMainLinesPopulated; }
            set
            {
                if (_IsMainLinesPopulated != value)
                {
                    _IsMainLinesPopulated = value;
                    OnPropertyChanged("IsMainLinesPopulated");
                }
            }
        }
        public bool IsMultipleDetections
        {
            get { return _IsMultipleDetections; }
            set
            {
                if (_IsMultipleDetections != value)
                {
                    _IsMultipleDetections = value;
                    OnPropertyChanged("IsMultipleDetections");
                }
            }
        }
        public bool IsRoiCanvasShowing
        {
            get { return _IsRoiCanvasShowing; }
            set
            {
                if (_IsRoiCanvasShowing != value)
                {
                    _IsRoiCanvasShowing = value;
                    OnPropertyChanged("IsRoiCanvasShowing");
                }
            }
        }
        public bool IsRoiGroupShowing
        {
            get { return _IsRoiGroupShowing; }
            set
            {
                if (_IsRoiGroupShowing != value)
                {
                    _IsRoiGroupShowing = value;
                    OnPropertyChanged("IsRoiGroupShowing");
                }
            }
        }
        public bool IsSamMultiMask
        {
            get { return _IsSamMultiMask; }
            set
            {
                if (_IsSamMultiMask != value)
                {
                    _IsSamMultiMask = value;
                    OnPropertyChanged("IsSamMultiMask");
                }
            }
        }
        public bool IsSamMultiMaskButtonsShown
        {
            get { return _IsSamMultiMaskButtonsShown; }
            set
            {
                if (_IsSamMultiMaskButtonsShown != value)
                {
                    _IsSamMultiMaskButtonsShown = value;
                    OnPropertyChanged("IsSamMultiMaskButtonsShown");
                }
            }
        }
        public bool IsShowAcceptLabelButton
        {
            get { return _IsShowAcceptLabelButton; }
            set
            {
                if (_IsShowAcceptLabelButton != value)
                {
                    _IsShowAcceptLabelButton = value;
                    OnPropertyChanged("IsShowAcceptLabelButton");
                }
            }
        }
        public bool IsShowUpdateLabelButton
        {
            get { return _IsShowUpdateLabelButton; }
            set
            {
                if (_IsShowUpdateLabelButton != value)
                {
                    _IsShowUpdateLabelButton = value;
                    if (value == false) IsDetectionUpdateWaiting = false;
                    OnPropertyChanged("IsShowUpdateLabelButton");
                }
            }
        }

        public bool IsSplitContourMode
        {
            get { return _IsSplitContourMode; }
            set
            {
                if (_IsSplitContourMode != value)
                {
                    _IsSplitContourMode = value;
                    if (_IsSplitContourMode) IsBoxPadding = false;
                    OnPropertyChanged("IsSplitContourMode");
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
        public int MultiMaskState
        {
            get { return _MultiMaskState; }
            set
            {
                if (_MultiMaskState != value)
                {
                    _MultiMaskState = value;
                    OnPropertyChanged("MultiMaskState");
                }
            }
        }
        public int OperationState
        {
            get { return _OperationState; }
            set
            {
                if (_OperationState != value)
                {
                    _OperationState = value;
                    OnPropertyChanged("OperationState");
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
        public string Name
        {
            get { return "Page 2"; }
        }
        public int RBFullImageOrBoxState
        {
            get { return _RBFullImageOrBoxState; }
            set
            {
                if (_RBFullImageOrBoxState != value)
                {
                    _RBFullImageOrBoxState = value;
                    OnPropertyChanged("RBFullImageOrBoxState");
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
                if (tag.Equals("AcceptLabel"))
                {
                    OpenSafeListOfInt safeList = OpenIP.BinaryMatToListOfInt(SamModel.WorkingDetections[CurrentRoi].mtMask);
                    if (safeList.IsSuccess)
                    {
                        System.Drawing.Rectangle Rect = SamModel.WorkingDetections[CurrentRoi].Rect;
                        SamDataSet sds = new SamDataSet(MainLabel, SubLabel, safeList.ListOfInt, Rect, 0,
                           new List<int> { SamModel.mtImage.Rows, SamModel.mtImage.Cols }, SamModel.WorkingDetections[CurrentRoi].Score);
                        int dataSetId = sds.UniqueID;
                        SamModel.WorkingDetections[CurrentRoi].DataSetUniqueID = dataSetId;
                        SamModel.DataSet.Add(sds);
                        TabItemState |= (int)P2Tabs.DataSet;
                        IsShowAcceptLabelButton = false;
                    }
                }
                else if (tag.Equals("UpdateLabel"))
                {
                    UpdateDataSetFromCurrentSamDetection();
                }
                else if (tag.StartsWith("Sam2Accept"))
                {
                    int firstUniqueID = -1;
                    for (int i = 0; i < SamModel.WorkingDetections.Count(); i++)
                    {
                        OpenSafeListOfInt safeList = OpenIP.BinaryMatToListOfInt(SamModel.WorkingDetections[i].mtMask);
                        if (safeList.IsSuccess)
                        {
                            System.Drawing.Rectangle Rect = SamModel.WorkingDetections[i].Rect;
                            string substr = FindUniqueNameInDataSet("Sam2_");
                            SamDataSet sds = new SamDataSet("New Detection", substr, safeList.ListOfInt, Rect, 0,
                               new List<int> { SamModel.mtImage.Rows, SamModel.mtImage.Cols }, SamModel.WorkingDetections[i].Score);
                            int dataSetId = sds.UniqueID;
                            if (firstUniqueID == -1) firstUniqueID = dataSetId;
                            SamModel.WorkingDetections[i].DataSetUniqueID = dataSetId;
                            SamModel.DataSet.Add(sds);
                            TabItemState |= (int)P2Tabs.DataSet;
                        }
                        if (SamModel.WorkingDetections.Count() == 1)
                        {
                            PageAction?.Invoke(new PageMessage(3, thisPage, new DataSetPayload(OperationEnum.Sam2Single, firstUniqueID)));
                        }
                        else
                        {
                            PageAction?.Invoke(new PageMessage(3, thisPage, new DataSetPayload(OperationEnum.Sam2Multiple, firstUniqueID)));
                        }
                    }
                }
                else if (tag.StartsWith("MultiMaskAccept"))
                {
                    CurrentRoi = (int)Math.Log((int)MultiMaskState, 2);
                    OpenSafeListOfInt safeList = OpenIP.BinaryMatToListOfInt(SamModel.WorkingDetections[CurrentRoi].mtMask);
                    if (safeList.IsSuccess)
                    {
                        System.Drawing.Rectangle Rect = SamModel.WorkingDetections[CurrentRoi].Rect;
                        string substr = FindUniqueNameInDataSet("Multi_");
                        float detectionScore = DetectionAScore;
                        if (CurrentRoi == 1) detectionScore = DetectionBScore;
                        if (CurrentRoi == 2) detectionScore = DetectionCScore;
                        SamDataSet sds = new SamDataSet("New MultiMask", substr, safeList.ListOfInt, Rect, 0,
                            new List<int> { SamModel.mtImage.Rows, SamModel.mtImage.Cols }, detectionScore);
                        int dataSetId = sds.UniqueID;
                        SamModel.WorkingDetections[CurrentRoi].DataSetUniqueID = dataSetId;
                        SamModel.DataSet.Add(sds);
                        TabItemState |= (int)P2Tabs.DataSet;
                        IsShowAcceptLabelButton = false;
                        //      eventBus.Publish(new EventMessage(-2, 2, EventEnum.ResetXaml));
                        PageAction?.Invoke(new PageMessage(3, thisPage, new DataSetPayload(OperationEnum.Sam2MultiMask, sds.UniqueID)));
                        IsSamMultiMask = false;
                    }
                }
                else if ((tag.Equals("MultiMaskReject")) || (tag.Equals("Sam2Reject")))
                {
                    IsRoiCanvasShowing = false;
                    IsRoiGroupShowing = false;
                    IsSamMultiMask = false;
                    IsSamMultiMaskButtonsShown = false;
                    MultiMaskState = 0;
                    CurrentRoi = 0;
                    SamModel.ClearSamDetections();
                    AreRadioButtonsActive = false;
                    eventBus.Publish(new EventMessage(-2, 2, EventEnum.ResetXaml));
                    PageAction?.Invoke(new PageMessage(1, thisPage, new DataSetPayload(OperationEnum.SamNew, -1)));
                }
                else if (tag.Equals("RemoveNoise"))
                {
                    OpenSafeMat asm = OpenIP.RemoveSmallContours(SamModel.WorkingDetections[CurrentRoi].mtMask, 0.2);
                    if (asm.IsSuccess)
                    {
                        OpenSafeRectangle asr = OpenIP.FindMaskBoundingBox(asm.Mt);
                        if (asr.IsSuccess)
                        {
                            SamModel.WorkingDetections[CurrentRoi].mtMask = asm.Mt!.Clone();
                            SamModel.WorkingDetections[CurrentRoi].Rect =
                                new System.Drawing.Rectangle(asr.Rect.X, asr.Rect.Y, asr.Rect.Width, asr.Rect.Height);
                            if (asm.Mt != null) asm.Mt.Dispose();
                            DisplayDetection(DetectionView, CurrentRoi);
                            AreFullMasksNeeded = true;
                        }
                    }
                    SamModel.CombineDetectionMasks();
                }
                else if (tag.Equals("RoiLeft"))
                {
                    if (CurrentRoi > 0)
                    {
                        DisplayDetection(DetectionView, --CurrentRoi);
                        int count = SamModel.WorkingDetections.Count();
                        DetectionMessage = "Showing Detection " + (CurrentRoi + 1).ToString() + " of " + count.ToString();
                        SubLabel = SamModel.WorkingDetections[CurrentRoi].SubLabel;
                        Lines.Clear();

                    }
                }
                else if (tag.Equals("RoiRight"))
                {
                    if (CurrentRoi < (SamModel.WorkingDetections.Count - 1))
                    {
                        DisplayDetection(DetectionView, ++CurrentRoi);
                        int count = SamModel.WorkingDetections.Count();
                        DetectionMessage = "Showing Detection " + (CurrentRoi + 1).ToString() + " of " + count.ToString();
                        SubLabel = SamModel.WorkingDetections[CurrentRoi].SubLabel;
                        Lines.Clear();
                    }
                }
                else if (tag.Equals("EditDetection"))
                {
                    if (IsSplitContourMode)
                    {
                        SplitSegmentation();
                        return;
                    }
                    List<OpenPoints> aPoints = new List<OpenPoints>();
                    for (int i = 0; i < Lines.Count(); i++)
                    {
                        aPoints.Add(new OpenPoints(Lines[i].IsLeftMouse, Lines[i].Index, Lines[i].X1, Lines[i].Y1));
                    }
                    System.Drawing.Rectangle rect = SamModel.WorkingDetections[CurrentRoi].Rect;
                    if (IsBoxPadding) rect = DisplayedRect;  // if padded, that changes are match the DisplayedRectBox
                    OpenSafeMat asm = OpenIP.EditDetection(SamModel.WorkingDetections[CurrentRoi].mtMask, DisplayedRect,
                        BS_Roi_Width, BS_Roi_Height, aPoints);
                    if (asm.IsSuccess)
                    {
                        OpenSafeRectangle asr = OpenIP.FindMaskBoundingBox(asm.Mt);
                        if (asr.IsSuccess)
                        {
                            SamModel.WorkingDetections[CurrentRoi].mtMask = asm.Mt!.Clone();
                            SamModel.WorkingDetections[CurrentRoi].Rect
                                = new System.Drawing.Rectangle(asr.Rect.X, asr.Rect.Y, asr.Rect.Width, asr.Rect.Height);
                            if (asm.Mt != null) asm.Mt.Dispose();
                            DisplayDetection(DetectionView, CurrentRoi);
                            AreFullMasksNeeded = true;
                            if ((IsShowAcceptLabelButton == false) && (MainLabel.Length > 0))
                            {
                                IsShowUpdateLabelButton = true;
                                IsDetectionUpdateWaiting = true;
                            }

                        }
                    }
                    Lines.Clear();
                    IsLinesPopulated = false;
                }
                else if (tag.Equals("EditClearLast"))
                {
                    if (Lines.Count() > 0)
                    {
                        int iLast = Lines[Lines.Count() - 1].Index;
                        for (int i = Lines.Count() - 1; i >= 0; i--)
                        {
                            if (Lines[i].Index == iLast)
                            {
                                Lines.RemoveAt(i);
                            }
                            else
                            {
                                break;
                            }
                        }
                        IsLinesPopulated = _Lines.Any();
                    }
                }
                else if (tag.Equals("EditClearAll"))
                {
                    Lines.Clear();
                }
                else if (tag.Equals("SegmentationAddToDataSet"))
                {
                    List<OpenPoints> aPoints = new List<OpenPoints>();
                    for (int i = 0; i < MainLines.Count(); i++)
                    {
                        aPoints.Add(new OpenPoints(MainLines[i].IsLeftMouse, MainLines[i].Index, MainLines[i].X1, MainLines[i].Y1));
                    }
                    OpenSafeMat asm = OpenIP.EditDetectionMain(SamModel.mtMask, BS_Width, BS_Height, aPoints);
                    //      BS = ImageSupportWPF.ToBitmapSource(asm.Mt);
                    if (asm.IsSuccess)
                    {
                        OpenSafeRectangle asr = OpenIP.FindMaskBoundingBox(asm.Mt);
                        if (asr.IsSuccess)
                        {
                            int index = SamModel.WorkingDetections.Count();
                            System.Drawing.Rectangle rect = asr.Rect;
                            string labelMain = "Manual";
                            if (MainLabel.Length > 0) labelMain = MainLabel;
                            WorkingDetection detection = new WorkingDetection(SamEnum.Sam2Manual, index, asm.Mt, asr.Rect, labelMain, "Instance");
                            detection.DataSetUniqueID = -1;
                            SamModel.WorkingDetections.Add(detection);
                            CurrentRoi = SamModel.WorkingDetections.Count() - 1;
                            MainLabel = SamModel.WorkingDetections[CurrentRoi].Label;
                            SubLabel = SamModel.WorkingDetections[CurrentRoi].SubLabel;
                            IsShowUpdateLabelButton = true;
                            IsDetectionUpdateWaiting = true;
                        }
                    }
                    MainLines.Clear();
                    IsMainLinesPopulated = false;

                    // This is analagous to an ROI EditDetection,  now just add it to the data set for a 1 click

                    OpenSafeListOfInt safeList = OpenIP.BinaryMatToListOfInt(SamModel.WorkingDetections[CurrentRoi].mtMask);
                    if (safeList.IsSuccess)
                    {
                        System.Drawing.Rectangle Rect = SamModel.WorkingDetections[CurrentRoi].Rect;
                        string substr = FindUniqueNameInDataSet(SubLabel + "_");
                        SamDataSet sds = new SamDataSet(MainLabel, substr, safeList.ListOfInt, Rect, 0,
                            new List<int> { SamModel.mtImage.Rows, SamModel.mtImage.Cols }, SamModel.WorkingDetections[CurrentRoi].Score);
                        int dataSetId = sds.UniqueID;
                        SamModel.WorkingDetections[CurrentRoi].SubLabel = substr;
                        SamModel.WorkingDetections[CurrentRoi].DataSetUniqueID = dataSetId;
                        SamModel.DataSet.Add(sds);
                        TabItemState |= (int)P2Tabs.DataSet;
                        int count = SamModel.WorkingDetections.Count();
                        DetectionMessage = "Showing Detection " + (CurrentRoi + 1).ToString() + " of " + count.ToString();
                        IsShowAcceptLabelButton = false;
                        OpenSafeBool asb = SamModel.CombineDetectionMasks();
                        if (asb.IsSuccess) AreFullMasksNeeded = false;
                        RefreshShownImages();
                        MainLines.Clear();
                    }
                }
                else if (tag.Equals("SegmentationEditClear"))
                {
                    MainLines.Clear();
                }
            }
        }

        public void ImageMouseDown(object sender, MouseEventArgs e)
        {
            Canvas c = (Canvas)sender;
            if (c.Tag is not null)
            {
                if (c.Tag.ToString()!.Equals("Main"))
                {
                    if (IsRoiCanvasShowing) return;
                    if (e.RightButton == MouseButtonState.Pressed) return;   // No Subtractions in Big Image
                }
                else
                {
                    if (IsRoiCanvasShowing == false) return;
                }

                Point p = e.GetPosition(c);
                MouseDownX = p.X;
                MouseDownY = p.Y;
                MouseStartX = MouseDownX;
                MouseStartY = MouseDownY;
                IsMouseDown = true;
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    IsLeftMouse = false;
                }
                else
                {
                    if (IsSplitContourMode)
                    {
                        IsMouseDown = false;
                        IsLeftMouse = false;
                        return;  // No additions in SplitContourMode
                    }
                    IsLeftMouse = true;
                }
                if (c.Tag.ToString()!.Equals("Main"))
                {
                    MainLines.Clear();     // Only allow 1 manual instance at a time in the main image.
                    EditIndex = 0;
                }
                else
                {
                    if (Lines.Count() == 0)
                    {
                        EditIndex = 0;
                    }
                    else
                    {
                        EditIndex = Lines.Max(p => p.Index);
                        EditIndex++;
                    }
                }
            }
        }
        public void ImageMouseUp(object sender, MouseEventArgs e)
        {
            Canvas c = (Canvas)sender;
            if (c.Tag is not null)
            {
                if (c.Tag.ToString()!.Equals("Main"))
                {
                    if (IsRoiCanvasShowing) return;
                }
                else
                {
                    if (IsRoiCanvasShowing == false) return;
                }

                bool wasMouseDown = IsMouseDown;
                IsMouseDown = false;
                if (wasMouseDown)
                {
                    Point p = e.GetPosition(c);
                    if (c.Tag.ToString()!.Equals("Main"))
                    {
                        MainLines.Add(new LineSegment(IsLeftMouse, EditIndex, MouseDownX, MouseDownY, p.X, p.Y));
                        MainLines.Add(new LineSegment(IsLeftMouse, EditIndex, MouseStartX, MouseStartY, p.X, p.Y));
                        if (MainLines.Count() < 9)
                        {
                            MainLines.Clear();
                            IsMainLinesPopulated = false;
                        }

                    }
                    else
                    {
                        int linemin = 9;
                        Lines.Add(new LineSegment(IsLeftMouse, EditIndex, MouseDownX, MouseDownY, p.X, p.Y));
                        if (IsSplitContourMode == false)
                        {
                            Lines.Add(new LineSegment(IsLeftMouse, EditIndex, MouseStartX, MouseStartY, p.X, p.Y));
                        }
                        else
                        {
                            linemin = 4;  // SplitContourMode dont close and only need 4 points
                        }
                        if (Lines.Count() < linemin)
                        {
                            Lines.Clear();
                            IsLinesPopulated = false;
                        }
                    }
                }
            }
        }
        public void ImageMouseMove(object sender, MouseEventArgs e)
        {
            Canvas c = (Canvas)sender;
            if (c.Tag is not null)
            {
                if (c.Tag.ToString()!.Equals("Main"))
                {
                    if (IsRoiCanvasShowing) return;
                }
                else
                {
                    if (IsRoiCanvasShowing == false) return;
                }
                if (IsMouseDown)
                {
                    Point p = e.GetPosition(c);
                    double xdiff = Math.Abs(MouseDownX - p.X);
                    double ydiff = Math.Abs(MouseDownY - p.Y);
                    if ((xdiff + ydiff) > 5)
                    {
                        if (c.Tag.ToString()!.Equals("Main"))
                        {
                            MainLines.Add(new LineSegment(IsLeftMouse, EditIndex, MouseDownX, MouseDownY, p.X, p.Y));
                        }
                        else
                        {
                            Lines.Add(new LineSegment(IsLeftMouse, EditIndex, MouseDownX, MouseDownY, p.X, p.Y));
                        }
                        MouseDownX = p.X;
                        MouseDownY = p.Y;
                    }
                }
            }
        }
        public void ImageMouseLeave(object sender, MouseEventArgs e)
        {
            if (MainLines.Count() < 9)
            {
                IsMainLinesPopulated = false;
                MainLines.Clear();
            }
            if (Lines.Count() < 9)
            {
                IsLinesPopulated = false;
                Lines.Clear();
            }

            IsMouseDown = false;
        }

        public void RadioButtonChecked(object sender, EventArgs e)
        {
            if (AreRadioButtonsActive == false) return;
            IsSamMultiMaskButtonsShown = false;
            RadioButton tb = (RadioButton)sender;
            string? tag = tb.Tag.ToString();
            string? group = tb.GroupName;
            if ((tag is null) || (group is null)) return;
            int itag = 0;
            bool tagSuccess = int.TryParse(tag, out itag);
            if (tagSuccess)
            {
                if (group.Equals("RBFullImageOrBox"))
                {
                    int previousState = RBFullImageOrBoxState;
                    RBFullImageOrBoxState = itag;
                    if (previousState != itag) RefreshShownImages();
                }
                else if (group.Equals("RBMaskView"))
                {
                    int previousState = RBMaskViewState;
                    RBMaskViewState = itag;   
                    if (previousState != itag) RefreshShownImages();
                }
            }

            if (tag!.Equals("ViewOverlayMaskA"))
            {
                if (SamModel.WorkingDetections.Count() > 0)
                {
                    ViewMultiMaskOverlay(SamModel.WorkingDetections[0].mtMask);
                    ShowFullImageRegion();
                    IsSamMultiMaskButtonsShown = true;
                    MultiMaskState = (int)MultiMaskEnum.One;
                }
            }
            else if (tag!.Equals("ViewOverlayMaskB"))
            {
                if (SamModel.WorkingDetections.Count() > 1)
                {
                    ViewMultiMaskOverlay(SamModel.WorkingDetections[1].mtMask);
                    ShowFullImageRegion();
                    IsSamMultiMaskButtonsShown = true;
                    MultiMaskState = (int)MultiMaskEnum.Two;
                }
            }
            else if (tag!.Equals("ViewOverlayMaskC"))
            {
                if (SamModel.WorkingDetections.Count() > 2)
                {
                    ViewMultiMaskOverlay(SamModel.WorkingDetections[2].mtMask);
                    ShowFullImageRegion();
                    IsSamMultiMaskButtonsShown = true;
                    MultiMaskState = (int)MultiMaskEnum.Three;
                }
            }
            /*
            else if (tag!.Equals("RoiOriginal"))
            {
                DetectionView = 0;
                DisplayDetection(DetectionView, CurrentRoi);
            }
            else if (tag!.Equals("RoiMask"))
            {
                DetectionView = 1;
                DisplayDetection(DetectionView, CurrentRoi);
            }
            else if (tag!.Equals("RoiOverlay"))
            {
                DetectionView = 2;
                DisplayDetection(DetectionView, CurrentRoi);
            }
            else if (tag!.Equals("RoiLocation"))
            {
                if (AreFullMasksNeeded)
                {
                    OpenSafeBool asb = SamModel.CombineDetectionMasks();
                    if (asb.IsSuccess) AreFullMasksNeeded = false;
                }
                OpenSafeMat asm = OpenIP.CreateRoiLocationMat(SamModel.mtMask, SamModel.SamDetections[CurrentRoi].Rect);
                if (asm.IsSuccess)
                {
                    BS = ImageSupportWPF.ToBitmapSource(asm.Mt!);
                    ShowRoiImageRegion(false);
                    if (asm.Mt != null) asm.Mt.Dispose();
                }
            }
            */
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
                else if (tag.Equals("Setup"))   //  Swap views
                {
                    SamModel.ClearSamDetections();   // if not saved to DataSet, clean
                    MainLines.Clear();
                    Lines.Clear();
                    DataSetPayload payload = new DataSetPayload(OperationEnum.Setup, -1);
                    PageAction?.Invoke(new PageMessage(1, thisPage, payload));
                }
                else if (tag.Equals("DataSet"))   //  Swap views
                {
                    int unique = -1;
                    if ((CurrentRoi >= 0) && (CurrentRoi < SamModel.WorkingDetections.Count()))
                        unique = SamModel.WorkingDetections[CurrentRoi].DataSetUniqueID;
                    DataSetPayload payload = new DataSetPayload(OperationEnum.Edit, unique);
                    PageAction?.Invoke(new PageMessage(3, thisPage, payload));
                }
            }
        }

        /*
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
            if (CurrentRoi < SamModel.SamDetections.Count())
            {
                int datasetID = SamModel.SamDetections[CurrentRoi].DataSetUniqueID;
         //       int datasetIDindex = SamModel.DataSet.FindIndex(a => a.UniqueID == datasetID);
                if (datasetID < 0) 
                {
                    if (MainLabel.Length > 0)
                    {
                        LabelMessage = "Checking for existing Label";
                        IsShowAcceptLabelButton = true;
                    }
                    else
                    {
                        LabelMessage = "Enter label above";
                        IsShowAcceptLabelButton = false;
                    }
                }
                else
                {
                    if (MainLabel.Length > 0)
                    {
                        LabelMessage = "Checking for existing Label";
                        IsShowUpdateLabelButton = true;
                    }
                    else
                    {
                        LabelMessage = "Enter label above";
                        IsShowUpdateLabelButton = false;
                    }
                }
            }   
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
            if (CurrentRoi < SamModel.SamDetections.Count())
            {
                int datasetID = SamModel.SamDetections[CurrentRoi].DataSetUniqueID;
                //       int datasetIDindex = SamModel.DataSet.FindIndex(a => a.UniqueID == datasetID);
                if (datasetID < 0)
                {
                    if ((MainLabel.Length > 0) && (SubLabel.Length > 0))
                    {
                        LabelMessage = "Checking for existing Label";
                        IsShowAcceptLabelButton = true;
                    }
                    else
                    {
                        LabelMessage = "Enter label above";
                        IsShowAcceptLabelButton = false;
                    }
                }
                else
                {
                    if ((MainLabel.Length > 0) && (SubLabel.Length > 0))
                    {
                        LabelMessage = "Checking for existing Label";
                        IsShowUpdateLabelButton = true;
                    }
                    else
                    {
                        LabelMessage = "Enter label above";
                        IsShowUpdateLabelButton = false;
                    }
                }
            }
        }
        */

        #endregion

        #region Methods

        private void DisplayDetection(int mode, int index)
        {
            DisplayedRect = SamModel.WorkingDetections[index].Rect;
            if (IsBoxPadding)
            {
                DisplayedRect = OpenIP.AddPaddingToRect(DisplayedRect, SamModel.mtImage.Rows, SamModel.mtImage.Cols, 8);
            }
            OpenSafeMat asm = new OpenSafeMat();
            if (index >= SamModel.WorkingDetections.Count())
            {
                return;
            }
            //         OpenSafeMat asm = OpenIP.CreateRoiMat(Sam3Data.SamDetections[index].mtMask, Sam3Data.SamDetections[index].Rect);
            if (mode == 1)
            {
                asm = OpenIP.CreateRoiMat(SamModel.mtImage, DisplayedRect);
            }
            else if (mode == 0)
            {
                asm = OpenIP.CreateRoiMat(SamModel.WorkingDetections[index].mtMask, DisplayedRect);
            }
            else
            {
                if (MainLabel.Length == 0)
                {
                    if (SamModel.WorkingDetections[index].Label.Length > 0)
                    {
                        MainLabel = SamModel.WorkingDetections[index].Label;
                        if (SubLabel.Length == 0)
                        {
                            SubLabel = SamModel.WorkingDetections[index].SubLabel;
                        }
                    }
                }
                OpenSafeMat asmOutline = OpenIP.CreateOutlineMat(SamModel.mtImage, SamModel.WorkingDetections[index].mtMask);
                if (asmOutline.IsSuccess)
                {
                    asm = OpenIP.CreateRoiMat(asmOutline.Mt!, DisplayedRect);
                    if (asm.IsSuccess)
                    {
                        int imageSize = asm.Mt.Rows * asm.Mt.Cols;
                        if (imageSize > 600000)
                        {
                            int growth = 1;
                            if (imageSize > 3000000) growth = 2;
                            if (imageSize > 8000000) growth = 3;
                            if (imageSize > 16000000) growth = 4;
                            CvInvoke.Dilate(asm.Mt, asm.Mt, null, new System.Drawing.Point(1, 1), growth, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
                        }
                    }
                }
            }

            if (asm.IsSuccess)
            {
                BS = ImageSupportWPF.ToBitmapSource(asm.Mt!);
                Resize_Roi_Bitmap(asm.Mt!);
                ShowRoiImageRegion(true);
                if (asm.Mt != null) asm.Mt.Dispose();
            }

        }

        private string FindUniqueNameInDataSet(string rootstr)
        {
            string result = "";
            for (int i = 1; i < 1000; i++)
            {
                result = rootstr + i.ToString("D3");
                bool isTaken = SamModel.DataSet.Exists(a => a.SubLabel == result);
                if (isTaken == false) break;
            }
            return result;
        }

        private string FindUniqueSplitNameInDataSet(string rootstr)
        {
            string result = "";
            for (int i = 65; i < 90; i++)
            {
                result = rootstr + "_" + ((char)i).ToString();
                bool isTaken = SamModel.DataSet.Exists(a => a.SubLabel == result);
                if (isTaken == false) break;
            }
            return result;
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
            }
            else
            {
                BS_Width = (int)BS_Max_Width;
                BS_Left = 5;
                double newHeight = SamModel.mtImage.Rows * colRatio;
                BS_Height = (int)newHeight;
                BS_Top = (((int)(BS_Max_Height) - BS_Height) >> 1) + 5;
            }
        }

        private void Resize_Roi_Bitmap(Mat mt)
        {
            if ((mt.Rows == 0) || (mt.Cols == 0)) return;
            double rowRatio = BS_Max_Height / mt.Rows;
            double colRatio = BS_Max_Width / mt.Cols;

            if (rowRatio < colRatio)
            {
                BS_Roi_Height = (int)BS_Max_Height;
                BS_Roi_Top = 5;
                double newWidth = mt.Cols * rowRatio;
                BS_Roi_Width = (int)newWidth;
                BS_Roi_Left = (((int)(BS_Max_Width) - BS_Roi_Width) >> 1) + 5;
            }
            else
            {
                BS_Roi_Width = (int)BS_Max_Width;
                BS_Roi_Left = 5;
                double newHeight = mt.Rows * colRatio;
                BS_Roi_Height = (int)newHeight;
                BS_Roi_Top = (((int)(BS_Max_Height) - BS_Roi_Height) >> 1) + 5;
            }
        }

        private void CombineMasks()
        {
            if (SamModel.WorkingDetections.Count > 0)
            {
                Mat mtCombinedMask = new Mat(SamModel.mtImage.Rows, SamModel.mtImage.Cols, DepthType.Cv8U, 1);
                Mat mtOutlineMask = new Mat(SamModel.mtImage.Rows, SamModel.mtImage.Cols, DepthType.Cv8U, 1);
                mtCombinedMask.SetTo(new MCvScalar(0));
                mtOutlineMask.SetTo(new MCvScalar(0));
                foreach (var detection in SamModel.WorkingDetections)
                {
                    if (detection.mtMask != null)
                    {
                        Mat mtShrink = new Mat();
                        CvInvoke.BitwiseOr(mtCombinedMask, detection.mtMask, mtCombinedMask);
                        CvInvoke.Erode(detection.mtMask, mtShrink, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
                        CvInvoke.BitwiseXor(mtShrink, detection.mtMask, mtShrink);
                        CvInvoke.BitwiseOr(mtOutlineMask, mtShrink, mtOutlineMask);
                        mtShrink.Dispose();
                    }
                }
                SamModel.mtMask = mtCombinedMask.Clone();
                mtCombinedMask.Dispose();
                SamModel.mtOutlineMask = mtOutlineMask.Clone();
                mtOutlineMask.Dispose();
                if (SamModel.mtOutlineMask.Rows > 0) ViewOverlay();
            }
        }

        private void RefreshShownImages()
        {
            if (RBMaskViewState == (int)RBMaskView.Original)
            {
                if (RBFullImageOrBoxState == (int)RBFullImageOrBox.FullImage)
                {
                    if (SamModel.mtImage.Rows == 0) return;
                    BS = ImageSupportWPF.ToBitmapSource(SamModel.mtImage);
                    ShowFullImageRegion();
                }
                else
                {
                    DetectionView = (int)RBMaskView.Original - 1;
                    DisplayDetection(DetectionView, CurrentRoi);
                }
            }
            else if (RBMaskViewState == (int)RBMaskView.FullMask)
            {
                if (RBFullImageOrBoxState == (int)RBFullImageOrBox.FullImage)
                {
                    if (SamModel.mtMask.Rows == 0) return;
                    if (AreFullMasksNeeded)
                    {
                        OpenSafeBool asb = SamModel.CombineDetectionMasks();
                        if (asb.IsSuccess) AreFullMasksNeeded = false;
                    }
                    BS = ImageSupportWPF.ToBitmapSource(SamModel.mtMask);
                    ShowFullImageRegion();
                }
                else
                {
                    DetectionView = (int)RBMaskView.FullMask - 1;
                    DisplayDetection(DetectionView, CurrentRoi);
                }
            }
            else if (RBMaskViewState == (int)RBMaskView.Overlay)
            {
                if (RBFullImageOrBoxState == (int)RBFullImageOrBox.FullImage)
                {
                    if (SamModel.mtOutlineMask.Rows == 0) return;
                    if (AreFullMasksNeeded)
                    {
                        OpenSafeBool asb = SamModel.CombineDetectionMasks();
                        if (asb.IsSuccess) AreFullMasksNeeded = false;
                    }
                    ViewOverlay();
                    ShowFullImageRegion();
                }
                else
                {
                    DetectionView = (int)RBMaskView.Overlay - 1;
                    DisplayDetection(DetectionView, CurrentRoi);
                }
            }
        }

        private void ShowFullImageRegion()
        {
            IsRoiCanvasShowing = false;
            IsRoiGroupShowing = false;
        }

        private void ShowRoiImageRegion(bool useRoiCanvas)
        {
            IsRoiCanvasShowing = useRoiCanvas;
            IsRoiGroupShowing = true;
        }

        private void SplitSegmentation()
        {
            List<OpenPoints> aPoints = new List<OpenPoints>();
            for (int i = 0; i < Lines.Count(); i++)
            {
                aPoints.Add(new OpenPoints(Lines[i].IsLeftMouse, Lines[i].Index, Lines[i].X1, Lines[i].Y1));
            }
            System.Drawing.Rectangle rect = SamModel.WorkingDetections[CurrentRoi].Rect;
            if (IsBoxPadding) rect = DisplayedRect;  // if padded, that changes are match the DisplayedRectBox
            OpenSafeMat asm = OpenIP.SplitDetection(SamModel.WorkingDetections[CurrentRoi].mtMask, DisplayedRect,
                BS_Roi_Width, BS_Roi_Height, aPoints);
            if (asm.IsSuccess)
            {
                float score = 0.0f;
                OpenSafeMat asmSplitA = OpenIP.ExactThreshold(asm.Mt, 255);
                if (asmSplitA.IsSuccess)
                {
                    OpenSafeRectangle asr = OpenIP.FindMaskBoundingBox(asmSplitA.Mt);
                    if (asr.IsSuccess)
                    {
                        SamModel.WorkingDetections[CurrentRoi].mtMask = asmSplitA.Mt!.Clone();
                        SamModel.WorkingDetections[CurrentRoi].Rect
                            = new System.Drawing.Rectangle(asr.Rect.X, asr.Rect.Y, asr.Rect.Width, asr.Rect.Height);
                        score = SamModel.WorkingDetections[CurrentRoi].Score;
                        if (asmSplitA.Mt != null) asmSplitA.Mt.Dispose();
                        IsDetectionUpdateWaiting = true;
                        UpdateDataSetFromCurrentSamDetection();
                    }
                }
                OpenSafeMat asmSplitB = OpenIP.ExactThreshold(asm.Mt, 127);
                if (asmSplitB.IsSuccess)
                {
                    OpenSafeRectangle asr = OpenIP.FindMaskBoundingBox(asmSplitB.Mt);
                    if (asr.IsSuccess)
                    {
                        OpenSafeListOfInt safeList = OpenIP.BinaryMatToListOfInt(asmSplitB.Mt);
                        if (safeList.IsSuccess)
                        {
                            string splitLabel = FindUniqueSplitNameInDataSet(SubLabel);
                            SamDataSet sds = new SamDataSet(MainLabel, splitLabel, safeList.ListOfInt, asr.Rect, 0,
                               new List<int> { SamModel.mtImage.Rows, SamModel.mtImage.Cols }, score);
                            int dataSetId = sds.UniqueID;
                            SamModel.DataSet.Add(sds);
                            DataSetPayload payload = new DataSetPayload(OperationEnum.Edit, dataSetId);
                            PageAction?.Invoke(new PageMessage(3, thisPage, payload));
                        }
                    }
                }
            }
            Lines.Clear();
            IsLinesPopulated = false;
        }

        private void UpdateDataSetFromCurrentSamDetection()
        {
            int datasetID = SamModel.WorkingDetections[CurrentRoi].DataSetUniqueID;
            if (datasetID > -1)
            {
                int datasetIDindex = SamModel.DataSet.FindIndex(a => a.UniqueID == datasetID);
                if (datasetIDindex >= 0)
                {
                    if (IsDetectionUpdateWaiting) //  need to update Mask
                    {
                        OpenSafeListOfInt safeList = OpenIP.BinaryMatToListOfInt(SamModel.WorkingDetections[CurrentRoi].mtMask);
                        if (safeList.IsSuccess)
                        {
                            SamModel.DataSet[datasetIDindex].MaskRle = safeList.ListOfInt;
                        }
                        SamModel.DataSet[datasetIDindex].Rect = SamModel.WorkingDetections[CurrentRoi].Rect;
                    }
                    SamModel.DataSet[datasetIDindex].Label = MainLabel;
                    SamModel.DataSet[datasetIDindex].SubLabel = SubLabel;
                    IsShowUpdateLabelButton = false;
                }
            }
        }

        private void ViewOverlay()
        {
            if (RBFullImageOrBoxState == (int)RBFullImageOrBox.Box) return;
            if (SamModel.mtImage.Rows == 0) return;
            if (SamModel.mtOutlineMask.Rows == 0) return;

            Mat mtDisplay = SamModel.mtImage.Clone();
            Mat mask3Channel = new Mat();
            Mat mtHardToSee = SamModel.mtOutlineMask.Clone();
            int imageSize = mtHardToSee.Rows * mtHardToSee.Cols;
            if (imageSize > 600000)
            {
                int growth = 1;
                if (imageSize > 3000000) growth = 2;
                if (imageSize > 8000000) growth = 3;
                if (imageSize > 16000000) growth = 4;
                CvInvoke.Dilate(mtHardToSee, mtHardToSee, null, new System.Drawing.Point(1, 1), growth, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
            }
            CvInvoke.CvtColor(mtHardToSee, mask3Channel, Emgu.CV.CvEnum.ColorConversion.Gray2Bgr);
            CvInvoke.BitwiseOr(mtDisplay, mask3Channel, mtDisplay);
            BS = ImageSupportWPF.ToBitmapSource(mtDisplay);
            if (mask3Channel != null) mask3Channel.Dispose();
            if (mtDisplay != null) mtDisplay.Dispose();
            if (mtHardToSee != null) mtHardToSee!.Dispose();
        }
        private void ViewMultiMaskOverlay(Mat mtMaskSource)
        {
            if (SamModel.mtImage.Rows == 0) return;
            Mat mtDisplay = SamModel.mtImage.Clone();
            Mat mask3Channel = new Mat();
            Mat mtMask = new Mat();            
            CvInvoke.Erode(mtMaskSource, mtMask, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
            CvInvoke.BitwiseXor(mtMask, mtMaskSource, mtMask);
            Mat mtHardToSee = mtMask.Clone();
            int imageSize = mtHardToSee.Rows * mtHardToSee.Cols;
            if (imageSize > 600000)
            {
                int growth = 1;
                if (imageSize > 3000000) growth = 2;
                if (imageSize > 8000000) growth = 3;
                if (imageSize > 16000000) growth = 4;
                CvInvoke.Dilate(mtHardToSee, mtHardToSee, null, new System.Drawing.Point(1, 1), growth, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
            }
            CvInvoke.CvtColor(mtHardToSee, mask3Channel, Emgu.CV.CvEnum.ColorConversion.Gray2Bgr);
            CvInvoke.BitwiseOr(mtDisplay, mask3Channel, mtDisplay);
            BS = ImageSupportWPF.ToBitmapSource(mtDisplay);
            if (mask3Channel != null) mask3Channel.Dispose();
            if (mtDisplay != null) mtDisplay.Dispose();
            if (mtMask != null) mtMask.Dispose();
            if (mtHardToSee != null) mtHardToSee.Dispose();
        }

        #endregion

        #region Timers

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (UpdateTimer.Tag is not null)
            {
                if (UpdateTimer.Tag.ToString() == "CombineMasks")
                {
                    UpdateTimer.Tag = "";
                    UpdateTimer.Stop();
                    CombineMasks();
                    if (SamModel.SEnum == SamEnum.Sam2MultiMask)
                    {
                        BS = ImageSupportWPF.ToBitmapSource(SamModel.mtImage);
                        IsSamMultiMask = true;
                        if (SamModel.WorkingDetections.Count() > 0) DetectionAScore = SamModel.WorkingDetections[0].Score;
                        if (SamModel.WorkingDetections.Count() > 1) DetectionBScore = SamModel.WorkingDetections[1].Score;
                        if (SamModel.WorkingDetections.Count() > 2) DetectionCScore = SamModel.WorkingDetections[2].Score;
                    }
                    else
                    {

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

                else if (uimess.Code == UiEnum.SamDetection)
                {
                    if (uimess.Ob is WorkingDetection)
                    {
                        WorkingDetection sam2d = (WorkingDetection)uimess.Ob;
                        SamModel.WorkingDetections.Add(sam2d);
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

    public class LineSegment : ObservableObject
    {
        public bool IsLeftMouse { get; set; }
        public int Index { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public Brush Color { get; set; }


        public LineSegment(bool ilm, int ind, double ax, double ay, double bx, double by)
        {
            IsLeftMouse = ilm;
            Index = ind;
            X1 = ax;
            Y1 = ay;
            X2 = bx;
            Y2 = by;
            if (IsLeftMouse)
            {
                Color = Brushes.LimeGreen;
            }
            else
            {
                Color = Brushes.Red;
            }
        }
    }

    public enum RBFullImageOrBox
    {
        NotAssigned = 0,
        FullImage = 1,
        Box = 2
    }

    public enum P2Tabs
    {
        Review = 1,
        Edit = 2,
        DataSet = 4,
        Setup = 16,
        Console = 32
    }

    public enum MultiMaskEnum
    {
        One = 1,
        Two = 2,
        Three = 4
    }

}
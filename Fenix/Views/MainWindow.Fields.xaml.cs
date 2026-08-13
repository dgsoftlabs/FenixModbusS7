using AvalonDock.Layout;
using Fenix.ViewModels;
using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Fenix
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel = new MainWindowViewModel();
        public Boolean mFile { get => _viewModel.mFile; set => _viewModel.mFile = value; }
        public Boolean mNew { get => _viewModel.mNew; set => _viewModel.mNew = value; }
        public Boolean mOpen { get => _viewModel.mOpen; set => _viewModel.mOpen = value; }
        public Boolean mAdd { get => _viewModel.mAdd; set => _viewModel.mAdd = value; }
        public Boolean mConnection { get => _viewModel.mConnection; set => _viewModel.mConnection = value; }
        public Boolean mDevice { get => _viewModel.mDevice; set => _viewModel.mDevice = value; }
        public Boolean mTag { get => _viewModel.mTag; set => _viewModel.mTag = value; }
        public Boolean mIntTag { get => _viewModel.mIntTag; set => _viewModel.mIntTag = value; }
        public Boolean mScriptFile { get => _viewModel.mScriptFile; set => _viewModel.mScriptFile = value; }
        public Boolean mFolder { get => _viewModel.mFolder; set => _viewModel.mFolder = value; }
        public Boolean mInFile { get => _viewModel.mInFile; set => _viewModel.mInFile = value; }
        public Boolean mClosePr { get => _viewModel.mClosePr; set => _viewModel.mClosePr = value; }
        public Boolean mSave { get => _viewModel.mSave; set => _viewModel.mSave = value; }
        public Boolean mSaveAs { get => _viewModel.mSaveAs; set => _viewModel.mSaveAs = value; }
        public Boolean mExit { get => _viewModel.mExit; set => _viewModel.mExit = value; }

        public Boolean mEdit { get => _viewModel.mEdit; set => _viewModel.mEdit = value; }
        public Boolean mCut { get => _viewModel.mCut; set => _viewModel.mCut = value; }
        public Boolean mCopy { get => _viewModel.mCopy; set => _viewModel.mCopy = value; }
        public Boolean mPaste { get => _viewModel.mPaste; set => _viewModel.mPaste = value; }
        public Boolean mDelete { get => _viewModel.mDelete; set => _viewModel.mDelete = value; }

        public Boolean mView { get => _viewModel.mView; set => _viewModel.mView = value; }
        public Boolean mSolution { get => _viewModel.mSolution; set => _viewModel.mSolution = value; }
        public Boolean mProperties { get => _viewModel.mProperties; set => _viewModel.mProperties = value; }
        public Boolean mOutput { get => _viewModel.mOutput; set => _viewModel.mOutput = value; }
        public Boolean mTable { get => _viewModel.mTable; set => _viewModel.mTable = value; }
        public Boolean mChart { get => _viewModel.mChart; set => _viewModel.mChart = value; }
        public Boolean mCommView { get => _viewModel.mCommView; set => _viewModel.mCommView = value; }
        public Boolean mEditor { get => _viewModel.mEditor; set => _viewModel.mEditor = value; }

        public Boolean mDriversSt { get => _viewModel.mDriversSt; set => _viewModel.mDriversSt = value; }
        public Boolean mStart { get => _viewModel.mStart; set => _viewModel.mStart = value; }
        public Boolean mStop { get => _viewModel.mStop; set => _viewModel.mStop = value; }
        public Boolean mStartAll { get => _viewModel.mStartAll; set => _viewModel.mStartAll = value; }
        public Boolean mStopAll { get => _viewModel.mStopAll; set => _viewModel.mStopAll = value; }

        public Boolean mTools { get => _viewModel.mTools; set => _viewModel.mTools = value; }
        public Boolean mBlock { get => _viewModel.mBlock; set => _viewModel.mBlock = value; }
        public Boolean mUnBlock { get => _viewModel.mUnBlock; set => _viewModel.mUnBlock = value; }
        public Boolean mShowLoc { get => _viewModel.mShowLoc; set => _viewModel.mShowLoc = value; }
        public Boolean mDrivers { get => _viewModel.mDrivers; set => _viewModel.mDrivers = value; }

        public Boolean mDatabase { get => _viewModel.mDatabase; set => _viewModel.mDatabase = value; }
        public Boolean mDbShowFile { get => _viewModel.mDbShowFile; set => _viewModel.mDbShowFile = value; }
        public Boolean mDbReset { get => _viewModel.mDbReset; set => _viewModel.mDbReset = value; }
        public Boolean mShowDb { get => _viewModel.mShowDb; set => _viewModel.mShowDb = value; }
        public Boolean mShowTrendDb { get => _viewModel.mShowTrendDb; set => _viewModel.mShowTrendDb = value; }
        public Boolean mSaveCSV { get => _viewModel.mSaveCSV; set => _viewModel.mSaveCSV = value; }

        public Boolean mHelp { get => _viewModel.mHelp; set => _viewModel.mHelp = value; }
        public Boolean mUpdates { get => _viewModel.mUpdates; set => _viewModel.mUpdates = value; }
        public Boolean mAbout { get => _viewModel.mAbout; set => _viewModel.mAbout = value; }
        public Boolean mViewHelp { get => _viewModel.mViewHelp; set => _viewModel.mViewHelp = value; }

        private ProjectContainer PrCon = new ProjectContainer();
        private ElementKind actualKindElement;

        private object SelObj;
        private Guid SelGuid;
        private TimersFolder _selectedTimersFolder;

        private Project Pr;
        private string pathRun = "";

        private string SelSrcPath = string.Empty;

        private LayoutAnchorable laPropGrid = new LayoutAnchorable();
        private PropertiesView propManag = new PropertiesView();

        private LayoutAnchorable laTvMain = new LayoutAnchorable();
        private SolutionExplorer tvMain = new SolutionExplorer();

        private LayoutAnchorGroup laGrOutput = new LayoutAnchorGroup();
        private LayoutAnchorable laOutput = new LayoutAnchorable();
        private OutputView frOutput;

        private ObservableCollection<CustomException> exList = new ObservableCollection<CustomException>();
    }
}
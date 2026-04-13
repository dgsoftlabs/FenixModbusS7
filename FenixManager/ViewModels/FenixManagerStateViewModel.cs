namespace FenixWPF.ViewModels
{
    public class FenixManagerStateViewModel : ViewModelBase
    {
        private bool _mFile;
        private bool _mNew;
        private bool _mOpen;
        private bool _mAdd;
        private bool _mConnection;
        private bool _mDevice;
        private bool _mTag;
        private bool _mIntTag;
        private bool _mScriptFile;
        private bool _mFolder;
        private bool _mInFile;
        private bool _mClosePr;
        private bool _mSave;
        private bool _mSaveAs;
        private bool _mExit;

        private bool _mEdit;
        private bool _mCut;
        private bool _mCopy;
        private bool _mPaste;
        private bool _mDelete;

        private bool _mView;
        private bool _mSolution;
        private bool _mProperties;
        private bool _mOutput;
        private bool _mTable;
        private bool _mChart;
        private bool _mCommView;
        private bool _mEditor;

        private bool _mDriversSt;
        private bool _mStart;
        private bool _mStop;
        private bool _mStartAll;
        private bool _mStopAll;

        private bool _mTools;
        private bool _mBlock;
        private bool _mUnBlock;
        private bool _mSimulate;
        private bool _mShowLoc;
        private bool _mDrivers;

        private bool _mDatabase;
        private bool _mDbShowFile;
        private bool _mDbReset;
        private bool _mShowDb;
        private bool _mShowTrendDb;
        private bool _mSaveCSV;

        private bool _mHelp;
        private bool _mUpdates;
        private bool _mAbout;
        private bool _mViewHelp;

        private void SetField(ref bool field, bool value, string name)
        {
            SetProperty(ref field, value, name);
        }

        public bool mFile { get => _mFile; set => SetField(ref _mFile, value, nameof(mFile)); }
        public bool mNew { get => _mNew; set => SetField(ref _mNew, value, nameof(mNew)); }
        public bool mOpen { get => _mOpen; set => SetField(ref _mOpen, value, nameof(mOpen)); }
        public bool mAdd { get => _mAdd; set => SetField(ref _mAdd, value, nameof(mAdd)); }
        public bool mConnection { get => _mConnection; set => SetField(ref _mConnection, value, nameof(mConnection)); }
        public bool mDevice { get => _mDevice; set => SetField(ref _mDevice, value, nameof(mDevice)); }
        public bool mTag { get => _mTag; set => SetField(ref _mTag, value, nameof(mTag)); }
        public bool mIntTag { get => _mIntTag; set => SetField(ref _mIntTag, value, nameof(mIntTag)); }
        public bool mScriptFile { get => _mScriptFile; set => SetField(ref _mScriptFile, value, nameof(mScriptFile)); }
        public bool mFolder { get => _mFolder; set => SetField(ref _mFolder, value, nameof(mFolder)); }
        public bool mInFile { get => _mInFile; set => SetField(ref _mInFile, value, nameof(mInFile)); }
        public bool mClosePr { get => _mClosePr; set => SetField(ref _mClosePr, value, nameof(mClosePr)); }
        public bool mSave { get => _mSave; set => SetField(ref _mSave, value, nameof(mSave)); }
        public bool mSaveAs { get => _mSaveAs; set => SetField(ref _mSaveAs, value, nameof(mSaveAs)); }
        public bool mExit { get => _mExit; set => SetField(ref _mExit, value, nameof(mExit)); }

        public bool mEdit { get => _mEdit; set => SetField(ref _mEdit, value, nameof(mEdit)); }
        public bool mCut { get => _mCut; set => SetField(ref _mCut, value, nameof(mCut)); }
        public bool mCopy { get => _mCopy; set => SetField(ref _mCopy, value, nameof(mCopy)); }
        public bool mPaste { get => _mPaste; set => SetField(ref _mPaste, value, nameof(mPaste)); }
        public bool mDelete { get => _mDelete; set => SetField(ref _mDelete, value, nameof(mDelete)); }

        public bool mView { get => _mView; set => SetField(ref _mView, value, nameof(mView)); }
        public bool mSolution { get => _mSolution; set => SetField(ref _mSolution, value, nameof(mSolution)); }
        public bool mProperties { get => _mProperties; set => SetField(ref _mProperties, value, nameof(mProperties)); }
        public bool mOutput { get => _mOutput; set => SetField(ref _mOutput, value, nameof(mOutput)); }
        public bool mTable { get => _mTable; set => SetField(ref _mTable, value, nameof(mTable)); }
        public bool mChart { get => _mChart; set => SetField(ref _mChart, value, nameof(mChart)); }
        public bool mCommView { get => _mCommView; set => SetField(ref _mCommView, value, nameof(mCommView)); }
        public bool mEditor { get => _mEditor; set => SetField(ref _mEditor, value, nameof(mEditor)); }

        public bool mDriversSt { get => _mDriversSt; set => SetField(ref _mDriversSt, value, nameof(mDriversSt)); }
        public bool mStart { get => _mStart; set => SetField(ref _mStart, value, nameof(mStart)); }
        public bool mStop { get => _mStop; set => SetField(ref _mStop, value, nameof(mStop)); }
        public bool mStartAll { get => _mStartAll; set => SetField(ref _mStartAll, value, nameof(mStartAll)); }
        public bool mStopAll { get => _mStopAll; set => SetField(ref _mStopAll, value, nameof(mStopAll)); }

        public bool mTools { get => _mTools; set => SetField(ref _mTools, value, nameof(mTools)); }
        public bool mBlock { get => _mBlock; set => SetField(ref _mBlock, value, nameof(mBlock)); }
        public bool mUnBlock { get => _mUnBlock; set => SetField(ref _mUnBlock, value, nameof(mUnBlock)); }
        public bool mSimulate { get => _mSimulate; set => SetField(ref _mSimulate, value, nameof(mSimulate)); }
        public bool mShowLoc { get => _mShowLoc; set => SetField(ref _mShowLoc, value, nameof(mShowLoc)); }
        public bool mDrivers { get => _mDrivers; set => SetField(ref _mDrivers, value, nameof(mDrivers)); }

        public bool mDatabase { get => _mDatabase; set => SetField(ref _mDatabase, value, nameof(mDatabase)); }
        public bool mDbShowFile { get => _mDbShowFile; set => SetField(ref _mDbShowFile, value, nameof(mDbShowFile)); }
        public bool mDbReset { get => _mDbReset; set => SetField(ref _mDbReset, value, nameof(mDbReset)); }
        public bool mShowDb { get => _mShowDb; set => SetField(ref _mShowDb, value, nameof(mShowDb)); }
        public bool mShowTrendDb { get => _mShowTrendDb; set => SetField(ref _mShowTrendDb, value, nameof(mShowTrendDb)); }
        public bool mSaveCSV { get => _mSaveCSV; set => SetField(ref _mSaveCSV, value, nameof(mSaveCSV)); }

        public bool mHelp { get => _mHelp; set => SetField(ref _mHelp, value, nameof(mHelp)); }
        public bool mUpdates { get => _mUpdates; set => SetField(ref _mUpdates, value, nameof(mUpdates)); }
        public bool mAbout { get => _mAbout; set => SetField(ref _mAbout, value, nameof(mAbout)); }
        public bool mViewHelp { get => _mViewHelp; set => SetField(ref _mViewHelp, value, nameof(mViewHelp)); }
    }
}

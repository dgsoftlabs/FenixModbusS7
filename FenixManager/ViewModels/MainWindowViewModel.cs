using ProjectDataLib;

namespace FenixWPF.ViewModels
{
    public class MainWindowViewModel : FenixManagerStateViewModel
    {
        public bool CheckAccessForNodes(object selectedItem, object selectedObject, ElementKind srcType, bool anyCommunication, bool currentPropertiesEnabled)
        {
            bool propertiesEnabled = currentPropertiesEnabled;

            if (selectedItem == null)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = false;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = false;
                mInFile = false;
                mClosePr = false;
                mSave = false;
                mSaveAs = false;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = false;
                mDelete = false;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = false;

                mDriversSt = false;
                mStart = false;
                mStop = false;
                mStartAll = false;
                mStopAll = false;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = false;
                mDbShowFile = false;
                mDbReset = false;
                mShowDb = false;
                mShowTrendDb = false;
                mSaveCSV = false;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;
            }

            if (selectedItem is Project)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = true;
                mConnection = true;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = false;
                mInFile = false;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = srcType == ElementKind.Connection ? true : false;
                mDelete = false;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = true;
                mChart = true;
                mCommView = true;
                mEditor = false;

                mDriversSt = true;
                mStart = false;
                mStop = false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = true;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;
            }

            if (selectedItem is InternalTagsDriver)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = true;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = true;
                mScriptFile = false;
                mFolder = false;
                mInFile = false;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = false;
                mDelete = false;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = false;

                mDriversSt = true;
                mStart = (!((ITreeViewModel)selectedObject).IsBlocked && !((ITreeViewModel)selectedObject).IsLive) ? true : false;
                mStop = (!((ITreeViewModel)selectedObject).IsBlocked && ((ITreeViewModel)selectedObject).IsLive) ? true : false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = !((ITreeViewModel)selectedObject).IsBlocked;
                mUnBlock = ((ITreeViewModel)selectedObject).IsBlocked;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (selectedItem is ScriptsDriver)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = true;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = true;
                mFolder = false;
                mInFile = true;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = false;
                mDelete = false;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = false;

                mDriversSt = true;
                mStart = (!((ITreeViewModel)selectedObject).IsBlocked && !((ITreeViewModel)selectedObject).IsLive) ? true : false;
                mStop = (!((ITreeViewModel)selectedObject).IsBlocked && ((ITreeViewModel)selectedObject).IsLive) ? true : false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = !((ITreeViewModel)selectedObject).IsBlocked;
                mUnBlock = ((ITreeViewModel)selectedObject).IsBlocked;
                mShowLoc = true;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (selectedItem is CusFile)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = !((CusFile)selectedItem).IsFile;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = !((CusFile)selectedItem).IsFile;
                mInFile = !((CusFile)selectedItem).IsFile;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = ((CusFile)selectedItem).IsFile;
                mCopy = ((CusFile)selectedItem).IsFile;
                mPaste = !((CusFile)selectedItem).IsFile && srcType == ElementKind.InFile;
                mDelete = true;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = ((CusFile)selectedItem).IsFile;

                mDriversSt = true;
                mStart = false;
                mStop = false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = !((CusFile)selectedItem).IsFile;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (selectedItem is ScriptFile)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = false;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = false;
                mInFile = false;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = false;
                mDelete = true;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = true;

                mDriversSt = true;
                mStart = false;
                mStop = false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = !((ITreeViewModel)selectedObject).IsBlocked;
                mUnBlock = ((ITreeViewModel)selectedObject).IsBlocked;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (selectedItem is InTag)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = false;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = false;
                mInFile = false;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = false;
                mDelete = true;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = false;

                mDriversSt = true;
                mStart = false;
                mStop = false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (selectedItem is DatabaseModel)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = false;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = false;
                mInFile = false;
                mClosePr = false;
                mSave = false;
                mSaveAs = false;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = false;
                mDelete = false;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = false;

                mDriversSt = false;
                mStart = false;
                mStop = false;
                mStartAll = false;
                mStopAll = false;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;
            }

            if (selectedItem is Connection)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = true;
                mConnection = false;
                mDevice = true;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mInFile = false;
                mFolder = false;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = true;
                mCopy = true;
                mPaste = srcType == ElementKind.Device ? true : false;
                mDelete = true;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = true;
                mChart = true;
                mCommView = true;
                mEditor = false;

                mDriversSt = true;
                mStart = (!((ITreeViewModel)selectedObject).IsBlocked && !((ITreeViewModel)selectedObject).IsLive) ? true : false;
                mStop = (!((ITreeViewModel)selectedObject).IsBlocked && ((ITreeViewModel)selectedObject).IsLive) ? true : false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = !((ITreeViewModel)selectedObject).IsBlocked;
                mUnBlock = ((ITreeViewModel)selectedObject).IsBlocked;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (selectedItem is WebServer)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = true;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = true;
                mInFile = true;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = srcType == ElementKind.InFile;
                mDelete = false;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = false;

                mDriversSt = true;
                mStart = false;
                mStop = false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = true;
                mSimulate = true;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;
            }

            if (selectedItem is Device)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = true;
                mConnection = false;
                mDevice = false;
                mTag = true;
                mScriptFile = false;
                mFolder = false;
                mIntTag = false;
                mInFile = false;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = true;
                mCopy = true;
                mPaste = srcType == ElementKind.Tag ? true : false;
                mDelete = true;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = true;
                mChart = true;
                mCommView = true;
                mEditor = false;

                mDriversSt = true;
                mStart = false;
                mStop = false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (selectedItem is Tag)
            {
                mFile = true;
                mNew = true;
                mOpen = true;
                mAdd = false;
                mConnection = false;
                mDevice = false;
                mTag = false;
                mIntTag = false;
                mScriptFile = false;
                mFolder = false;
                mInFile = false;
                mClosePr = true;
                mSave = true;
                mSaveAs = true;
                mExit = true;

                mEdit = false;
                mCut = true;
                mCopy = true;
                mPaste = false;
                mDelete = true;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;
                mTable = false;
                mChart = false;
                mCommView = false;
                mEditor = false;

                mDriversSt = true;
                mStart = false;
                mStop = false;
                mStartAll = true;
                mStopAll = true;

                mTools = true;
                mBlock = false;
                mUnBlock = false;
                mShowLoc = false;
                mSimulate = false;
                mDrivers = true;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = true;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = true;
                mViewHelp = true;

                propertiesEnabled = !((ITreeViewModel)selectedObject).IsLive;
            }

            if (anyCommunication)
            {
                if (selectedObject == null)
                    return propertiesEnabled;

                mFile = true;
                mNew = false;
                mOpen = false;
                mAdd = true;
                mConnection = !((ITreeViewModel)selectedObject).IsLive;
                mDevice = !((ITreeViewModel)selectedObject).IsLive;
                mTag = !((ITreeViewModel)selectedObject).IsLive;
                mIntTag = !((ITreeViewModel)selectedObject).IsLive;
                mScriptFile = false;
                mFolder = true;
                mInFile = !((ITreeViewModel)selectedObject).IsLive;
                mClosePr = false;
                mSave = true;
                mSaveAs = false;
                mExit = true;

                mEdit = false;
                mCut = false;
                mCopy = false;
                mPaste = false;
                mDelete = false;

                mView = true;
                mSolution = true;
                mProperties = true;
                mOutput = true;

                mBlock = false;
                mUnBlock = false;
                mDrivers = false;

                mDatabase = true;
                mDbShowFile = true;
                mDbReset = false;
                mShowDb = true;
                mShowTrendDb = true;
                mSaveCSV = true;

                mHelp = true;
                mAbout = true;
                mUpdates = false;
                mViewHelp = true;
            }

            return propertiesEnabled;
        }
    }
}

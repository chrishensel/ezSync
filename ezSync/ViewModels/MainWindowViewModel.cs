using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace ezSync.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private string _source;
        private string _destination;
        private List<string> _extensions;
        private double _progress;

        private bool _isStarted;
        private Thread _workerThread;

        #endregion

        #region Properties

        public string Source
        {
            get { return _source; }
            set
            {
                if (value != _source)
                {
                    _source = value;
                    OnPropertyChanged("Source");

                    Properties.Settings.Default.MruSourceDir = value;
                }
            }
        }

        public string Destination
        {
            get { return _destination; }
            set
            {
                if (value != _destination)
                {
                    _destination = value;
                    OnPropertyChanged("Destination");

                    Properties.Settings.Default.MruDestinationDir = value;
                }
            }
        }

        public bool IsStarted
        {
            get { return _isStarted; }
            private set
            {
                if (value != _isStarted)
                {
                    _isStarted = value;
                    OnPropertyChangedDispatched("IsStarted");
                }
            }
        }

        public double Progress
        {
            get { return _progress; }
            set
            {
                if (value != _progress)
                {
                    _progress = value;
                    OnPropertyChangedDispatched("Progress");
                }
            }
        }

        private string _progressText;

        public string ProgressText
        {
            get { return _progressText; }
            private set
            {
                if (value != _progressText)
                {
                    _progressText = value;
                    OnPropertyChangedDispatched("ProgressText");
                }
            }
        }

        public string ExtensionMask
        {
            get { return string.Join(" ", _extensions); }
            set
            {
                _extensions.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _extensions.AddRange(value.Split(' '));
                }

                Properties.Settings.Default.CopyExtensionMask = value;
            }
        }

        #endregion

        #region Commands

        #region Command "StartProcessCommand"

        /// <summary>
        /// The StartProcessCommand command.
        /// </summary>
        public ICommand StartProcessCommand { get; private set; }

        private bool StartProcessCommand_CanExecute(object parameter)
        {
            return !string.IsNullOrWhiteSpace(_source) && !string.IsNullOrWhiteSpace(_destination) && !_isStarted;
        }

        private void StartProcessCommand_Execute(object parameter)
        {
            if (IsStarted)
            {
                return;
            }

            IsStarted = true;

            _workerThread = new Thread(WorkerThreadMethod);
            _workerThread.Priority = ThreadPriority.BelowNormal;
            _workerThread.IsBackground = true;
            _workerThread.Start();
        }

        #endregion

        #endregion

        #region Constructors

        public MainWindowViewModel()
        {
            _extensions = new List<string>();

            _source = Properties.Settings.Default.MruSourceDir;
            _destination = Properties.Settings.Default.MruDestinationDir;
            ExtensionMask = Properties.Settings.Default.CopyExtensionMask;

            this.StartProcessCommand = new RelayCommand(StartProcessCommand_Execute, StartProcessCommand_CanExecute);
        }

        #endregion

        #region Methods

        private void WorkerThreadMethod()
        {
            ProgressText = "Checking directories...";

            DirectoryInfo dirSource = new DirectoryInfo(_source);
            if (!dirSource.Exists)
            {
                ProgressText = "Source directory does not exist!";
                IsStarted = false;
                return;
            }

            DirectoryInfo dirDestination = new DirectoryInfo(_destination);
            if (!dirDestination.Exists)
            {
                dirDestination.Create();
                dirDestination.Refresh();
            }

            Progress = 0d;

            ProgressText = "Retrieving files from source directory (this may take a while)...";

            FileInfo[] sourceFiles = dirSource.GetFiles("*.*", SearchOption.AllDirectories);

            ProgressText = string.Format("Retrieved {0} file(s) for copying.", sourceFiles.Length);
            Thread.Sleep(500);

            int i = 0;
            int total = sourceFiles.Length + 1;
            foreach (FileInfo sourceFile in sourceFiles)
            {
                i++;
                Progress = ((double)i * 100d) / (double)total;

                if (!IsExtensionOk(sourceFile))
                {
                    continue;
                }

                try
                {
                    string srcFileAbs = sourceFile.FullName;
                    string destFilePathRel = sourceFile.FullName.Replace(_source, "").Remove(0, 1);
                    string destFilePathAbs = Path.Combine(_destination, destFilePathRel);

                    string destFileDirAbs = Path.GetDirectoryName(destFilePathAbs);
                    if (!Directory.Exists(destFileDirAbs))
                    {
                        Directory.CreateDirectory(destFileDirAbs);
                    }

                    ProgressText = string.Format("File: {0}", srcFileAbs);

                    FileInfo destFile = new FileInfo(destFilePathAbs);
                    if (destFile.Exists)
                    {
                        if (sourceFile.Length == destFile.Length &&
                            sourceFile.LastWriteTimeUtc == destFile.LastWriteTimeUtc)
                        {
                            continue;
                        }
                    }

                    File.Copy(srcFileAbs, destFilePathAbs, true);
                }
                catch (Exception ex)
                {
                    ProgressText = string.Format("Error while copying: {0}", ex.Message);
                    Thread.Sleep(125);
                }
            }

            IsStarted = false;
        }

        private bool IsExtensionOk(FileInfo file)
        {
            string ext = file.Extension;
            if (ext == null)
            {
                ext = "";
            }

            if (ext.StartsWith("."))
            {
                ext = ext.Remove(0, 1);
            }

            if (_extensions.Count == 0)
            {
                return true;
            }

            return _extensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase);
        }

        #endregion
    }
}

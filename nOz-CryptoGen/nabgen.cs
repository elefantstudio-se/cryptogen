using System;
using System.Collections.Generic; 
using System.ComponentModel; 
using System.Data;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace nOz_CryptoGen
{
    public partial class nabgen : Form
    {
        public static ListBoxLog listBoxLog;
        public nabgen()
        {
            InitializeComponent();
            
            listBoxLog = new ListBoxLog(listBox1);
            Thread thread = new Thread(LogStuffThread);
            thread.IsBackground = true;
            thread.Start();
        }

        private void nabgen_Load(object sender, EventArgs e)
        {
            XDocument doc;
            string filePath = "nabkeys.xml";

            if (!File.Exists(filePath))
            {
                doc = new XDocument(new XElement("nOz-nabkeys"));
                doc.Save(filePath);
                MessageBox.Show("Created file!", "No File!", MessageBoxButtons.OK);
                listBoxLog.Log(Level.Warning, "nabkeys.xml created - no file found -SUCCESS");

            }
            else
            {
                
                XElement element = XElement.Load("nabkeys.xml");
                foreach (XElement item in element.Elements("KEYS"))
                {
                    comboBox1.Items.Add(item.Value);
                    listBoxLog.Log(Level.Debug, "-nabkeys.xml loaded OK on LOAD -SUCCESS");
                }
                btnValidate.Enabled = true; // Möjliggör knapp för att söka i fil efter nycklar.
                // Start the BackgroundWorker.
                bgFlow.WorkerSupportsCancellation = true;
                bgFlow.WorkerReportsProgress = true;
                label5.Text = "v*";
                btnLogsave.Enabled = false;
            }
        }

        private void btnGen_Click(object sender, EventArgs e)
        {

            Debug.Assert(true);
           
            //string GetSerialNumber();
            Guid serialGuid = Guid.NewGuid();
            string uniqueSerial = serialGuid.ToString("N");
            string uniqueSerialLength = uniqueSerial.Substring(0, 28).ToUpper();
            char[] serialArray = uniqueSerialLength.ToCharArray();
            string finalSerialNumber = "";

            int j = 0;
            for (int i = 0; i < 28; i++)
            {
                for (j = i; j < 4 + i; j++)
                {
                    finalSerialNumber += serialArray[j];
                }
                if (j == 28)
                {
                    break;
                }
                else
                {
                    i = (j) - 1;
                    finalSerialNumber += "-";

                }
            }

            comboBox1.Text = finalSerialNumber;
            comboBox1.Items.Add(finalSerialNumber);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            XElement element = new XElement("KEYS");
            foreach (Object item in comboBox1.Items)
            {
                XElement el = new XElement("KEYS", item.ToString());
                element.Add(el);
            }
            element.Save("nabkeys.xml");
            comboBox1.Items.Clear(); // rensar box efter lyckat sparande.
            comboBox1.Text = ">>> All Keys saved! <<<";
            listBoxLog.Log(Level.Debug, "-KEYS saved to FILE -SUCCESS!");

            XElement.Load("nabkeys.xml");
            foreach (XElement item in element.Elements("KEYS"))
            {
                comboBox1.Items.Add(item.Value);
                listBoxLog.Log(Level.Debug, "-New KEYS Reloaded into ComboBox -SUCCESS!");
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {

            //Validera Nycklarna för att vara UNIQUE
            StreamReader s = new StreamReader("nabkeys.xml"); //öppnar filen
            string currentLine;
            string searchString = comboBox1.Text; // tar värdet från combobox
            bool foundkey = false;

            do
            {
                currentLine = s.ReadLine();
                if (currentLine != null)
                {
                    foundkey = currentLine.Contains(searchString);
                }
            }
            while (currentLine != null && !foundkey);

            if (foundkey)
            {
                MessageBox.Show("KEY >>> " + searchString + " <<< IS VALID!", "Key Validation:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                listBoxLog.Log(Level.Error, "-KEY was Found in file(search)=VALID-\n");
                s.ReadToEnd();
            }
            else
                MessageBox.Show("KEY >>> " + searchString + " <<< NOT VALID!", "Key Validation:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                listBoxLog.Log(Level.Error, "-KEY was not Found in file(search)-\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Skriva till local DB eller Extern.
            MessageBox.Show("Not implented yet..", "Write to DB", MessageBoxButtons.OK);
        }

        private void bgFlow_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            for (int i = 1; i <= 10; i++)
            {
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    // Perform a time consuming operation and report progress.
                    System.Threading.Thread.Sleep(10000);
                    worker.ReportProgress((i * 10));
                }

            }
        }

        private void bgFlow_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            listBoxLog.Log(Level.Debug, "-Backgroundworker loaded");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {

        }

        private void bntAsm_Click(object sender, System.EventArgs e)
        {

            // Stores a key pair in the key container.
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly == true)
                txtBoxkey.Text = "Key: " + cspp.KeyContainerName + " - Public key";
            else
                txtBoxkey.Text = "Key: " + cspp.KeyContainerName + " - FullKey";

        }

        private void btnDec_Click(object sender, EventArgs e)
        {
            if (rsa == null)
                MessageBox.Show("Set a key first!");
            else
            {
                // Öppnar dialog för att välja fil!
                openFileDialog2.InitialDirectory = EncrFolder;
                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    string fName = openFileDialog2.FileName;
                    if (fName != null)
                    {
                        FileInfo fi = new FileInfo(fName);
                        string name = fi.Name;
                        DecryptFile(name);
                        listBoxLog.Log(Level.Debug, "-Decryption Algorithm runned.. SUCCESS!-");
                    }
                }
            }
        }

        private void toolFlowbar_Click(object sender, EventArgs e)
        {

        }

        private void btnGen_Click_1(object sender, EventArgs e)
        {

            //string GetSerialNumber();
            Guid serialGuid = Guid.NewGuid();
            string uniqueSerial = serialGuid.ToString("N");
            string uniqueSerialLength = uniqueSerial.Substring(0, 28).ToUpper();
            char[] serialArray = uniqueSerialLength.ToCharArray();
            string finalSerialNumber = "";

            int j = 0;
            for (int i = 0; i < 28; i++)
            {
                for (j = i; j < 4 + i; j++)
                {
                    finalSerialNumber += serialArray[j];
                }
                if (j == 28)
                {
                    break;
                }
                else
                {
                    i = (j) - 1;
                    finalSerialNumber += "-";
                }
            }

            comboBox1.Text = finalSerialNumber;
            comboBox1.Items.Add(finalSerialNumber);
            listBoxLog.Log(Level.Debug, "Generating serial from finalSerialNumber()");
        }


        private void btnExport_Click(object sender, EventArgs e)
        {
            {
                // Save the public key created by the RSA 
                // to a file. Caution, persisting the 
                // key to a file is a security risk.
                Directory.CreateDirectory(EncrFolder);
                StreamWriter sw = new StreamWriter(PubKeyFile, false);
                sw.Write(rsa.ToXmlString(false));
                sw.Close();
                listBoxLog.Log(Level.Debug, "-PubKEY exported -SUCCESS!");
            }
        }

        private void button1_Click_2(object sender, EventArgs e)
        {

            StreamReader sr = new StreamReader(PubKeyFile);
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            string keytxt = sr.ReadToEnd();
            rsa.FromXmlString(keytxt);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly == true)
                txtBoxkey.Text = "Key: " + cspp.KeyContainerName + " - Public Only";
            else
                txtBoxkey.Text = "Key: " + cspp.KeyContainerName + " - Full Key Pair";
            sr.Close();
            listBoxLog.Log(Level.Debug, "Created PubKeyFile");

        }

        private void txtBoxkey_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnEnc_Click(object sender, EventArgs e)
        {

            {
                if (rsa == null)
                    MessageBox.Show("Key not set!");

                else
                {
                    // Öppnar filväljare, välj fil som ska krypteras
                    openFileDialog1.InitialDirectory = SrcFolder;
                    if (openFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        string fName = openFileDialog1.FileName;
                        if (fName != null)
                        {
                            FileInfo fInfo = new FileInfo(fName);
                            // Pass the file name without the path. 
                            string name = fInfo.FullName;
                            EncryptFile(name);
                            txtBoxkey.Text = "->" + EncFile + " <-- is NOW RSA-256bit Encrypted\n";
                            //txtBoxkey.Text = "Starting 3DES Encryption: " + Environment.NewLine + "{code goes here}";
                            listBoxLog.Log(Level.Debug, "-Encryption Algorithm runned.. SUCCESS!");
                            {
                            }
                            {

                            }
                        }
                    }
                }
            }
        }

        private void LogStuffThread()
        {
            int number = 0;
            while (true)
            {
                listBoxLog.Log(Level.Info, "NAB-Logger # {0,0000} :", number++);
                Thread.Sleep(1000);
            }
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            listBoxLog.Paused = !listBoxLog.Paused;
        }

        private void btnLogsave_Click(object sender, EventArgs e)
        {
            if (Sfd1.ShowDialog() == DialogResult.OK)
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(Sfd1.FileName);
                foreach (object item in listBox1.Text)
                    sw.WriteLine(item.ToString());
                sw.Close();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }
    }
    public enum Level : int
    {
        Critical = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4,
        Debug = 5
    };
    public sealed class ListBoxLog : IDisposable
    {
        private const string DEFAULT_MESSAGE_FORMAT = "{4} [{5}] : {8}";
        private const int DEFAULT_MAX_LINES_IN_LISTBOX = 5000;

        private bool _disposed;
        private ListBox _listBox;
        private string _messageFormat;
        private int _maxEntriesInListBox;
        private bool _canAdd;
        private bool _paused;

        private void OnHandleCreated(object sender, EventArgs e)
        {
            _canAdd = true;
        }
        private void OnHandleDestroyed(object sender, EventArgs e)
        {
            _canAdd = false;
        }
        private void DrawItemHandler(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.DrawBackground();
                e.DrawFocusRectangle();

                LogEvent logEvent = ((ListBox)sender).Items[e.Index] as LogEvent;

                // SafeGuard against wrong configuration of list box
                if (logEvent == null)
                {
                    logEvent = new LogEvent(Level.Critical, ((ListBox)sender).Items[e.Index].ToString());
                }

                Color color;
                switch (logEvent.Level)
                {
                    case Level.Critical:
                        color = Color.LimeGreen;
                        break;
                    case Level.Error:
                        color = Color.Red;
                        break;
                    case Level.Warning:
                        color = Color.Goldenrod;
                        break;
                    case Level.Info:
                        color = Color.Orange;
                        break;
                    case Level.Verbose:
                        color = Color.LightBlue;
                        break;
                    default:
                        color = Color.White;
                        break;
                }

                if (logEvent.Level == Level.Critical)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.Red), e.Bounds);
                }
                e.Graphics.DrawString(FormatALogEventMessage(logEvent, _messageFormat), new Font("Lucida Console", 8.25f, FontStyle.Regular), new SolidBrush(color), e.Bounds);
            }
        }
        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers == Keys.Control) && (e.KeyCode == Keys.C))
            {
                CopyToClipboard();
            }
        }
        private void CopyMenuOnClickHandler(object sender, EventArgs e)
        {
            CopyToClipboard();
        }
        private void CopyMenuPopupHandler(object sender, EventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            if (menu != null)
            {
                menu.MenuItems[0].Enabled = (_listBox.SelectedItems.Count > 0);
            }
        }

        private class LogEvent
        {
            public LogEvent(Level level, string message)
            {
                EventTime = DateTime.Now;
                Level = level;
                Message = message;
            }

            public readonly DateTime EventTime;

            public readonly Level Level;
            public readonly string Message;
        }
        private void WriteEvent(LogEvent logEvent)
        {
            if ((logEvent != null) && (_canAdd))
            {
                _listBox.BeginInvoke(new AddALogEntryDelegate(AddALogEntry), logEvent);
            }
        }
        private delegate void AddALogEntryDelegate(object item);
        private void AddALogEntry(object item)
        {
            _listBox.Items.Add(item);

            if (_listBox.Items.Count > _maxEntriesInListBox)
            {
                _listBox.Items.RemoveAt(0);
            }

            if (!_paused) _listBox.TopIndex = _listBox.Items.Count - 1;
        }
        private string LevelName(Level level)
        {
            switch (level)
            {
                case Level.Critical: return "Critical";
                case Level.Error: return "Error";
                case Level.Warning: return "Warning";
                case Level.Info: return "Info";
                case Level.Verbose: return "Verbose";
                case Level.Debug: return "Debug";
                default: return string.Format("<value={0}>", (int)level);
            }
        }
        private string FormatALogEventMessage(LogEvent logEvent, string messageFormat)
        {
            string message = logEvent.Message;
            if (message == null) { message = "<NULL>"; }
            return string.Format(messageFormat,
                /* {0} */ logEvent.EventTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                /* {1} */ logEvent.EventTime.ToString("yyyy-MM-dd HH:mm:ss"),
                /* {2} */ logEvent.EventTime.ToString("yyyy-MM-dd"),
                /* {3} */ logEvent.EventTime.ToString("HH:mm:ss.fff"),
                /* {4} */ logEvent.EventTime.ToString("HH:mm:ss"),

                /* {5} */ LevelName(logEvent.Level)[0],
                /* {6} */ LevelName(logEvent.Level),
                /* {7} */ (int)logEvent.Level,

                /* {8} */ message);
        }
        private void CopyToClipboard()
        {
            if (_listBox.SelectedItems.Count > 0)
            {
                StringBuilder selectedItemsAsRTFText = new StringBuilder();
                selectedItemsAsRTFText.AppendLine(@"{\rtf1\ansi\deff0{\fonttbl{\f0\fcharset0 Courier;}}");
                selectedItemsAsRTFText.AppendLine(@"{\colortbl;\red255\green255\blue255;\red255\green0\blue0;\red218\green165\blue32;\red0\green128\blue0;\red0\green0\blue255;\red0\green0\blue0}");
                foreach (LogEvent logEvent in _listBox.SelectedItems)
                {
                    selectedItemsAsRTFText.AppendFormat(@"{{\f0\fs16\chshdng0\chcbpat{0}\cb{0}\cf{1} ", (logEvent.Level == Level.Critical) ? 2 : 1, (logEvent.Level == Level.Critical) ? 1 : ((int)logEvent.Level > 5) ? 6 : ((int)logEvent.Level) + 1);
                    selectedItemsAsRTFText.Append(FormatALogEventMessage(logEvent, _messageFormat));
                    selectedItemsAsRTFText.AppendLine(@"\par}");
                }
                selectedItemsAsRTFText.AppendLine(@"}");
                System.Diagnostics.Debug.WriteLine(selectedItemsAsRTFText.ToString());
                Clipboard.SetData(DataFormats.Rtf, selectedItemsAsRTFText.ToString());
            }

        }

        public ListBoxLog(ListBox listBox) : this(listBox, DEFAULT_MESSAGE_FORMAT, DEFAULT_MAX_LINES_IN_LISTBOX) { }
        public ListBoxLog(ListBox listBox, string messageFormat) : this(listBox, messageFormat, DEFAULT_MAX_LINES_IN_LISTBOX) { }
        public ListBoxLog(ListBox listBox, string messageFormat, int maxLinesInListbox)
        {
            _disposed = false;

            _listBox = listBox;
            _messageFormat = messageFormat;
            _maxEntriesInListBox = maxLinesInListbox;

            _paused = false;

            _canAdd = listBox.IsHandleCreated;

            _listBox.SelectionMode = SelectionMode.MultiExtended;

            _listBox.HandleCreated += OnHandleCreated;
            _listBox.HandleDestroyed += OnHandleDestroyed;
            _listBox.DrawItem += DrawItemHandler;
            _listBox.KeyDown += KeyDownHandler;

            MenuItem[] menuItems = new MenuItem[] { new MenuItem("Copy Line", new EventHandler(CopyMenuOnClickHandler)) };
            _listBox.ContextMenu = new ContextMenu(menuItems);
            _listBox.ContextMenu.Popup += new EventHandler(CopyMenuPopupHandler);

            _listBox.DrawMode = DrawMode.OwnerDrawFixed;
        }

        public void Log(string message) { Log(Level.Debug, message); }
        public void Log(string format, params object[] args) { Log(Level.Debug, (format == null) ? null : string.Format(format, args)); }
        public void Log(Level level, string format, params object[] args) { Log(level, (format == null) ? null : string.Format(format, args)); }
        public void Log(Level level, string message)
        {
            WriteEvent(new LogEvent(level, message));
        }

        public bool Paused
        {
            get { return _paused; }
            set { _paused = value; }
        }

        ~ListBoxLog()
        {
            if (!_disposed)
            {
                Dispose(false);
                _disposed = true;
            }
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }
        private void Dispose(bool disposing)
        {
            if (_listBox != null)
            {
                _canAdd = false;

                _listBox.HandleCreated -= OnHandleCreated;
                _listBox.HandleCreated -= OnHandleDestroyed;
                _listBox.DrawItem -= DrawItemHandler;
                _listBox.KeyDown -= KeyDownHandler;

                _listBox.ContextMenu.MenuItems.Clear();
                _listBox.ContextMenu.Popup -= CopyMenuPopupHandler;
                _listBox.ContextMenu = null;

                _listBox.Items.Clear();
                _listBox.DrawMode = DrawMode.Normal;
                _listBox = null;
            }
        }
    }
}
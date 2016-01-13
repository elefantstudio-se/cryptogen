using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;


namespace nOz_CryptoGen
{
    partial class nabgen
    {
        // Declare CspParmeters and RsaCryptoServiceProvider 
        // objects with global scope of your Form class.
        CspParameters cspp = new CspParameters();
        RSACryptoServiceProvider rsa;

        // Path variables for source, encryption, and 
        // decryption folders. Must end with a backslash. 
        const string EncrFolder = @"Data\Enc\";
        const string DecrFolder = @"Data\Dec\";
        const string SrcFolder = @"";
        const string EncFile = @"nabkeys.nab";
        // Public key file 
        const string PubKeyFile = @"nabcryptoPublic.txt";

        // Key container name for 
        // private/public key value pair. 
        const string keyName = "/g@)Rhe8w@-u$jsb(*Pad+R3uPdayfA!+FaGN-qz44tm-@**knUwgzvz-@@7nVVB";

        private void EncryptFile(string inFile)
        {

            // Create instance of Rijndael for 
            // symetric encryption of the data.
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;
            ICryptoTransform transform = rjndl.CreateEncryptor();

            // Use RSACryptoServiceProvider to 
            // enrypt the Rijndael key. 
            // rsa is previously instantiated:  
            //    rsa = new RSACryptoServiceProvider(cspp); 
            byte[] keyEncrypted = rsa.Encrypt(rjndl.Key, false);

            // Create byte arrays to contain 
            // the length values of the key and IV. 
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            LenK = BitConverter.GetBytes(lKey);
            int lIV = rjndl.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            // Write the following to the FileStream 
            // for the encrypted file (outFs): 
            // - length of the key 
            // - length of the IV 
            // - ecrypted key 
            // - the IV 
            // - the encrypted cipher content 

            int startFileName = inFile.LastIndexOf("\\") + 1;
            // Change the file's extension to ".enc" 
            string outFile = EncrFolder + inFile.Substring(startFileName, inFile.LastIndexOf(".") - startFileName) + ".nab";

            using (FileStream outFs = new FileStream(outFile, FileMode.Create))
            {

                outFs.Write(LenK, 0, 4);
                outFs.Write(LenIV, 0, 4);
                outFs.Write(keyEncrypted, 0, lKey);
                outFs.Write(rjndl.IV, 0, lIV);

                // Now write the cipher text using 
                // a CryptoStream for encrypting. 
                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {

                    // By encrypting a chunk at 
                    // a time, you can save memory 
                    // and accommodate large files. 
                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size. 
                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;

                    using (FileStream inFs = new FileStream(inFile, FileMode.Open))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        }
                        while (count > 0);
                        inFs.Close();
                    }
                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();
                }
                outFs.Close();
            }

        }

        private void DecryptFile(string inFile)
        {

            // Create instance of Rijndael for 
            // symetric decryption of the data.
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;

            // Create byte arrays to get the length of 
            // the encrypted key and IV. 
            // These values were stored as 4 bytes each 
            // at the beginning of the encrypted package. 
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            // Consruct the file name for the decrypted file. 
            string outFile = DecrFolder + inFile.Substring(0, inFile.LastIndexOf(".")) + ".xml";

            // Use FileStream objects to read the encrypted 
            // file (inFs) and save the decrypted file (outFs). 
            using (FileStream inFs = new FileStream(EncrFolder + inFile, FileMode.Open))
            {

                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(LenK, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                // Convert the lengths to integer values. 
                int lenK = BitConverter.ToInt32(LenK, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                // Determine the start postition of 
                // the ciphter text (startC) 
                // and its length(lenC). 
                int startC = lenK + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                // Create the byte arrays for 
                // the encrypted Rijndael key, 
                // the IV, and the cipher text. 
                byte[] KeyEncrypted = new byte[lenK];
                byte[] IV = new byte[lenIV];

                // Extract the key and IV 
                // starting from index 8 
                // after the length values.
                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenK);
                inFs.Seek(8 + lenK, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);
                Directory.CreateDirectory(DecrFolder);
                // Use RSACryptoServiceProvider 
                // to decrypt the Rijndael key. 
                byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                // Decrypt the key.
                ICryptoTransform transform = rjndl.CreateDecryptor(KeyDecrypted, IV);

                // Decrypt the cipher text from 
                // from the FileSteam of the encrypted 
                // file (inFs) into the FileStream 
                // for the decrypted file (outFs). 
                using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                {

                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size. 
                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];


                    // By decrypting a chunk a time, 
                    // you can save memory and 
                    // accommodate large files. 

                    // Start at the beginning 
                    // of the cipher text.
                    inFs.Seek(startC, SeekOrigin.Begin);
                    using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);

                        }
                        while (count > 0);

                        outStreamDecrypted.FlushFinalBlock();
                        outStreamDecrypted.Close();
                    }
                    outFs.Close();
                }
                inFs.Close();
            }

        }
 public void castdata(String inName, String outName, byte[] tdesKey, byte[] tdesIV)
        {


            //Create the file streams to handle the input and output files.
            FileStream fin = new FileStream(inName, FileMode.Open, FileAccess.Read);
            FileStream fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write);
            fout.SetLength(0);

            //Create variables to help with read and write. 
            byte[] bin = new byte[100]; //This is intermediate storage for the encryption.
            long rdlen = 0;              //This is the total number of bytes written. 
            long totlen = fin.Length;    //This is the total length of the input file. 
            int len;                     //This is the number of bytes to be written at a time.

            TripleDESCryptoServiceProvider tDESalg = new TripleDESCryptoServiceProvider();
            CryptoStream encStream = new CryptoStream(fout, tDESalg.CreateEncryptor(tdesKey, tdesIV), CryptoStreamMode.Write);
            while (rdlen < totlen)
            {
                len = fin.Read(bin, 0, 100);
                encStream.Write(bin, 0, len);
                rdlen = rdlen + len;
                txtBoxkey.Text = "\r\n{0} bytes processed" + rdlen + "";
                encStream.Close();
            }
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(nabgen));
            this.btnGen = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.tab_main = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.btnValidate = new System.Windows.Forms.Button();
            this.bntSFL = new System.Windows.Forms.Button();
            this.btnSDB = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnImport = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnDec = new System.Windows.Forms.Button();
            this.txtBoxkey = new System.Windows.Forms.TextBox();
            this.btnEnc = new System.Windows.Forms.Button();
            this.bntAsm = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.button1 = new System.Windows.Forms.Button();
            this.btnLogsave = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.bgFlow = new System.ComponentModel.BackgroundWorker();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
            this.Sfd1 = new System.Windows.Forms.SaveFileDialog();
            this.fWatchA = new System.IO.FileSystemWatcher();
            this.tab_main.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fWatchA)).BeginInit();
            this.SuspendLayout();
            // 
            // btnGen
            // 
            this.btnGen.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGen.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnGen.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGen.Location = new System.Drawing.Point(51, 86);
            this.btnGen.Name = "btnGen";
            this.btnGen.Size = new System.Drawing.Size(75, 23);
            this.btnGen.TabIndex = 0;
            this.btnGen.Text = "&Generate";
            this.btnGen.UseVisualStyleBackColor = true;
            this.btnGen.Click += new System.EventHandler(this.btnGen_Click_1);
            // 
            // comboBox1
            // 
            this.comboBox1.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.comboBox1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox1.ForeColor = System.Drawing.Color.OrangeRed;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(51, 59);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(321, 21);
            this.comboBox1.TabIndex = 2;
            // 
            // tab_main
            // 
            this.tab_main.Controls.Add(this.tabPage1);
            this.tab_main.Controls.Add(this.tabPage2);
            this.tab_main.Controls.Add(this.tabPage4);
            this.tab_main.Controls.Add(this.tabPage3);
            this.tab_main.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tab_main.Location = new System.Drawing.Point(1, 0);
            this.tab_main.Name = "tab_main";
            this.tab_main.SelectedIndex = 0;
            this.tab_main.Size = new System.Drawing.Size(448, 225);
            this.tab_main.TabIndex = 4;
            // 
            // tabPage1
            // 
            this.tabPage1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.btnValidate);
            this.tabPage1.Controls.Add(this.bntSFL);
            this.tabPage1.Controls.Add(this.btnSDB);
            this.tabPage1.Controls.Add(this.comboBox1);
            this.tabPage1.Controls.Add(this.btnGen);
            this.tabPage1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(440, 199);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Alpha & Beta - Keymuddler";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(48, 39);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(150, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "Alpha && Beta Muddler:";
            // 
            // btnValidate
            // 
            this.btnValidate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnValidate.Enabled = false;
            this.btnValidate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnValidate.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnValidate.Location = new System.Drawing.Point(132, 86);
            this.btnValidate.Name = "btnValidate";
            this.btnValidate.Size = new System.Drawing.Size(94, 23);
            this.btnValidate.TabIndex = 5;
            this.btnValidate.Text = "&Check if Used";
            this.btnValidate.UseVisualStyleBackColor = true;
            this.btnValidate.Click += new System.EventHandler(this.button3_Click);
            // 
            // bntSFL
            // 
            this.bntSFL.Cursor = System.Windows.Forms.Cursors.Hand;
            this.bntSFL.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bntSFL.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bntSFL.Location = new System.Drawing.Point(289, 115);
            this.bntSFL.Name = "bntSFL";
            this.bntSFL.Size = new System.Drawing.Size(83, 22);
            this.bntSFL.TabIndex = 4;
            this.bntSFL.Text = "&Write to File";
            this.bntSFL.UseVisualStyleBackColor = true;
            this.bntSFL.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnSDB
            // 
            this.btnSDB.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSDB.Enabled = false;
            this.btnSDB.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSDB.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSDB.Location = new System.Drawing.Point(289, 86);
            this.btnSDB.Name = "btnSDB";
            this.btnSDB.Size = new System.Drawing.Size(83, 23);
            this.btnSDB.TabIndex = 3;
            this.btnSDB.Text = "Sa&ve to DB";
            this.btnSDB.UseVisualStyleBackColor = true;
            this.btnSDB.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.checkBox2);
            this.tabPage2.Controls.Add(this.checkBox1);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.btnImport);
            this.tabPage2.Controls.Add(this.btnExport);
            this.tabPage2.Controls.Add(this.btnDec);
            this.tabPage2.Controls.Add(this.txtBoxkey);
            this.tabPage2.Controls.Add(this.btnEnc);
            this.tabPage2.Controls.Add(this.bntAsm);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(440, 199);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "NAB - Encryption";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Enabled = false;
            this.checkBox2.Font = new System.Drawing.Font("Segoe UI", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox2.Location = new System.Drawing.Point(330, 83);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(104, 17);
            this.checkBox2.TabIndex = 10;
            this.checkBox2.Text = "- 3DES \"Ontop\"";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Enabled = false;
            this.checkBox1.Font = new System.Drawing.Font("Segoe UI", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox1.Location = new System.Drawing.Point(331, 56);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(108, 17);
            this.checkBox1.TabIndex = 9;
            this.checkBox1.Text = "+ 3DES \"Ontop\"";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 107);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Output:";
            // 
            // btnImport
            // 
            this.btnImport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImport.Location = new System.Drawing.Point(110, 79);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(112, 24);
            this.btnImport.TabIndex = 7;
            this.btnImport.Text = "Import Public Key";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.button1_Click_2);
            // 
            // btnExport
            // 
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Location = new System.Drawing.Point(110, 49);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(112, 24);
            this.btnExport.TabIndex = 6;
            this.btnExport.Text = "Export Public Key";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnDec
            // 
            this.btnDec.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDec.Location = new System.Drawing.Point(228, 79);
            this.btnDec.Name = "btnDec";
            this.btnDec.Size = new System.Drawing.Size(98, 24);
            this.btnDec.TabIndex = 5;
            this.btnDec.Text = "Decrypt";
            this.btnDec.UseVisualStyleBackColor = true;
            this.btnDec.Click += new System.EventHandler(this.btnDec_Click);
            // 
            // txtBoxkey
            // 
            this.txtBoxkey.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList;
            this.txtBoxkey.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.txtBoxkey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBoxkey.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBoxkey.ForeColor = System.Drawing.Color.OrangeRed;
            this.txtBoxkey.Location = new System.Drawing.Point(4, 123);
            this.txtBoxkey.Multiline = true;
            this.txtBoxkey.Name = "txtBoxkey";
            this.txtBoxkey.ReadOnly = true;
            this.txtBoxkey.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtBoxkey.Size = new System.Drawing.Size(435, 70);
            this.txtBoxkey.TabIndex = 4;
            this.txtBoxkey.UseWaitCursor = true;
            this.txtBoxkey.TextChanged += new System.EventHandler(this.txtBoxkey_TextChanged);
            // 
            // btnEnc
            // 
            this.btnEnc.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEnc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEnc.Location = new System.Drawing.Point(228, 49);
            this.btnEnc.Name = "btnEnc";
            this.btnEnc.Size = new System.Drawing.Size(98, 24);
            this.btnEnc.TabIndex = 3;
            this.btnEnc.Text = "Encrypt";
            this.btnEnc.UseVisualStyleBackColor = true;
            this.btnEnc.Click += new System.EventHandler(this.btnEnc_Click);
            // 
            // bntAsm
            // 
            this.bntAsm.Cursor = System.Windows.Forms.Cursors.Hand;
            this.bntAsm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bntAsm.Location = new System.Drawing.Point(6, 49);
            this.bntAsm.Name = "bntAsm";
            this.bntAsm.Size = new System.Drawing.Size(98, 24);
            this.bntAsm.TabIndex = 1;
            this.bntAsm.Text = "Create Key";
            this.bntAsm.UseVisualStyleBackColor = true;
            this.bntAsm.Click += new System.EventHandler(this.bntAsm_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(149, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "NAB - Encrypter";
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.button1);
            this.tabPage4.Controls.Add(this.btnLogsave);
            this.tabPage4.Controls.Add(this.btnPause);
            this.tabPage4.Controls.Add(this.listBox1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(440, 199);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Debug/Logg";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 174);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(48, 19);
            this.button1.TabIndex = 9;
            this.button1.Text = "Clear!";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // btnLogsave
            // 
            this.btnLogsave.Location = new System.Drawing.Point(334, 174);
            this.btnLogsave.Name = "btnLogsave";
            this.btnLogsave.Size = new System.Drawing.Size(48, 19);
            this.btnLogsave.TabIndex = 8;
            this.btnLogsave.Text = "Save";
            this.btnLogsave.UseVisualStyleBackColor = true;
            this.btnLogsave.Click += new System.EventHandler(this.btnLogsave_Click);
            // 
            // btnPause
            // 
            this.btnPause.Location = new System.Drawing.Point(388, 174);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(48, 19);
            this.btnPause.TabIndex = 7;
            this.btnPause.Text = "Pause";
            this.btnPause.UseVisualStyleBackColor = true;
            // 
            // listBox1
            // 
            this.listBox1.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.listBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listBox1.ForeColor = System.Drawing.SystemColors.Window;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(3, 3);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(433, 171);
            this.listBox1.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.pictureBox2);
            this.tabPage3.Controls.Add(this.label5);
            this.tabPage3.Controls.Add(this.label2);
            this.tabPage3.Controls.Add(this.pictureBox1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(440, 199);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "About";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::nOz_CryptoGen.Properties.Resources.shake;
            this.pictureBox2.Location = new System.Drawing.Point(397, 160);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(39, 33);
            this.pictureBox2.TabIndex = 3;
            this.pictureBox2.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(398, 46);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "label5";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(212, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(224, 21);
            this.label2.TabIndex = 0;
            this.label2.Text = "nOz - Alpha && Beta Muddler";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::nOz_CryptoGen.Properties.Resources.giphy;
            this.pictureBox1.Location = new System.Drawing.Point(-4, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(448, 200);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // bgFlow
            // 
            this.bgFlow.WorkerSupportsCancellation = true;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // openFileDialog2
            // 
            this.openFileDialog2.FileName = "openFileDialog2";
            // 
            // Sfd1
            // 
            this.Sfd1.FileName = "*.nabl";
            this.Sfd1.Filter = "NAB Logs |*.nabl";
            this.Sfd1.Title = "NAB Debug Logger - Save";
            // 
            // fWatchA
            // 
            this.fWatchA.EnableRaisingEvents = true;
            this.fWatchA.SynchronizingObject = this;
            // 
            // nabgen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(447, 225);
            this.Controls.Add(this.tab_main);
            this.ForeColor = System.Drawing.SystemColors.WindowFrame;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "nabgen";
            this.Padding = new System.Windows.Forms.Padding(20, 30, 20, 20);
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "n0z-Alpha&Beta - Generator";
            this.Load += new System.EventHandler(this.nabgen_Load);
            this.tab_main.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fWatchA)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnGen;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TabControl tab_main;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btnSDB;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button bntSFL;
        private System.Windows.Forms.Button btnValidate;
        private System.Windows.Forms.Label label1;
        private System.ComponentModel.BackgroundWorker bgFlow;
        private System.Windows.Forms.Button bntAsm;
        private System.Windows.Forms.Button btnEnc;
        private Button btnDec;
        private TextBox txtBoxkey;
        private OpenFileDialog openFileDialog1;
        private OpenFileDialog openFileDialog2;
        private Button btnExport;
        private Button btnImport;
        private TabPage tabPage4;
        private TabPage tabPage3;
        private Label label2;
        private Label label3;
        private Label label4;
        private PictureBox pictureBox1;
        private Label label5;
        private ListBox listBox1;
        private Button btnPause;
        private Button btnLogsave;
        private SaveFileDialog Sfd1;
        private Button button1;
        private PictureBox pictureBox2;
        private CheckBox checkBox2;
        private CheckBox checkBox1;
        private FileSystemWatcher fWatchA;
    }
}

            
using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AI_GUI
{
    public partial class Form1 : Form
    {
        // Hardcoded settings you should verify for your local system
        private static string aiPath = @"C:\src\VQGAN-CLIP"; // Where the generate.py is located, basically the working directory that is used all time

        // Form level variables
        private string output = "";
        int lastLength = 0;
        Process process = new Process();
        private static string endMarker = "AI GUI task finished!"; // So it's possible to recognize end of image generation from stdout stream
        private string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string fileName = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            txtOutput.Text = "";
            tmrUpdate.Enabled = true;
            lastLength = 0;
            output = "";

            btnOpenFile.Enabled = false;
            btnCopy.Enabled = false;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            txtWords.Enabled = false;

            // Generate a safe image file name from the given words
            fileName = Regex.Replace(txtWords.Text.Trim(), "[^a-zA-Z0-9_]+", "").Replace(" ", "_") + ".png";

            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/K " + userPath + @"\anaconda3\Scripts\activate.bat " + userPath + @"\anaconda3"; // Go to per-user installed anaconda
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = aiPath;

            process.Start();

            // Asynchronous reading of stdout that is not depended on newlines
            var _ = ConsumeReader(process.StandardOutput);
            async Task ConsumeReader(TextReader reader)
            {
                char[] buffer = new char[1];

                while ((await process.StandardOutput.ReadAsync(buffer, 0, 1)) > 0)
                {
                    output += buffer[0];
                }
            }

            process.StandardInput.WriteLine("conda activate vqgan");
            process.StandardInput.WriteLine("python -u generate.py -p \"" + txtWords.Text.Replace("\"", "\\\"") + "\""); // Escape quote in words for command line
            process.StandardInput.WriteLine("copy /y output.png " + fileName);
            process.StandardInput.WriteLine("echo " + endMarker);
        }

        private void tmrUpdate_Tick(object sender, EventArgs e)
        {
            int length = output.Length;

            if (length > lastLength)
            {
                txtOutput.AppendText(output.Substring(lastLength, length - lastLength));
                lastLength = length;

                if (txtOutput.Text.Contains(endMarker))
                {
                    process.Kill();

                    tmrUpdate.Enabled = false;
                    btnStart.Enabled = true;
                    btnCopy.Enabled = true;
                    btnOpenFile.Enabled = true;
                    btnStop.Enabled = false;
                    txtWords.Enabled = true;

                    pbOutput.ImageLocation = aiPath + @"\" + fileName;
                }
            }            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            tmrUpdate.Enabled = false;
            try { process.Kill(true); } catch (Exception) { };
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            tmrUpdate.Enabled = false;
            try { process.Kill(true); } catch (Exception) { };
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            txtWords.Enabled = true;
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { Arguments = aiPath, FileName = "explorer.exe" });
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { Arguments = "/K start " + aiPath + @"\" + fileName, FileName = "cmd.exe", CreateNoWindow = true });
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(pbOutput.Image);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Recycle the existing form icon as a placeholder picture for the AI generated picture preview
            pbOutput.Image = this.Icon.ToBitmap();
        }
    }
}
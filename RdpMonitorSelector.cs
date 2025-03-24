using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace RdpMonitorSelector
{
    public partial class MainForm : Form
    {
        private List<MonitorPanel> monitorPanels = new List<MonitorPanel>();
        private float scaleFactor = 0.1f; // Scale down monitor sizes for display
        private int padding = 20;
        private string selectedRdpFilePath = null;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "RDP Monitor Selector";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            DisplayMonitors();
            AddControls();
        }

        private void DisplayMonitors()
        {
            // Get all screens
            Screen[] screens = Screen.AllScreens;
            
            // Find the bounding rectangle of all monitors
            Rectangle allScreensBounds = Rectangle.Empty;
            foreach (Screen screen in screens)
            {
                if (allScreensBounds == Rectangle.Empty)
                {
                    allScreensBounds = screen.Bounds;
                }
                else
                {
                    allScreensBounds = Rectangle.Union(allScreensBounds, screen.Bounds);
                }
            }

            // Adjust scale factor if needed
            int maxWidth = (int)(allScreensBounds.Width * scaleFactor) + padding * 2;
            int maxHeight = (int)(allScreensBounds.Height * scaleFactor) + padding * 2;
            if (maxWidth > 700 || maxHeight > 500)
            {
                float scaleX = 700f / maxWidth;
                float scaleY = 500f / maxHeight;
                scaleFactor *= Math.Min(scaleX, scaleY);
            }

            // Create visual representation for each monitor
            for (int i = 0; i < screens.Length; i++)
            {
                Screen screen = screens[i];
                
                // Create scaled position and size
                int x = padding + (int)((screen.Bounds.X - allScreensBounds.X) * scaleFactor);
                int y = padding + (int)((screen.Bounds.Y - allScreensBounds.Y) * scaleFactor);
                int width = (int)(screen.Bounds.Width * scaleFactor);
                int height = (int)(screen.Bounds.Height * scaleFactor);

                // Create panel for this monitor
                MonitorPanel panel = new MonitorPanel(i, screen.Bounds);
                panel.Location = new Point(x, y);
                panel.Size = new Size(width, height);
                panel.BackColor = Color.LightBlue;
                panel.Text = $"Monitor {i}\n{screen.Bounds.Width}x{screen.Bounds.Height}";
                
                monitorPanels.Add(panel);
                this.Controls.Add(panel);
            }
        }

        private void AddControls()
        {
            // Instruction label
            Label instructionLabel = new Label();
            instructionLabel.Text = "Click on monitors to select/deselect them for your RDP session (only contiguous monitors can be selected)";
            instructionLabel.AutoSize = true;
            instructionLabel.Location = new Point(padding, this.ClientSize.Height - 100);
            this.Controls.Add(instructionLabel);

            // Browse button for existing RDP files
            Button browseButton = new Button();
            browseButton.Text = "Select Existing RDP File";
            browseButton.Size = new Size(150, 30);
            browseButton.Location = new Point(padding, this.ClientSize.Height - 70);
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            // Selected file label
            Label fileLabel = new Label();
            fileLabel.Name = "FilePathLabel";
            fileLabel.Text = "No file selected - will create new";
            fileLabel.AutoSize = true;
            fileLabel.Location = new Point(padding + 160, this.ClientSize.Height - 65);
            this.Controls.Add(fileLabel);

            // Generate button
            Button generateButton = new Button();
            generateButton.Text = "Generate RDP File && Connect";
            generateButton.Size = new Size(200, 30);
            generateButton.Location = new Point(this.ClientSize.Width / 2 - 100, this.ClientSize.Height - 30);
            generateButton.Click += GenerateButton_Click;
            this.Controls.Add(generateButton);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "RDP Files (*.rdp)|*.rdp";
                openFileDialog.Title = "Select an RDP File to Edit";
                openFileDialog.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedRdpFilePath = openFileDialog.FileName;
                    
                    // Update the label to show the selected file
                    Label fileLabel = (Label)this.Controls.Find("FilePathLabel", true)[0];
                    fileLabel.Text = "Selected: " + Path.GetFileName(selectedRdpFilePath);
                    
                    // Load and display the monitors selected in the file
                    LoadMonitorSelectionFromFile(selectedRdpFilePath);
                }
            }
        }

        private void LoadMonitorSelectionFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"The file {filePath} does not exist.", 
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // First, reset all monitors to unselected
                foreach (MonitorPanel panel in monitorPanels)
                {
                    panel.SetSelected(false);
                }

                // Read the file and look for the selectedmonitors line
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("selectedmonitors:s:"))
                    {
                        // Extract the monitor indices
                        string monitorsValue = line.Substring("selectedmonitors:s:".Length).Trim();
                        string[] monitorIndices = monitorsValue.Split(',');

                        // Set the selected state for each monitor found in the file
                        foreach (string indexStr in monitorIndices)
                        {
                            if (int.TryParse(indexStr, out int monitorIndex))
                            {
                                // Find and select the monitor panel with this index
                                foreach (MonitorPanel panel in monitorPanels)
                                {
                                    if (panel.MonitorIndex == monitorIndex)
                                    {
                                        panel.SetSelected(true);
                                        break;
                                    }
                                }
                            }
                        }
                        
                        // We found and processed the line, so we can stop
                        return;
                    }
                }

                // If we get here, no selectedmonitors line was found
                MessageBox.Show("No monitor selection found in the RDP file. All monitors will be unselected.",
                    "No Selection Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading monitor selection from file: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            List<int> selectedMonitorIndices = new List<int>();
            
            foreach (MonitorPanel panel in monitorPanels)
            {
                if (panel.Selected)
                {
                    selectedMonitorIndices.Add(panel.MonitorIndex);
                }
            }

            if (selectedMonitorIndices.Count == 0)
            {
                MessageBox.Show("Please select at least one monitor.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rdpFilePath;
            if (selectedRdpFilePath != null)
            {
                // Edit existing RDP file
                rdpFilePath = EditExistingRdpFile(selectedRdpFilePath, selectedMonitorIndices);
            }
            else
            {
                // Create new RDP file
                rdpFilePath = GenerateRdpFile(selectedMonitorIndices);
            }
            
            LaunchRdpSession(rdpFilePath);
        }

        private string EditExistingRdpFile(string filePath, List<int> selectedMonitorIndices)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"The file {filePath} no longer exists. Creating a new file instead.", 
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return GenerateRdpFile(selectedMonitorIndices);
            }

            try
            {
                // Read the entire file
                string[] lines = File.ReadAllLines(filePath);
                bool monitorLineFound = false;
                
                // Create the new monitor selection line
                string monitorSelectionLine = "selectedmonitors:s:";
                for (int i = 0; i < selectedMonitorIndices.Count; i++)
                {
                    monitorSelectionLine += selectedMonitorIndices[i].ToString();
                    if (i < selectedMonitorIndices.Count - 1)
                    {
                        monitorSelectionLine += ",";
                    }
                }

                // Replace or add the monitor selection line
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("selectedmonitors:s:"))
                    {
                        lines[i] = monitorSelectionLine;
                        monitorLineFound = true;
                        break;
                    }
                }

                // If monitor line wasn't found, ensure multimon is enabled and add the line
                if (!monitorLineFound)
                {
                    // Check if multimon is enabled
                    bool multimonEnabled = false;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("use multimon:i:"))
                        {
                            lines[i] = "use multimon:i:1";
                            multimonEnabled = true;
                            break;
                        }
                    }

                    // If multimon line wasn't found, add it
                    List<string> linesList = new List<string>(lines);
                    if (!multimonEnabled)
                    {
                        linesList.Add("use multimon:i:1");
                    }
                    linesList.Add(monitorSelectionLine);
                    lines = linesList.ToArray();
                }

                // Write the updated contents back to the file
                File.WriteAllLines(filePath, lines);
                MessageBox.Show($"RDP file has been updated:\n{filePath}", "File Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating RDP file: {ex.Message}\nCreating a new file instead.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return GenerateRdpFile(selectedMonitorIndices);
            }
        }

        private string GenerateRdpFile(List<int> selectedMonitorIndices)
        {
            string rdpContent = 
                "screen mode id:i:2\n" +
                "use multimon:i:1\n" +
                "selectedmonitors:s:";

            // Add selected monitors by index
            for (int i = 0; i < selectedMonitorIndices.Count; i++)
            {
                int monitorIndex = selectedMonitorIndices[i];
                rdpContent += monitorIndex.ToString();
                
                if (i < selectedMonitorIndices.Count - 1)
                {
                    rdpContent += ",";
                }
            }

            rdpContent += "\n";
            
            // Add other common RDP settings
            rdpContent += 
                "desktopwidth:i:1920\n" +
                "desktopheight:i:1080\n" +
                "session bpp:i:32\n" +
                "winposstr:s:0,3,0,0,800,600\n" +  // Value of 3 allows window to be maximized
                "compression:i:1\n" +
                "keyboardhook:i:2\n" +
                "audiocapturemode:i:0\n" +
                "videoplaybackmode:i:1\n" +
                "connection type:i:7\n" +
                "networkautodetect:i:1\n" +
                "bandwidthautodetect:i:1\n" +
                "displayconnectionbar:i:1\n" +
                "enableworkspacereconnect:i:0\n" +
                "disable wallpaper:i:0\n" +
                "allow font smoothing:i:1\n" +
                "allow desktop composition:i:1\n" +
                "disable full window drag:i:0\n" +
                "disable menu anims:i:0\n" +
                "disable themes:i:0\n" +
                "disable cursor setting:i:0\n" +
                "bitmapcachepersistenable:i:1\n" +
                "audiomode:i:0\n" +
                "redirectprinters:i:1\n" +
                "redirectcomports:i:0\n" +
                "redirectsmartcards:i:1\n" +
                "redirectclipboard:i:1\n" +
                "redirectposdevices:i:0\n" +
                "redirectdirectx:i:1\n" +
                "autoreconnection enabled:i:1\n" +
                "authentication level:i:2\n" +
                "prompt for credentials:i:0\n" +
                "negotiate security layer:i:1\n" +
                "remoteapplicationmode:i:0\n" +
                "alternate shell:s:\n" +
                "shell working directory:s:\n" +
                "gatewayhostname:s:\n" +
                "gatewayusagemethod:i:4\n" +
                "gatewaycredentialssource:i:4\n" +
                "gatewayprofileusagemethod:i:0\n" +
                "promptcredentialonce:i:0\n" +
                "use redirection server name:i:0\n";

            // Save next to the executable instead of on the Desktop
            string executableDir = Path.GetDirectoryName(Application.ExecutablePath);
            string filePath = Path.Combine(
                executableDir,
                $"MonitorSelect_{DateTime.Now:yyyyMMdd_HHmmss}.rdp");

            File.WriteAllText(filePath, rdpContent);
            MessageBox.Show($"RDP file has been saved next to the executable:\n{filePath}", "File Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return filePath;
        }

        private void LaunchRdpSession(string rdpFilePath)
        {
            try
            {
                Process.Start("mstsc.exe", $"\"{rdpFilePath}\"");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching RDP session: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Custom panel class for monitor representation
    public class MonitorPanel : Panel
    {
        public int MonitorIndex { get; private set; }
        public Rectangle MonitorBounds { get; private set; }
        public bool Selected { get; private set; } = true; // Selected by default

        public MonitorPanel(int index, Rectangle bounds)
        {
            MonitorIndex = index;
            MonitorBounds = bounds;
            this.Paint += MonitorPanel_Paint;
            this.Click += MonitorPanel_Click;
        }

        // Add a method to set the selected state programmatically
        public void SetSelected(bool selected)
        {
            Selected = selected;
            this.Invalidate(); // Redraw the panel
        }

        private void MonitorPanel_Click(object sender, EventArgs e)
        {
            Selected = !Selected;
            this.Invalidate(); // Redraw the panel
        }

        private void MonitorPanel_Paint(object sender, PaintEventArgs e)
        {
            // Draw border
            using (Pen borderPen = new Pen(Selected ? Color.Green : Color.Gray, 3))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // Draw text
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    e.Graphics.DrawString(this.Text, this.Font, textBrush, 
                        new RectangleF(0, 0, this.Width, this.Height), sf);
                }
            }

            // Draw selection indicator
            if (Selected)
            {
                using (Brush checkBrush = new SolidBrush(Color.Green))
                {
                    e.Graphics.FillEllipse(checkBrush, this.Width - 20, 5, 15, 15);
                }
            }
        }
    }
} 
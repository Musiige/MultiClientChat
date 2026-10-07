using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class Form1 : Form
    {
        // --- Core Networking Fields ---
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread receiveThread;
        private bool isConnected = false;

        // --- Custom Modern UI Controls ---
        private Panel pnlHeader;
        private Label lblAppName;
        private Label lblStatusDot;
        private Label lblStatusText;
        private Label lblConnInfo;
        private Button btnThemeToggle;

        private SplitContainer splitMain;
        private ListBox lstChat;
        private ListBox lstUsers;

        private Panel pnlInput;
        private Panel pnlEmojiBar;
        private TextBox txtMessage;
        private Button btnSend;

        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtUsername;
        private Button btnConnect;

        // Modern Palette Definitions
        private Color colorFormBg;
        private Color colorControlsBg;
        private Color colorText;
        private Color colorAccent;
        private Color colorSystemMessage = Color.FromArgb(100, 255, 100);  // Soft Green
        private Color colorPrivateMessage = Color.FromArgb(200, 160, 255); // Soft Purple
        private bool isDarkMode = true;
        private Font modernFontRegular;
        private Font modernFontBold;

        public Form1()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
            modernFontRegular = new Font("Segoe UI Emoji", 10F, FontStyle.Regular);
            modernFontBold = new Font("Segoe UI Emoji", 10F, FontStyle.Bold);
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "C# TCP Chat Room Hub";
            this.Size = new Size(1000, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            ApplyModernTheme();

            // =========================================================================================
            // SECTION 1: TOP HEADER BAR
            // =========================================================================================
            pnlHeader = new Panel() { Dock = DockStyle.Top, Height = 55, BackColor = colorFormBg };

            lblAppName = new Label() { Text = "💬 Network Hub", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(15, 13), AutoSize = true, ForeColor = Color.White };

            lblStatusDot = new Label() { Text = "○", Font = new Font("Arial", 14F, FontStyle.Bold), Location = new Point(175, 14), AutoSize = true, ForeColor = Color.DarkGray };
            lblStatusText = new Label() { Text = "Offline", Font = modernFontBold, Location = new Point(200, 16), AutoSize = true, ForeColor = Color.DarkGray };

            btnThemeToggle = new Button() { Text = isDarkMode ? "☀️ Light Mode" : "🌙 Dark Mode", Font = new Font("Segoe UI", 8F), Location = new Point(900, 12), Width = 80, Height = 30, FlatStyle = FlatStyle.Flat };
            btnThemeToggle.Click += BtnTheme_Click;

            // Simple connection settings in header
            lblConnInfo = new Label() { Text = "IP:Port/User:", Location = new Point(320, 18), AutoSize = true, Font = modernFontRegular, ForeColor = Color.White };
            txtIp = new TextBox() { Text = "127.0.0.1", Location = new Point(410, 15), Width = 80, Font = modernFontRegular, BackColor = Color.White, ForeColor = Color.Black };
            txtPort = new TextBox() { Text = "8765", Location = new Point(495, 15), Width = 40, Font = modernFontRegular, BackColor = Color.White, ForeColor = Color.Black };
            txtUsername = new TextBox() { Text = "Yawe", Location = new Point(540, 15), Width = 80, Font = modernFontRegular, BackColor = Color.White, ForeColor = Color.Black };
            btnConnect = new Button() { Text = "Connect", Location = new Point(630, 13), Width = 80, Height = 28, Font = modernFontBold };
            btnConnect.Click += BtnConnect_Click;

            pnlHeader.Controls.Add(lblAppName);
            pnlHeader.Controls.Add(lblStatusDot);
            pnlHeader.Controls.Add(lblStatusText);
            pnlHeader.Controls.Add(btnThemeToggle);
            pnlHeader.Controls.Add(lblConnInfo);
            pnlHeader.Controls.Add(txtIp);
            pnlHeader.Controls.Add(txtPort);
            pnlHeader.Controls.Add(txtUsername);
            pnlHeader.Controls.Add(btnConnect);

            // =========================================================================================
            // SECTION 2: MAIN WORKSPACE (Split View: Chat | Users)
            // =========================================================================================
            splitMain = new SplitContainer() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 750 };

            // 2A: CHAT FEED (Owner-Draw for message color-coding)
            lstChat = new ListBox()
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Emoji", 10F, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 26
            };
            lstChat.DrawItem += LstChat_DrawItem;
            splitMain.Panel1.Controls.Add(lstChat);

            // 2B: ONLINE USERS SIDEBAR (Owner-Draw for status dots)
            lstUsers = new ListBox() { Dock = DockStyle.Fill, Font = modernFontRegular, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 35 };
            lstUsers.DrawItem += LstUsers_DrawItem;
            lstUsers.Items.Add("🌐 Everyone (Broadcast)");
            lstUsers.SelectedIndex = 0;
            splitMain.Panel2.Controls.Add(lstUsers);

            // =========================================================================================
            // SECTION 3: INPUT FOOTER (Emoji bar + Send Controls)
            // =========================================================================================
            pnlInput = new Panel() { Dock = DockStyle.Bottom, Height = 100, BackColor = colorFormBg };

            // 3A: EMOJI BAR
            pnlEmojiBar = new Panel() { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(colorFormBg.R - 5, colorFormBg.G - 5, colorFormBg.B - 5) };
            string[] quickEmojis = { "👍", "❤️", "😂", "🎉", "👋", "🔥", "💯", "✅", "⚠️", "🆘" };
            int currentX = 15;
            foreach (string emoji in quickEmojis)
            {
                Button btnEmoji = new Button() { Text = emoji, Size = new Size(40, 30), Location = new Point(currentX, 5), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
                btnEmoji.FlatAppearance.BorderSize = 0;
                btnEmoji.Click += (s, e) => { txtMessage.Text += emoji; txtMessage.Focus(); };
                pnlEmojiBar.Controls.Add(btnEmoji);
                currentX += 45;
            }

            // 3B: SEND CONTROLS
            txtMessage = new TextBox()
            {
                Location = new Point(15, 55),
                Width = 850,
                Font = new Font("Segoe UI Emoji", 11F, FontStyle.Regular),
                Height = 35,
                Enabled = false
            };
            txtMessage.KeyDown += TxtMessage_KeyDown;
            btnSend = new Button() { Text = "SEND", Location = new Point(875, 53), Width = 105, Height = 35, Font = modernFontBold, Enabled = false };
            btnSend.Click += BtnSend_Click;

            pnlInput.Controls.Add(pnlEmojiBar);
            pnlInput.Controls.Add(txtMessage);
            pnlInput.Controls.Add(btnSend);

            // Final Assembly
            this.Controls.Add(splitMain);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlInput);

            ApplyControlPalette();
        }

        // =========================================================================================
        // OWNER-DRAW LOGIC (COLOR CODING & STATUS DOTS)
        // =========================================================================================

        private void LstChat_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            string text = lstChat.Items[e.Index].ToString();

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(50, colorAccent.R, colorAccent.G, colorAccent.B)), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(colorControlsBg), e.Bounds);
            }

            Brush textBrush = new SolidBrush(colorText);

            // Robust timestamp extraction
            int closeBracketIdx = text.IndexOf(']');
            string timestamp = closeBracketIdx != -1 ? text.Substring(0, closeBracketIdx + 1) : "";
            string msgContent = closeBracketIdx != -1 ? text.Substring(closeBracketIdx + 1) : text;

            // System Message Logic
            if (msgContent.TrimStart().StartsWith("SERVER System:") || msgContent.TrimStart().StartsWith("SERVER:"))
            {
                textBrush = new SolidBrush(colorSystemMessage);
            }
            // Private Message Logic
            else if (msgContent.TrimStart().StartsWith("[PRIVATE from") || msgContent.TrimStart().StartsWith("[PRIVATE to"))
            {
                textBrush = new SolidBrush(colorPrivateMessage);
            }
            // Regular Broadcast (Bold Username)
            else if (msgContent.Contains(": "))
            {
                int colonIdx = msgContent.IndexOf(": ");
                string username = timestamp + msgContent.Substring(0, colonIdx + 2);
                string actualText = msgContent.Substring(colonIdx + 2);

                e.Graphics.DrawString(username, modernFontBold, textBrush, e.Bounds.X + 10, e.Bounds.Y + 2);
                float userNameWidth = e.Graphics.MeasureString(username, modernFontBold).Width;
                e.Graphics.DrawString(actualText, modernFontRegular, textBrush, e.Bounds.X + 10 + userNameWidth, e.Bounds.Y + 2);
                return;
            }

            e.Graphics.DrawString(text, modernFontRegular, textBrush, e.Bounds.X + 10, e.Bounds.Y + 2);
        }

        private void LstUsers_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            string text = lstUsers.Items[e.Index].ToString();
            Graphics g = e.Graphics;

            Color userBack = lstUsers.Parent.BackColor;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                g.FillRectangle(new SolidBrush(colorAccent), e.Bounds);
            }
            else
            {
                g.FillRectangle(new SolidBrush(userBack), e.Bounds);
            }

            Color fore = isDarkMode || (e.State & DrawItemState.Selected) == DrawItemState.Selected ? Color.White : Color.Black;

            if (text == "🌐 Everyone (Broadcast)")
            {
                g.DrawString(text, modernFontRegular, new SolidBrush(fore), e.Bounds.X + 15, e.Bounds.Y + 8);
            }
            else
            {
                g.FillEllipse(new SolidBrush(colorSystemMessage), e.Bounds.X + 15, e.Bounds.Y + 12, 10, 10);
                g.DrawString(text, modernFontBold, new SolidBrush(fore), e.Bounds.X + 35, e.Bounds.Y + 8);
            }
        }

        // =========================================================================================
        // THEME & STYLING
        // =========================================================================================
        private void ApplyModernTheme()
        {
            if (isDarkMode)
            {
                colorFormBg = Color.FromArgb(23, 23, 27);
                colorControlsBg = Color.FromArgb(30, 31, 34);
                colorAccent = Color.FromArgb(88, 101, 242);
                colorText = Color.White;
            }
            else
            {
                colorFormBg = Color.FromArgb(240, 240, 243);
                colorControlsBg = Color.White;
                colorAccent = Color.FromArgb(108, 92, 231);
                colorText = Color.Black;
            }
        }

        private void ApplyControlPalette()
        {
            this.BackColor = colorControlsBg;
            lstChat.BackColor = colorControlsBg;
            lstChat.ForeColor = colorText;
            lstUsers.BackColor = Color.FromArgb(colorControlsBg.R + (isDarkMode ? 5 : -10), colorControlsBg.G + (isDarkMode ? 5 : -10), colorControlsBg.B + (isDarkMode ? 5 : -10));
            lstUsers.ForeColor = colorText;
            txtMessage.BackColor = colorControlsBg;
            txtMessage.ForeColor = colorText;
            txtMessage.BorderStyle = BorderStyle.FixedSingle;

            StyleAccentButton(btnSend);
            StyleAccentButton(btnConnect);
            StyleAccentButton(btnThemeToggle);
        }

        private void StyleAccentButton(Button? b)
        {
            if (b == null || b.IsDisposed) return;

            b.BackColor = colorAccent;
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;

            if (b.FlatAppearance != null)
            {
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(colorAccent);
            }
        }
        private void BtnTheme_Click(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            btnThemeToggle.Text = isDarkMode ? "☀️ Light Mode" : "🌙 Dark Mode";
            ApplyModernTheme();
            ApplyControlPalette();
            pnlHeader.BackColor = colorFormBg;
            if (lblConnInfo != null) lblConnInfo.ForeColor = isDarkMode ? Color.White : Color.Black;
            lstChat.Invalidate();
            lstUsers.Invalidate();
        }

        // =========================================================================================
        // NETWORK OPERATIONS
        // =========================================================================================
        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                try
                {
                    string ip = txtIp.Text.Trim();
                    int port = int.Parse(txtPort.Text.Trim());
                    string username = txtUsername.Text.Trim();

                    if (string.IsNullOrEmpty(username))
                    {
                        MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    client = new TcpClient(ip, port);
                    stream = client.GetStream();
                    reader = new StreamReader(stream);
                    writer = new StreamWriter(stream);

                    writer.WriteLine(username);
                    writer.Flush();

                    isConnected = true;
                    btnConnect.Text = "Disconnect";
                    btnConnect.BackColor = Color.FromArgb(180, 50, 50);
                    txtMessage.Enabled = true;
                    btnSend.Enabled = true;
                    txtUsername.Enabled = false; txtIp.Enabled = false; txtPort.Enabled = false;

                    receiveThread = new Thread(ReceiveMessages); receiveThread.IsBackground = true; receiveThread.Start();

                    lblStatusDot.Text = "●"; lblStatusDot.ForeColor = colorSystemMessage;
                    lblStatusText.Text = $"Connected to {ip}:{port} as {username}"; lblStatusText.ForeColor = colorSystemMessage;

                    lstChat.Items.Add($"*** Connected to server as {username} ***");
                    lstChat.TopIndex = lstChat.Items.Count - 1;
                }
                catch (Exception ex) { MessageBox.Show("Connection Failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            else { DisconnectFromServer(); }
        }

        private void BtnSend_Click(object sender, EventArgs e) { SendMessage(); }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { SendMessage(); e.SuppressKeyPress = true; }
        }

        private void SendMessage()
        {
            if (isConnected && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                try
                {
                    string msg = txtMessage.Text.Trim();
                    string selectedRecipient = lstUsers.SelectedItem?.ToString() ?? "🌐 Everyone (Broadcast)";

                    if (selectedRecipient != "🌐 Everyone (Broadcast)")
                    {
                        msg = $"/pm {selectedRecipient} {msg}";
                    }

                    writer.WriteLine(msg);
                    writer.Flush();
                    txtMessage.Clear();
                }
                catch (Exception ex) { MessageBox.Show("Send Error: " + ex.Message); }
            }
        }

        private void ReceiveMessages()
        {
            while (isConnected)
            {
                try
                {
                    string message = reader.ReadLine();
                    if (message == null) break;

                    this.Invoke(new Action(() =>
                    {
                        if (message.StartsWith("SERVER: ") && message.EndsWith(" joined the chat room."))
                        {
                            string newUser = message.Replace("SERVER: ", "").Replace(" joined the chat room.", "").Trim();
                            if (newUser != txtUsername.Text && !lstUsers.Items.Contains(newUser))
                            {
                                lstUsers.Items.Add(newUser);
                            }
                        }
                        else if (message.StartsWith("SERVER: ") && message.EndsWith(" left the chat room."))
                        {
                            string leftUser = message.Replace("SERVER: ", "").Replace(" left the chat room.", "").Trim();
                            if (lstUsers.Items.Contains(leftUser))
                            {
                                lstUsers.Items.Remove(leftUser);
                                lstUsers.SelectedIndex = 0;
                            }
                        }

                        string timestampedMsg = $"[{DateTime.Now:HH:mm:ss}] {message}";
                        lstChat.Items.Add(timestampedMsg);
                        lstChat.TopIndex = lstChat.Items.Count - 1;
                        SystemSounds.Asterisk.Play();
                    }));
                }
                catch { break; }
            }
            this.Invoke(new Action(() => DisconnectFromServer()));
        }

        private void DisconnectFromServer()
        {
            isConnected = false;
            try { writer?.Close(); reader?.Close(); stream?.Close(); client?.Close(); } catch { }

            this.Invoke(new Action(() => {
                btnConnect.Text = "Connect";
                StyleAccentButton(btnConnect);
                txtMessage.Enabled = false;
                btnSend.Enabled = false;
                lstUsers.Items.Clear();
                lstUsers.Items.Add("🌐 Everyone (Broadcast)");
                lstUsers.SelectedIndex = 0;
                txtUsername.Enabled = true; txtIp.Enabled = true; txtPort.Enabled = true;

                lblStatusDot.Text = "○"; lblStatusDot.ForeColor = Color.DarkGray;
                lblStatusText.Text = "Offline"; lblStatusText.ForeColor = Color.DarkGray;

                lstChat.Items.Add("*** Disconnected from server ***");
                lstChat.TopIndex = lstChat.Items.Count - 1;
            }));
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { DisconnectFromServer(); base.OnFormClosing(e); }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AutoMissaoDiscord
{
    public class GameItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string executable { get; set; }
        public string windowTitle { get; set; }
        public int durationMinutes { get; set; }
        public bool isPriority { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(executable))
                return name ?? "";
            return string.Format("{0}  [{1}]", name, executable);
        }
    }

    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Verifica se está sendo executado como o Dummy Runner de um jogo
            // Argumentos esperados: --dummy "<windowTitle>" <totalSeconds> "<gameName>"
            if (args != null && args.Length >= 3 && args[0] == "--dummy")
            {
                string windowTitle = args[1];
                int totalSeconds = 900;
                int.TryParse(args[2], out totalSeconds);
                if (totalSeconds <= 0) totalSeconds = 900;
                string gameName = (args.Length >= 4) ? args[3] : windowTitle;

                Application.Run(new DummyGameForm(windowTitle, gameName, totalSeconds));
                return;
            }

            // Execução normal do Painel Gerenciador
            Application.Run(new MainManagerForm());
        }
    }

    // =========================================================================
    // JANELA DO JOGO FALSO (RENDERIZAÇÃO LEVE PARA STREAM NO DISCORD)
    // =========================================================================
    public class DummyGameForm : Form
    {
        private string gameName;
        private int totalSeconds;
        private int remainingSeconds;
        private Timer tickTimer;
        private Timer animTimer;
        private float animPhase = 0f;

        public DummyGameForm(string windowTitle, string gameName, int seconds)
        {
            this.Text = windowTitle;
            this.gameName = gameName;
            this.totalSeconds = seconds;
            this.remainingSeconds = seconds;

            this.ClientSize = new Size(840, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(17, 18, 20); // Fundo escuro
            this.DoubleBuffered = true;

            // Timer do contador (1 segundo)
            tickTimer = new Timer();
            tickTimer.Interval = 1000;
            tickTimer.Tick += new EventHandler(TickTimer_Tick);
            tickTimer.Start();

            // Timer de animação leve (15 FPS) para que o gancho de captura do Discord 
            // receba frames constantes e não congele a transmissão
            animTimer = new Timer();
            animTimer.Interval = 66; // ~15 FPS, quase 0% CPU
            animTimer.Tick += new EventHandler(AnimTimer_Tick);
            animTimer.Start();
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            animPhase += 0.08f;
            if (animPhase > (float)(Math.PI * 2))
                animPhase -= (float)(Math.PI * 2);

            this.Invalidate();
        }

        private void TickTimer_Tick(object sender, EventArgs e)
        {
            if (remainingSeconds > 0)
            {
                remainingSeconds--;
            }
            else
            {
                tickTimer.Stop();
                try { SystemSounds.Exclamation.Play(); } catch { }
                MessageBox.Show(
                    "Tempo de missão atingido com sucesso!\nVocê já pode encerrar o stream e resgatar sua recompensa no Discord.",
                    "Missão Concluída!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                this.Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            // Fundo degradê sutil
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                new Point(0, 0), new Point(0, h),
                Color.FromArgb(20, 22, 26), Color.FromArgb(12, 13, 15)))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            // Barra superior
            using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(32, 34, 38)))
            {
                g.FillRectangle(headerBrush, 0, 0, w, 50);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(45, 48, 55)))
            {
                g.DrawLine(borderPen, 0, 50, w, 50);
            }

            // Título do cabeçalho
            using (Font headerFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (SolidBrush fontBrush = new SolidBrush(Color.FromArgb(88, 101, 242))) // Discord Blurple
            {
                g.DrawString("🎮 DISCORD QUEST - SIMULADOR DE JANELA ATIVA", headerFont, fontBrush, 20, 15);
            }

            // Círculo indicador pulsante no cabeçalho
            int pulseAlpha = (int)(160 + 80 * Math.Sin(animPhase));
            if (pulseAlpha < 40) pulseAlpha = 40;
            if (pulseAlpha > 255) pulseAlpha = 255;
            using (SolidBrush liveBrush = new SolidBrush(Color.FromArgb(pulseAlpha, 35, 165, 90)))
            {
                g.FillEllipse(liveBrush, w - 100, 19, 12, 12);
            }
            using (Font liveFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush liveTextBrush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                g.DrawString("AO VIVO", liveFont, liveTextBrush, w - 82, 17);
            }

            // Nome do Jogo no Centro
            using (Font gameFont = new Font("Segoe UI", 26f, FontStyle.Bold))
            using (SolidBrush whiteBrush = new SolidBrush(Color.FromArgb(242, 243, 245)))
            {
                SizeF size = g.MeasureString(gameName, gameFont);
                g.DrawString(gameName, gameFont, whiteBrush, (w - size.Width) / 2, 110);
            }

            // Subtítulo
            using (Font subFont = new Font("Segoe UI", 11f, FontStyle.Regular))
            using (SolidBrush grayBrush = new SolidBrush(Color.FromArgb(148, 155, 164)))
            {
                string subText = "Processo ativo e renderizando para transmissão no Discord";
                SizeF size = g.MeasureString(subText, subFont);
                g.DrawString(subText, subFont, grayBrush, (w - size.Width) / 2, 165);
            }

            // Cronômetro Central
            int mins = remainingSeconds / 60;
            int secs = remainingSeconds % 60;
            string timeStr = string.Format("{0:00}:{1:00}", mins, secs);

            using (Font timerFont = new Font("Segoe UI", 44f, FontStyle.Bold))
            using (SolidBrush timerBrush = new SolidBrush(Color.FromArgb(88, 101, 242)))
            {
                SizeF size = g.MeasureString(timeStr, timerFont);
                g.DrawString(timeStr, timerFont, timerBrush, (w - size.Width) / 2, 210);
            }

            // Barra de Progresso
            int barWidth = 460;
            int barHeight = 12;
            int barX = (w - barWidth) / 2;
            int barY = 295;

            using (SolidBrush barBg = new SolidBrush(Color.FromArgb(43, 45, 49)))
            {
                g.FillRectangle(barBg, barX, barY, barWidth, barHeight);
            }

            float progress = 1.0f - ((float)remainingSeconds / (float)totalSeconds);
            if (progress < 0f) progress = 0f;
            if (progress > 1f) progress = 1f;

            int fillWidth = (int)(barWidth * progress);
            if (fillWidth > 0)
            {
                using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                    new Rectangle(barX, barY, fillWidth, barHeight),
                    Color.FromArgb(88, 101, 242), Color.FromArgb(35, 165, 90),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(fillBrush, barX, barY, fillWidth, barHeight);
                }
            }

            using (Pen barBorder = new Pen(Color.FromArgb(60, 63, 69)))
            {
                g.DrawRectangle(barBorder, barX, barY, barWidth, barHeight);
            }

            // Dica de Instrução
            using (Font tipFont = new Font("Segoe UI", 10f, FontStyle.Italic))
            using (SolidBrush tipBrush = new SolidBrush(Color.FromArgb(170, 175, 185)))
            {
                string tip1 = "Dica: Entre em um canal de voz no Discord e transmita ESTA JANELA.";
                string tip2 = "Você pode minimizar ou sobrepor outras janelas enquanto transmite.";
                SizeF s1 = g.MeasureString(tip1, tipFont);
                SizeF s2 = g.MeasureString(tip2, tipFont);
                g.DrawString(tip1, tipFont, tipBrush, (w - s1.Width) / 2, 345);
                g.DrawString(tip2, tipFont, tipBrush, (w - s2.Width) / 2, 370);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (tickTimer != null) tickTimer.Stop();
            if (animTimer != null) animTimer.Stop();
            base.OnFormClosing(e);
        }
    }

    // =========================================================================
    // FORMULÁRIO PRINCIPAL - GERENCIADOR DE MISSÕES
    // =========================================================================
    public class MainManagerForm : Form
    {
        private List<GameItem> allGames = new List<GameItem>();
        private List<GameItem> filteredGames = new List<GameItem>();
        private string jsonPath;
        private string fakesDir;

        private TextBox txtSearch;
        private Button btnClearSearch;
        private Label lblCount;
        private Button btnSyncDiscord;
        private ComboBox cbGames;
        private Label lblExeVal;
        private Label lblTitleVal;
        private Label lblDurationVal;
        private Button btnStart;
        private Button btnStop;
        private Button btnAddGame;
        private Button btnEditGame;
        private Button btnDeleteGame;
        private Button btnOpenFakes;
        private Button btnClearFakes;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Label lblTimer;
        private Timer monitorTimer;

        private Process activeProcess = null;
        private GameItem currentGame = null;
        private int missionTotalSec = 900;
        private int missionElapsedSec = 0;

        public MainManagerForm()
        {
            this.Text = "Auto Missão Discord - Gerenciador Completo";
            this.ClientSize = new Size(720, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 31, 34); // Discord Dark Background
            this.ForeColor = Color.FromArgb(242, 243, 245);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            jsonPath = Path.Combine(baseDir, "games.json");
            fakesDir = Path.Combine(baseDir, "fakes");

            if (!Directory.Exists(fakesDir))
            {
                try { Directory.CreateDirectory(fakesDir); } catch { }
            }

            BuildUI();
            LoadGames();

            monitorTimer = new Timer();
            monitorTimer.Interval = 1000;
            monitorTimer.Tick += new EventHandler(MonitorTimer_Tick);
        }

        private void BuildUI()
        {
            // Cabeçalho
            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 70;
            headerPanel.BackColor = Color.FromArgb(43, 45, 49);

            Label titleLabel = new Label();
            titleLabel.Text = "🎯 Auto Missão Discord";
            titleLabel.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(242, 243, 245);
            titleLabel.Location = new Point(20, 10);
            titleLabel.AutoSize = true;

            Label subTitleLabel = new Label();
            subTitleLabel.Text = "Simulador de jogos para completar Discord Quests com banco oficial do Discord";
            subTitleLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            subTitleLabel.ForeColor = Color.FromArgb(148, 155, 164);
            subTitleLabel.Location = new Point(23, 40);
            subTitleLabel.AutoSize = true;

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subTitleLabel);
            this.Controls.Add(headerPanel);

            // Container Central
            Panel mainPanel = new Panel();
            mainPanel.Location = new Point(20, 80);
            mainPanel.Size = new Size(680, 500);

            // 1. Barra de Busca e Filtro
            Label lblSearch = new Label();
            lblSearch.Text = "🔍 Buscar jogo ou executável:";
            lblSearch.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblSearch.ForeColor = Color.FromArgb(219, 222, 225);
            lblSearch.Location = new Point(0, 0);
            lblSearch.AutoSize = true;

            txtSearch = new TextBox();
            txtSearch.Location = new Point(0, 22);
            txtSearch.Size = new Size(390, 26);
            txtSearch.Font = new Font("Segoe UI", 10f);
            txtSearch.BackColor = Color.FromArgb(43, 45, 49);
            txtSearch.ForeColor = Color.White;
            txtSearch.TextChanged += new EventHandler(TxtSearch_TextChanged);

            btnClearSearch = CreateButton("✕", Color.FromArgb(60, 63, 69), new Point(395, 21), new Size(28, 28));
            btnClearSearch.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnClearSearch.Click += new EventHandler((s, e) => { txtSearch.Text = ""; txtSearch.Focus(); });

            lblCount = new Label();
            lblCount.Text = "Carregando...";
            lblCount.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblCount.ForeColor = Color.FromArgb(148, 155, 164);
            lblCount.Location = new Point(430, 26);
            lblCount.Size = new Size(110, 20);

            btnSyncDiscord = CreateButton("🔄 Sincronizar Discord", Color.FromArgb(88, 101, 242), new Point(545, 21), new Size(135, 28));
            btnSyncDiscord.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSyncDiscord.Click += new EventHandler(BtnSyncDiscord_Click);

            mainPanel.Controls.Add(lblSearch);
            mainPanel.Controls.Add(txtSearch);
            mainPanel.Controls.Add(btnClearSearch);
            mainPanel.Controls.Add(lblCount);
            mainPanel.Controls.Add(btnSyncDiscord);

            // 2. Seletor do Jogo
            Label lblSelect = new Label();
            lblSelect.Text = "Selecione o jogo correspondente:";
            lblSelect.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblSelect.ForeColor = Color.FromArgb(219, 222, 225);
            lblSelect.Location = new Point(0, 55);
            lblSelect.AutoSize = true;

            cbGames = new ComboBox();
            cbGames.DropDownStyle = ComboBoxStyle.DropDownList;
            cbGames.Location = new Point(0, 77);
            cbGames.Size = new Size(460, 28);
            cbGames.Font = new Font("Segoe UI", 10f);
            cbGames.BackColor = Color.FromArgb(43, 45, 49);
            cbGames.ForeColor = Color.White;
            cbGames.SelectedIndexChanged += new EventHandler(CbGames_SelectedIndexChanged);

            btnAddGame = CreateButton("➕ Novo Jogo", Color.FromArgb(88, 101, 242), new Point(470, 76), new Size(100, 30));
            btnAddGame.Click += new EventHandler(BtnAddGame_Click);

            btnDeleteGame = CreateButton("🗑️ Excluir", Color.FromArgb(78, 80, 88), new Point(580, 76), new Size(100, 30));
            btnDeleteGame.Click += new EventHandler(BtnDeleteGame_Click);

            mainPanel.Controls.Add(lblSelect);
            mainPanel.Controls.Add(cbGames);
            mainPanel.Controls.Add(btnAddGame);
            mainPanel.Controls.Add(btnDeleteGame);

            // 3. Cartão de Informações do Jogo Selecionado
            Panel infoCard = new Panel();
            infoCard.Location = new Point(0, 116);
            infoCard.Size = new Size(680, 95);
            infoCard.BackColor = Color.FromArgb(43, 45, 49);
            infoCard.BorderStyle = BorderStyle.FixedSingle;

            Label lblExeTitle = new Label();
            lblExeTitle.Text = "Executável Detectado:";
            lblExeTitle.ForeColor = Color.FromArgb(148, 155, 164);
            lblExeTitle.Location = new Point(15, 12);
            lblExeTitle.AutoSize = true;

            lblExeVal = new Label();
            lblExeVal.Text = "-";
            lblExeVal.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblExeVal.ForeColor = Color.FromArgb(0, 168, 252);
            lblExeVal.Location = new Point(160, 12);
            lblExeVal.AutoSize = true;

            Label lblTitleText = new Label();
            lblTitleText.Text = "Título da Janela:";
            lblTitleText.ForeColor = Color.FromArgb(148, 155, 164);
            lblTitleText.Location = new Point(15, 38);
            lblTitleText.AutoSize = true;

            lblTitleVal = new Label();
            lblTitleVal.Text = "-";
            lblTitleVal.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblTitleVal.ForeColor = Color.FromArgb(242, 243, 245);
            lblTitleVal.Location = new Point(160, 38);
            lblTitleVal.AutoSize = true;

            Label lblDurText = new Label();
            lblDurText.Text = "Tempo de Missão:";
            lblDurText.ForeColor = Color.FromArgb(148, 155, 164);
            lblDurText.Location = new Point(15, 64);
            lblDurText.AutoSize = true;

            lblDurationVal = new Label();
            lblDurationVal.Text = "15 minutos";
            lblDurationVal.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblDurationVal.ForeColor = Color.FromArgb(35, 165, 90);
            lblDurationVal.Location = new Point(160, 64);
            lblDurationVal.AutoSize = true;

            btnEditGame = CreateButton("✏️ Editar", Color.FromArgb(60, 63, 69), new Point(570, 32), new Size(95, 28));
            btnEditGame.Click += new EventHandler(BtnEditGame_Click);

            infoCard.Controls.Add(lblExeTitle);
            infoCard.Controls.Add(lblExeVal);
            infoCard.Controls.Add(lblTitleText);
            infoCard.Controls.Add(lblTitleVal);
            infoCard.Controls.Add(lblDurText);
            infoCard.Controls.Add(lblDurationVal);
            infoCard.Controls.Add(btnEditGame);
            mainPanel.Controls.Add(infoCard);

            // 4. Botões de Ação Principal
            btnStart = CreateButton("▶  INICIAR MISSÃO", Color.FromArgb(35, 165, 90), new Point(0, 222), new Size(335, 45));
            btnStart.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnStart.Click += new EventHandler(BtnStart_Click);

            btnStop = CreateButton("⏹  PARAR MISSÃO", Color.FromArgb(218, 55, 60), new Point(345, 222), new Size(335, 45));
            btnStop.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnStop.Enabled = false;
            btnStop.Click += new EventHandler(BtnStop_Click);

            mainPanel.Controls.Add(btnStart);
            mainPanel.Controls.Add(btnStop);

            // 5. Painel de Status e Progresso
            Panel statusPanel = new Panel();
            statusPanel.Location = new Point(0, 277);
            statusPanel.Size = new Size(680, 95);
            statusPanel.BackColor = Color.FromArgb(43, 45, 49);
            statusPanel.BorderStyle = BorderStyle.FixedSingle;

            lblStatus = new Label();
            lblStatus.Text = "Status: Pronto para iniciar.";
            lblStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(219, 222, 225);
            lblStatus.Location = new Point(15, 12);
            lblStatus.Size = new Size(460, 22);

            lblTimer = new Label();
            lblTimer.Text = "00:00 / 15:00";
            lblTimer.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTimer.ForeColor = Color.FromArgb(88, 101, 242);
            lblTimer.TextAlign = ContentAlignment.TopRight;
            lblTimer.Location = new Point(485, 12);
            lblTimer.Size = new Size(180, 22);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(15, 45);
            progressBar.Size = new Size(650, 18);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;

            Label lblHintStatus = new Label();
            lblHintStatus.Text = "O Discord reconhecerá o processo automaticamente enquanto estiver rodando.";
            lblHintStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblHintStatus.ForeColor = Color.FromArgb(148, 155, 164);
            lblHintStatus.Location = new Point(15, 68);
            lblHintStatus.AutoSize = true;

            statusPanel.Controls.Add(lblStatus);
            statusPanel.Controls.Add(lblTimer);
            statusPanel.Controls.Add(progressBar);
            statusPanel.Controls.Add(lblHintStatus);
            mainPanel.Controls.Add(statusPanel);

            // 6. Rodapé com Guia e Ferramentas
            btnOpenFakes = CreateButton("📂 Abrir Pasta dos Fakes", Color.FromArgb(60, 63, 69), new Point(0, 382), new Size(210, 30));
            btnOpenFakes.Click += new EventHandler(BtnOpenFakes_Click);

            btnClearFakes = CreateButton("🧹 Limpar Arquivos Temporários", Color.FromArgb(60, 63, 69), new Point(220, 382), new Size(230, 30));
            btnClearFakes.Click += new EventHandler(BtnClearFakes_Click);

            Label guideLabel = new Label();
            guideLabel.Text = "Passo a passo:\n1. Digite o nome do jogo no campo de busca para encontrá-lo rapidamente.\n2. Clique em 'Iniciar Missão' e transmita a janela aberta no canal de voz do Discord.\n3. Pela conta secundária, assista à transmissão em tela cheia/foco até concluir os 15 min.";
            guideLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            guideLabel.ForeColor = Color.FromArgb(148, 155, 164);
            guideLabel.Location = new Point(0, 420);
            guideLabel.Size = new Size(680, 65);

            mainPanel.Controls.Add(btnOpenFakes);
            mainPanel.Controls.Add(btnClearFakes);
            mainPanel.Controls.Add(guideLabel);

            this.Controls.Add(mainPanel);
        }

        private Button CreateButton(string text, Color backColor, Point loc, Size size)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = loc;
            btn.Size = size;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void LoadGames()
        {
            allGames.Clear();
            if (File.Exists(jsonPath))
            {
                try
                {
                    string content = File.ReadAllText(jsonPath);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = int.MaxValue;
                    allGames = serializer.Deserialize<List<GameItem>>(content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao carregar games.json: " + ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (allGames == null || allGames.Count == 0)
            {
                allGames = GetDefaultGames();
                SaveGames();
            }

            ApplyFilter();
        }

        private void SaveGames()
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                string json = serializer.Serialize(allGames);
                File.WriteAllText(jsonPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar games.json: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string query = (txtSearch.Text != null) ? txtSearch.Text.Trim() : "";
            filteredGames.Clear();

            if (string.IsNullOrEmpty(query))
            {
                filteredGames.AddRange(allGames);
            }
            else
            {
                for (int i = 0; i < allGames.Count; i++)
                {
                    GameItem g = allGames[i];
                    bool matchName = (g.name != null && g.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
                    bool matchExe = (g.executable != null && g.executable.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
                    bool matchTitle = (g.windowTitle != null && g.windowTitle.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (matchName || matchExe || matchTitle)
                    {
                        filteredGames.Add(g);
                    }
                }
            }

            cbGames.BeginUpdate();
            cbGames.Items.Clear();
            for (int i = 0; i < filteredGames.Count; i++)
            {
                cbGames.Items.Add(filteredGames[i]);
            }
            cbGames.EndUpdate();

            if (cbGames.Items.Count > 0)
            {
                cbGames.SelectedIndex = 0;
            }
            else
            {
                lblExeVal.Text = "-";
                lblTitleVal.Text = "-";
                lblDurationVal.Text = "-";
            }

            if (string.IsNullOrEmpty(query))
            {
                lblCount.Text = string.Format("{0:N0} jogos", allGames.Count);
            }
            else
            {
                lblCount.Text = string.Format("{0:N0} de {1:N0}", filteredGames.Count, allGames.Count);
            }
        }

        private void BtnSyncDiscord_Click(object sender, EventArgs e)
        {
            btnSyncDiscord.Enabled = false;
            btnSyncDiscord.Text = "Baixando...";
            lblStatus.Text = "Status: Conectando à API oficial do Discord...";

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    // Habilita TLS 1.2
                    System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)3072;
                    System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("https://discord.com/api/v9/applications/detectable");
                    req.UserAgent = "Mozilla/5.0";
                    req.Timeout = 30000;

                    string jsonResp;
                    using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse())
                    using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    {
                        jsonResp = reader.ReadToEnd();
                    }

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = int.MaxValue;
                    object[] rawApps = serializer.Deserialize<object[]>(jsonResp);

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        try
                        {
                            MergeDiscordApps(rawApps);
                            SaveGames();
                            ApplyFilter();
                            lblStatus.Text = string.Format("Status: Sincronização concluída! {0:N0} jogos cadastrados.", allGames.Count);
                            MessageBox.Show(string.Format("Banco do Discord sincronizado com sucesso!\nTotal de jogos Windows disponíveis: {0:N0}", allGames.Count), "Sincronização Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        finally
                        {
                            btnSyncDiscord.Enabled = true;
                            btnSyncDiscord.Text = "🔄 Sincronizar Discord";
                        }
                    });
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        btnSyncDiscord.Enabled = true;
                        btnSyncDiscord.Text = "🔄 Sincronizar Discord";
                        lblStatus.Text = "Status: Falha ao sincronizar.";
                        MessageBox.Show("Erro ao conectar ao Discord:\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    });
                }
            });
        }

        private void MergeDiscordApps(object[] rawApps)
        {
            if (rawApps == null) return;

            Dictionary<string, bool> existingPairs = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allGames.Count; i++)
            {
                string key = string.Format("{0}|{1}", allGames[i].name, allGames[i].executable);
                existingPairs[key] = true;
            }

            int addedCount = 0;
            for (int i = 0; i < rawApps.Length; i++)
            {
                Dictionary<string, object> app = rawApps[i] as Dictionary<string, object>;
                if (app == null) continue;

                string name = app.ContainsKey("name") ? Convert.ToString(app["name"]).Trim() : "";
                string appId = app.ContainsKey("id") ? Convert.ToString(app["id"]) : "";
                if (string.IsNullOrEmpty(name)) continue;

                object exesObj;
                if (!app.TryGetValue("executables", out exesObj) || exesObj == null) continue;

                object[] exesList = exesObj as object[];
                if (exesList == null) continue;

                for (int j = 0; j < exesList.Length; j++)
                {
                    Dictionary<string, object> exeMap = exesList[j] as Dictionary<string, object>;
                    if (exeMap == null) continue;

                    string os = exeMap.ContainsKey("os") ? Convert.ToString(exeMap["os"]) : "";
                    if (os != "win32") continue;

                    string exePath = exeMap.ContainsKey("name") ? Convert.ToString(exeMap["name"]).Trim() : "";
                    if (string.IsNullOrEmpty(exePath)) continue;

                    string key = string.Format("{0}|{1}", name, exePath);
                    if (!existingPairs.ContainsKey(key))
                    {
                        existingPairs[key] = true;
                        allGames.Add(new GameItem
                        {
                            id = string.Format("{0}_{1}", appId, j),
                            name = name,
                            executable = exePath,
                            windowTitle = name,
                            durationMinutes = 15,
                            isPriority = false
                        });
                        addedCount++;
                    }
                }
            }
        }

        private List<GameItem> GetDefaultGames()
        {
            List<GameItem> list = new List<GameItem>();
            list.Add(new GameItem { id = "cod_mw4", name = "⭐ Call of Duty: Modern Warfare 4", executable = "cod26-cod.exe", windowTitle = "Call of Duty: Modern Warfare 4", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "cod_mw3", name = "⭐ Call of Duty: Modern Warfare III", executable = "cod23/cod23-cod.exe", windowTitle = "Call of Duty: Modern Warfare III", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "cod_general", name = "⭐ Call of Duty (HQ / Geral)", executable = "cod.exe", windowTitle = "Call of Duty®", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "fc25", name = "⭐ EA SPORTS FC 25", executable = "ea sports fc 25/fc25.exe", windowTitle = "EA SPORTS FC 25", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "clean_crew_roblox", name = "⭐ Clean Crew (Roblox)", executable = "RobloxPlayerBeta.exe", windowTitle = "Roblox", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "nekome_demo", name = "⭐ Nekome Demo", executable = "nekomedemo.exe", windowTitle = "Nekome Demo", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "endfield", name = "⭐ Arknights: Endfield", executable = "endfield.exe", windowTitle = "Arknights: Endfield", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "genshin", name = "⭐ Genshin Impact", executable = "GenshinImpact.exe", windowTitle = "Genshin Impact", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "starrail", name = "⭐ Honkai: Star Rail", executable = "games/starrail.exe", windowTitle = "Honkai: Star Rail", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "zenless", name = "⭐ Zenless Zone Zero", executable = "zenlesszonezero game/zenlesszonezero.exe", windowTitle = "ZenlessZoneZero", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "fortnite", name = "⭐ Fortnite", executable = "FortniteClient-Win64-Shipping.exe", windowTitle = "Fortnite", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "valorant", name = "⭐ Valorant", executable = "win64/valorant-win64-shipping.exe", windowTitle = "VALORANT", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "marvel_rivals", name = "⭐ Marvel Rivals", executable = "win64/marvel-win64-shipping.exe", windowTitle = "Marvel Rivals", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "first_descendant", name = "⭐ The First Descendant", executable = "m1-win64-shipping.exe", windowTitle = "The First Descendant", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "overwatch2", name = "⭐ Overwatch 2", executable = "overwatch.exe", windowTitle = "Overwatch", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "apex", name = "⭐ Apex Legends", executable = "apex/r5apex.exe", windowTitle = "Apex Legends", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "lol", name = "⭐ League of Legends", executable = "league of legends.exe", windowTitle = "League of Legends (TM) Client", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "dbd", name = "⭐ Dead by Daylight", executable = "deadbydaylight-win64-shipping.exe", windowTitle = "DeadByDaylight", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "warframe", name = "⭐ Warframe", executable = "warframe.x64.exe", windowTitle = "Warframe", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "helldivers2", name = "⭐ Helldivers 2", executable = "bin/helldivers2.exe", windowTitle = "HELLDIVERS™ 2", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "wuthering", name = "⭐ Wuthering Waves", executable = "wuthering waves game/client/binaries/win64/client-win64-shipping.exe", windowTitle = "Wuthering Waves", durationMinutes = 15, isPriority = true });
            list.Add(new GameItem { id = "monster_hunter_wilds", name = "⭐ Monster Hunter Wilds", executable = "monsterhunterwilds/monsterhunterwilds.exe", windowTitle = "Monster Hunter Wilds", durationMinutes = 15, isPriority = true });
            return list;
        }

        private void CbGames_SelectedIndexChanged(object sender, EventArgs e)
        {
            GameItem selected = cbGames.SelectedItem as GameItem;
            if (selected != null)
            {
                lblExeVal.Text = selected.executable;
                lblTitleVal.Text = string.IsNullOrEmpty(selected.windowTitle) ? selected.name : selected.windowTitle;
                lblDurationVal.Text = string.Format("{0} minutos", selected.durationMinutes);
                lblTimer.Text = string.Format("00:00 / {0:00}:00", selected.durationMinutes);
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            GameItem selected = cbGames.SelectedItem as GameItem;
            if (selected == null) return;

            string currentExe = Application.ExecutablePath;
            if (!Directory.Exists(fakesDir))
            {
                Directory.CreateDirectory(fakesDir);
            }

            string safeRelPath = selected.executable.Replace('/', Path.DirectorySeparatorChar);
            string targetExe = Path.Combine(fakesDir, safeRelPath);
            string targetDir = Path.GetDirectoryName(targetExe);

            try
            {
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Se já existir, sobrescreve para garantir que esteja atualizado
                if (!File.Exists(targetExe) || new FileInfo(targetExe).Length != new FileInfo(currentExe).Length)
                {
                    File.Copy(currentExe, targetExe, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível preparar o executável:\n" + ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (!File.Exists(targetExe))
            {
                MessageBox.Show("Arquivo de destino não encontrado: " + targetExe, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                int durationSec = (selected.durationMinutes > 0 ? selected.durationMinutes : 15) * 60;
                string wTitle = string.IsNullOrEmpty(selected.windowTitle) ? selected.name : selected.windowTitle;

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = targetExe;
                psi.Arguments = string.Format("--dummy \"{0}\" {1} \"{2}\"", wTitle, durationSec, selected.name);
                psi.WorkingDirectory = targetDir;

                activeProcess = Process.Start(psi);
                currentGame = selected;
                missionTotalSec = durationSec;
                missionElapsedSec = 0;

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                cbGames.Enabled = false;
                txtSearch.Enabled = false;
                btnClearSearch.Enabled = false;
                btnSyncDiscord.Enabled = false;
                btnAddGame.Enabled = false;
                btnEditGame.Enabled = false;
                btnDeleteGame.Enabled = false;

                lblStatus.Text = string.Format("🟢 Transmitindo: {0}", selected.name);
                lblStatus.ForeColor = Color.FromArgb(35, 165, 90);

                monitorTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Falha ao iniciar o processo:\n" + ex.Message, "Erro ao Iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            if (activeProcess == null || activeProcess.HasExited)
            {
                StopMonitoring(activeProcess != null && missionElapsedSec >= missionTotalSec);
                return;
            }

            missionElapsedSec++;
            int remaining = missionTotalSec - missionElapsedSec;
            if (remaining < 0) remaining = 0;

            int curMins = missionElapsedSec / 60;
            int curSecs = missionElapsedSec % 60;
            int totMins = missionTotalSec / 60;
            int totSecs = missionTotalSec % 60;

            lblTimer.Text = string.Format("{0:00}:{1:00} / {2:00}:{3:00}", curMins, curSecs, totMins, totSecs);

            int pct = (int)((missionElapsedSec / (float)missionTotalSec) * 100);
            if (pct > 100) pct = 100;
            progressBar.Value = pct;

            if (missionElapsedSec >= missionTotalSec)
            {
                StopMonitoring(true);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (activeProcess != null && !activeProcess.HasExited)
            {
                try
                {
                    activeProcess.Kill();
                }
                catch { }
            }
            StopMonitoring(false);
        }

        private void StopMonitoring(bool completed)
        {
            monitorTimer.Stop();
            activeProcess = null;

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            cbGames.Enabled = true;
            txtSearch.Enabled = true;
            btnClearSearch.Enabled = true;
            btnSyncDiscord.Enabled = true;
            btnAddGame.Enabled = true;
            btnEditGame.Enabled = true;
            btnDeleteGame.Enabled = true;

            if (completed)
            {
                lblStatus.Text = "🎉 Missão Concluída! Resgate no Discord.";
                lblStatus.ForeColor = Color.FromArgb(88, 101, 242);
                progressBar.Value = 100;
                try { SystemSounds.Asterisk.Play(); } catch { }
            }
            else
            {
                lblStatus.Text = "Status: Missão parada ou processo fechado.";
                lblStatus.ForeColor = Color.FromArgb(219, 222, 225);
            }
        }

        private void BtnAddGame_Click(object sender, EventArgs e)
        {
            using (GameEditorForm dlg = new GameEditorForm(null))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    allGames.Insert(0, dlg.Game);
                    SaveGames();
                    ApplyFilter();
                    cbGames.SelectedItem = dlg.Game;
                }
            }
        }

        private void BtnEditGame_Click(object sender, EventArgs e)
        {
            GameItem selected = cbGames.SelectedItem as GameItem;
            if (selected == null) return;

            using (GameEditorForm dlg = new GameEditorForm(selected))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    selected.name = dlg.Game.name;
                    selected.executable = dlg.Game.executable;
                    selected.windowTitle = dlg.Game.windowTitle;
                    selected.durationMinutes = dlg.Game.durationMinutes;
                    SaveGames();

                    int idx = cbGames.SelectedIndex;
                    cbGames.Items[idx] = selected;
                    CbGames_SelectedIndexChanged(null, null);
                }
            }
        }

        private void BtnDeleteGame_Click(object sender, EventArgs e)
        {
            GameItem selected = cbGames.SelectedItem as GameItem;
            if (selected == null) return;

            DialogResult res = MessageBox.Show(
                string.Format("Deseja realmente remover '{0}' da lista de jogos?", selected.name),
                "Confirmar Exclusão",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                allGames.Remove(selected);
                SaveGames();
                ApplyFilter();
            }
        }

        private void BtnOpenFakes_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(fakesDir)) Directory.CreateDirectory(fakesDir);
                Process.Start("explorer.exe", fakesDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível abrir a pasta: " + ex.Message);
            }
        }

        private void BtnClearFakes_Click(object sender, EventArgs e)
        {
            if (activeProcess != null && !activeProcess.HasExited)
            {
                MessageBox.Show("Pare a missão atual antes de limpar os executáveis.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int count = 0;
            if (Directory.Exists(fakesDir))
            {
                string[] files = Directory.GetFiles(fakesDir, "*.exe", SearchOption.AllDirectories);
                foreach (string f in files)
                {
                    try
                    {
                        File.Delete(f);
                        count++;
                    }
                    catch { }
                }
            }

            MessageBox.Show(string.Format("{0} executável(is) temporário(s) removido(s) da pasta fakes.", count), "Limpeza Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (activeProcess != null && !activeProcess.HasExited)
            {
                DialogResult res = MessageBox.Show(
                    "Uma missão ainda está em execução. Deseja encerrar o jogo falso antes de sair?",
                    "Missão em Andamento",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (res == DialogResult.Yes)
                {
                    try { activeProcess.Kill(); } catch { }
                }
            }

            base.OnFormClosing(e);
        }
    }

    // =========================================================================
    // MODAL DE CADASTRO / EDIÇÃO DE JOGOS
    // =========================================================================
    public class GameEditorForm : Form
    {
        public GameItem Game { get; private set; }

        private TextBox txtName;
        private TextBox txtExe;
        private TextBox txtTitle;
        private NumericUpDown numDuration;
        private Button btnSave;
        private Button btnCancel;

        public GameEditorForm(GameItem existing)
        {
            this.Text = (existing == null) ? "Adicionar Novo Jogo para Missão" : "Editar Jogo";
            this.ClientSize = new Size(460, 310);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 31, 34);
            this.ForeColor = Color.FromArgb(242, 243, 245);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            BuildUI();

            if (existing != null)
            {
                txtName.Text = existing.name;
                txtExe.Text = existing.executable;
                txtTitle.Text = existing.windowTitle;
                numDuration.Value = (existing.durationMinutes > 0) ? existing.durationMinutes : 15;
                this.Game = existing;
            }
            else
            {
                this.Game = new GameItem();
                this.Game.id = Guid.NewGuid().ToString("N");
                this.Game.isPriority = true;
            }
        }

        private void BuildUI()
        {
            int y = 20;

            Label lblName = new Label();
            lblName.Text = "Nome de Exibição (ex: Genshin Impact):";
            lblName.Location = new Point(20, y);
            lblName.AutoSize = true;
            y += 24;

            txtName = new TextBox();
            txtName.Location = new Point(20, y);
            txtName.Size = new Size(420, 26);
            txtName.BackColor = Color.FromArgb(43, 45, 49);
            txtName.ForeColor = Color.White;
            y += 35;

            Label lblExe = new Label();
            lblExe.Text = "Nome/Caminho Exato do Executável (.exe):";
            lblExe.Location = new Point(20, y);
            lblExe.AutoSize = true;
            y += 24;

            txtExe = new TextBox();
            txtExe.Location = new Point(20, y);
            txtExe.Size = new Size(420, 26);
            txtExe.BackColor = Color.FromArgb(43, 45, 49);
            txtExe.ForeColor = Color.White;
            y += 35;

            Label lblTitle = new Label();
            lblTitle.Text = "Título da Janela (Opcional - padrão é o Nome):";
            lblTitle.Location = new Point(20, y);
            lblTitle.AutoSize = true;
            y += 24;

            txtTitle = new TextBox();
            txtTitle.Location = new Point(20, y);
            txtTitle.Size = new Size(420, 26);
            txtTitle.BackColor = Color.FromArgb(43, 45, 49);
            txtTitle.ForeColor = Color.White;
            y += 35;

            Label lblDur = new Label();
            lblDur.Text = "Duração da Missão (em minutos):";
            lblDur.Location = new Point(20, y);
            lblDur.AutoSize = true;

            numDuration = new NumericUpDown();
            numDuration.Location = new Point(250, y - 2);
            numDuration.Size = new Size(80, 26);
            numDuration.Minimum = 1;
            numDuration.Maximum = 120;
            numDuration.Value = 15;
            numDuration.BackColor = Color.FromArgb(43, 45, 49);
            numDuration.ForeColor = Color.White;
            y += 45;

            btnSave = new Button();
            btnSave.Text = "Salvar Jogo";
            btnSave.Location = new Point(230, y);
            btnSave.Size = new Size(110, 32);
            btnSave.BackColor = Color.FromArgb(35, 165, 90);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += new EventHandler(BtnSave_Click);

            btnCancel = new Button();
            btnCancel.Text = "Cancelar";
            btnCancel.Location = new Point(350, y);
            btnCancel.Size = new Size(90, 32);
            btnCancel.BackColor = Color.FromArgb(78, 80, 88);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += new EventHandler((s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); });

            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblExe);
            this.Controls.Add(txtExe);
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtTitle);
            this.Controls.Add(lblDur);
            this.Controls.Add(numDuration);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string exe = txtExe.Text.Trim();
            string title = txtTitle.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Informe o nome do jogo.", "Campo Obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(exe))
            {
                MessageBox.Show("Informe o executável (ex: jogo.exe ou subpasta/jogo.exe).", "Campo Obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                exe += ".exe";
            }

            if (string.IsNullOrEmpty(title))
            {
                title = name;
            }

            Game.name = name;
            Game.executable = exe;
            Game.windowTitle = title;
            Game.durationMinutes = (int)numDuration.Value;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

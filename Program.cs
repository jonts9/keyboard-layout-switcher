using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.Management;

class KeyboardLayoutSwitcher : ApplicationContext
{
    // ── Configuração ──────────────────────────────────────────────
    const string DEVICE_VID_PID   = "VID_046D&PID_C548"; // Receptor Bolt Logitech
    const int    POLL_INTERVAL_MS = 3000;                 // verificar a cada 3 segundos

    const uint KLID_INTL  = 0xF0010416; // POR-INTL  (teclado mecânico americano)
    const uint KLID_ABNT2 = 0xF0100416; // POR-PTB2  (teclado notebook ABNT2)
    // ─────────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("user32.dll")]
    static extern IntPtr GetKeyboardLayout(uint idThread);

    const byte VK_LWIN         = 0x5B;
    const byte VK_SPACE        = 0x20;
    const uint KEYEVENTF_KEYUP = 0x0002;

    NotifyIcon trayIcon;
    System.Threading.Timer timer;
    bool? lastState  = null;
    bool  paused     = false;
    SynchronizationContext uiContext;

    ToolStripMenuItem itemStatus;
    ToolStripMenuItem itemPause;

    public KeyboardLayoutSwitcher()
    {
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        trayIcon = new NotifyIcon()
        {
            Icon = CreateIcon(false, false),
            Text = "KeySwitch — iniciando...",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        timer = new System.Threading.Timer(Check, null, 500, POLL_INTERVAL_MS);
    }

    // ── Verifica estado do dispositivo e corrige layout ───────────
    void Check(object? _)
    {
        bool boltPresent = IsBoltPresent();

        // Se pausado, só atualiza o status no menu — não altera o layout
        if (!paused)
            ApplyLayout(boltPresent);

        // Notifica e atualiza ícone apenas quando o estado do hub muda
        if (boltPresent == lastState) return;
        lastState = boltPresent;

        string nome = boltPresent
            ? "POR-INTL (teclado externo)"
            : "POR-PTB2 / ABNT2 (teclado notebook)";

        uiContext.Post(_ =>
        {
            trayIcon.Icon = CreateIcon(boltPresent, paused);
            trayIcon.Text = paused
                ? $"KeySwitch — PAUSADO ({(boltPresent ? "INTL" : "ABNT2")} detectado)"
                : $"KeySwitch — {nome}";
            if (!paused)
                trayIcon.ShowBalloonTip(3000, "Layout alterado", nome, ToolTipIcon.Info);
        }, null);
    }

    // ── Detecta receptor Bolt ativo via WMI ──────────────────────
    bool IsBoltPresent()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Status FROM Win32_PnPEntity WHERE DeviceID LIKE '%" + DEVICE_VID_PID + "%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                string status = obj["Status"]?.ToString() ?? "";
                if (status.Equals("OK", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { /* ignora erros WMI transitórios */ }

        return false;
    }

    // ── Lê o layout ativo na janela em foco ───────────────────────
    uint GetCurrentLayoutId()
    {
        IntPtr hwnd   = GetForegroundWindow();
        uint   thread = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
        IntPtr hkl    = GetKeyboardLayout(thread);
        return (uint)(hkl.ToInt32());
    }

    // ── Aplica layout via Win+Space apenas se necessário ─────────
    void ApplyLayout(bool wantExternal)
    {
        uint wanted  = wantExternal ? KLID_INTL : KLID_ABNT2;
        uint current = GetCurrentLayoutId();

        if (current == wanted) return;

        keybd_event(VK_LWIN,  0, 0,               UIntPtr.Zero);
        keybd_event(VK_SPACE, 0, 0,               UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(VK_SPACE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN,  0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    // ── Menu da bandeja ───────────────────────────────────────────
    ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        itemStatus = new ToolStripMenuItem("Status: verificando...") { Enabled = false };
        menu.Items.Add(itemStatus);
        menu.Items.Add(new ToolStripSeparator());

        itemPause = new ToolStripMenuItem("Pausar troca automática");
        itemPause.Click += (s, e) =>
        {
            paused = !paused;
            itemPause.Checked = paused;
            itemPause.Text = paused ? "Retomar troca automática" : "Pausar troca automática";

            bool present = lastState == true;
            trayIcon.Icon = CreateIcon(present, paused);
            trayIcon.Text = paused
                ? $"KeySwitch — PAUSADO ({(present ? "INTL" : "ABNT2")} detectado)"
                : $"KeySwitch — {(present ? "POR-INTL (teclado externo)" : "POR-PTB2 / ABNT2 (teclado notebook)")}";

            trayIcon.ShowBalloonTip(2000,
                paused ? "KeySwitch pausado" : "KeySwitch ativo",
                paused ? "Troca automática desativada." : "Troca automática reativada.",
                ToolTipIcon.Info);
        };
        menu.Items.Add(itemPause);
        menu.Items.Add(new ToolStripSeparator());

        var itemStartup = new ToolStripMenuItem("Iniciar com o Windows");
        itemStartup.Checked = IsInStartup();
        itemStartup.Click += (s, e) =>
        {
            if (itemStartup.Checked) RemoveFromStartup();
            else AddToStartup();
            itemStartup.Checked = !itemStartup.Checked;
        };
        menu.Items.Add(itemStartup);
        menu.Items.Add(new ToolStripSeparator());

        var itemExit = new ToolStripMenuItem("Fechar");
        itemExit.Click += (s, e) =>
        {
            timer.Dispose();
            trayIcon.Visible = false;
            Application.Exit();
        };
        menu.Items.Add(itemExit);

        menu.Opening += (s, e) =>
        {
            bool present = lastState == true;
            string layoutAtivo = present ? "INTL" : "ABNT2";
            itemStatus.Text = paused
                ? $"⏸  Pausado — {layoutAtivo} detectado"
                : (present ? "⌨  Teclado externo: conectado (INTL)" : "⌨  Teclado externo: desconectado (ABNT2)");
        };

        return menu;
    }

    // ── Startup no registro ───────────────────────────────────────
    const string RUN_KEY  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    const string APP_NAME = "KeyboardLayoutSwitcher";

    bool IsInStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY);
        return key?.GetValue(APP_NAME) != null;
    }

    void AddToStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
        key?.SetValue(APP_NAME, $"\"{Application.ExecutablePath}\"");
    }

    void RemoveFromStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
        key?.DeleteValue(APP_NAME, false);
    }

    // ── Ícone: azul=INTL, cinza=ABNT2, laranja=pausado ───────────
    Icon CreateIcon(bool isExternal, bool isPaused)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        Color bg = isPaused
            ? Color.FromArgb(200, 120, 0)       // laranja = pausado
            : isExternal
                ? Color.FromArgb(30, 140, 255)  // azul    = INTL
                : Color.FromArgb(80, 80, 80);   // cinza   = ABNT2

        using var brush = new SolidBrush(bg);
        g.FillEllipse(brush, 0, 0, 15, 15);

        string letra = isPaused ? "P" : "K";
        using var font      = new Font("Arial", 7, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(letra, font, textBrush, isPaused ? 2.5f : 2f, 3f);

        return Icon.FromHandle(bmp.GetHicon());
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var mutex = new Mutex(true, "KeyboardLayoutSwitcher_Mutex", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("KeyboardLayoutSwitcher já está rodando.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new KeyboardLayoutSwitcher());
    }
}
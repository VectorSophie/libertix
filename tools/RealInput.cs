// Real-window input check for ResizeDisk (evidence only, not part of any PR).
// Opens the page in a topmost window and drives it with OS-injected mouse and keyboard input.
// Usage: RealInput.exe <out-prefix>
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Libertix.Models;
using Libertix.Pages;

static class RealInput
{
    const long Gib = 1024L * 1024 * 1024;
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    static IntPtr handle;
    static bool aborted;
    const uint LeftDown = 0x02, LeftUp = 0x04, KeyUp = 0x02;
    const byte VkLeft = 0x25, VkRight = 0x27, VkUp = 0x26, VkDown = 0x28, VkNext = 0x22, VkHome = 0x24, VkEnd = 0x23, VkControl = 0x11, VkA = 0x41, VkPrior = 0x21;

    static ResizeDisk page;
    static Window window;
    static readonly StringBuilder log = new StringBuilder();
    static string prefix;

    [STAThread]
    static int Main(string[] args)
    {
        prefix = args[0];
        try { return Run(); }
        catch (Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
                log.AppendLine("EXCEPTION " + e.GetType().FullName + ": " + e.Message + "\n" + e.StackTrace);
            System.IO.File.WriteAllText(prefix + ".txt", log.ToString());
            return 2;
        }
    }

    static int Run()
    {
        var app = new Libertix.App();
        app.InitializeComponent();
        Libertix.Localization.SetLanguage("en");
        var windows = new InstallationTargetInfo
        {
            Drive = "C:", IsWindows = true, DiskNumber = 0, PartitionStyle = "GPT", FriendlyName = "NVMe SSD",
            SizeBytes = 476 * Gib, FreeBytes = 168 * Gib, MinimumSizeBytes = 300 * Gib
        };
        var data = new InstallationTargetInfo
        {
            Drive = "D:", DiskNumber = 1, PartitionStyle = "GPT", FriendlyName = "Samsung SSD 870 EVO 1TB",
            SizeBytes = 931 * Gib, FreeBytes = 610 * Gib, MinimumSizeBytes = 330 * Gib
        };
        var state = new InstallationState
        {
            SelectedDistro = new DistroInfo { Id = "mint", Name = "Linux Mint 22.3 Cinnamon", IsoInstallerSizeBytes = 3091660800 },
            Compatibility = new CompatibilityInfo
            {
                Firmware = "UEFI", ShrinkAvailableBytes = 120 * Gib, InstallationTargets = new[] { windows, data }
            }
        };
        var ctor = typeof(ResizeDisk).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(InstallationState), typeof(long), typeof(long) }, null);
        page = (ResizeDisk)ctor.Invoke(new object[] { state, windows.SizeBytes, windows.FreeBytes });
        window = new Window
        {
            Title = "Libertix allocation input check", Content = page, Width = 1070, Height = 630,
            ResizeMode = ResizeMode.NoResize, Topmost = true, WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        window.ContentRendered += (s, e) => Steps();
        // Hard stop so a stuck run never leaves a window behind.
        var guard = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        guard.Tick += (s, e) => { log.AppendLine("TIMEOUT"); Finish(); };
        guard.Start();
        // Not app.Run(window): Application.Run raises Startup, which would run Libertix's
        // real App.OnStartup (single-instance mutex, permission prompt, log repair).
        window.Show();
        Dispatcher.Run();
        return 0;
    }

    static Thumb Thumb()
    {
        var slider = (Slider)page.FindName("PartitionSlider");
        return ((Track)slider.Template.FindName("PART_Track", slider)).Thumb;
    }

    static Point Screen(FrameworkElement element, double x, double y) { return element.PointToScreen(new Point(x, y)); }

    static void Record(string step)
    {
        page.UpdateLayout();
        var thumb = Thumb();
        var bar = (Grid)page.FindName("DiskVisualization");
        double centre = thumb.ActualWidth > 0 ? thumb.TransformToAncestor(page).Transform(new Point(thumb.ActualWidth / 2, 0)).X : double.NaN;
        double boundary = bar.TransformToAncestor(page).Transform(new Point(bar.ColumnDefinitions[0].ActualWidth, 0)).X;
        log.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0,-34} size={1,4} manual={2,4} sizeError={3,-5} focus={4,-14} thumbCentre={5,8:F2} boundary={6,8:F2} offset={7,5:F2} DIP",
            step, page.SelectedSize, page.ManualSize, page.HasSizeError,
            FocusName(),
            centre, boundary, centre - boundary));
    }

    static string FocusName()
    {
        var focused = System.Windows.Input.Keyboard.FocusedElement as FrameworkElement;
        return focused == null || string.IsNullOrEmpty(focused.Name) ? "(other)" : focused.Name;
    }

    static void Key(byte vk, bool ctrl = false)
    {
        if (ctrl) keybd_event(VkControl, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KeyUp, UIntPtr.Zero);
        if (ctrl) keybd_event(VkControl, 0, KeyUp, UIntPtr.Zero);
    }

    static void Steps()
    {
        var source = PresentationSource.FromVisual(window);
        handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        window.Activate();
        SetForegroundWindow(handle);
        DpiScale dpi = VisualTreeHelper.GetDpi(page);
        log.AppendLine(string.Format("DPI {0}x{1} (scale {2})", dpi.PixelsPerInchX, dpi.PixelsPerInchY, dpi.DpiScaleX));
        var queue = new Queue<Action>();
        Record("initial");
        Point start = default(Point);
        queue.Enqueue(() =>
        {
            var thumb = Thumb();
            start = Screen(thumb, thumb.ActualWidth / 2, thumb.ActualHeight / 2);
            SetCursorPos((int)start.X, (int)start.Y);
            mouse_event(LeftDown, 0, 0, 0, UIntPtr.Zero);
        });
        for (int i = 1; i <= 10; i++)
        {
            int step = i;
            queue.Enqueue(() => SetCursorPos((int)(start.X - step * 15 * dpi.DpiScaleX), (int)start.Y));
        }
        queue.Enqueue(() => { mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero); });
        queue.Enqueue(() => Record("pointer drag 150 DIP left"));
        foreach (var press in new[] {
            new { Name = "Left x3", Keys = new byte[] { VkLeft, VkLeft, VkLeft } },
            new { Name = "Right x1", Keys = new byte[] { VkRight } },
            new { Name = "Up x1", Keys = new byte[] { VkUp } },
            new { Name = "Down x1", Keys = new byte[] { VkDown } },
            new { Name = "PageUp", Keys = new byte[] { VkPrior } },
            new { Name = "PageDown", Keys = new byte[] { VkNext } },
            new { Name = "Home", Keys = new byte[] { VkHome } },
            new { Name = "End", Keys = new byte[] { VkEnd } } })
        {
            var current = press;
            queue.Enqueue(() => { foreach (byte key in current.Keys) Key(key); });
            queue.Enqueue(() => Record(current.Name));
        }
        queue.Enqueue(() =>
        {
            // Click inside the slider's own track, a quarter of the way from its left end.
            var slider = (Slider)page.FindName("PartitionSlider");
            Point target = Screen(slider, slider.ActualWidth * 0.25, slider.ActualHeight / 2);
            SetCursorPos((int)target.X, (int)target.Y);
            mouse_event(LeftDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero);
        });
        queue.Enqueue(() => Record("click at 25% of the slider track"));
        queue.Enqueue(() =>
        {
            var box = (TextBox)page.FindName("ManualSizeBox");
            Point target = Screen(box, box.ActualWidth / 2, box.ActualHeight / 2);
            SetCursorPos((int)target.X, (int)target.Y);
            mouse_event(LeftDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero);
        });
        queue.Enqueue(() => Key(VkA, ctrl: true));
        queue.Enqueue(() => { Key(0x34); Key(0x32); });
        queue.Enqueue(() => Record("typed 42 into the size box"));
        queue.Enqueue(() => Shot("after-input"));
        queue.Enqueue(Finish);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        timer.Tick += (s, e) =>
        {
            if (queue.Count == 0 || aborted) { timer.Stop(); return; }
            if (GetForegroundWindow() != handle)
            {
                aborted = true;
                timer.Stop();
                log.AppendLine("ABORTED: harness window is not the foreground window; no further input sent.");
                Finish();
                return;
            }
            queue.Dequeue()();
        };
        timer.Start();
    }

    static void Shot(string name)
    {
        var bitmap = new RenderTargetBitmap((int)page.ActualWidth, (int)page.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(page);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = System.IO.File.Create(prefix + "-" + name + ".png"))
            encoder.Save(stream);
    }

    static void Finish()
    {
        System.IO.File.WriteAllText(prefix + ".txt", log.ToString());
        window.Close();
        Dispatcher.CurrentDispatcher.InvokeShutdown();
    }
}

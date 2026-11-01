// Offscreen renderer for Libertix wizard pages (evidence only, not part of any PR).
// Runs from a copy of bin\Release so Libertix resolves its Resources folder as it does at runtime.
// Usage: RenderPage.exe <resize|choose> <scenario> <language> <out.png> [banner]
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Libertix.Models;

static class RenderPage
{
    const long Gib = 1024L * 1024 * 1024;

    [STAThread]
    static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception ex)
        {
            var log = new System.Text.StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
                log.AppendLine(e.GetType().FullName + ": " + e.Message + "\n" + e.StackTrace);
            System.IO.File.WriteAllText(args[3] + ".error.txt", log.ToString());
            return 2;
        }
    }

    static int Run(string[] args)
    {
        var app = new Libertix.App();
        app.InitializeComponent();
        Libertix.Localization.SetLanguage(args[2]);
        bool banner = args.Length > 4 && args[4] == "banner";

        var windows = new InstallationTargetInfo
        {
            Drive = "C:", IsWindows = true, DiskNumber = 0, PartitionStyle = "GPT",
            FriendlyName = "NVMe SSD", SizeBytes = 476 * Gib, FreeBytes = 168 * Gib, MinimumSizeBytes = 300 * Gib
        };
        var data = new InstallationTargetInfo
        {
            Drive = "D:", DiskNumber = 1, PartitionStyle = "GPT", FriendlyName = "Samsung SSD 870 EVO 1TB",
            SizeBytes = 931 * Gib, FreeBytes = 610 * Gib, MinimumSizeBytes = 330 * Gib
        };
        var mint = new DistroInfo
        {
            Id = "mint", Name = "Linux Mint 22.3 Cinnamon", IsoInstallerSizeBytes = 3091660800,
            Description = "Linux Mint is an operating system for desktop and laptop computers. It is designed to work out of the box and comes fully equipped with the apps most people need.",
            ImageUrl = System.IO.Path.GetFullPath(@"screens\mint.jpg")
        };
        var zorin = new DistroInfo
        {
            Id = "zorin", Name = "Zorin OS 18.1 Core", IsoInstallerSizeBytes = 3909091328,
            Description = "Zorin OS Core is a free Ubuntu-based desktop operating system with standard apps and four desktop layouts, designed to make your computer faster, more powerful and secure.",
            ImageUrl = System.IO.Path.GetFullPath(@"screens\zorin.png")
        };
        var state = new InstallationState
        {
            SelectedDistro = mint,
            Compatibility = new CompatibilityInfo
            {
                Firmware = "UEFI", ShrinkAvailableBytes = 120 * Gib,
                InstallationTargets = args[1] == "single" ? new[] { windows } : new[] { windows, data }
            }
        };

        FrameworkElement page;
        if (args[0] == "resize")
        {
            var ctor = typeof(Libertix.Pages.ResizeDisk).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[] { typeof(InstallationState), typeof(long), typeof(long) }, null);
            var resize = (Libertix.Pages.ResizeDisk)ctor.Invoke(new object[] { state, windows.SizeBytes, windows.FreeBytes });
            if (args[1] == "secondary")
                resize.SelectedTarget = data;
            if (args[1] == "maximum")
                resize.SelectedSize = resize.MaximumSize;
            page = resize;
        }
        else
        {
            Libertix.Helpers.FilepoolConfig pool;
            string error;
            if (!Libertix.Helpers.FilepoolConfig.TryCreate("https://example.invalid/", out pool, out error))
                throw new InvalidOperationException(error);
            var choose = (Libertix.Pages.ChooseDistro)Activator.CreateInstance(
                typeof(Libertix.Pages.ChooseDistro), BindingFlags.Public | BindingFlags.Instance, null,
                new object[] { state, pool }, null);
            var list = (ListBox)choose.FindName("DistrosListBox");
            var items = new ObservableCollection<DistroInfo> { mint, zorin };
            list.ItemsSource = items;
            if (args[1] != "none")
            {
                list.SelectedItem = args[1] == "zorin" ? zorin : mint;
            }
            page = choose;
        }

        int bannerHeight = banner ? 32 : 0;
        int pageHeight = 591 - bannerHeight;
        // No dispatcher pump: screenshots are local copies of the catalogue images, so page
        // code that would run in the live app (catalogue loading, logging) is never reached.
        page.Measure(new Size(1054, pageHeight));
        page.Arrange(new Rect(0, 0, 1054, pageHeight));
        page.UpdateLayout();
        if (args[1] == "drag")
        {
            var resize = (Libertix.Pages.ResizeDisk)page;
            var slider = (Slider)resize.FindName("PartitionSlider");
            var track = (System.Windows.Controls.Primitives.Track)slider.Template.FindName("PART_Track", slider);
            var thumb = track.Thumb;
            double before = resize.SelectedSize;
            thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragStartedEventArgs(0, 0));
            thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(-200, 0));
            thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(-200, 0, false));
            page.UpdateLayout();
            System.IO.File.WriteAllText(args[3] + ".drag.txt", string.Format(
                "before={0} after={1} manual={2} min={3} max={4} sizeError={5}",
                before, resize.SelectedSize, resize.ManualSize, resize.MinimumSize, resize.MaximumSize, resize.HasSizeError));
        }
        var pageBitmap = new RenderTargetBitmap(1054, pageHeight, 96, 96, PixelFormats.Pbgra32);
        pageBitmap.Render(page);

        var composite = new DrawingVisual();
        using (DrawingContext context = composite.RenderOpen())
        {
            if (banner)
            {
                // Same values as MainWindow.xaml's DevelopmentModeBanner.
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x3A, 0x24, 0x00)), null, new Rect(0, 0, 1054, 32));
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xF0, 0xA2, 0x34)), null, new Rect(0, 31, 1054, 1));
                var text = new FormattedText("DEVELOPMENT MODE - catalog signature verification is disabled.",
                    System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                    14, new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x47)));
                context.DrawText(text, new Point((1054 - text.Width) / 2, (32 - text.Height) / 2));
            }
            context.DrawImage(pageBitmap, new Rect(0, bannerHeight, 1054, pageHeight));
        }
        var bitmap = new RenderTargetBitmap(1054, 591, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(composite);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = System.IO.File.Create(args[3]))
            encoder.Save(stream);
        return 0;
    }
}

using System.Runtime.InteropServices;
using System.Text;
using NacreCraft.Logging;

namespace NacreCraft.Util;

//NativeErrorWindow drives the error window that lives in the native layer
//The window belongs there on purpose: it has to come up in the runs where the managed side is already gone, which is
//exactly the case a managed window cannot cover. This class only hands over the text to show
//Everything here is best effort. A missing layer, a missing entry point or a window that fails to start all fall back
//to the text report, and that report is the part which must never fail
public static class NativeErrorWindow
{
    //LibraryName file the native layer builds into, without extension
    private const string LibraryName = "nacrecraft_native";

    //Enabled whether the window is shown at all
    //The server turns this off when it runs without a GUI: there is no desktop to draw on and a window that fails there
    //would only delay the shutdown
    public static bool Enabled { get; set; } = true;

    //The native side takes every string as UTF-8 bytes plus a length, laid out in one request struct
    [StructLayout(LayoutKind.Sequential)]
    private struct ReportWindowRequest
    {
        public IntPtr Title;
        public UIntPtr TitleLength;
        public IntPtr Summary;
        public UIntPtr SummaryLength;
        public IntPtr ReportPath;
        public UIntPtr ReportPathLength;
        public IntPtr LabelOpenFolder;
        public UIntPtr LabelOpenFolderLength;
        public IntPtr LabelCopy;
        public UIntPtr LabelCopyLength;
        public IntPtr LabelCopied;
        public UIntPtr LabelCopiedLength;
        public IntPtr LabelClose;
        public UIntPtr LabelCloseLength;
        public byte Blocking;
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_show_report_window(IntPtr request);

    //ShowCrash shows the window for a crash report and waits for the user to close it
    //The wait belongs here: the caller continues its shutdown once this returns, and the process would otherwise end
    //before the window could be read
    public static void ShowCrash(string reportPath, string summary)
        => Show("nacrecraft.error_window.crash_title", summary, reportPath, blocking: true);

    //ShowModLoadFailure shows the window once mod loading is done, and does not block: startup carries on behind it
    public static void ShowModLoadFailure(string reportPath, int failedCount)
        => Show("nacrecraft.error_window.mod_title",
            Loc.Format("nacrecraft.error_window.mod_summary", failedCount), reportPath, blocking: false);

    //Show resolves the labels, hands the request over and reports whether anything came of it
    private static void Show(string titleKey, string summary, string reportPath, bool blocking)
    {
        if (!Enabled) return;

        var handles = new List<GCHandle>(6);
        try
        {
            var request = new ReportWindowRequest
            {
                Title = Pin(Loc.Get(titleKey), handles, out var titleLength),
                TitleLength = titleLength,
                Summary = Pin(summary, handles, out var summaryLength),
                SummaryLength = summaryLength,
                ReportPath = Pin(reportPath, handles, out var pathLength),
                ReportPathLength = pathLength,
                LabelOpenFolder = Pin(Loc.Get("nacrecraft.error_window.open_folder"), handles, out var openLength),
                LabelOpenFolderLength = openLength,
                LabelCopy = Pin(Loc.Get("nacrecraft.error_window.copy"), handles, out var copyLength),
                LabelCopyLength = copyLength,
                LabelCopied = Pin(Loc.Get("nacrecraft.error_window.copied"), handles, out var copiedLength),
                LabelCopiedLength = copiedLength,
                LabelClose = Pin(Loc.Get("nacrecraft.error_window.close"), handles, out var closeLength),
                LabelCloseLength = closeLength,
                Blocking = blocking ? (byte)1 : (byte)0,
            };

            var buffer = Marshal.AllocHGlobal(Marshal.SizeOf<ReportWindowRequest>());
            try
            {
                Marshal.StructureToPtr(request, buffer, false);
                var result = ncn_show_report_window(buffer);
                if (result != 0)
                    Log.Warning($"The native error window declined the request, code {result}; the report on disk is unaffected");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (DllNotFoundException)
        {
            //The native layer is not in this process, the normal case when profiling was never enabled for the run
        }
        catch (EntryPointNotFoundException)
        {
            Log.Warning("The native layer is present but has no error window entry point");
        }
        catch (Exception exception)
        {
            Log.Warning($"Showing the native error window failed: {exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            foreach (var handle in handles) handle.Free();
        }
    }

    //Pin keeps one UTF-8 string alive for the whole call
    //The blocking path parks this thread inside the native code, so the buffers have to survive collection while it waits
    private static IntPtr Pin(string text, List<GCHandle> handles, out UIntPtr length)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        handles.Add(handle);
        length = (UIntPtr)bytes.Length;
        return handle.AddrOfPinnedObject();
    }
}

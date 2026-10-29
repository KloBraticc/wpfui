using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

[StructLayout(LayoutKind.Sequential)]
public partial struct POINT
{
    public int x;

    public int y;
}

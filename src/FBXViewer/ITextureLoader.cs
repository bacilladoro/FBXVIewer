using System.Drawing;
using System.IO;

namespace FBXViewer
{
    public interface ITextureLoader<out TBitmap>
    {
        TBitmap FromPath(string path);
        TBitmap FromStream(Stream stream);
        TBitmap FromColor(in Color color);
    }
}
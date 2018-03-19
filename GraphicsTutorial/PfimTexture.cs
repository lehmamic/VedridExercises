using System;
using Pfim;
using SixLabors.ImageSharp;
using Veldrid.ImageSharp;

namespace GraphicsTutorial
{
    public class PfimTexture : ImageSharpTexture
    {
        public PfimTexture(string path)
            : this(path, true)
        {
        }

        public PfimTexture(string path, bool mipmap)
            : this(Pfim.Pfim.FromFile(path), mipmap)
        {
        }

        public PfimTexture(IImage image, bool mipmap = true)
            : base(Image.LoadPixelData<Rgba32>(image.Data, image.Width, image.Height), mipmap)
        {
        }
    }
}

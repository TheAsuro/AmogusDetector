using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class Extensions
{
    public static Image<L8> ArrayToImage(this PxType[,] data)
    {
        var tmpImg = new Image<L8>(data.GetLength(1), data.GetLength(0));
        for (int y = 0; y < data.GetLength(0); ++y)
        {
            for (int x = 0; x < data.GetLength(1); ++x)
            {
                tmpImg[x, y] = new L8((byte)data[y, x]);
            }
        }
        return tmpImg;
    }

    public static PxType[,] ImageToArray(this Image<L8> image)
    {
        var data = new PxType[image.Height, image.Width];
        for (int y = 0; y < data.GetLength(0); ++y)
            for (int x = 0; x < data.GetLength(1); ++x)
                data[y, x] = (PxType)image[x, y].PackedValue;
        return data;
    }
}
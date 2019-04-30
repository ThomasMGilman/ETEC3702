using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using System;

class TGAHeader
{
    public byte commentSize;                //can be zero
    public byte colorType;                  //0
    public byte compression;                //2=none, 10=compressed
    public byte[] colorMap = new byte[5];   //unused
    public ushort[] origin = new ushort[2]; //0,0
    public ushort width, height;
    public byte bitsPerPixel;               //24=BGR, 32=BGRA
    public byte descriptor;                 //0 for BGR; 8 for BGRA
    byte[] comment;                         //variable size
    public TGAHeader(BinaryReader R)
    {
        this.commentSize = R.ReadByte();
        this.colorType = R.ReadByte(); //0
        this.compression = R.ReadByte();
        R.Read(this.colorMap, 0, this.colorMap.Length);
        this.origin[0] = R.ReadUInt16();
        this.origin[1] = R.ReadUInt16();
        this.width = R.ReadUInt16();
        this.height = R.ReadUInt16();
        this.bitsPerPixel = R.ReadByte();
        this.descriptor = R.ReadByte();
        if(this.commentSize > 0) {
            this.comment = new byte[this.commentSize];
            R.Read(this.comment, 0, this.comment.Length);
        } else {
            this.comment = new byte[0];
        }
    }

    public void Write(BinaryWriter W)
    {
        W.Write(this.commentSize);
        W.Write(this.colorType);
        W.Write(this.compression);
        W.Write(this.colorMap, 0, this.colorMap.Length);
        W.Write(this.origin[0]);
        W.Write(this.origin[1]);
        W.Write(this.width);
        W.Write(this.height);
        W.Write(this.bitsPerPixel);
        W.Write(this.descriptor);
        if(this.commentSize > 0)
            W.Write(this.comment);
    }
}

struct Pixel
{
    public byte b, g, r;

    public override bool Equals(object o)
    {
        return this == (Pixel)o;
    }

    public static bool operator==(Pixel p1, Pixel p2)
    {
        return p1.r == p2.r && p1.g == p2.g && p1.b == p2.b;
    }

    public static bool operator!=(Pixel p1, Pixel p2)
    {
        return !(p1 == p2);
    }

    public override int GetHashCode()
    {
        return r ^ g ^ b;   //FIXME: Make better
    }
}



class Program
{
    public static void outputPixel(Pixel p, ref List<byte> opix)
    {
        opix.Add(p.b);      //Blue
        opix.Add(p.g);      //Green
        opix.Add(p.r);      //Red
    }

    public static void outputByte(int n, ref List<byte> opix)
    {
        opix.Add((byte)n);
    }

    //ipix = uncompressed data, opix = new data list to compress to
    static void compressIt(Pixel[] ipix, List<byte> opix, TGAHeader hdr)
    {
        int ipixSize = ipix.Length;
        //Parallel.For(0, hdr.height, k => {
        //    int start = k * hdr.width;
        //    int stop = (k + 1) * hdr.width;
        //});

        int i, n;
        int[] repeats = new int[ipix.Length];
        for (i = ipixSize - 2; i >= 0; i--)         //accumulate repeats of each pixel in ipix
        {
            if (ipix[i] == ipix[i + 1]) repeats[i] = repeats[i + 1] + 1;
            else repeats[i] = 1;
        }

        i = 0;
        while(i < ipixSize)                         //Compress Image
        {
            int j = i;
            while (j < ipixSize && repeats[j] < 2)  //find two identical pixels
                j++;
            while(i < j)
            {
                n = j - i;
                if (n > 128)
                    n = 128;

                outputByte(n - 1, ref opix);
                while(n-- > 0)
                    outputPixel(ipix[i++], ref opix);
            }

            if (i == ipixSize)  //check at end, else guaranteed run of identical pixels
                break;

            n = repeats[i];
            if (n > 128)
                n = 128;
            outputByte(0x80 + (n - 1), ref opix);
            outputPixel(ipix[i], ref opix);
            i += n;
        }
    }

    public static void Main(string[] args)
    {
        TGAHeader hdr;
        Pixel[] pix;
        

        //Read in tga file
        using(var ifile = new FileStream("in.tga", FileMode.Open)) {
            using(var bfile = new BinaryReader(ifile)) {
                hdr = new TGAHeader(bfile);
                pix = new Pixel[hdr.width * hdr.height];
                byte[] tmp = new byte[pix.Length * 3];
                bfile.Read(tmp, 0, tmp.Length);
                for(int i = 0, j = 0; i < pix.Length; ++i) {
                    pix[i].b = tmp[j++];
                    pix[i].g = tmp[j++];
                    pix[i].r = tmp[j++];
                }
            }
        }

        Console.WriteLine("Read TGA file");

        //check tga header for bad data
        if(hdr.colorType != 0)
            throw new Exception("Bad colorType");
        if(hdr.compression != 2)
            throw new Exception("Can only use uncompressed input");
        if(hdr.bitsPerPixel != 24)
            throw new Exception("Can only use BGR (24 bit)");
        if(hdr.descriptor != 0)
            throw new Exception("Can only use BGR (descriptor 0)");

        //create empty byte list and compress to it
        var opix = new List<byte>();
        compressIt(pix, opix, hdr);
        hdr.compression = 10;           //File is compressed

        //Write out compressed tga file
        using(var ofile = new FileStream("out.tga", FileMode.Create)) {
            using(var bfile = new BinaryWriter(ofile)) {
                hdr.Write(bfile);
                byte[] tmp = new byte[opix.Count];
                opix.CopyTo(tmp);
                bfile.Write(tmp);
            }
        }
        Console.WriteLine("Done");
    }

}

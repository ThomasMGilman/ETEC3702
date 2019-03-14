using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

class MainClass
{
    static List<Task> tasks = new List<Task>();

    static void convert(string path){
        var bmp = new Bitmap(path);
        var gray = new Bitmap(bmp.Width, bmp.Height);
        for(var y = 0; y < bmp.Height; ++y) {
            for(var x = 0; x < bmp.Width; ++x) {
                var pix = bmp.GetPixel(x, y);
                byte r = pix.R;
                byte g = pix.G;
                byte b = pix.B;
                int avg = (r + g + b) / 3;
                gray.SetPixel(x, y, Color.FromArgb(255,avg,avg,avg));
            }
        }
        var newName = path.Replace(".png", ".jpg");
        gray.Save(newName, System.Drawing.Imaging.ImageFormat.Jpeg);
        Console.WriteLine("Converted "+path+" to "+newName);
    }

    static void walk(string folder){
        foreach(var dir in Directory.GetDirectories(folder)) {
            walk(dir);
        }
        //lambda closures in C# are *not* like Javascript
        foreach(string file in Directory.GetFiles(folder,"*")) {
            if(file.EndsWith(".png")) {
                tasks.Add(Task.Run(() => convert(file)));
            }
        }
    }
    public static void Main (string[] args)
    {
        string folder = "../images";
        walk(folder);
        foreach(var t in tasks) {
            t.Wait();
        }
    }
}

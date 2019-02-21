//Thomas Gilman
//James Hudson
//ETEC 3702 OS2
//12th February, 2019
//Assignment 6 Synchronous
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

//MultiThreaded Blur image class, just pass image, numThreads, and numBlurs
class ImageBlurrer
{
    BitmapData bdata;
    Barrier barrier;
    Stopwatch timer = new Stopwatch();
    List<Thread> threads;
    static byte[] pix;
    static byte[] pix2;
    int timesToBlur, sectionAmount;
    bool swap = false;
    
    public ImageBlurrer(Bitmap image, int numThreads, int numBlurs)
    {
        bdata = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        pix = new byte[bdata.Stride * bdata.Height];
        pix2 = new byte[bdata.Stride * bdata.Height];
        Marshal.Copy(bdata.Scan0, pix, 0, pix.Length);
        sectionAmount = bdata.Height / numThreads;                          //gets the sections to divide work up into
        timesToBlur = numBlurs;
        threads = new List<Thread>();

        //when last thread goes through the barrier, it swaps byte arrays and decreases number of blurs and resets the barrier
        barrier = new Barrier(numThreads, (b) =>
        {
            if (timesToBlur - 1 == 0)
                timer.Stop();
            swap = !swap;
            --timesToBlur;
        });

        timer.Start();
        //Thread is made with starting x, y, and y end position. The y starting pos and end is divied up between section amounts
        for (int t = 0; t < numThreads; t++)
        {
            int secMul = t;
            Thread worker;
            if(t == numThreads - 1)             //last threads section goes up to bdata.Height
                worker = new Thread(() => imageBlur(0, sectionAmount * secMul, bdata.Height, secMul));                
            else                                //thread works section from startPos to endPos
                worker = new Thread(() => imageBlur(0, sectionAmount * secMul, sectionAmount * (secMul + 1), secMul));
            worker.Start();
            threads.Add(worker);
        }

        foreach (Thread t in threads)
            t.Join();

        if (swap)
            Marshal.Copy(pix2, 0, bdata.Scan0, pix2.Length);
        else
            Marshal.Copy(pix, 0, bdata.Scan0, pix.Length);
        image.UnlockBits(bdata);
        image.Save("out.png");
        Console.WriteLine("Elapsed Time for blurring: {0}", timer.Elapsed);
    }
    private void imageBlur(int x, int y, int sectionAmount, int threadNumber)
    {
        int Blue, Green, Red;
        int itemsToAvg, toAvg;
        int xPix, yPix;

        while (Interlocked.Add(ref timesToBlur, 0) > 0)
        {
            for (yPix = y; yPix < sectionAmount; yPix++)
            {
                for (xPix = x; xPix < bdata.Width; xPix++)
                {
                    Blue = 0; Green = 0; Red = 0;
                    itemsToAvg = 0;
                    toAvg = yPix * bdata.Stride + xPix * 3;
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int ry = dy + yPix;
                        if (ry > 0 && ry < bdata.Height)
                        {
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                int rx = dx + xPix;
                                if (rx > 0 && rx < bdata.Width)
                                {
                                    itemsToAvg++;
                                    int idx = ry * bdata.Stride + rx * 3;
                                    if (swap)
                                    {
                                        Blue    += pix2[idx];        //BLUE
                                        Green   += pix2[idx + 1];    //GREEN
                                        Red     += pix2[idx + 2];    //RED
                                    }
                                    else
                                    {
                                        Blue    += pix[idx];        //BLUE
                                        Green   += pix[idx + 1];    //GREEN
                                        Red     += pix[idx + 2];    //RED
                                    }
                                }
                            }
                        }
                    }
                    
                    Blue = Blue / itemsToAvg; Green = Green / itemsToAvg; Red = Red / itemsToAvg;
                    if (swap)
                    {
                        pix[toAvg]        = (byte)Blue;
                        pix[toAvg + 1]    = (byte)Green;
                        pix[toAvg + 2]    = (byte)Red;
                    }
                    else
                    {
                        pix2[toAvg]         = (byte)Blue;
                        pix2[toAvg + 1]     = (byte)Green;
                        pix2[toAvg + 2]     = (byte)Red;
                    }
                }
            }
            this.barrier.SignalAndWait();
        }
    }
}

class Program
{
    //Single Threaded BlurImage
    public static void BlurImage(Bitmap image, int numBlurs)
    {
        BitmapData bdata = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        byte[] pix = new byte[bdata.Stride * bdata.Height];
        byte[] otherPix = new byte[bdata.Stride * bdata.Height];
        Marshal.Copy(bdata.Scan0, pix, 0, pix.Length);
        Stopwatch timer = new Stopwatch();
        int itemsToAvg = 0, timesToBlur = numBlurs;
        int ry, rx, idx, toAvg, Blue, Green, Red;
        bool swap = false;

        timer.Start();
        while(timesToBlur-- > 0)
        {
            swap = !swap;
            for (int y = 0; y < bdata.Height; y++)
            {
                for (int x = 0; x < bdata.Width; x++)
                {
                    Blue = 0; Green = 0; Red = 0;
                    itemsToAvg = 0;
                    toAvg = y * bdata.Stride + x * 3;
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        ry = dy + y;
                        if (ry > 0 && ry < bdata.Height)
                        {
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                rx = dx + x;
                                if (rx > 0 && rx < bdata.Width)
                                {
                                    itemsToAvg++;
                                    idx = ry * bdata.Stride + rx * 3;
                                    if (swap)
                                    {
                                        Blue += pix[idx];            //BLUE
                                        Green += pix[idx + 1];    //GREEN
                                        Red += pix[idx + 2];    //RED
                                    }
                                    else
                                    {
                                        Blue += otherPix[idx];            //BLUE
                                        Green += otherPix[idx + 1];    //GREEN
                                        Red += otherPix[idx + 2];    //RED
                                    }
                                }
                            }
                        }
                    }
                    Blue = Blue / itemsToAvg; Green = Green / itemsToAvg; Red = Red / itemsToAvg;
                    if (swap)
                    {
                        otherPix[toAvg] = Convert.ToByte(Blue);
                        otherPix[toAvg + 1] = Convert.ToByte(Green);
                        otherPix[toAvg + 2] = Convert.ToByte(Red);
                    }
                    else
                    {
                        pix[toAvg] = Convert.ToByte(Blue);
                        pix[toAvg + 1] = Convert.ToByte(Green);
                        pix[toAvg + 2] = Convert.ToByte(Red);
                    }
                }
            }
        }
        if(swap)
            Marshal.Copy(otherPix, 0, bdata.Scan0, otherPix.Length);
        else
            Marshal.Copy(pix, 0, bdata.Scan0, pix.Length);
        image.UnlockBits(bdata);
        image.Save("out.png");
        Console.WriteLine("ElapsedTime: {0}", timer.Elapsed);
    }

    static void Main(string[] args)
    {
        Bitmap img;
        int numThreads = 0, numBlurs = 0;

        if (args.Length == 3)
        {
            img = (Bitmap)Image.FromFile(args[0]);          //Get image
            if (Int32.TryParse(args[1], out numThreads))    //Get number Threads
                numThreads = Int32.Parse(args[1]);
            else
            {
                Console.WriteLine("please input a integer value as the second input for number of threads");
                Console.Read();
                Environment.Exit(-1);
            }
            if (Int32.TryParse(args[2], out numBlurs))      //Get number of blurs
                numBlurs = Int32.Parse(args[2]);
            else
            {
                Console.WriteLine("please input a integer value as the third input for number of blurs");
                Console.Read();
                Environment.Exit(-1);
            }
            Console.WriteLine("imageName:{0}, numThreads:{1}, numBlurs:{2}\n", args[0], args[1], args[2]);
            ImageBlurrer imageBlur = new ImageBlurrer(img, numThreads, numBlurs);
        }
        else
        {
            Console.WriteLine("please pass three inputs: { 'imageFile.txt', 'numThreads', 'numBlurs' }");
            Console.Read();
            Environment.Exit(-1);
        }

        Console.Read();
    }
}
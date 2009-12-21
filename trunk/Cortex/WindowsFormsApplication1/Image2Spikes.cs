﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace WindowsFormsApplication1
{
    class Image2Spikes
    {

        private UdpClient udpClient;
        private int bufsize;
        private static Bitmap bmp;
        private PictureBox bitmap;
        private int scan = 0;
        Random rnd;
        public static byte[] reverse ={
                               0x00,0x80,0x40,0xC0,0x20,0xA0,0x60,0xE0,0x10,0x90,0x50,0xD0,0x30,0xB0,0x70,0xF0,
                               0x08,0x88,0x48,0xC8,0x28,0xA8,0x68,0xE8,0x18,0x98,0x58,0xD8,0x38,0xB8,0x78,0xF8,
                               0x04,0x84,0x44,0xC4,0x24,0xA4,0x64,0xE4,0x14,0x94,0x54,0xD4,0x34,0xB4,0x74,0xF4,
                               0x0C,0x8C,0x4C,0xCC,0x2C,0xAC,0x6C,0xEC,0x1C,0x9C,0x5C,0xDC,0x3C,0xBC,0x7C,0xFC,
                               0x02,0x82,0x42,0xC2,0x22,0xA2,0x62,0xE2,0x12,0x92,0x52,0xD2,0x32,0xB2,0x72,0xF2,
                               0x0A,0x8A,0x4A,0xCA,0x2A,0xAA,0x6A,0xEA,0x1A,0x9A,0x5A,0xDA,0x3A,0xBA,0x7A,0xFA,
                               0x06,0x86,0x46,0xC6,0x26,0xA6,0x66,0xE6,0x16,0x96,0x56,0xD6,0x36,0xB6,0x76,0xF6,
                               0x0E,0x8E,0x4E,0xCE,0x2E,0xAE,0x6E,0xEE,0x1E,0x9E,0x5E,0xDE,0x3E,0xBE,0x7E,0xFE,
                               0x01,0x81,0x41,0xC1,0x21,0xA1,0x61,0xE1,0x11,0x91,0x51,0xD1,0x31,0xB1,0x71,0xF1,
                               0x09,0x89,0x49,0xC9,0x29,0xA9,0x69,0xE9,0x19,0x99,0x59,0xD9,0x39,0xB9,0x79,0xF9,
                               0x05,0x85,0x45,0xC5,0x25,0xA5,0x65,0xE5,0x15,0x95,0x55,0xD5,0x35,0xB5,0x75,0xF5,
                               0x0D,0x8D,0x4D,0xCD,0x2D,0xAD,0x6D,0xED,0x1D,0x9D,0x5D,0xDD,0x3D,0xBD,0x7D,0xFD,
                               0x03,0x83,0x43,0xC3,0x23,0xA3,0x63,0xE3,0x13,0x93,0x53,0xD3,0x33,0xB3,0x73,0xF3,
                               0x0B,0x8B,0x4B,0xCB,0x2B,0xAB,0x6B,0xEB,0x1B,0x9B,0x5B,0xDB,0x3B,0xBB,0x7B,0xFB,
                               0x07,0x87,0x47,0xC7,0x27,0xA7,0x67,0xE7,0x17,0x97,0x57,0xD7,0x37,0xB7,0x77,0xF7,
                               0x0F,0x8F,0x4F,0xCF,0x2F,0xAF,0x6F,0xEF,0x1F,0x9F,0x5F,0xDF,0x3F,0xBF,0x7F,0xFF,
                               };

        public Image2Spikes(PictureBox bitmap, UdpClient udpClient)
        {
            this.bitmap = bitmap;
            this.udpClient = udpClient;
            bufsize = udpClient.Client.SendBufferSize;  // Maximum Transmission Unit of UDP sockets
            rnd = new Random();

            Console.WriteLine("Soft MTU=" + bufsize);
            bmp = new Bitmap("c:\\Users\\sdenis\\Pictures\\any-key-gray-64x64.bmp");
        }

        public void SendImage()
        {

            // transmit the bitmap as a evenly distributed sequence of packets (retinal neuron spikes)
            // where the sum of 256 packets have covered the intensity of each pixels
            List<int> spikes;
            if(bitmap!=null){
                spikes = Spikes(bitmap,rnd.Next()%255);// reverse[scan]); // reverse seems a bad idea in a lossless world
            }
            else{
                // display 64x64 grid 
                spikes= new List<int>();
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x += (y%8==0?1:8)) spikes.Add(x+y*64);
            }
            transmitAsDeltasUDPStream(spikes);
            scan++;
            scan %= 256;

        }

        public void transmitAsDeltasUDPStream(List<int> spikes)
        {
            List<byte> stream = new List<byte>();
            int streamIndex = 0;

            foreach (int spikeAdrs in spikes)
            {
                int offset = (spikeAdrs + 1) - streamIndex;

                while (offset > 255)
                {
                    if (stream.Count < bufsize)
                    {
                        stream.Add(0);
                    }
                    else break;
                    streamIndex += 255;
                    offset = (spikeAdrs + 1) - streamIndex;

                }
                if (offset == 0 || offset > 255) throw new Exception("transmitAsDeltasUDPStream error in streaming algorithm.");
                if (stream.Count < bufsize)
                    stream.Add((byte)offset);
                else
                {
                    int dropped = spikes.Count - bufsize;
                    Console.WriteLine("(Spikes)" + spikes.Count + " @ " + streamIndex + " - (MTU)" + bufsize + " = " + dropped + " spikes dropped.");
                    break;
                }
                streamIndex += offset;

            }
            udpClient.Send(stream.ToArray(), stream.Count);
        }

        public static List<int> Spikes(PictureBox src, int treshold)
        // Returns the list of neurons(=pixels) that exceeds the treshold
        {
            List<int> spikes = new List<int>();


            Rectangle srcRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData sight = bmp.LockBits(srcRect, ImageLockMode.ReadWrite, src.Image.PixelFormat);

            // process
            int PixelSize = 4;
            int x = 0, y = 0;
            int w = sight.Width, h = sight.Height;
            int identity = 0; // neuron identity = pixel number in scan order
            int luma;


            unsafe
            {
                for (int yScan = y; yScan < (y + h); yScan++)
                {
                    byte* row = (byte*)sight.Scan0 + yScan * sight.Stride;
                    for (int xScan = x; xScan < (x + w); xScan++)
                    {
                        int xRef = xScan * PixelSize;
                        byte r, g, b, a;

                        b = row[xRef];
                        g = row[xRef + 1];
                        r = row[xRef + 2];
                        a = row[xRef + 3];
                        luma = (r + g + b) / 3;
                        if (luma > treshold)
                        {
                            spikes.Add(identity);
                        }
                        identity++;
                    }
                }
            }

            // close
            bmp.UnlockBits(sight);
            return spikes;
        }

    }
}
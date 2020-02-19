/** 
 *
 * (IEC16022Sharp DataMatrix bar code generation lib)
 * 
 * FastBWBmp: a class for direct creation of 1 bit/pixel BMP file
  * (c) 2007 Fabrizio Accatino <fhtino@yahoo.com>
 * 
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301 USA
 *
 */

// Many informations about BMP format from http://www.fortunecity.com/skyscraper/windows/364/bmpffrmt.html


using System;
using System.IO;


namespace IEC16022Sharp
{
    public class FastBWBmp
    {
        
        private int _width;
        private int _height;
        private byte[,] _dots;
        private byte[] _pixelData;
        private byte[] _fileBytes;


        public FastBWBmp( byte[,] dots)
        {
            _width = dots.GetLength(1);
            _height = dots.GetLength(0);
            _dots = dots;
            _pixelData = ConvertTo1BitPixelData();
            _fileBytes = BuildFileBytes();
        }



        /// <summary>
        /// Get the byte array of the bmp file data
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return _fileBytes;
        }

       

        /// <summary>
        /// Save bmp to file
        /// </summary>
        public void Save(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(_fileBytes, 0, _fileBytes.Length);
            }
        }

        /// <summary>
        /// Save bmp to stream
        /// </summary>
        public void Save(Stream strm)
        {
            strm.Write(_fileBytes, 0, _fileBytes.Length);
        }



        private byte[] ConvertTo1BitPixelData()
        {
            int rows = _dots.GetLength(0);
            int cols = _dots.GetLength(1);

            // intero superiore
            int bytesPerRow = cols / 8 + (cols % 8 == 0 ? 0 : 1);
            // arrotonda sempre a multipli di 4 bytes
            if (bytesPerRow % 4 > 0)
                bytesPerRow += 4 - bytesPerRow % 4;

            // Alloca spazio per i pixel
            byte[] bytes = new byte[bytesPerRow * rows];

            // Ciclo allocazione dot --> pixel
            for (int r = 0; r < rows; r++)
            {
                // Idea iniziale:  ogni byte � composto da 8 dot
                // (il problema � controllare di non sfondare la matrice dots)
                //
                //for (int c = 0; c < cols; c = c + 8)
                //{
                //    bytes[r * bytesPerRow + c / 8] = (byte)(
                //        (dots[r, c] & 1) |
                //        (dots[r, c + 1] & 1) << 1 |
                //        (dots[r, c + 2] & 1) << 2 |
                //        (dots[r, c + 3] & 1) << 3 |
                //        (dots[r, c + 4] & 1) << 4 |
                //        (dots[r, c + 5] & 1) << 5 |
                //        (dots[r, c + 6] & 1) << 6 |
                //        (dots[r, c + 7] & 1) << 7
                //        );
                //}

                // Nuova versione  (performance ???)
                for (int c = 0; c < cols; c++)
                {
                    // Attenzione: le righe dell'immagine sono memorizzate nell'ordine inverso sul file Bmp
                    bytes[(rows - r - 1) * bytesPerRow + c / 8] = (byte)
                         (
                             bytes[(rows - r - 1) * bytesPerRow + c / 8] |
                             ((_dots[r, c] & 1) << (7 - c % 8))
                         );
                }
            }

            return bytes;
        }



        private byte[] BuildFileBytes()
        {
            BITMAPFILEHEADER fileHeader = new BITMAPFILEHEADER();
            fileHeader.bfOffBits = 14 + 40 + 2 * 4;   // BITMAPFILEHEADER + BITMAPINFOHEADER + 2 * RGBQUAD
            fileHeader.bfSize = fileHeader.bfOffBits + (UInt32)_pixelData.Length; // dataLength + headersLength
            byte[] fileHeaderBytes = fileHeader.ToByteArray();

            BITMAPINFOHEADER infoHeader = new BITMAPINFOHEADER();
            infoHeader.biWidth = (UInt32)_width;
            infoHeader.biHeight = (UInt32)_height;
            infoHeader.biBitCount = 1;
            infoHeader.biSizeImage = (UInt32)_pixelData.Length;
            infoHeader.biXPelsPerMeter = 3780;
            infoHeader.biYPelsPerMeter = 3780;
            byte[] infoHeaderBytes = infoHeader.ToByteArray();

            RGBQUAD black = new RGBQUAD(0, 0, 0);
            byte[] blackBytes = black.ToByteArray();

            RGBQUAD white = new RGBQUAD(255, 255, 255);
            byte[] whiteBytes = white.ToByteArray();


            // Scrittura dati
            using (MemoryStream outStream = new MemoryStream())
            {
                outStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);
                outStream.Write(infoHeaderBytes, 0, infoHeaderBytes.Length);
                outStream.Write(blackBytes, 0, blackBytes.Length);
                outStream.Write(whiteBytes, 0, whiteBytes.Length);
                outStream.Write(_pixelData, 0, _pixelData.Length);
                return outStream.ToArray();
            }
        }





        #region Conversion (int to byte[])

        private static byte[] IntTo2Bytes(UInt16 i)
        {
            return new byte[] {
                (byte)(i & 255),
                (byte)((i>>8) & 255)
            };
        }


        private static byte[] IntTo4Bytes(UInt32 i)
        {
            return new byte[] {
                (byte)(i & 255),
                (byte)((i>>8) & 255),
                (byte)((i>>16) & 255),
                (byte)((i>>24) & 255)
            };
        }

        #endregion



        #region Subclasses

        private class BITMAPFILEHEADER
        {
            private UInt16 bfType = 19778;   // "BM"
            public UInt32 bfSize;            // specifies the size of the file in bytes.
            private UInt16 bfReserved1 = 0;  // must always be set to zero.
            private UInt16 bfReserved2 = 0;  // must always be set to zero.
            public UInt32 bfOffBits;         // specifies the offset from the beginning of the file to the bitmap data.


            public byte[] ToByteArray()
            {
                byte[] b = new byte[14];
                IntTo2Bytes(bfType).CopyTo(b, 0);
                IntTo4Bytes(bfSize).CopyTo(b, 2);
                IntTo2Bytes(bfReserved1).CopyTo(b, 2 + 4);
                IntTo2Bytes(bfReserved2).CopyTo(b, 2 + 4 + 2);
                IntTo4Bytes(bfOffBits).CopyTo(b, 2 + 4 + 2 + 2);
                return b;
            }
        }


        private class BITMAPINFOHEADER
        {
            public UInt32 biSize = 40;          // specifies the size of the BITMAPINFOHEADER structure, in bytes.
            public UInt32 biWidth = 0;	        // specifies the width of the image, in pixels.
            public UInt32 biHeight = 0;	        // specifies the height of the image, in pixels.
            private UInt16 biPlanes = 1;	    // specifies the number of planes of the target device, must be set to zero.
            public UInt16 biBitCount = 8;       // specifies the number of bits per pixel.
            private UInt32 biCompression = 0;   // Specifies the type of compression, usually set to zero (no compression).
            public UInt32 biSizeImage = 0;	    // specifies the size of the image data, in bytes. If there is no compression, it is valid to set this member to zero.
            public UInt32 biXPelsPerMeter = 0;	// specifies the the horizontal pixels per meter on the designated targer device, usually set to zero.
            public UInt32 biYPelsPerMeter = 0;  // specifies the the vertical pixels per meter on the designated targer device, usually set to zero.
            private UInt32 biClrUsed = 0;       // specifies the number of colors used in the bitmap, if set to zero the number of colors is calculated using the biBitCount member.
            private UInt32 biClrImportant = 0;  // specifies the number of color that are 'important' for the bitmap, if set to zero, all colors are important.

            public byte[] ToByteArray()
            {
                byte[] b = new byte[4 + 4 + 4 + 2 + 2 + 4 + 4 + 4 + 4 + 4 + 4];
                IntTo4Bytes(biSize).CopyTo(b, 0);
                IntTo4Bytes(biWidth).CopyTo(b, 0 + 4);
                IntTo4Bytes(biHeight).CopyTo(b, 4 + 4);
                IntTo2Bytes(biPlanes).CopyTo(b, 8 + 4);
                IntTo2Bytes(biBitCount).CopyTo(b, 12 + 2);
                IntTo4Bytes(biCompression).CopyTo(b, 14 + 2);
                IntTo4Bytes(biSizeImage).CopyTo(b, 16 + 4);
                IntTo4Bytes(biXPelsPerMeter).CopyTo(b, 20 + 4);
                IntTo4Bytes(biYPelsPerMeter).CopyTo(b, 24 + 4);
                IntTo4Bytes(biClrUsed).CopyTo(b, 28 + 4);
                IntTo4Bytes(biClrImportant).CopyTo(b, 32 + 4);
                return b;
            }

        }


        private class RGBQUAD
        {
            public byte rgbBlue = 0;       // specifies the blue part of the color.
            public byte rgbGreen = 0;	   // specifies the green part of the color.
            public byte rgbRed = 0;        // specifies the red part of the color.
            private byte rgbReserved = 0;  // must always be set to zero.


            public RGBQUAD(byte red, byte green, byte blue)
            {
                rgbBlue = blue;
                rgbGreen = green;
                rgbRed = red;
            }

            public byte[] ToByteArray()
            {
                // Attenzione: l'ordine non � RGB ma BGR + 1 byte riservato (a 0)
                return new byte[] { rgbBlue, rgbGreen, rgbRed, rgbReserved };
            }
        }

        #endregion



    }
}

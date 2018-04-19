using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureRecognition
{
    class GestureMessage
    {
        private int action;

        private float x;

        private float y;

        private float z;

        private float scale;

        public byte[] generateTranslationMessage(float x, float y, float z)
        {
            MemoryStream ms = new MemoryStream(16);

            ms.Write(BitConverter.GetBytes(1), 0, 4);
            ms.Write(BitConverter.GetBytes(x), 0, 4);
            ms.Write(BitConverter.GetBytes(y), 0, 4);
            ms.Write(BitConverter.GetBytes(z), 0, 4);

            return ms.ToArray();
        }

        public byte[] generateRotationMessage(float x, float y, float z)
        {
            MemoryStream ms = new MemoryStream(16);

            ms.Write(BitConverter.GetBytes(2), 0, 4);
            ms.Write(BitConverter.GetBytes(x), 0, 4);
            ms.Write(BitConverter.GetBytes(y), 0, 4);
            ms.Write(BitConverter.GetBytes(z), 0, 4);

            return ms.ToArray();
        }

        public byte[] generateScaleMessage(float scale)
        {
            MemoryStream ms = new MemoryStream(16);

            ms.Write(BitConverter.GetBytes(3), 0, 4);
            ms.Write(BitConverter.GetBytes(scale), 0, 4);
            // add padding
            ms.Write(BitConverter.GetBytes(0), 0, 4);
            ms.Write(BitConverter.GetBytes(0), 0, 4);

            return ms.ToArray();
        }
    }
}

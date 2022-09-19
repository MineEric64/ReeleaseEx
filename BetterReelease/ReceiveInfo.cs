using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MessagePack;

namespace ReeleaseEx.BetterReelease
{
    [MessagePackObject]
    public class ReceiveInfo
    {
        [Key(0)]
        public int Step { get; set; }

        [Key(1)]
        public byte[] Buffer { get; set; }

        /// <summary>
        /// MessagePack을 위한 LZ4 압축 옵션
        /// </summary>
        [IgnoreMember]
        internal static MessagePackSerializerOptions LZ4_OPTIONS => MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        [IgnoreMember]
        public static ReceiveInfo Empty => new ReceiveInfo(0, new byte[0]);

        public ReceiveInfo(int step, byte[] buffer)
        {
            Step = step;
            Buffer = buffer;
        }
    }
}

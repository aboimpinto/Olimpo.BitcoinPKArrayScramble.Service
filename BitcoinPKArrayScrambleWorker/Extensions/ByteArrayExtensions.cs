namespace BitcoinPKArrayScrambleWorker.Extensions
{
    public static class ByteArrayExtensions
    {
        public static string ToDescription(this byte[] byteArray)
        {
            return string.Join(", ", byteArray);
        }

        public static void EmptyByteArray(this byte[] byteArray)
        {
            for (var i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = (byte)0;
            }
        }
    }
}

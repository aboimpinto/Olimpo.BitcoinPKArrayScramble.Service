namespace BitcoinPKArrayScrambleWorker.Model
{
    public class WorkProgress
    {
        public byte[] LastByteArray { get; }

        public int LastIndex { get; }

        public WorkProgress(int lastIndex, byte[] lastByteArray)
        {
            this.LastIndex = lastIndex;
            this.LastByteArray = lastByteArray;
        }
    }
}

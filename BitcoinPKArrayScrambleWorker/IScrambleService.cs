using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace BitcoinPKArrayScrambleWorker
{
    public interface IScrambleService
    {
        Subject<byte[]> OnNewByteArray { get; } 

        void Scramble(byte[] sourceArray);
    }
}
